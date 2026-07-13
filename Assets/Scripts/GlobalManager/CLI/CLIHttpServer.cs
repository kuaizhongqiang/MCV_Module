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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// CLI HTTP 服务器 — 管理 EmbedIO WebServer 生命周期、请求路由、鉴权、/cmd 和 /data/export 端点。
    /// 运行在后台线程，通过事件向上层通知命令到达。
    /// 实现 IDisposable 以确保 IL2CPP 兼容的资源清理。
    /// </summary>
    public class CLIHttpServer : IDisposable
    {
        private readonly int _port;
        private readonly string _token;
        private readonly CLIWebSocketManager _wsManager;
        private WebServer _server;
        private bool _disposed;
        private static readonly Encoding s_Utf8 = new UTF8Encoding(false);

        /// <summary>服务器是否正在运行</summary>
        public bool IsRunning => _server != null && !_disposed;

        /// <summary>收到新命令时触发</summary>
        public event Action<CommandContext> OnCommandReceived;

        /// <summary>日志回调</summary>
        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;
        public event Action<string> OnLogError;

        public CLIHttpServer(int port, string token, CLIWebSocketManager wsManager)
        {
            _port = port;
            _token = token;
            _wsManager = wsManager;
        }

        /// <summary>
        /// 启动 EmbedIO WebServer，开始监听请求。
        /// </summary>
        public async Task StartAsync(CancellationToken ct)
        {
            try
            {
                _server = new WebServer(o => o
                        .WithUrlPrefix($"http://127.0.0.1:{_port}/")
                        .WithMode(HttpListenerMode.EmbedIO))
                    .WithWebApi("/", m => m
                        .WithController(() => new CommandController(this))
                        .WithController(() => new DataExportController(this)))
                    .WithWebSocket("/ws", m => m.WithModule(() => _wsManager));

                _server.StateChanged += (s, e) =>
                {
                    OnLog?.Invoke($"[CLIHttpServer] State: {e.NewState}");
                };

                OnLog?.Invoke($"[CLIHttpServer] Starting on port {_port}");
                await _server.RunAsync(ct);
                OnLog?.Invoke($"[CLIHttpServer] Server stopped");
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown via cancellation token
            }
            catch (Exception e)
            {
                OnLogError?.Invoke($"[CLIHttpServer] Failed to start: {e.Message}");
            }
        }

        /// <summary>
        /// 停止并释放 WebServer。
        /// </summary>
        public void Stop()
        {
            if (_server != null)
            {
                try
                {
                    _server.Dispose();
                }
                catch (Exception e)
                {
                    OnLogWarning?.Invoke($"[CLIHttpServer] Stop error: {e.Message}");
                }
                _server = null;
            }
        }

        // ── IDisposable ─────────────────────────────────────────

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Stop();
            }
            _disposed = true;
        }

        // ── 内部辅助（由 Controller 使用）────────────────────────

        internal bool CheckAuth(IHttpContext context)
        {
            if (string.IsNullOrEmpty(_token))
                return true; // 无 token 时放行（开发模式）

            var authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader))
                return false;

            return authHeader == $"Bearer {_token}";
        }

        internal async Task SendJsonResponse(IHttpContext context, int statusCode, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var bytes = s_Utf8.GetBytes(json);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        internal List<object> CollectDataExport()
        {
            var items = new List<object>();

            try
            {
                var dataMgrType = Type.GetType("MCV_Module.GlobalManager.GlobalDataMgr");
                if (dataMgrType != null)
                {
                    var instance = dataMgrType.GetProperty("Instance")?.GetValue(null);
                    if (instance != null)
                    {
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
                OnLogWarning?.Invoke($"[CLIHttpServer] Data export error: {e.Message}");
            }

            return items;
        }

        // ── WebApi Controllers ─────────────────────────────────

        /// <summary>
        /// 处理 POST /cmd — 接收 Agent 命令，解析 JSON 体，触发 OnCommandReceived 事件。
        /// </summary>
        private class CommandController : WebApiController
        {
            private readonly CLIHttpServer _server;

            public CommandController(CLIHttpServer server)
            {
                _server = server;
            }

            [Route(HttpVerbs.Post, "/cmd")]
            public async Task ReceiveCommand()
            {
                // 鉴权
                if (!_server.CheckAuth(HttpContext))
                {
                    await _server.SendJsonResponse(HttpContext, 401, new
                    {
                        status = "error",
                        code = 401,
                        message = "Unauthorized"
                    });
                    return;
                }

                // 读取请求体
                string body;
                using (var reader = new System.IO.StreamReader(
                    HttpContext.Request.InputStream, s_Utf8))
                {
                    body = await reader.ReadToEndAsync();
                }

                // 解析 JSON
                JObject request;
                try
                {
                    request = JObject.Parse(body);
                }
                catch (JsonReaderException)
                {
                    await _server.SendJsonResponse(HttpContext, 400, new
                    {
                        status = "error",
                        code = 400,
                        message = "Invalid JSON"
                    });
                    return;
                }

                string requestId = request.Value<string>("requestId") ?? "";
                string command = request.Value<string>("command") ?? "";
                string paramsJson = request.Value<string>("params") ?? "{}";

                if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(command))
                {
                    await _server.SendJsonResponse(HttpContext, 400, new
                    {
                        status = "error",
                        code = 400,
                        message = "Missing requestId or command"
                    });
                    return;
                }

                // 立即回复 received
                await _server.SendJsonResponse(HttpContext, 200, new
                {
                    requestId,
                    status = "received"
                });

                // 通知上层处理命令
                var cmdCtx = new CommandContext
                {
                    RequestId = requestId,
                    Command = command,
                    ParamsJson = paramsJson,
                    ReceivedAt = DateTime.UtcNow,
                };

                _server.OnCommandReceived?.Invoke(cmdCtx);
            }
        }

        /// <summary>
        /// 处理 GET /data/export — 导出 GlobalDataMgr 中所有 DataBase 派生属性的简明信息。
        /// </summary>
        private class DataExportController : WebApiController
        {
            private readonly CLIHttpServer _server;

            public DataExportController(CLIHttpServer server)
            {
                _server = server;
            }

            [Route(HttpVerbs.Get, "/data/export")]
            public async Task GetExportData()
            {
                // 数据导出不要求鉴权（或可按需开启）
                var items = _server.CollectDataExport();
                await _server.SendJsonResponse(HttpContext, 200, items);
            }
        }
    }
}
