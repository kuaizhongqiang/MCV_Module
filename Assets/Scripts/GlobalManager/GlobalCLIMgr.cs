using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCV_Module.Data.System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MCV_Module.GlobalManager
{
    /// <summary>
    /// AgentCanvas CLI 全局管理器
    ///
    /// 职责：
    ///   1. 启动/管理 mcp.exe 子进程
    ///   2. 运行 HTTP+WebSocket 服务器（端口 3748）
    ///   3. 接收 Agent 命令 → 主线程调度执行 → WS 回执推送
    ///   4. Token 鉴权
    ///   5. 提供 /data/export 数据导出端点
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
        public string AgentStatus { get; private set; } = "idle"; // idle|thinking|searching|rendering

        private HttpListener _httpListener;
        private CancellationTokenSource _cts;
        private Process _cliProcess;
        private readonly Dictionary<string, WebSocket> _wsClients = new();
        private readonly Dictionary<string, CommandContext> _pendingCommands = new();
        private static readonly Encoding s_Utf8 = new UTF8Encoding(false);

        // ── 内部类型 ────────────────────────────────────────────

        private class CommandContext
        {
            public string RequestId;
            public string Command;
            public string ParamsJson;
            public DateTime ReceivedAt;
        }

        // ── 生命周期 ────────────────────────────────────────────

        protected override IEnumerator DelayInit()
        {
            // 从 GlobalDataMgr 读取 CLI 配置
            if (GlobalDataMgr.Exists && GlobalDataMgr.Instance != null)
            {
                var cliConfig = GlobalDataMgr.Instance.SystemData.cliConfig;
                if (cliConfig != null)
                {
                    // Inspector 值优先于配置文件
                    if (_port <= 0) _port = cliConfig.cliPort;
                    if (string.IsNullOrEmpty(_token)) _token = cliConfig.cliToken;
                    _autoStart = cliConfig.cliAutoStart;
                }
            }

            Debug.Log($"[GlobalCLIMgr] Port={_port}, AutoStart={_autoStart}, Token={(string.IsNullOrEmpty(_token) ? "(none)" : _token)}");

            // 启动 CLI 子进程
            if (_autoStart)
            {
                StartCLIProcess();
            }

            // 启动 HTTP 服务器
            _cts = new CancellationTokenSource();
            _ = StartHttpServerAsync(_cts.Token);

            // 确保 UnityMainThreadDispatcher 存在
            var _ = UnityMainThreadDispatcher.Instance;

            yield break;
        }

        protected override void OnDestroy()
        {
            StopCLIProcess();
            StopHttpServer();
            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            StopCLIProcess();
            StopHttpServer();
            base.OnApplicationQuit();
        }

        // ── CLI 进程管理 ───────────────────────────────────────

        private void StartCLIProcess()
        {
            string exePath = Path.Combine(
                Application.streamingAssetsPath,
                "AgentCanvas",
                "mcp.exe"
            );

            if (!File.Exists(exePath))
            {
                Debug.LogWarning($"[GlobalCLIMgr] mcp.exe not found at {exePath}");
                return;
            }

            try
            {
                _cliProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    },
                    EnableRaisingEvents = true,
                };

                _cliProcess.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.Log($"[CLI stdout] {e.Data}");
                };
                _cliProcess.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Debug.LogWarning($"[CLI stderr] {e.Data}");
                };
                _cliProcess.Exited += (_, _) =>
                {
                    Debug.LogWarning($"[GlobalCLIMgr] CLI process exited");
                    _cliProcess = null;
                };

                _cliProcess.Start();
                _cliProcess.BeginOutputReadLine();
                _cliProcess.BeginErrorReadLine();

                Debug.Log($"[GlobalCLIMgr] CLI process started (PID: {_cliProcess.Id})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GlobalCLIMgr] Failed to start CLI process: {e.Message}");
            }
        }

        private void StopCLIProcess()
        {
            if (_cliProcess != null && !_cliProcess.HasExited)
            {
                try
                {
                    _cliProcess.Kill();
                    _cliProcess.WaitForExit(5000);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GlobalCLIMgr] Error stopping CLI: {e.Message}");
                }
                _cliProcess.Close();
                _cliProcess = null;
            }
        }

        // ── HTTP 服务器 ───────────────────────────────────────

        private async Task StartHttpServerAsync(CancellationToken ct)
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                _httpListener.Start();
                Debug.Log($"[GlobalCLIMgr] HTTP server started on port {_port}");

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var ctx = await _httpListener.GetContextAsync().WaitAsync(ct);
                        _ = HandleRequestAsync(ctx, ct);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (HttpListenerException) { break; }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GlobalCLIMgr] HTTP error: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GlobalCLIMgr] Failed to start HTTP server: {e.Message}");
            }
        }

        private void StopHttpServer()
        {
            _cts?.Cancel();
            try { _httpListener?.Stop(); } catch { }
            try { _httpListener?.Close(); } catch { }
            _httpListener = null;
        }

        // ── 请求路由 ───────────────────────────────────────────

        private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            try
            {
                var path = ctx.Request.Url.AbsolutePath.TrimEnd('/');

                // 鉴权检查（WebSocket 在升级时验证 token）
                if (path != "/ws" && !CheckAuth(ctx.Request))
                {
                    await SendJsonResponse(ctx.Response, 401, new
                    {
                        status = "error",
                        code = 401,
                        message = "Unauthorized"
                    });
                    return;
                }

                switch (path)
                {
                    case "/cmd" when ctx.Request.HttpMethod == "POST":
                        await HandleCommandAsync(ctx, ct);
                        break;

                    case "/ws":
                        await HandleWebSocketAsync(ctx, ct);
                        break;

                    case "/data/export":
                        await HandleDataExportAsync(ctx, ct);
                        break;

                    default:
                        await SendJsonResponse(ctx.Response, 404, new
                        {
                            status = "error",
                            code = 404,
                            message = "Not found"
                        });
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GlobalCLIMgr] Request error: {e.Message}");
                try
                {
                    await SendJsonResponse(ctx.Response, 500, new
                    {
                        status = "error",
                        code = 500,
                        message = e.Message
                    });
                }
                catch { }
            }
        }

        // ── 鉴权 ───────────────────────────────────────────────

        private bool CheckAuth(HttpListenerRequest request)
        {
            if (string.IsNullOrEmpty(_token))
                return true; // 无 token 时放行（开发模式）

            var authHeader = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader))
                return false;

            return authHeader == $"Bearer {_token}";
        }

        // ── POST /cmd ──────────────────────────────────────────

        private async Task HandleCommandAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, s_Utf8))
                body = await reader.ReadToEndAsync();

            var request = SimpleJson.Parse(body);
            if (request == null)
            {
                await SendJsonResponse(ctx.Response, 400, new
                {
                    status = "error",
                    code = 400,
                    message = "Invalid JSON"
                });
                return;
            }

            string requestId = request.GetString("requestId", "");
            string command = request.GetString("command", "");
            string paramsJson = request.GetRaw("params", "{}");

            if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(command))
            {
                await SendJsonResponse(ctx.Response, 400, new
                {
                    status = "error",
                    code = 400,
                    message = "Missing requestId or command"
                });
                return;
            }

            // 立即回复 received
            await SendJsonResponse(ctx.Response, 200, new
            {
                requestId,
                status = "received"
            });

            // 记录到待处理队列
            var cmdCtx = new CommandContext
            {
                RequestId = requestId,
                Command = command,
                ParamsJson = paramsJson,
                ReceivedAt = DateTime.UtcNow,
            };
            _pendingCommands[requestId] = cmdCtx;

            // 调度到主线程执行
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                ExecuteCommand(cmdCtx));
        }

        // ── 命令执行（主线程） ─────────────────────────────────

        private void ExecuteCommand(CommandContext ctx)
        {
            AgentStatus = "thinking";

            try
            {
                // 解析参数
                var args = SimpleJson.Parse(ctx.ParamsJson);

                // 根据命令分发
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

                // 推送 WS 回执
                _ = PushReceiptAsync(ctx.RequestId, resultJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GlobalCLIMgr] Command error: {ctx.Command} - {e.Message}");
                _ = PushReceiptAsync(ctx.RequestId, SimpleJson.Serialize(new
                {
                    status = "error",
                    code = 500,
                    message = e.Message
                }));
            }
            finally
            {
                _pendingCommands.Remove(ctx.RequestId);
                AgentStatus = "idle";
            }
        }

        // ── WS 推送 ────────────────────────────────────────────

        private async Task PushReceiptAsync(string requestId, string resultJson)
        {
            var payload = $"{{\"requestId\":\"{requestId}\",\"status\":\"completed\",\"data\":{resultJson}}}";

            var deadClients = new List<string>();
            foreach (var kvp in _wsClients)
            {
                try
                {
                    if (kvp.Value.State == WebSocketState.Open)
                    {
                        var bytes = s_Utf8.GetBytes(payload);
                        await kvp.Value.SendAsync(
                            new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                    else
                    {
                        deadClients.Add(kvp.Key);
                    }
                }
                catch
                {
                    deadClients.Add(kvp.Key);
                }
            }

            foreach (var key in deadClients)
                _wsClients.Remove(key);
        }

        // ── WebSocket ─────────────────────────────────────────

        private async Task HandleWebSocketAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            // 验证 token（query 参数）
            var token = ctx.Request.QueryString["token"];
            if (!string.IsNullOrEmpty(_token) && token != _token)
            {
                ctx.Response.StatusCode = 401;
                ctx.Response.Close();
                return;
            }

            if (!ctx.Request.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Close();
                return;
            }

            WebSocketContext wsCtx;
            try
            {
                wsCtx = await ctx.AcceptWebSocketAsync(null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GlobalCLIMgr] WebSocket upgrade failed: {e.Message}");
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
                return;
            }

            var ws = wsCtx.WebSocket;
            var clientId = Guid.NewGuid().ToString("N")[..8];
            _wsClients[clientId] = ws;
            Debug.Log($"[GlobalCLIMgr] WS client connected: {clientId}");

            // WS 是单向推送通道，不接收消息
            // 等待连接关闭
            try
            {
                var buffer = new byte[1024];
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None);
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            catch (Exception e)
            {
                Debug.LogWarning($"[GlobalCLIMgr] WS error ({clientId}): {e.Message}");
            }
            finally
            {
                _wsClients.Remove(clientId);
                Debug.Log($"[GlobalCLIMgr] WS client disconnected: {clientId}");
            }
        }

        // ── GET /data/export ──────────────────────────────────

        private async Task HandleDataExportAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            // 导出所有 DataBase 子类的数据
            var items = CollectDataExport();
            var json = SimpleJson.Serialize(items);
            var bytes = s_Utf8.GetBytes(json);

            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, ct);
            ctx.Response.Close();
        }

        private List<object> CollectDataExport()
        {
            var items = new List<object>();

            // 使用反射查找所有 DataBase 子类的实例
            // 实际项目中，这里会遍历 GlobalDataMgr 中的数据
            // 此处返回示例数据，实际实现需对接数据层
            try
            {
                var dataMgrType = Type.GetType("MCV_Module.GlobalManager.GlobalDataMgr");
                if (dataMgrType != null)
                {
                    var instance = dataMgrType.GetProperty("Instance")?.GetValue(null);
                    if (instance != null)
                    {
                        // 遍历所有 DataBase 属性
                        var props = instance.GetType().GetProperties(
                            BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in props)
                        {
                            if (typeof(DataBase).IsAssignableFrom(prop.PropertyType))
                            {
                                var db = prop.GetValue(instance) as DataBase;
                                if (db != null)
                                {
                                    items.Add(new
                                    {
                                        id = db.id,
                                        displayName = db.displayName,
                                        description = db.description ?? "",
                                        tag = Array.Empty<string>(),
                                        data = new { },
                                        knowledgeOriginal = "",
                                        templateType = ""
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GlobalCLIMgr] Data export error: {e.Message}");
            }

            return items;
        }

        // ── 命令处理程序 ───────────────────────────────────────

        // 注意：以下为简化实现，完整实现需对接具体数据层和 UI 系统

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
            Debug.Log($"[GlobalCLIMgr] Run page: {pageId} (file: {filePath})");
            return SimpleJson.Serialize(new { pageId, status = "rendered" });
        }

        private string HandleUpdate(SimpleJson.JsonObject args)
        {
            var pageId = args?.GetString("pageId", "");
            Debug.Log($"[GlobalCLIMgr] Update page: {pageId}");
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
            AgentStatus = "idle";
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
            // Embedding 由 Python MCP Server 处理
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
                commandCount = _pendingCommands.Count,
                wsClients = _wsClients.Count,
                status = AgentStatus,
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
                queueLength = 0
            });
        }

        private string HandleInit(SimpleJson.JsonObject args)
        {
            // TODO: 持久化配置
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
                status = AgentStatus,
                pendingCommands = _pendingCommands.Keys.ToArray(),
                wsClients = _wsClients.Count,
            });
        }

        // ── HTTP 辅助 ─────────────────────────────────────────

        private static async Task SendJsonResponse(HttpListenerResponse response, int statusCode, object data)
        {
            var json = SimpleJson.Serialize(data);
            var bytes = s_Utf8.GetBytes(json);

            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }
    }

    // ── 简单 JSON 工具（无第三方依赖） ────────────────────────

    /// <summary>
    /// 极简 JSON 解析/序列化工具，仅支持 GlobalCLIMgr 所需的数据结构。
    /// </summary>
    internal static class SimpleJson
    {
        public class JsonObject : Dictionary<string, object>
        {
            public string GetString(string key, string defaultValue = "")
            {
                if (TryGetValue(key, out var val) && val != null)
                    return val.ToString();
                return defaultValue;
            }

            public string GetRaw(string key, string defaultValue = "{}")
            {
                return GetString(key, defaultValue);
            }
        }

        public static JsonObject Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var obj = new JsonObject();
                var trimmed = json.Trim();
                if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
                    return null;

                // 移除花括号
                var inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (string.IsNullOrEmpty(inner))
                    return obj;

                // 简单 key-value 解析（不支持嵌套对象/数组）
                int i = 0;
                while (i < inner.Length)
                {
                    // 跳过空白
                    while (i < inner.Length && char.IsWhiteSpace(inner[i])) i++;
                    if (i >= inner.Length) break;

                    // 读取 key
                    if (inner[i] != '"') { i++; continue; }
                    i++;
                    var key = new StringBuilder();
                    while (i < inner.Length && inner[i] != '"')
                    {
                        if (inner[i] == '\\') { i++; if (i < inner.Length) key.Append(inner[i]); }
                        else key.Append(inner[i]);
                        i++;
                    }
                    i++; // 跳过 closing quote

                    // 跳过冒号
                    while (i < inner.Length && (inner[i] == ':' || char.IsWhiteSpace(inner[i]))) i++;

                    // 读取 value
                    if (i < inner.Length)
                    {
                        if (inner[i] == '"')
                        {
                            // 字符串值
                            i++;
                            var val = new StringBuilder();
                            while (i < inner.Length && inner[i] != '"')
                            {
                                if (inner[i] == '\\') { i++; if (i < inner.Length) val.Append(inner[i]); }
                                else val.Append(inner[i]);
                                i++;
                            }
                            i++;
                            obj[key.ToString()] = val.ToString();
                        }
                        else if (inner[i] == '{' || inner[i] == '[')
                        {
                            // 嵌套对象/数组 — 跳过（简化处理）
                            char depthChar = inner[i];
                            int depth = 1; i++;
                            while (i < inner.Length && depth > 0)
                            {
                                if (inner[i] == depthChar) depth++;
                                else if (inner[i] == (depthChar == '{' ? '}' : ']')) depth--;
                                if (depth > 0) i++;
                            }
                            i++;
                            obj[key.ToString()] = "";
                        }
                        else
                        {
                            // 数字/布尔/null
                            var val = new StringBuilder();
                            while (i < inner.Length && inner[i] != ',' && inner[i] != '}' && inner[i] != ']')
                            {
                                val.Append(inner[i]);
                                i++;
                            }
                            obj[key.ToString()] = val.ToString().Trim();
                        }
                    }

                    // 跳过逗号
                    while (i < inner.Length && (inner[i] == ',' || char.IsWhiteSpace(inner[i]))) i++;
                }

                return obj;
            }
            catch
            {
                return null;
            }
        }

        public static string Serialize(object obj)
        {
            if (obj == null) return "null";
            if (obj is string s) return $"\"{EscapeJson(s)}\"";
            if (obj is bool b) return b ? "true" : "false";
            if (obj is int i) return i.ToString();
            if (obj is long l) return l.ToString();
            if (obj is float f) return f.ToString("G");
            if (obj is double d) return d.ToString("G");
            if (obj is decimal m) return m.ToString("G");
            if (obj is DateTime dt) return $"\"{dt:O}\"";
            if (obj is System.Collections.IEnumerable enumerable && obj is not string)
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                    items.Add(Serialize(item));
                return "[" + string.Join(",", items) + "]";
            }

            // 通过反射序列化属性/字段
            var sb = new StringBuilder("{");
            var type = obj.GetType();
            bool first = true;

            // 字段
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{field.Name}\":");
                sb.Append(Serialize(field.GetValue(obj)));
                first = false;
            }

            // 属性
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                {
                    if (!first) sb.Append(",");
                    sb.Append($"\"{prop.Name}\":");
                    sb.Append(Serialize(prop.GetValue(obj, null)));
                    first = false;
                }
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
