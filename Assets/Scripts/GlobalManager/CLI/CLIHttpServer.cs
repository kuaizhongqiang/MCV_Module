using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCV_Module.Data;
using UnityEngine;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// CLI HTTP 服务器 — 管理 HttpListener 生命周期、请求路由、鉴权和 /data/export 端点。
    /// 运行在后台线程，通过事件向上层通知命令到达。
    /// </summary>
    public class CLIHttpServer
    {
        private readonly int _port;
        private readonly string _token;
        private readonly CLIWebSocketManager _wsManager;
        private HttpListener _httpListener;
        private static readonly Encoding s_Utf8 = new UTF8Encoding(false);

        /// <summary>服务器是否正在运行</summary>
        public bool IsRunning => _httpListener != null && _httpListener.IsListening;

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
        /// 启动 HTTP 服务器，开始监听请求。
        /// </summary>
        public async Task StartAsync(CancellationToken ct)
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                _httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                _httpListener.Prefixes.Add($"http://[::1]:{_port}/");
                _httpListener.Start();
                OnLog?.Invoke($"[CLIHttpServer] HTTP server started on port {_port}");

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var ctx = await _httpListener.GetContextAsync();
                        _ = HandleRequestAsync(ctx, ct);
                    }
                    catch (HttpListenerException) { break; }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception e)
                    {
                        if (ct.IsCancellationRequested) break;
                        OnLogError?.Invoke($"[CLIHttpServer] HTTP error: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                OnLogError?.Invoke($"[CLIHttpServer] Failed to start: {e.Message}");
            }
        }

        /// <summary>
        /// 停止 HTTP 服务器。
        /// </summary>
        public void Stop()
        {
            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch { }
            _httpListener = null;
        }

        // ── 请求路由 ───────────────────────────────────────────

        private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            try
            {
                var path = ctx.Request.Url.AbsolutePath.TrimEnd('/');

                // 鉴权检查（WebSocket 在升级时验证 token，不由这里鉴权）
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
                        await _wsManager.HandleWebSocketAsync(ctx, _token, ct);
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
                OnLogError?.Invoke($"[CLIHttpServer] Request error: {e.Message}");
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

            // 通知上层处理命令
            var cmdCtx = new CommandContext
            {
                RequestId = requestId,
                Command = command,
                ParamsJson = paramsJson,
                ReceivedAt = DateTime.UtcNow,
            };

            OnCommandReceived?.Invoke(cmdCtx);
        }

        // ── GET /data/export ──────────────────────────────────

        private async Task HandleDataExportAsync(HttpListenerContext ctx, CancellationToken ct)
        {
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
}
