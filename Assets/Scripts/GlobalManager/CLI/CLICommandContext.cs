using System;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// 命令上下文 — 携带一条命令的完整信息，在 HTTP 服务器和命令处理器之间传递。
    /// </summary>
    public class CommandContext
    {
        /// <summary>请求 ID（用于回执匹配）</summary>
        public string RequestId { get; set; }

        /// <summary>命令名称（如 "get.data", "page.create"）</summary>
        public string Command { get; set; }

        /// <summary>参数 JSON 原文</summary>
        public string ParamsJson { get; set; }

        /// <summary>命令接收时间戳</summary>
        public DateTime ReceivedAt { get; set; }
    }
}
