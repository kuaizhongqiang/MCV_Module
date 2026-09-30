using System.Collections;
using MCV_Module.Utils;
using MCV_Module.Managers;
using MCV_Module.Net;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 上下文拼接/历史/token 截断/预热全在 AiServer(EXE), Unity 是纯前台不得自行拼接; 面板随 Canvas 重建会重新 Bind, 订阅必须放在 OnViewBound 且先清后加
    /// <summary>AI 对话控制器 —— 面板与 GlobalAiMgr 之间的调度层：发送、流式回填气泡、预热门控与未就绪重试。</summary>
    public class AiDialogController : ControllerBase<AiDialogPanel>
    {
        /// <summary>单次对话最长重试次数(AiServer 未就绪时)</summary>
        const int MAX_RETRY = 3;

        /// <summary>是否有请求进行中</summary>
        bool busy;

        /// <summary>等待预热完成的协程引用(防止重复启动)</summary>
        Coroutine warmupWaitCoroutine;

        // WHY: 模型目录是"面板每次重建都要用"的数据, 而面板是新的、列表是空的 —— 缓存放常驻 Controller 里, 重绑时按它重装列表
        /// <summary>已取到的模型目录(providers / 默认 provider)；null = 还没取到。</summary>
        AiModelsResult cachedModels;

        /// <summary>取目录的请求是否在途(去重: 一次重建只发一个请求)。</summary>
        bool modelsFetching;

        /// <summary>当前选中的 provider / model(空 = 没选过, 请求里不填, 由 EXE 用它自己的默认)。</summary>
        string currentProvider = string.Empty;
        string currentModel = string.Empty;

        public override void OnViewBound()
        {
            // 先清后加, 避免面板重建后重复订阅
            View.OnSendRequested -= HandleSend;
            View.OnSendRequested += HandleSend;
            // 模型切换: 面板只上报"展开了面板 / 选了哪个模型", 取目录与记住选择都在本控制器
            View.OnPanelOpened -= RequestModelList;
            View.OnPanelOpened += RequestModelList;
            View.OnModelSelected -= HandleModelSelected;
            View.OnModelSelected += HandleModelSelected;

            // 预热门控: 预热完成前禁止输入
            if (!IsWarmupDone())
            {
                View.SetInputInteractable(false);
                View.SetInfoText(Lang.Get("ui.ai.initializing"));
                StartWaitWarmupOnce();
            }
            else
            {
                View.SetInputInteractable(!busy);
            }

            if (!View.HasMessage)
            {
                View.AddSystemMessage("你好，我是你的电路智能教师。可以问我电路原理、实验步骤或接线问题。");
            }

            // 模型目录: 拿到过就按缓存重装列表(面板是新的、列表是空的), 没有就拉一次
            RequestModelList();
        }

        public override void OnDispose()
        {
            if (View != null)
            {
                View.OnSendRequested -= HandleSend;
                View.OnPanelOpened -= RequestModelList;
                View.OnModelSelected -= HandleModelSelected;
            }

            // WHY: 拉取协程由管理器在 OnDispose 前整组停掉 —— 在途标记必须一起复位, 否则这次拉取永远算"在途", 之后没人再发请求
            modelsFetching = false;

            // WHY: 协程宿主已移到 GlobalControllerMgr，管理器会先整组停掉本控制器的协程再调 OnDispose；这里只作释放引用
            warmupWaitCoroutine = null;
            ClearView();
        }

        // ───────────────────────── 预热门控 ─────────────────────────

        /// <summary>当前是否已允许用户输入(服务就绪 + 预热完成 + 非忙碌)。</summary>
        bool IsWarmupDone()
        {
            var mgr = GlobalAiMgr.Instance;
            return mgr != null && mgr.IsWarmupDone;
        }

        /// <summary>启动等待预热完成的协程(仅一次)。预热完成后恢复输入。</summary>
        void StartWaitWarmupOnce()
        {
            if (warmupWaitCoroutine != null) return;
            warmupWaitCoroutine = Run(WaitWarmupAndEnableInput());
        }

        IEnumerator WaitWarmupAndEnableInput()
        {
            // 轮询等待预热完成(预热是异步后台进行)
            int guard = 0;
            while (!IsWarmupDone() && guard < 300) // 最多等 30 秒(0.1s 间隔)
            {
                yield return new WaitForSeconds(0.1f);
                guard++;
            }

            warmupWaitCoroutine = null;
            if (View == null) yield break;

            if (IsWarmupDone())
            {
                View.SetInfoText("");
                View.SetInputInteractable(!busy);
            }
            else
            {
                View.SetInfoText(Lang.Get("ui.ai.failed"));
                View.SetInputInteractable(false);
            }
        }

        // ───────────────────────── 模型切换 ─────────────────────────

        /// <summary>取模型目录：有缓存就直接重装列表，否则发一次请求（失败后展开面板时会再试）。</summary>
        void RequestModelList()
        {
            if (View == null) return;

            if (cachedModels != null)
            {
                View.BuildModelList(cachedModels);
                View.ApplyCurrentModel(currentProvider, currentModel);
                return;
            }

            if (modelsFetching) return;
            modelsFetching = true;
            Run(FetchModels());
        }

        // WHY: 目录是"每次重建都要用"的数据而面板会重建, 所以要缓存; 在途标记兼作去重, 失败时复位以便重试
        /// <summary>拉取模型目录：成功后装配列表；用户还没选过模型时用目录里的默认 provider / 模型回填按钮文字。</summary>
        IEnumerator FetchModels()
        {
            var mgr = GlobalAiMgr.Instance;
            if (mgr == null)
            {
                modelsFetching = false;
                yield break;
            }

            yield return mgr.FetchModelsAsync(
                result =>
                {
                    modelsFetching = false;
                    cachedModels = result;
                    // WHY: 面板可能已随 Canvas 重建销毁 —— Unity 的 == 对已销毁对象返回 true，这一句就把这种情况挡掉了
                    if (View == null) return;

                    View.BuildModelList(result);
                    if (string.IsNullOrEmpty(currentModel)) ApplyDefaultModel(result);
                    else View.ApplyCurrentModel(currentProvider, currentModel);
                },
                error =>
                {
                    modelsFetching = false;
                    Log.Warning($"[AiDialog] 拉取模型目录失败（下次展开 AI 面板时会重试）：{error}");
                });
        }

        // WHY: 必须优先"已配置密钥"的 provider —— 服务端的自动回退只在**请求不带 provider** 时才生效，
        //      显式指定一个没配密钥的 provider 会直接 503（实测：默认 provider=deepseek 但只配了 mimo）。
        /// <summary>按目录回填默认模型：defaultProvider 且已配置 > 第一个已配置 > 未配置的 defaultProvider > 第一个。</summary>
        void ApplyDefaultModel(AiModelsResult result)
        {
            if (View == null || result == null || result.providers == null) return;

            AiModelProviderInfo picked = null;
            AiModelProviderInfo unconfiguredDefault = null;
            AiModelProviderInfo firstConfigured = null;
            AiModelProviderInfo firstAny = null;

            for (int i = 0; i < result.providers.Length; i++)
            {
                AiModelProviderInfo provider = result.providers[i];
                if (provider == null) continue;

                if (firstAny == null) firstAny = provider;
                if (provider.configured && firstConfigured == null) firstConfigured = provider;

                if (provider.name == result.defaultProvider)
                {
                    if (provider.configured) { picked = provider; break; }
                    unconfiguredDefault = provider;
                }
            }

            if (picked == null) picked = firstConfigured;
            if (picked == null) picked = unconfiguredDefault;
            if (picked == null) picked = firstAny;
            if (picked == null) return;

            string model = picked.defaultModel;
            if (string.IsNullOrEmpty(model) && picked.models != null && picked.models.Length > 0) model = picked.models[0];

            currentProvider = picked.name ?? string.Empty;
            currentModel = model ?? string.Empty;
            View.ApplyCurrentModel(currentProvider, currentModel);
        }

        /// <summary>用户选定模型：记住选择（之后每次请求都带上）。</summary>
        void HandleModelSelected(string provider, string model)
        {
            currentProvider = provider ?? string.Empty;
            currentModel = model ?? string.Empty;
            Log.Info($"[AiDialog] 模型已切换：{currentProvider} / {currentModel}");
        }

        // ───────────────────────── 发送流程 ─────────────────────────

        void HandleSend(string userText)
        {
            if (busy)
            {
                View.SetInfoText(Lang.Get("ui.ai.thinking"));
                return;
            }

            if (string.IsNullOrWhiteSpace(userText))
            {
                View.SetInfoText(Lang.Get("ui.ai.empty"));
                return;
            }

            if (!IsWarmupDone())
            {
                View.SetInfoText(Lang.Get("ui.ai.initializing"));
                return;
            }

            View.AddUserMessage(userText);
            View.BeginAssistantReply();
            View.SetInfoText("");
            View.SetInputInteractable(false);
            busy = true;

            Run(RunChat(userText, MAX_RETRY));
        }

        // WHY: 界面状态为空时必须退化为仅用户输入, 否则会拼出孤立分隔词
        /// <summary>组装发给 AI 的最终用户消息 = 便携提示词(当前界面状态) + 用户输入。</summary>
        string BuildUserText(string userText)
        {
            string state = GlobalUIMgr.CurrentStateDescription();
            string text = userText ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }
            return string.IsNullOrWhiteSpace(state)
                ? text
                : $"{state} 用户输入内容为：{text}";
        }

        // WHY: 只传 session_id + user_text 给 GlobalAiMgr, 历史拼接在 EXE
        /// <summary>执行一次对话；AiServer 未就绪时最多重试 MAX_RETRY 次(每次间隔 1 秒)。</summary>
        IEnumerator RunChat(string userText, int retriesLeft)
        {
            var mgr = GlobalAiMgr.Instance;
            if (mgr == null)
            {
                Finish(false, "AI 服务未初始化");
                yield break;
            }

            string finalString = BuildUserText(userText);
            var request = new AiChatRequest(mgr.SessionId, finalString, stream: true);

            // 选过模型就显式带上（没选过则留空，由 EXE 用它自己的默认 provider / model）
            if (!string.IsNullOrEmpty(currentProvider)) request.provider = currentProvider;
            if (!string.IsNullOrEmpty(currentModel)) request.model = currentModel;

            yield return mgr.ChatAsync(request,
                onDelta: chunk =>
                {
                    if (View == null) return;
                    if (chunk.HasReasoning)
                        View.AppendAssistantReasoning(chunk.choices[0].delta.reasoningContent);
                    if (chunk.HasContent)
                        View.AppendAssistantContent(chunk.choices[0].delta.content);
                },
                onDone: result =>
                {
                    if (View == null) return;
                    // 流式结束: 对累积的完整正文/思考一次性做 markdown 转换（核心转换时机）
                    View.FinalizeAssistantReply();
                    Finish(result.success, result.success ? "" : result.error);
                },
                onError: error =>
                {
                    if (View == null) return;
                    if (error != null && error.Contains("未就绪") && retriesLeft > 0)
                    {
                        // 服务还在启动, 提示后稍等重试
                        View.SetInfoText(Lang.Get("ui.ai.connecting", retriesLeft));
                        Run(RetryAfterDelay(userText, retriesLeft - 1));
                    }
                    else
                    {
                        Finish(false, error);
                    }
                });
        }

        IEnumerator RetryAfterDelay(string userText, int retriesLeft)
        {
            yield return new WaitForSeconds(1f);
            yield return RunChat(userText, retriesLeft);
        }

        /// <summary>收尾: 恢复输入, 展示结果/错误; 失败时把 AiServer 日志尾部打到 Unity Console 便于定位</summary>
        void Finish(bool success, string message)
        {
            busy = false;
            View.SetInputInteractable(!busy);
            View.SelectInput();

            if (success)
            {
                View.SetInfoText("");
            }
            else
            {
                View.SetInfoText(Lang.Get("ui.ai.error", message));
                Log.Error($"[AiDialog] AI 请求失败: {message}");
                Run(DumpServerLogs());
            }
        }

        /// <summary>拉取 AiServer 最近日志并打到 Unity Console(不黑箱, 快速定位问题)</summary>
        IEnumerator DumpServerLogs()
        {
            yield return GlobalAiMgr.Instance.FetchServerLogsAsync(15, text =>
            {
                if (!string.IsNullOrEmpty(text))
                    Log.Error("[AiDialog] AiServer 最近日志:\n" + text);
            });
        }
    }
}
