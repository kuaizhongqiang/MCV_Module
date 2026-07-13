using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.WebSockets;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// WebSocket 管理器 — EmbedIO WebSocketModule 子类。
    /// 处理 WS 客户端连接/断开、token 鉴权、连接池维护和回执推送。
    /// </summary>
    public class CLIWebSocketManager : WebSocketModule
    {
        private readonly string _token;
        private readonly ConcurrentDictionary<string, IWebSocketContext> _wsClients = new();
        private static readonly Encoding s_Utf8 = new UTF8Encoding(false);

        /// <summary>是否有已连接的 WS 客户端</summary>
        public bool HasClients => _wsClients.Count > 0;

        /// <summary>WS 客户端数量</summary>
        public int ClientCount => _wsClients.Count;

        /// <summary>日志回调</summary>
        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;

        /// <summary>
        /// 创建 WebSocket 模块。
        /// </summary>
        /// <param name="token">可选的鉴权 token，为空时放行所有连接。</param>
        public CLIWebSocketManager(string token = "")
            : base("/ws", true) // keepOpen=true 允许服务器主动推送
        {
            _token = token;
        }

        /// <summary>
        /// 新客户端连接时调用 — 进行 token 鉴权并注册到连接池。
        /// </summary>
        protected override async Task OnClientConnectedAsync(IWebSocketContext context)
        {
            // Token 鉴权（通过查询参数 ?token=xxx）
            if (!string.IsNullOrEmpty(_token))
            {
                var queryToken = ExtractQueryParam(context.RequestUri, "token");
                if (queryToken != _token)
                {
                    OnLogWarning?.Invoke("[WSManager] WS client rejected: invalid token");
                    await CloseAsync(context, WebSocketCloseStatus.PolicyViolation, "Unauthorized");
                    return;
                }
            }

            var clientId = Guid.NewGuid().ToString("N")[..8];
            context.Items["clientId"] = clientId;
            _wsClients[clientId] = context;

            OnLog?.Invoke($"[WSManager] WS client connected: {clientId} (total: {_wsClients.Count})");
        }

        /// <summary>
        /// 客户端断开连接时调用 — 从连接池中移除。
        /// </summary>
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            if (context.Items.TryGetValue("clientId", out var idObj) && idObj is string clientId)
            {
                _wsClients.TryRemove(clientId, out _);
                OnLog?.Invoke($"[WSManager] WS client disconnected: {clientId} (remaining: {_wsClients.Count})");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 收到客户端消息时调用（当前未使用，保持连接活跃）。
        /// </summary>
        protected override Task OnMessageReceivedAsync(
            IWebSocketContext context,
            byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            // 当前不处理来自客户端的消息；可用于 ping/pong 或 future 协议扩展
            return Task.CompletedTask;
        }

        /// <summary>
        /// 向所有已连接的 WS 客户端推送命令执行回执。
        /// </summary>
        public async Task PushReceiptAsync(string requestId, string resultJson)
        {
            var payload = $"{{\"requestId\":\"{requestId}\",\"status\":\"completed\",\"data\":{resultJson}}}";
            var bytes = s_Utf8.GetBytes(payload);

            var deadClients = new List<string>();
            foreach (var kvp in _wsClients)
            {
                try
                {
                    var ctx = kvp.Value;
                    if (ctx.WebSocket.State == WebSocketState.Open)
                    {
                        await SendAsync(ctx, bytes, 0, bytes.Length, true);
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
            {
                _wsClients.TryRemove(key, out _);
            }

            if (deadClients.Count > 0)
            {
                OnLog?.Invoke($"[WSManager] Cleaned up {deadClients.Count} dead client(s)");
            }
        }

        /// <summary>
        /// 从 URI 查询字符串中提取指定参数的值。
        /// </summary>
        private static string ExtractQueryParam(Uri uri, string paramName)
        {
            if (uri == null || string.IsNullOrEmpty(uri.Query))
                return null;

            var query = uri.Query.TrimStart('?');
            var parts = query.Split('&');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0].Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(kv[1]);
            }

            return null;
        }
    }
}
