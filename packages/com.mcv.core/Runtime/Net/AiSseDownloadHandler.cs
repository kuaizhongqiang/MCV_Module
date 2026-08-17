using System;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MCV_Module.Net
{
    /// <summary>
    /// SSE 下载处理器: 在 UnityWebRequest 底层接收线程上按行解析
    /// "data: {...}" 事件, 放入线程安全队列; 主线程协程逐帧取用。
    ///
    /// 注意: ReceiveData 在后台线程回调, 因此只做入队, 不做任何 Unity API 调用。
    /// </summary>
    public class AiSseDownloadHandler : DownloadHandlerScript
    {
        readonly ConcurrentQueue<string> _events = new ConcurrentQueue<string>();
        readonly StringBuilder _buffer = new StringBuilder();

        /// <summary>取出一条原始事件(data: 后的 JSON 文本); 无事件返回 false</summary>
        public bool TryDequeue(out string json)
        {
            return _events.TryDequeue(out json);
        }

        public int PendingCount
        {
            get { return _events.Count; }
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0)
                return true;

            string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
            _buffer.Append(chunk);

            while (true)
            {
                int idx = _buffer.ToString().IndexOf("\n", StringComparison.Ordinal);
                if (idx < 0)
                    break;

                string line = _buffer.ToString().Substring(0, idx).TrimEnd('\r');
                _buffer.Remove(0, idx + 1);

                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    string payload = line.Substring(5).Trim();
                    if (payload.Length > 0)
                        _events.Enqueue(payload);
                }
            }
            return true;
        }

        protected override void CompleteContent()
        {
            // 尾部残留(无换行的最后一行)
            string tail = _buffer.ToString().Trim();
            if (tail.StartsWith("data:", StringComparison.Ordinal))
            {
                string payload = tail.Substring(5).Trim();
                if (payload.Length > 0)
                    _events.Enqueue(payload);
            }
        }
    }
}
