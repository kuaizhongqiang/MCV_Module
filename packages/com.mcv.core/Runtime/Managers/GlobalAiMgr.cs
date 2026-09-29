using System;
using MCV_Module.Utils;
using System.Collections;
using MCV_Module.Net;
using MCV_Module.Singleton;
using UnityEngine;

namespace MCV_Module.Managers
{
    // WHY: Unity 只做前台（显示/输入/session_id + user_text），会话历史、system/portable 组装、token 截断、预热、tool 调用全在 EXE
    // WHY: enableAi = false 时也必须照常置 isInit，否则会阻塞 Setup 启动链
    /// <summary>AI 总入口：建客户端与 EXE 宿主、持有会话 id、等待就绪、执行启动预热，退出时关闭 EXE。</summary>
    public class GlobalAiMgr : SingletonGlobalMgr<GlobalAiMgr>
    {
        #region 参数
        /// <summary>AI 总开关：false 时不建客户端、不拉起 EXE、不预热、不允许通讯（UI 用 IsAiEnabled 判断入口）。</summary>
        [SerializeField, Header("启用 AI（关闭则不加载 / 不预热 / 不允许通讯）")]
        bool enableAi = true;

        /// <summary>AI 是否启用（只读）。关闭时所有 AI 通讯入口直接拒绝。</summary>
        public bool IsAiEnabled { get { return enableAi; } set { enableAi = value; } }

        /// <summary>启动预热开关：false 时启动只拉起 AiServer 并发就绪，不发预热轮（调试 AI 相关 UI 用，免去每次预热的开销）。</summary>
        [SerializeField, Header("启动预热（关闭则启动不发预热轮, UI 直接放行）")]
        bool enableStartupWarmup = true;

        /// <summary>启动是否发送预热轮（只读）。关闭时服务就绪后 IsWarmupDone 直接置 true，否则 UI 输入会被预热门控永久拦住。</summary>
        public bool IsStartupWarmupEnabled { get { return enableStartupWarmup; } set { enableStartupWarmup = value; } }

        /// <summary>服务启动就绪等待超时(秒)。Node SEA EXE 首次启动(88MB+杀软扫描)可能较慢, 取 30s。</summary>
        [SerializeField, Header("AiServer 就绪超时(秒)")] float readyTimeoutSeconds = 30f;

        // WHY: 凭据不硬编码进 DLL，由本 Inspector 字段配置后运行时注入 AiServerClient
        /// <summary>客户端鉴权名称：必须与 EXE 内嵌白名单（.env CLIENT_WHITELIST）中一组一致。</summary>
        [SerializeField, Header("客户端鉴权(与 .env CLIENT_WHITELIST 一致)")]
        string _authName = "asdf";

        /// <summary>客户端鉴权令牌 —— 必须与 AiServer EXE 内嵌白名单中一组一致。</summary>
        [SerializeField] string _authToken = "asdfghjkl";

        /// <summary>由 GlobalAiMgr 控制的通讯客户端（纯协议，编入 MCV.AiClient.dll）。AI 关闭时为 null。</summary>
        public AiServerClient Client { get; private set; }

        /// <summary>EXE 宿主进程管理（留源码，含 #if !UNITY_WEBGL）。AI 关闭时为 null。</summary>
        AiServerProcess _process;

        /// <summary>EXE 是否已就绪(health 通过)</summary>
        public bool IsServerReady { get { return enableAi && Client != null && Client.IsReady; } }

        /// <summary>当前服务地址(便于调试显示)</summary>
        public string ServerUrl { get { return Client != null ? Client.BaseUrl : ""; } }

        /// <summary>会话 id（每次应用生命周期一个），预热与后续对话共用；EXE 按它维护历史与上下文。</summary>
        public string SessionId { get; private set; }

        /// <summary>预热是否已完成(EXE 预热轮返回 warmup_done=true；预热开关关闭时为"无需预热"，服务就绪即置 true)。预热完成前禁止用户输入。</summary>
        public bool IsWarmupDone { get; private set; }

        /// <summary>系统提示词：外部可注入；为空则回退到默认万能指导老师内容（预热时传给 EXE 记住）。</summary>
        [SerializeField, Header("System Prompt(可选, 覆盖默认万能指导老师)")] string _systemPrompt = "";
        public string SystemPrompt
        {
            get { return string.IsNullOrEmpty(_systemPrompt) ? DefaultSystemPrompt : _systemPrompt; }
            set { _systemPrompt = value; }
        }

        /// <summary>默认系统提示词（万能学科指导老师结构，改 subject 即换学科）。</summary>
        [SerializeField, Header("默认提示词(万能指导老师, 改 subject 切学科)")]
        AiChatSystemPrompt defaultPrompt = new AiChatSystemPrompt();

        /// <summary>由 AiChatSystemPrompt 组装的默认系统提示词，注入学习内容与目录结构描述。</summary>
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
            // WHY: AI 关闭也照常置 isInit（不加载客户端 / EXE / 会话 / 预热），否则会阻塞 Setup 启动链
            if (!enableAi)
            {
                Client = null;
                _process = null;
                SessionId = null;
                IsWarmupDone = false;
                Log.Info("[GlobalAiMgr] enableAi = false：不加载 AiServer、不预热、不允许通讯");
                isInit = true;
                yield break;
            }

            // 凭据由 Inspector 配置（_authName/_authToken），运行时注入客户端 —— DLL 内无硬编码密钥
            Client = new AiServerClient(_authName, _authToken);
            _process = new AiServerProcess(Client);

            // 生成会话 id（Unity 侧唯一标识, 传给 EXE 用于会话历史管理）
            SessionId = Guid.NewGuid().ToString("N");

            // WHY: 不在这里等服务就绪，置 isInit 后立即返回 —— 就绪与预热都是异步的，不能阻塞 Setup 启动链
            isInit = true;

            // WHY: 预热开关只管启动这一次 —— false 时仍拉起服务等待就绪, 只是不发预热轮
            StartCoroutine(EnsureReadyAndWarmupAsync(enableStartupWarmup));
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
        /// <summary>通讯守卫：AI 关闭时记警告并回错误，返回 true 表示已拒绝（调用方应 yield break）。</summary>
        bool RejectIfAiDisabled(Action<string> onError, string api)
        {
            if (enableAi) return false;
            Log.Warning($"[GlobalAiMgr] AI 已关闭（enableAi = false），拒绝 {api}");
            onError?.Invoke("AI 已关闭");
            return true;
        }

        /// <summary>后台拉起并等待 AiServer 就绪, 就绪后执行启动预热(幂等, 可重复调用)。AI 关闭时直接返回。warmup = false 时只等就绪、跳过预热轮。</summary>
        public IEnumerator EnsureReadyAndWarmupAsync(bool warmup = true)
        {
            if (!enableAi)
            {
                Log.Warning("[GlobalAiMgr] AI 已关闭（enableAi = false），跳过就绪等待与预热");
                yield break;
            }
            if (Client == null) yield break;

            bool ready = false;
            yield return Client.EnsureReadyAsync(_process.TryLaunch, ok => ready = ok, readyTimeoutSeconds);

            if (!ready)
            {
                Log.Warning("[GlobalAiMgr] AiServer 未就绪, 预热未执行, 可稍后调用 EnsureReadyAndWarmupAsync 重试");
                yield break;
            }

            Log.Info($"[GlobalAiMgr] AiServer 就绪: {Client.BaseUrl}");

            // WHY: 不预热也要放行 UI —— AiDialogController 只认 IsWarmupDone, 保持 false 会让输入被预热门控永久拦住
            if (!warmup)
            {
                IsWarmupDone = true;
                Log.Info("[GlobalAiMgr] 预热开关关闭（enableStartupWarmup = false），跳过预热轮, IsWarmupDone 视为 true");
                yield break;
            }

            // 服务就绪后执行预热（若尚未完成）
            if (!IsWarmupDone)
            {
                yield return StartWarmupAsync();
            }
        }

        /// <summary>一次性对话(整段返回)。EXE 负责历史拼接。AI 关闭时直接回错误。</summary>
        public IEnumerator Ask(string userText, Action<AiChatResult> onDone, Action<string> onError = null)
        {
            return ChatAsync(new AiChatRequest(SessionId, userText, stream: false), null, onDone, onError);
        }

        /// <summary>流式对话(逐段回调增量, 含思考内容增量)。EXE 负责历史拼接。AI 关闭时直接回错误。</summary>
        public IEnumerator AskStream(string userText, Action<AiChatChunk> onDelta,
            Action<AiChatResult> onDone, Action<string> onError = null)
        {
            return ChatAsync(new AiChatRequest(SessionId, userText, stream: true), onDelta, onDone, onError);
        }

        /// <summary>完整对话入口(自定义 provider / model / reasoning 参数)。AI 关闭时直接回错误。</summary>
        public IEnumerator ChatAsync(AiChatRequest request, Action<AiChatChunk> onDelta,
            Action<AiChatResult> onDone, Action<string> onError = null)
        {
            if (RejectIfAiDisabled(onError, "ChatAsync")) yield break;

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

        /// <summary>拉取 AiServer 最近日志(排障用)。AI 关闭时回空串。</summary>
        public IEnumerator FetchServerLogsAsync(int tail, Action<string> onResult)
        {
            if (!enableAi)
            {
                Log.Warning("[GlobalAiMgr] AI 已关闭（enableAi = false），拒绝 FetchServerLogsAsync");
                onResult?.Invoke("");
                yield break;
            }
            if (Client == null)
            {
                onResult?.Invoke("");
                yield break;
            }
            yield return Client.FetchLogsAsync(tail, onResult);
        }

        /// <summary>拉取 models 目录（providers / 模型 / 能力），供 UI 展示可选项；需服务就绪并鉴权。</summary>
        public IEnumerator FetchModelsAsync(Action<AiModelsResult> onResult, Action<string> onError = null)
        {
            if (RejectIfAiDisabled(onError, "FetchModelsAsync")) yield break;
            if (Client == null)
            {
                onError?.Invoke("AiServerClient 未初始化");
                yield break;
            }
            yield return Client.FetchModelsAsync(onResult, onError);
        }

        /// <summary>拉取服务信息（版本/默认 provider/活跃会话/能力目录）。AI 关闭时直接回错误。</summary>
        public IEnumerator FetchInfoAsync(Action<AiInfoResult> onResult, Action<string> onError = null)
        {
            if (RejectIfAiDisabled(onError, "FetchInfoAsync")) yield break;
            if (Client == null)
            {
                onError?.Invoke("AiServerClient 未初始化");
                yield break;
            }
            yield return Client.FetchInfoAsync(onResult, onError);
        }
        #endregion

        #region 启动预热
        // WHY: 预热轮是该 session 的历史起始（前缀连续利于 KVCache 命中）；IsWarmupDone=false 时用户输入会被拦截
        /// <summary>启动预热：调 /v1/warmup，成功后 IsWarmupDone = true；失败不阻塞启动但保持 false。</summary>
        IEnumerator StartWarmupAsync()
        {
            if (!enableAi) yield break;
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
