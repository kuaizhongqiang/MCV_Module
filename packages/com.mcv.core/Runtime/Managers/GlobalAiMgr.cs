using System;
using MCV_Module.Utils;
using System.Collections;
using MCV_Module.Net;
using MCV_Module.Singleton;
using UnityEngine;

namespace MCV_Module.Managers
{
    /// <summary>
    /// AI 全局管理器 —— 连接 AiServer(StreamingAssets/AiServer/AiServer.exe)。
    ///
    /// 分层原则(Unity 是纯前台):
    ///   - Unity 只负责: 显示、输入、把 session_id + user_text 交给 EXE。
    ///   - EXE 负责: 会话历史拼接、system/portable 组装、token 截断、预热、tool 调用等全部上下文逻辑。
    ///
    /// 职责:
    ///   - 启动时自动拉起 AiServer EXE 并等待就绪(异步, 不阻塞应用启动)
    ///   - 生成并持有 SessionId(Unity 生成 GUID, 预热与后续对话共用)
    ///   - 【启动预热】服务就绪后立即发一次 /v1/warmup(system/portable + 固定触发消息由 EXE 组织),
    ///     完成后 EXE 回调 warmup_done=true, 预热轮作为该 session 历史起始(前缀连续, 利于 KVCache 命中)。
    ///   - 应用退出时关闭 EXE(优雅 /v1/shutdown + 兜底 Kill)
    ///
    /// 分层:
    ///   - AiServerClient（MCV.AiClient.dll, 纯协议）: 鉴权/对话/日志/预热/模型信息
    ///     （凭据由本管理器 Inspector 字段配置后运行时注入，DLL 内无硬编码密钥）
    ///   - AiServerProcess（本程序集源码）: EXE 拉起/关闭（#if !UNITY_WEBGL）
    ///
    /// 用法(任意脚本):
    ///   GlobalAiMgr.Instance.Ask("你好", result => { ... });
    ///   GlobalAiMgr.Instance.AskStream("你好", chunk => {...}, result => {...});
    /// </summary>
    public class GlobalAiMgr : SingletonGlobalMgr<GlobalAiMgr>
    {
        #region 参数
        /// <summary>服务启动就绪等待超时(秒)。Node SEA EXE 首次启动(88MB+杀软扫描)可能较慢, 取 30s。</summary>
        [SerializeField, Header("AiServer 就绪超时(秒)")] float readyTimeoutSeconds = 30f;

        /// <summary>
        /// 客户端鉴权名称 —— 必须与 AiServer EXE 内嵌白名单(.env CLIENT_WHITELIST)中一组一致。
        /// DLL 化后凭据不再硬编码，由本 Inspector 字段配置并在运行时注入 AiServerClient。
        /// </summary>
        [SerializeField, Header("客户端鉴权(与 .env CLIENT_WHITELIST 一致)")]
        string _authName = "asdf";

        /// <summary>客户端鉴权令牌 —— 必须与 AiServer EXE 内嵌白名单中一组一致。</summary>
        [SerializeField] string _authToken = "asdfghjkl";

        /// <summary>由 GlobalAiMgr 控制的通讯客户端（纯协议，编入 MCV.AiClient.dll）</summary>
        public AiServerClient Client { get; private set; }

        /// <summary>EXE 宿主进程管理（留源码，含 #if !UNITY_WEBGL）</summary>
        AiServerProcess _process;

        /// <summary>EXE 是否已就绪(health 通过)</summary>
        public bool IsServerReady { get { return Client != null && Client.IsReady; } }

        /// <summary>当前服务地址(便于调试显示)</summary>
        public string ServerUrl { get { return Client != null ? Client.BaseUrl : ""; } }

        /// <summary>
        /// 会话 id —— Unity 生成(每次应用生命周期一个), 预热与后续对话共用同一 session_id。
        /// EXE 按此维护该会话历史并拼接上下文。
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>预热是否已完成(EXE 预热轮返回 warmup_done=true)。预热完成前禁止用户输入。</summary>
        public bool IsWarmupDone { get; private set; }

        /// <summary>
        /// 固定系统提示词 —— 由外部(其他系统)注入。若为空, 则回退到
        /// <see cref="defaultPrompt"/> 的 GetSystemPrompt() 万能指导老师默认内容。
        /// 预热时传给 EXE 记住并用于该 session 拼接。
        /// </summary>
        [SerializeField, Header("System Prompt(可选, 覆盖默认万能指导老师)")] string _systemPrompt = "";
        public string SystemPrompt
        {
            get { return string.IsNullOrEmpty(_systemPrompt) ? DefaultSystemPrompt : _systemPrompt; }
            set { _systemPrompt = value; }
        }

        /// <summary>
        /// 默认系统提示词 —— 万能学科指导老师结构, 改 subject 即可切换学科。
        /// 可在 Inspector 编辑默认内容; 也可通过外部赋值覆盖 SystemPrompt。
        /// </summary>
        [SerializeField, Header("默认提示词(万能指导老师, 改 subject 切学科)")]
        AiChatSystemPrompt defaultPrompt = new AiChatSystemPrompt();

        /// <summary>
        /// 由 AiChatSystemPrompt 组装的默认系统提示词, 注入当前学习内容与目录结构描述
        /// （来自 GlobalDataMgr.ProjectData / MenuData）。
        /// </summary>
        string DefaultSystemPrompt
        {
            get
            {
                if (defaultPrompt == null) return "";
                string contentDesc = "", menuDesc = "";
                var dataMgr = GlobalDataMgr.Instance;
                if (dataMgr != null)
                {
                    if (dataMgr.ProjectData != null)
                        contentDesc = dataMgr.ProjectData.ProjectDescription();
                    if (dataMgr.MenuData != null)
                        menuDesc = dataMgr.MenuData.MenuDataDescription();
                }
                return defaultPrompt.GetSystemPrompt(contentDesc, menuDesc);
            }
        }

        /// <summary>便携提示词 —— 由外部注入; 为空时回退到默认。预热时传给 EXE; 保持少量且恒定(利于缓存命中)。</summary>
        [SerializeField, Header("Portable Prompt(可选, 覆盖默认)")] string _portablePrompt = "";
        public string PortablePrompt
        {
            get { return string.IsNullOrEmpty(_portablePrompt) ? DefaultPortablePrompt : _portablePrompt; }
            set { _portablePrompt = value; }
        }

        /// <summary>默认便携提示词（来自 AiChatSystemPrompt）。</summary>
        string DefaultPortablePrompt
        {
            get { return defaultPrompt != null ? defaultPrompt.GetPortablePrompt() : ""; }
        }
        #endregion

        #region 生命周期
        protected GlobalAiMgr() { }

        protected override IEnumerator DelayInit()
        {
            // 凭据由 Inspector 配置（_authName/_authToken），运行时注入客户端 —— DLL 内无硬编码密钥
            Client = new AiServerClient(_authName, _authToken);
            _process = new AiServerProcess(Client);

            // 生成会话 id（Unity 侧唯一标识, 传给 EXE 用于会话历史管理）
            SessionId = Guid.NewGuid().ToString("N");

            // 注意: 这里不等待服务就绪, 置 isInit 后立即返回,
            // 避免阻塞 Setup 启动链 —— AI 服务就绪 + 预热是异步的。
            isInit = true;

            StartCoroutine(EnsureReadyAndWarmupAsync());
            yield break;
        }

        protected override void OnApplicationQuit()
        {
            if (_process != null)
                _process.ShutdownNow();
            base.OnApplicationQuit();
        }
        #endregion

        #region 公开方法
        /// <summary>后台拉起并等待 AiServer 就绪, 就绪后执行启动预热(幂等, 可重复调用)。</summary>
        public IEnumerator EnsureReadyAndWarmupAsync()
        {
            bool ready = false;
            yield return Client.EnsureReadyAsync(_process.TryLaunch, ok => ready = ok, readyTimeoutSeconds);

            if (!ready)
            {
                Log.Warning("[GlobalAiMgr] AiServer 未就绪, 预热未执行, 可稍后调用 EnsureReadyAndWarmupAsync 重试");
                yield break;
            }

            Log.Info($"[GlobalAiMgr] AiServer 就绪: {Client.BaseUrl}");

            // 服务就绪后执行预热（若尚未完成）
            if (!IsWarmupDone)
            {
                yield return StartWarmupAsync();
            }
        }

        /// <summary>一次性对话(整段返回)。EXE 负责历史拼接。</summary>
        public IEnumerator Ask(string userText, Action<AiChatResult> onDone, Action<string> onError = null)
        {
            return ChatAsync(new AiChatRequest(SessionId, userText, stream: false), null, onDone, onError);
        }

        /// <summary>流式对话(逐段回调增量, 含思考内容增量)。EXE 负责历史拼接。</summary>
        public IEnumerator AskStream(string userText, Action<AiChatChunk> onDelta,
            Action<AiChatResult> onDone, Action<string> onError = null)
        {
            return ChatAsync(new AiChatRequest(SessionId, userText, stream: true), onDelta, onDone, onError);
        }

        /// <summary>完整对话入口(自定义 provider / model / reasoning 参数)。</summary>
        public IEnumerator ChatAsync(AiChatRequest request, Action<AiChatChunk> onDelta,
            Action<AiChatResult> onDone, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(request.sessionId))
                request.sessionId = SessionId;

            bool ready = false;
            yield return Client.EnsureReadyAsync(_process.TryLaunch, r => ready = r, readyTimeoutSeconds);
            if (!ready)
            {
                onError?.Invoke("AiServer 未就绪: " + Client.BaseUrl);
                yield break;
            }

            yield return Client.ChatAsync(request, onDelta, result =>
            {
                if (result.success)
                    onDone?.Invoke(result);
                else
                    onError?.Invoke(result.error);
            });
        }

        /// <summary>拉取 AiServer 最近日志(排障用)。</summary>
        public IEnumerator FetchServerLogsAsync(int tail, Action<string> onResult)
        {
            yield return Client.FetchLogsAsync(tail, onResult);
        }

        /// <summary>
        /// 拉取 models 目录（providers/模型/能力，对齐 dsh-llm listProviders 概念）。
        /// 供 UI 展示可选 provider/model；需服务就绪并鉴权。
        /// </summary>
        public IEnumerator FetchModelsAsync(Action<AiModelsResult> onResult, Action<string> onError = null)
        {
            if (Client == null)
            {
                onError?.Invoke("AiServerClient 未初始化");
                yield break;
            }
            yield return Client.FetchModelsAsync(onResult, onError);
        }

        /// <summary>拉取服务信息（版本/默认 provider/活跃会话/能力目录）。</summary>
        public IEnumerator FetchInfoAsync(Action<AiInfoResult> onResult, Action<string> onError = null)
        {
            if (Client == null)
            {
                onError?.Invoke("AiServerClient 未初始化");
                yield break;
            }
            yield return Client.FetchInfoAsync(onResult, onError);
        }
        #endregion

        #region 启动预热
        /// <summary>
        /// 执行启动预热: 调 /v1/warmup, 由 EXE 组织 system/portable + 固定触发消息并发送上游。
        /// EXE 完成后回调 warmup_done=true, 预热轮作为该 session 历史起始。
        /// 预热失败不阻塞启动, 但 IsWarmupDone 保持 false(用户输入会被拦截)。
        /// </summary>
        IEnumerator StartWarmupAsync()
        {
            if (Client == null) yield break;

            // 提示词由 Unity 提供(字符串), 预热时传给 EXE 记住, 用于该 session 拼接
            var request = new AiWarmupRequest();
            request.sessionId = SessionId;
            request.systemPrompt = SystemPrompt;
            request.portablePrompt = GlobalUIMgr.CurrentStateDescription() + PortablePrompt;

            Log.Info("[GlobalAiMgr] 启动预热中…(不显示回复)");
            Log.Info($"[GlobalAiMgr] 系统提示词 ： {SystemPrompt}");
            yield return Client.WarmupAsync(request,
                onDone: result =>
                {
                    if (result != null && result.warmupDone)
                    {
                        IsWarmupDone = true;
                        Log.Info($"[GlobalAiMgr] 启动预热完成 (session={result.sessionId})");
                    }
                    else
                    {
                        Log.Warning("[GlobalAiMgr] 预热响应异常, IsWarmupDone 保持 false");
                        IsWarmupDone = false;
                    }
                },
                onError: err =>
                {
                    Log.Error($"[GlobalAiMgr] 启动预热失败: {err}");
                    IsWarmupDone = false;
                });
        }
        #endregion
    }
}
