using System;
using System.Collections;
using MCV_Module.Net;
using MCV_Module.Singleton;
using UnityEngine;

namespace MCV_Module.Managers
{
    /// <summary>
    /// AI 全局管理器 —— 连接 AiServer(StreamingAssets/AiServer/AiServer.exe)。
    ///
    /// 职责:
    ///   - 启动时自动拉起 AiServer EXE 并等待就绪(异步, 不阻塞应用启动)
    ///   - 通过 AiServerClient 与 EXE 走统一协议对话(流式/非流式, 支持 reasoning)
    ///   - 应用退出时关闭 EXE(优雅 /v1/shutdown + 兜底 Kill)
    ///
    /// 分层:
    ///   - AiServerClient（MCV.AiClient.dll, 纯协议）: 鉴权/对话/日志
    ///   - AiServerProcess（本程序集源码）: EXE 拉起/关闭（#if !UNITY_WEBGL）
    ///
    /// 用法(任意脚本):
    ///   GlobalAiMgr.Instance.Ask("你好", result => { ... });
    ///   GlobalAiMgr.Instance.AskStream("你好", chunk => {...}, result => {...});
    /// </summary>
    public class GlobalAiMgr : SingletonGlobalMgr<GlobalAiMgr>
    {
        #region 参数
        /// <summary>服务启动就绪等待超时(秒)</summary>
        [SerializeField, Header("AiServer 就绪超时(秒)")] float readyTimeoutSeconds = 15f;

        /// <summary>由 GlobalAiMgr 控制的通讯客户端（纯协议，编入 MCV.AiClient.dll）</summary>
        public AiServerClient Client { get; private set; }

        /// <summary>EXE 宿主进程管理（留源码，含 #if !UNITY_WEBGL）</summary>
        AiServerProcess _process;

        /// <summary>EXE 是否已就绪(health 通过)</summary>
        public bool IsServerReady { get { return Client != null && Client.IsReady; } }

        /// <summary>当前服务地址(便于调试显示)</summary>
        public string ServerUrl { get { return Client != null ? Client.BaseUrl : ""; } }
        #endregion

        #region 生命周期
        protected GlobalAiMgr() { }

        protected override IEnumerator DelayInit()
        {
            Client = new AiServerClient();
            _process = new AiServerProcess(Client);

            // 注意: 这里不等待服务就绪, 置 isInit 后立即返回,
            // 避免阻塞 Setup 启动链 —— AI 服务就绪是异步的, 由 EnsureServerReady 后台完成。
            isInit = true;

            StartCoroutine(EnsureServerReadyAsync());
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
        /// <summary>后台拉起并等待 AiServer 就绪(可重复调用, 幂等)</summary>
        public IEnumerator EnsureServerReadyAsync()
        {
            yield return Client.EnsureReadyAsync(_process.TryLaunch, ok =>
            {
                if (!ok)
                    Debug.LogWarning($"[GlobalAiMgr] AiServer 未就绪, 可稍后调用 EnsureServerReadyAsync 重试");
                else
                    Debug.Log($"[GlobalAiMgr] AiServer 就绪: {Client.BaseUrl}");
            }, readyTimeoutSeconds);
        }

        /// <summary>
        /// 拉取 AiServer 最近日志(排障用)。成功回调日志文本; 失败回调错误描述。
        /// 也可直接查看 EXE 日志文件: %TEMP%\AiServer.log
        /// </summary>
        public IEnumerator FetchServerLogsAsync(int tail, Action<string> onResult)
        {
            yield return Client.FetchLogsAsync(tail, onResult);
        }

        /// <summary>
        /// 一次性对话(整段返回)。
        /// </summary>
        /// <param name="userText">用户输入</param>
        /// <param name="onDone">完成回调(AiChatResult.success 表示成功)</param>
        /// <param name="onError">失败回调(可选; 不传则结果里看 error)</param>
        public IEnumerator Ask(string userText, Action<AiChatResult> onDone, Action<string> onError = null)
        {
            return ChatAsync(new AiChatRequest(userText, stream: false), null, onDone, onError);
        }

        /// <summary>
        /// 流式对话(逐段回调增量, 含思考内容增量)。
        /// </summary>
        /// <param name="userText">用户输入</param>
        /// <param name="onDelta">增量回调(chunk.HasReasoning / chunk.HasContent)</param>
        /// <param name="onDone">完成回调(累积好的完整结果)</param>
        /// <param name="onError">失败回调(可选)</param>
        public IEnumerator AskStream(string userText, Action<AiChatChunk> onDelta,
            Action<AiChatResult> onDone, Action<string> onError = null)
        {
            return ChatAsync(new AiChatRequest(userText, stream: true), onDelta, onDone, onError);
        }

        /// <summary>
        /// 完整对话入口(自定义消息序列 / provider / model / reasoning 参数)。
        /// 自动确保服务就绪; 未就绪则直接回调失败。
        /// </summary>
        public IEnumerator ChatAsync(AiChatRequest request, Action<AiChatChunk> onDelta,
            Action<AiChatResult> onDone, Action<string> onError = null)
        {
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
        #endregion
    }
}
