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

        public bool HasClients => _wsClients.Count > 0;
        public int ClientCount => _wsClients.Count;

        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;

        public CLIWebSocketManager(string token = "")
            : base("/ws", true)
        {
            _token = token;
        }

        protected override async Task OnClientConnectedAsync(IWebSocketContext context)
        {
            if (!string.IsNullOrEmpty(_token))
            {
                var queryToken = ExtractQueryParam(context.RequestUri, "token");
                if (queryToken != _token)
                {
                    OnLogWarning?.Invoke("[WSManager] WS client rejected: invalid token");
                    await context.WebSocket.CloseAsync().ConfigureAwait(false);
                    return;
                }
            }

            var clientId = Guid.NewGuid().ToString("N")[..8];
            context.Items["clientId"] = clientId;
            _wsClients[clientId] = context;
            OnLog?.Invoke($"[WSManager] WS client connected: {clientId} (total: {_wsClients.Count})");
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            if (context.Items.TryGetValue("clientId", out var idObj) && idObj is string clientId)
            {
                _wsClients.TryRemove(clientId, out _);
                OnLog?.Invoke($"[WSManager] WS client disconnected: {clientId} (remaining: {_wsClients.Count})");
            }
            return Task.CompletedTask;
        }

        protected override Task OnMessageReceivedAsync(
            IWebSocketContext context, byte[] rxBuffer, IWebSocketReceiveResult rxResult)
        {
            return Task.CompletedTask;
        }

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
                        await SendAsync(ctx, System.Text.Encoding.UTF8.GetString(bytes));
                    }
                    else deadClients.Add(kvp.Key);
                }
                catch { deadClients.Add(kvp.Key); }
            }
            foreach (var key in deadClients) _wsClients.TryRemove(key, out _);
        }

        private static string ExtractQueryParam(Uri uri, string paramName)
        {
            if (uri == null || string.IsNullOrEmpty(uri.Query)) return null;
            var query = uri.Query.TrimStart('?');
            foreach (var part in query.Split('&'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0].Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(kv[1]);
            }
            return null;
        }
    }
}
