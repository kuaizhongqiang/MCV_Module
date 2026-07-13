using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// 命令处理器 — 接收命令上下文并执行对应的命令处理程序。
    /// 纯逻辑，不持有 WebSocket 引用，通过回调返回结果和状态变化。
    /// </summary>
    public class CLICommandHandler
    {
        private readonly Dictionary<string, CommandContext> _pendingCommands = new();
        private readonly Action<string, string> _onResult;          // (requestId, resultJson)
        private readonly Action<string, string> _onCommandExecuted; // (command, requestId)
        private readonly Action<string> _setStatus;                 // AgentStatus 更新
        private readonly ConcurrentDictionary<string, string> _results = new();
        private readonly int _maxResults = 100;

        /// <summary>待处理的命令数</summary>
        public int PendingCount => _pendingCommands.Count;

        /// <summary>所有待处理命令的 requestId</summary>
        public string[] PendingRequestIds => _pendingCommands.Keys.ToArray();

        /// <summary>使用统计</summary>
        public int TotalExecuted { get; private set; }

        public CLICommandHandler(
            Action<string, string> onResult,
            Action<string, string> onCommandExecuted = null,
            Action<string> setStatus = null)
        {
            _onResult = onResult ?? throw new ArgumentNullException(nameof(onResult));
            _onCommandExecuted = onCommandExecuted;
            _setStatus = setStatus ?? (_ => { });
        }

        /// <summary>
        /// 注册待处理命令（从任意线程调用，线程安全）。
        /// </summary>
        public void RegisterPending(CommandContext ctx)
        {
            _pendingCommands[ctx.RequestId] = ctx;
        }

        /// <summary>
        /// 执行命令（必须在主线程调用）。
        /// </summary>
        public void Execute(CommandContext ctx)
        {
            _setStatus("thinking");

            try
            {
                var args = SimpleJson.Parse(ctx.ParamsJson);
                string resultJson;

                switch (ctx.Command)
                {
                    case "help":
                        resultJson = HandleHelp();
                        break;

                    case "whoami":
                        resultJson = HandleWhoami();
                        break;

                    case "list.templates":
                        resultJson = HandleListTemplates();
                        break;

                    case "dialog":
                        resultJson = HandleDialog(args);
                        break;

                    case "page.list":
                        resultJson = HandlePageList(args);
                        break;

                    case "page.create":
                        resultJson = HandlePageCreate(args);
                        break;

                    case "run":
                        resultJson = HandleRun(args);
                        break;

                    case "update":
                        resultJson = HandleUpdate(args);
                        break;

                    case "clear":
                        resultJson = HandleClear(args);
                        break;

                    case "result.show":
                        resultJson = HandleResultShow(args);
                        break;

                    case "page.delete":
                        resultJson = HandlePageDelete(args);
                        break;

                    case "stop":
                        resultJson = HandleStop();
                        break;

                    case "get.data":
                        resultJson = HandleGetData(args);
                        break;

                    case "search.data":
                        resultJson = HandleSearchData(args);
                        break;

                    case "usage":
                        resultJson = HandleUsage();
                        break;

                    case "docs":
                    case "docs_get":
                        resultJson = HandleDocs(args);
                        break;

                    case "queue":
                    case "queue.push":
                        resultJson = HandleQueue(ctx.Command, args);
                        break;

                    case "init":
                        resultJson = HandleInit(args);
                        break;

                    case "restart":
                        resultJson = HandleRestart();
                        break;

                    case "status.list":
                        resultJson = HandleStatusList(args);
                        break;

                    default:
                        resultJson = SimpleJson.Serialize(new
                        {
                            status = "error",
                            code = 400,
                            message = $"Unknown command: {ctx.Command}"
                        });
                        break;
                }

                _onResult(ctx.RequestId, resultJson);
                StoreResult(ctx.RequestId, resultJson);
                _onCommandExecuted?.Invoke(ctx.Command, ctx.RequestId);
                TotalExecuted++;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[CLICommandHandler] Command error: {ctx.Command} - {e.Message}");
                _onResult(ctx.RequestId, SimpleJson.Serialize(new
                {
                    status = "error",
                    code = 500,
                    message = e.Message
                }));
            }
            finally
            {
                _pendingCommands.Remove(ctx.RequestId);
                _setStatus("idle");
            }
        }

        /// <summary>
        /// 立即停止执行（eg. 收到 stop 命令时重置状态）。
        /// </summary>
        public void Reset()
        {
            _pendingCommands.Clear();
            _setStatus("idle");
        }

        /// <summary>Store a command result for HTTP polling (Editor mode WS fallback).</summary>
        public void StoreResult(string requestId, string resultJson)
        {
            _results[requestId] = resultJson;
            // Trim old entries to prevent memory leak
            if (_results.Count > _maxResults)
            {
                var oldest = _results.Keys.OrderBy(k => k).FirstOrDefault();
                if (oldest != null) _results.TryRemove(oldest, out _);
            }
        }

        /// <summary>Poll and consume a command result by requestId.</summary>
        public string PollResult(string requestId)
        {
            _results.TryRemove(requestId, out var result);
            return result;
        }

        // ── 命令处理程序 ───────────────────────────────────────

        private string HandleHelp()
        {
            return SimpleJson.Serialize(new
            {
                help = "Available commands: help, docs, whoami, dialog, list.templates, " +
                       "search.data, get.data, usage, page.create, page.list, run, update, " +
                       "clear, result.show, page.delete, stop, queue, init, restart, status.list"
            });
        }

        private string HandleWhoami()
        {
            return SimpleJson.Serialize(new
            {
                id = "agent_canvas",
                name = "AgentCanvas",
                role = "assistant",
                version = "0.1.0"
            });
        }

        private string HandleListTemplates()
        {
            return SimpleJson.Serialize(new[]
            {
                new { templateId = "free_stack", displayName = "自由堆积" },
                new { templateId = "waterfall", displayName = "瀑布流" },
                new { templateId = "three_column", displayName = "左中右一页" },
            });
        }

        private string HandleDialog(SimpleJson.JsonObject args)
        {
            var dialogId = args?.GetString("dialogId", "");
            return SimpleJson.Serialize(new
            {
                dialogs = new[]
                {
                    new { dialogId = dialogId ?? "default", status = "active", createdAt = DateTime.UtcNow.ToString("O") }
                }
            });
        }

        private string HandlePageList(SimpleJson.JsonObject args)
        {
            // TODO: 对接 UI 页面管理
            return SimpleJson.Serialize(new { pages = Array.Empty<object>() });
        }

        private string HandlePageCreate(SimpleJson.JsonObject args)
        {
            var pageId = args?.GetString("pageId", "");
            return SimpleJson.Serialize(new { pageId, status = "created" });
        }

        private string HandleRun(SimpleJson.JsonObject args)
        {
            var pageId = args?.GetString("pageId", "");
            var filePath = args?.GetString("filePath", "");
            // TODO: 通过 UI Toolkit 渲染页面
            UnityEngine.Debug.Log($"[CLICommandHandler] Run page: {pageId} (file: {filePath})");
            return SimpleJson.Serialize(new { pageId, status = "rendered" });
        }

        private string HandleUpdate(SimpleJson.JsonObject args)
        {
            var pageId = args?.GetString("pageId", "");
            UnityEngine.Debug.Log($"[CLICommandHandler] Update page: {pageId}");
            return SimpleJson.Serialize(new { pageId, status = "updated" });
        }

        private string HandleClear(SimpleJson.JsonObject args)
        {
            var pageId = args?.GetString("pageId", "");
            return SimpleJson.Serialize(new { pageId, status = "cleared" });
        }

        private string HandleResultShow(SimpleJson.JsonObject args)
        {
            var pageId = args?.GetString("pageId", "");
            var elementId = args?.GetString("elementId", "");
            return SimpleJson.Serialize(new { pageId, elementId, status = "shown" });
        }

        private string HandlePageDelete(SimpleJson.JsonObject args)
        {
            var pageId = args?.GetString("pageId", "");
            return SimpleJson.Serialize(new { pageId, status = "deleted" });
        }

        private string HandleStop()
        {
            _setStatus("idle");
            return SimpleJson.Serialize(new { status = "stopped" });
        }

        private string HandleGetData(SimpleJson.JsonObject args)
        {
            var dataId = args?.GetString("dataId", "");
            // TODO: 从数据层查找
            return SimpleJson.Serialize(new
            {
                id = dataId,
                displayName = dataId,
                description = ""
            });
        }

        private string HandleSearchData(SimpleJson.JsonObject args)
        {
            var query = args?.GetString("query", "");
            // 关键词降级搜索（Unity 侧不做 embedding）
            return SimpleJson.Serialize(new
            {
                query,
                results = Array.Empty<object>(),
                note = "Keyword search on Unity side; semantic search via MCP Server + LM Studio"
            });
        }

        private string HandleUsage()
        {
            return SimpleJson.Serialize(new
            {
                commandCount = PendingCount,
                totalExecuted = TotalExecuted,
                status = "idle",
            });
        }

        private string HandleDocs(SimpleJson.JsonObject args)
        {
            var name = args?.GetString("name", "");
            return SimpleJson.Serialize(new
            {
                docs = new[]
                {
                    new { title = "Architecture", summary = "System architecture overview" },
                    new { title = "Commands", summary = "All CLI commands" },
                },
                selected = name ?? ""
            });
        }

        private string HandleQueue(string command, SimpleJson.JsonObject args)
        {
            return SimpleJson.Serialize(new
            {
                status = "completed",
                queueLength = PendingCount
            });
        }

        private string HandleInit(SimpleJson.JsonObject args)
        {
            // TODO: 持久化配置到 {persistentDataPath}/CLI/config.json
            return SimpleJson.Serialize(new { status = "initialized" });
        }

        private string HandleRestart()
        {
            // TODO: 重置页面状态
            return SimpleJson.Serialize(new { status = "restarted" });
        }

        private string HandleStatusList(SimpleJson.JsonObject args)
        {
            var dialogId = args?.GetString("dialogId", "");
            return SimpleJson.Serialize(new
            {
                dialogId = dialogId ?? "default",
                status = "idle",
                pendingCommands = PendingRequestIds,
                totalExecuted = TotalExecuted,
            });
        }
    }
}
