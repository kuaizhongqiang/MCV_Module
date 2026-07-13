using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// WebSocket 管理器 — 处理 WS 客户端连接、维护连接池、推送回执和事件。
    /// </summary>
    public class CLIWebSocketManager
    {
        private readonly Dictionary<string, WebSocket> _wsClients = new();
        private static readonly Encoding s_Utf8 = new UTF8Encoding(false);

        /// <summary>是否有已连接的 WS 客户端</summary>
        public bool HasClients => _wsClients.Count > 0;

        /// <summary>WS 客户端数量</summary>
        public int ClientCount => _wsClients.Count;

        /// <summary>日志回调</summary>
        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;

        /// <summary>
        /// 处理 WebSocket 升级请求。
        /// </summary>
        public async Task HandleWebSocketAsync(HttpListenerContext ctx, string token, CancellationToken ct)
        {
            // 验证 token（query 参数）
            var queryToken = ctx.Request.QueryString["token"];
            if (!string.IsNullOrEmpty(token) && queryToken != token)
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
                OnLogWarning?.Invoke($"[WSManager] WebSocket upgrade failed: {e.Message}");
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
                return;
            }

            var ws = wsCtx.WebSocket;
            var clientId = Guid.NewGuid().ToString("N")[..8];
            _wsClients[clientId] = ws;
            OnLog?.Invoke($"[WSManager] WS client connected: {clientId}");

            await WaitForConnectionClose(ws, clientId, ct);
        }

        /// <summary>
        /// 向所有连接的 WS 客户端推送命令执行回执。
        /// </summary>
        public async Task PushReceiptAsync(string requestId, string resultJson)
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

        private async Task WaitForConnectionClose(WebSocket ws, string clientId, CancellationToken ct)
        {
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
                OnLogWarning?.Invoke($"[WSManager] WS error ({clientId}): {e.Message}");
            }
            finally
            {
                _wsClients.Remove(clientId);
                OnLog?.Invoke($"[WSManager] WS client disconnected: {clientId}");
            }
        }
    }
}
