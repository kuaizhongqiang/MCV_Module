using System;
using Newtonsoft.Json;

// ⚠️ 本文件编入 MCV.AiClient.dll（预编译 DLL）：公共类型变更必须
//    在 /bare 改源码 → 重建 DLL → 同步交付形态；禁止直接修改 DLL。

namespace MCV_Module.Net
{
    // ───────────────────────── 请求 ─────────────────────────

    /// <summary>对话消息</summary>
    [Serializable]
    public class AiChatMessage
    {
        [JsonProperty("role")] public string role = "user";
        [JsonProperty("content")] public string content = "";

        public AiChatMessage() { }
        public AiChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    /// <summary>
    /// 统一对话请求(Unity -> AiServer)。
    /// provider 留空用配置默认; model 留空用该 provider 默认。
    /// reasoningEffort: "low"|"medium"|"high", 仅对声明支持的 provider 生效。
    /// </summary>
    [Serializable]
    public class AiChatRequest
    {
        [JsonProperty("provider")] public string provider = "";
        [JsonProperty("model")] public string model = "";
        [JsonProperty("messages")] public AiChatMessage[] messages = new AiChatMessage[0];
        [JsonProperty("stream")] public bool stream = true;
        [JsonProperty("temperature")] public float? temperature = null;
        [JsonProperty("max_tokens")] public int? maxTokens = null;
        [JsonProperty("reasoning")] public bool reasoning = true;
        [JsonProperty("reasoning_effort")] public string reasoningEffort = null;

        public AiChatRequest() { }

        public AiChatRequest(string userText, bool stream = true)
        {
            messages = new[] { new AiChatMessage("user", userText) };
            this.stream = stream;
        }
    }

    // ───────────────────────── 响应 ─────────────────────────

    /// <summary>非流式完整响应(OpenAI 形状)</summary>
    [Serializable]
    public class AiChatResponse
    {
        [JsonProperty("id")] public string id = "";
        [JsonProperty("model")] public string model = "";
        [JsonProperty("provider")] public string provider = "";
        [JsonProperty("choices")] public AiChoice[] choices = new AiChoice[0];
        [JsonProperty("usage")] public AiUsage usage = null;

        [Serializable]
        public class AiChoice
        {
            [JsonProperty("index")] public int index;
            [JsonProperty("message")] public AiResponseMessage message = new AiResponseMessage();
            [JsonProperty("finish_reason")] public string finishReason = null;
        }

        [Serializable]
        public class AiResponseMessage
        {
            [JsonProperty("role")] public string role = "assistant";
            [JsonProperty("content")] public string content = "";
            [JsonProperty("reasoning_content")] public string reasoningContent = null;
        }
    }

    /// <summary>流式分片(SSE 的 data 内容)</summary>
    [Serializable]
    public class AiChatChunk
    {
        [JsonProperty("id")] public string id = "";
        [JsonProperty("model")] public string model = "";
        [JsonProperty("provider")] public string provider = "";
        [JsonProperty("choices")] public AiChunkChoice[] choices = new AiChunkChoice[0];
        [JsonProperty("usage")] public AiUsage usage = null;

        [Serializable]
        public class AiChunkChoice
        {
            [JsonProperty("index")] public int index;
            [JsonProperty("delta")] public AiDelta delta = new AiDelta();
            [JsonProperty("finish_reason")] public string finishReason = null;
        }

        [Serializable]
        public class AiDelta
        {
            [JsonProperty("role")] public string role = null;
            [JsonProperty("content")] public string content = null;
            [JsonProperty("reasoning_content")] public string reasoningContent = null;
        }

        /// <summary>当前分片是否携带正文增量</summary>
        public bool HasContent { get { return choices.Length > 0 && !string.IsNullOrEmpty(choices[0].delta.content); } }

        /// <summary>当前分片是否携带思考增量</summary>
        public bool HasReasoning { get { return choices.Length > 0 && !string.IsNullOrEmpty(choices[0].delta.reasoningContent); } }

        /// <summary>流是否已结束(服务端发来的哨兵分片)</summary>
        [JsonIgnore] public bool IsDone { get; set; }
    }

    [Serializable]
    public class AiUsage
    {
        [JsonProperty("prompt_tokens")] public int promptTokens;
        [JsonProperty("completion_tokens")] public int completionTokens;
        [JsonProperty("total_tokens")] public int totalTokens;
    }

    /// <summary>对话最终结果(累积完成后的便捷结构)</summary>
    public class AiChatResult
    {
        public string content = "";
        public string reasoningContent = "";
        public string provider = "";
        public string model = "";
        public bool success = false;
        public string error = "";
        public AiUsage usage = null;

        public AiChatResult() { }
        public AiChatResult(string error)
        {
            success = false;
            this.error = error;
        }
    }

}
