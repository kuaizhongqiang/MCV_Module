using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using MCV_Module.Data;
using UnityEngine;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// CLI HTTP 服务器 — 基于 EmbedIO WebServer，统一管理 HTTP API 和 WebSocket。
    /// 运行在后台线程，通过事件向上层通知命令到达。
    /// </summary>
    public class CLIHttpServer : IDisposable
    {
        private readonly int _port;
        private readonly string _token;
        private readonly CLIWebSocketManager _wsManager;
        private WebServer _server;
        private bool _disposed;
        internal CLICommandHandler CommandHandler { get; set; }

        public bool IsRunning => _server != null && !_disposed;

        public event Action<CommandContext> OnCommandReceived;
        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;
        public event Action<string> OnLogError;

        public CLIHttpServer(int port, string token, CLIWebSocketManager wsManager)
        {
            _port = port;
            _token = token;
            _wsManager = wsManager;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            try
            {
                _server = new WebServer(o => o
                        .WithUrlPrefix($"http://127.0.0.1:{_port}/")
                        .WithUrlPrefix($"http://localhost:{_port}/")
                        .WithMode(HttpListenerMode.EmbedIO))
                    .WithWebApi("/", m => m
                        .WithController(() => new CommandApiController(this)))
                    .WithModule(_wsManager);

                _server.StateChanged += (s, e) =>
                    OnLog?.Invoke($"[CLIHttpServer] State: {e.NewState}");

                OnLog?.Invoke($"[CLIHttpServer] Starting on port {_port}");
                await _server.RunAsync(ct);
                OnLog?.Invoke($"[CLIHttpServer] Server stopped");
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                OnLogError?.Invoke($"[CLIHttpServer] Failed to start: {e.Message}");
            }
        }

        public void Stop()
        {
            if (_server != null)
            {
                try { _server.Dispose(); }
                catch (Exception e) { OnLogWarning?.Invoke($"[CLIHttpServer] Stop error: {e.Message}"); }
                _server = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        // ── 鉴权 ───────────────────────────────────────────────

        private bool CheckAuth(IHttpContext context)
        {
            if (string.IsNullOrEmpty(_token)) return true;
            var authHeader = context.Request.Headers["Authorization"];
            return !string.IsNullOrEmpty(authHeader) && authHeader == $"Bearer {_token}";
        }

        // ── WebApi Controller ──────────────────────────────────

        private class CommandApiController : WebApiController
        {
            private readonly CLIHttpServer _server;

            public CommandApiController(CLIHttpServer server) => _server = server;

            [Route(HttpVerbs.Post, "/cmd")]
            public async Task ReceiveCommand()
            {
                if (!_server.CheckAuth(HttpContext))
                {
                    await SendError(401, "Unauthorized");
                    return;
                }

                string body = await HttpContext.GetRequestBodyAsStringAsync().ConfigureAwait(false);
                var request = SimpleJson.Parse(body);
                if (request == null)
                {
                    await SendError(400, "Invalid JSON");
                    return;
                }

                string requestId = request.GetString("requestId", "");
                string command = request.GetString("command", "");
                string paramsJson = request.GetRaw("params", "{}");

                if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(command))
                {
                    await SendError(400, "Missing requestId or command");
                    return;
                }

                await SendJson(200, new { requestId, status = "received" });

                var cmdCtx = new CommandContext
                {
                    RequestId = requestId,
                    Command = command,
                    ParamsJson = paramsJson,
                    ReceivedAt = DateTime.UtcNow,
                };
                _server.OnCommandReceived?.Invoke(cmdCtx);
            }

            [Route(HttpVerbs.Get, "/data/export")]
            public async Task GetExportData()
            {
                var items = _server.CollectDataExport();
                await SendJson(200, items);
            }

            [Route(HttpVerbs.Get, "/result")]
            public async Task PollResult()
            {
                var requestId = HttpContext.Request.QueryString["requestId"];
                if (string.IsNullOrEmpty(requestId))
                {
                    await SendJson(400, new { status = "error", message = "Missing requestId" });
                    return;
                }

                var result = _server.CommandHandler?.PollResult(requestId);
                if (result != null)
                {
                    await SendJson(200, new { status = "completed", data = SimpleJson.Parse(result) });
                }
                else
                {
                    await SendJson(200, new { status = "pending" });
                }
            }

            private async Task SendError(int code, string message)
            {
                await SendJson(code, new { status = "error", code, message });
            }

            private async Task SendJson(int statusCode, object data)
            {
                var json = SimpleJson.Serialize(data);
                HttpContext.Response.StatusCode = statusCode;
                await HttpContext.SendStringAsync(json, "application/json; charset=utf-8",
                    Encoding.UTF8);
            }
        }

        // ── /data/export 数据收集 ─────────────────────────────

        private List<object> CollectDataExport()
        {
            var items = new List<object>();
            try
            {
                var dataMgrType = Type.GetType("MCV_Module.GlobalManager.GlobalDataMgr");
                if (dataMgrType?.GetProperty("Instance")?.GetValue(null) is var instance && instance != null)
                {
                    foreach (var prop in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (typeof(DataBase).IsAssignableFrom(prop.PropertyType) && prop.GetValue(instance) is DataBase db)
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
            catch (Exception e)
            {
                OnLogWarning?.Invoke($"[CLIHttpServer] Data export error: {e.Message}");
            }
            return items;
        }
    }
}
