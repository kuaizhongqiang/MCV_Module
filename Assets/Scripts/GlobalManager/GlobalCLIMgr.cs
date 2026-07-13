using System.Collections;
using System.Threading;
using MCV_Module.Data.System;
using MCV_Module.Event;
using MCV_Module.GlobalManager.CLI;
using UnityEngine;

namespace MCV_Module.GlobalManager
{
    /// <summary>
    /// AgentCanvas CLI 全局管理器 — 门面控制器
    ///
    /// 职责：统一生命周期管理，协调子模块协作
    ///   - CLIProcessManager    → mcp.exe 子进程
    ///   - CLIHttpServer        → HTTP+WS 服务器（含路由 + 鉴权）
    ///   - CLIWebSocketManager  → WS 客户端连接池
    ///   - CLICommandHandler    → 命令分发和执行
    ///
    /// Agent ← MCP (stdio) → MCP Server (Python) ← HTTP/WS → GlobalCLIMgr
    /// </summary>
    public class GlobalCLIMgr : SingletonGlobalMgr<GlobalCLIMgr>
    {
        protected GlobalCLIMgr() { }

        // ── Inspector 配置 ────────────────────────────────────
        [SerializeField] private int _port = 3748;
        [SerializeField] private string _token = "";
        [SerializeField] private bool _autoStart = true;

        // ── 状态 ───────────────────────────────────────────────
        public string AgentStatus { get; private set; } = "idle";

        // ── 子模块 ─────────────────────────────────────────────
        private CLIProcessManager _processManager;
        private CLIHttpServer _httpServer;
        private CLIWebSocketManager _wsManager;
        private CLICommandHandler _commandHandler;
        private CancellationTokenSource _cts;

        // ── 生命周期 ────────────────────────────────────────────

        protected override IEnumerator DelayInit()
        {
            // 从 GlobalDataMgr 读取 CLI 配置
            if (GlobalDataMgr.Exists && GlobalDataMgr.Instance != null)
            {
                CLIConfig cliConfig = GlobalDataMgr.Instance.SystemData.cliConfig;
                if (cliConfig != null)
                {
                    if (_port <= 0) _port = cliConfig.cliPort;
                    if (string.IsNullOrEmpty(_token)) _token = cliConfig.cliToken;
                    _autoStart = cliConfig.cliAutoStart;
                }
            }

            Debug.Log($"[GlobalCLIMgr] Port={_port}, AutoStart={_autoStart}, " +
                      $"Token={(string.IsNullOrEmpty(_token) ? "(none)" : "***")}");

            // 创建子模块（先创建依赖项）
            _wsManager = new CLIWebSocketManager();
            _commandHandler = new CLICommandHandler(
                onResult: (requestId, resultJson) =>
                {
                    _ = _wsManager.PushReceiptAsync(requestId, resultJson);
                },
                onCommandExecuted: (command, requestId) =>
                    EventBus<CommandExecutedEvent>.Publish(new CommandExecutedEvent(command, requestId)),
                setStatus: status => SetAgentStatus(status)
            );
            _httpServer = new CLIHttpServer(_port, _token, _wsManager);
            _httpServer.CommandHandler = _commandHandler;
            _processManager = new CLIProcessManager();

            // 注册日志回调
            _processManager.OnLog += msg => Debug.Log(msg);
            _processManager.OnLogWarning += msg => Debug.LogWarning(msg);
            _processManager.OnLogError += msg => Debug.LogError(msg);
            _httpServer.OnLog += msg => Debug.Log(msg);
            _httpServer.OnLogWarning += msg => Debug.LogWarning(msg);
            _httpServer.OnLogError += msg => Debug.LogError(msg);
            _wsManager.OnLog += msg => Debug.Log(msg);
            _wsManager.OnLogWarning += msg => Debug.LogWarning(msg);

            // 注册进程退出事件
            _processManager.OnProcessExited += () =>
            {
                Debug.LogWarning("[GlobalCLIMgr] CLI process exited");
                EventBus<CLIProcessStatusEvent>.Publish(new CLIProcessStatusEvent(false));
            };

            // 注册命令接收事件：后台线程 → 主线程调度
            _httpServer.OnCommandReceived += ctx =>
            {
                _commandHandler.RegisterPending(ctx);
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    _commandHandler.Execute(ctx));
            };

            // 确保 UnityMainThreadDispatcher 存在
            _ = UnityMainThreadDispatcher.Instance;

            // 启动 CLI 子进程
            if (_autoStart)
            {
                string exePath = System.IO.Path.Combine(
                    Application.streamingAssetsPath,
                    "AgentCanvas",
                    "mcp.exe"
                );
                _processManager.Start(exePath);

                if (_processManager.IsRunning)
                    EventBus<CLIProcessStatusEvent>.Publish(new CLIProcessStatusEvent(true));
            }

            // 启动 HTTP 服务器
            _cts = new CancellationTokenSource();
            _ = _httpServer.StartAsync(_cts.Token);

            yield break;
        }

        protected override void OnDestroy()
        {
            Shutdown();
            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();
            base.OnApplicationQuit();
        }

        private void Shutdown()
        {
            SetAgentStatus("idle");

            bool wasRunning = _processManager?.IsRunning ?? false;
            _processManager?.Stop();
            _cts?.Cancel();
            _httpServer?.Stop();
            _commandHandler?.Reset();

            if (wasRunning)
                EventBus<CLIProcessStatusEvent>.Publish(new CLIProcessStatusEvent(false));

            Debug.Log("[GlobalCLIMgr] Shutdown complete");
        }

        // ── 状态管理（带 EventBus 通知）────────────────────────

        private void SetAgentStatus(string newStatus)
        {
            if (AgentStatus == newStatus) return;
            string prev = AgentStatus;
            AgentStatus = newStatus;
            EventBus<AgentStatusChangedEvent>.Publish(new AgentStatusChangedEvent(newStatus));
            Debug.Log($"[GlobalCLIMgr] Agent status: {prev} → {newStatus}");
        }
    }
}
