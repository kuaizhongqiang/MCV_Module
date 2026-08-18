using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace MCV_Module.Net
{
    /// <summary>
    /// AiServer(单 EXE) 的 Unity 客户端 —— 纯协议层，由 GlobalAiMgr 控制。
    ///
    /// ⚠️ 本文件编入 MCV.AiClient.dll（预编译 DLL）：公共类型/方法变更必须
    ///    在 /bare 改源码 → 重建 DLL → 同步交付形态；禁止直接修改 DLL。
    ///
    /// 职责（纯协议，跨平台，无进程操作）:
    ///   - 统一协议对话: POST /v1/chat/completions (流式 SSE / 非流式)
    ///   - 就绪探测: GET /health（轮询；"拉起 EXE"由宿主通过 tryLaunch 回调注入）
    ///   - 日志拉取: GET /v1/logs
    ///   - 请求鉴权头: X-Auth-Name / X-Auth-Token
    ///
    /// 进程管理（拉起/关闭 EXE）见 MCV_Module.Managers.AiServerProcess（留源码，含 #if !UNITY_WEBGL）。
    /// </summary>
    public class AiServerClient
    {
        readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>默认监听地址(与 EXE 内嵌配置一致)</summary>
        const string HOST = "127.0.0.1";

        /// <summary>默认监听端口(与 EXE 内嵌配置一致; Unity 拉起时会显式传 --port)</summary>
        const int PORT = 8765;

        /// <summary>上游对话请求超时(秒), 与 EXE 内嵌 timeout_seconds 一致</summary>
        const int REQUEST_TIMEOUT = 600;

        /// <summary>客户端鉴权名称 —— 必须与 EXE 内嵌白名单(.env CLIENT_WHITELIST)中一组一致。</summary>
        public const string AuthName = "asdf";

        /// <summary>客户端鉴权令牌 —— 必须与 EXE 内嵌白名单(.env CLIENT_WHITELIST)中一组一致</summary>
        public const string AuthToken = "asdfghjkl";

        bool _launching;
        bool _ready;

        /// <summary>服务是否已就绪(health 通过)</summary>
        public bool IsReady { get { return _ready; } }

        /// <summary>当前端口</summary>
        public int Port { get { return PORT; } }

        /// <summary>当前监听地址</summary>
        public string Host { get { return HOST; } }

        public string BaseUrl
        {
            get { return "http://" + Host + ":" + Port; }
        }

        /// <summary>给请求附加鉴权头(name + token 白名单)</summary>
        void ApplyAuthHeaders(UnityWebRequest uwr)
        {
            uwr.SetRequestHeader("X-Auth-Name", AuthName);
            uwr.SetRequestHeader("X-Auth-Token", AuthToken);
        }

        // ───────────────────────── 生命周期 ─────────────────────────

        /// <summary>
        /// 确保服务就绪: 已就绪则直接通过; 否则轮询 /health, 未就绪时调用 tryLaunch（由宿主拉起 EXE）。
        /// 可被多处调用, 内部保证只回调 tryLaunch 一次。WebGL 下 tryLaunch 由宿主实现为 no-op, 仅探测远程服务。
        /// </summary>
        /// <param name="tryLaunch">需要拉起进程时回调（宿主提供; 幂等）</param>
        public IEnumerator EnsureReadyAsync(Action tryLaunch, Action<bool> onReady, float timeoutSeconds = 15f)
        {
            if (_ready)
            {
                onReady?.Invoke(true);
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                bool ok = false;
                yield return CheckHealthAsync(r => ok = r);
                if (ok)
                {
                    _ready = true;
                    _launching = false;
                    Debug.Log($"[AiServerClient] 服务就绪: {BaseUrl}");
                    onReady?.Invoke(true);
                    yield break;
                }

                if (!_launching)
                {
                    _launching = true;
                    tryLaunch?.Invoke();
                }
                yield return new WaitForSeconds(0.25f);
            }

            _ready = false;
            Debug.LogWarning($"[AiServerClient] {timeoutSeconds}s 内未就绪: {BaseUrl} (EXE 是否存在? 端口是否被占?)");
            onReady?.Invoke(false);
        }

        /// <summary>探测 /health</summary>
        public IEnumerator CheckHealthAsync(Action<bool> onResult)
        {
            using (var uwr = UnityWebRequest.Get(BaseUrl + "/health"))
            {
                uwr.timeout = 2;
                ApplyAuthHeaders(uwr);
                yield return uwr.SendWebRequest();
                bool ok = uwr.result == UnityWebRequest.Result.Success;
                if (ok)
                {
                    try
                    {
                        var health = JsonConvert.DeserializeObject<AiHealth>(uwr.downloadHandler.text);
                        ok = health != null && health.status == "ok";
                    }
                    catch (Exception)
                    {
                        ok = false;
                    }
                }
                onResult?.Invoke(ok);
            }
        }

        /// <summary>标记服务已停止（宿主关闭进程后调用, 使后续 EnsureReadyAsync 重新探测）</summary>
        public void MarkStopped()
        {
            _ready = false;
        }

        /// <summary>拉取 EXE 最近日志(排障用, 需鉴权)。成功时回调日志文本, 失败回调错误描述。</summary>
        public IEnumerator FetchLogsAsync(int tail, Action<string> onResult)
        {
            using (var uwr = UnityWebRequest.Get(BaseUrl + "/v1/logs?tail=" + tail))
            {
                uwr.timeout = 5;
                ApplyAuthHeaders(uwr);
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var logs = JsonConvert.DeserializeObject<AiLogs>(uwr.downloadHandler.text);
                        onResult?.Invoke(logs != null && logs.lines != null ? string.Join("\n", logs.lines) : "");
                        yield break;
                    }
                    catch (Exception e)
                    {
                        onResult?.Invoke("日志解析失败: " + e.Message);
                        yield break;
                    }
                }
                onResult?.Invoke("拉取日志失败(" + uwr.responseCode + "): " + HttpErrorText(uwr));
            }
        }

        // ───────────────────────── 对话 ─────────────────────────

        /// <summary>
        /// 发送对话请求(统一协议)。stream=true 走 SSE, 逐段回调 onDelta;
        /// 结束后回调 onDone(AiChatResult 已累积 content/reasoningContent)。
        /// </summary>
        public IEnumerator ChatAsync(AiChatRequest request, Action<AiChatChunk> onDelta,
            Action<AiChatResult> onDone)
        {
            AiChatResult result = new AiChatResult();

            if (request.stream)
                yield return ChatStreamAsync(request, onDelta, result);
            else
                yield return ChatOnceAsync(request, result);

            result.success = result.error.Length == 0;
            onDone?.Invoke(result);
        }

        IEnumerator ChatOnceAsync(AiChatRequest request, AiChatResult result)
        {
            using (var uwr = new UnityWebRequest(BaseUrl + "/v1/chat/completions", UnityWebRequest.kHttpVerbPOST))
            {
                uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request, _jsonSettings)));
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = REQUEST_TIMEOUT;
                uwr.SetRequestHeader("Content-Type", "application/json");
                ApplyAuthHeaders(uwr);

                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<AiChatResponse>(uwr.downloadHandler.text);
                        if (resp != null && resp.choices.Length > 0)
                        {
                            result.content = resp.choices[0].message.content ?? "";
                            result.reasoningContent = resp.choices[0].message.reasoningContent ?? "";
                            result.model = resp.model;
                            result.provider = resp.provider;
                            result.usage = resp.usage;
                        }
                        else
                        {
                            result.error = "响应缺少 choices: " + uwr.downloadHandler.text;
                        }
                    }
                    catch (Exception e)
                    {
                        result.error = "响应解析失败: " + e.Message;
                    }
                }
                else
                {
                    result.error = HttpErrorText(uwr);
                }
            }
        }

        IEnumerator ChatStreamAsync(AiChatRequest request, Action<AiChatChunk> onDelta, AiChatResult result)
        {
            var sse = new AiSseDownloadHandler();
            using (var uwr = new UnityWebRequest(BaseUrl + "/v1/chat/completions", UnityWebRequest.kHttpVerbPOST))
            {
                uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request, _jsonSettings)));
                uwr.downloadHandler = sse;
                uwr.timeout = REQUEST_TIMEOUT;
                uwr.SetRequestHeader("Content-Type", "application/json");
                ApplyAuthHeaders(uwr);

                var op = uwr.SendWebRequest();
                while (!op.isDone)
                {
                    DrainSse(sse, onDelta, result);
                    yield return null;
                }
                DrainSse(sse, onDelta, result);

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    result.error = HttpErrorText(uwr);
                }
                else if (result.content.Length == 0 && result.reasoningContent.Length == 0)
                {
                    // 空回复也视为完成(可能被截断或上游返回空)
                    result.error = "";
                }
            }
        }

        void DrainSse(AiSseDownloadHandler sse, Action<AiChatChunk> onDelta, AiChatResult result)
        {
            string json;
            while (sse.TryDequeue(out json))
            {
                if (json == "[DONE]")
                    continue;
                try
                {
                    var chunk = JsonConvert.DeserializeObject<AiChatChunk>(json);
                    if (chunk == null)
                        continue;
                    if (chunk.HasContent)
                        result.content += chunk.choices[0].delta.content;
                    if (chunk.HasReasoning)
                        result.reasoningContent += chunk.choices[0].delta.reasoningContent;
                    if (!string.IsNullOrEmpty(chunk.model))
                        result.model = chunk.model;
                    if (!string.IsNullOrEmpty(chunk.provider))
                        result.provider = chunk.provider;
                    if (chunk.usage != null)
                        result.usage = chunk.usage;
                    onDelta?.Invoke(chunk);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AiServerClient] SSE 分片解析失败: {e.Message}");
                }
            }
        }

        static string HttpErrorText(UnityWebRequest uwr)
        {
            if (uwr.responseCode == 401)
            {
                return "鉴权失败(401): 请检查 AiServerClient.AuthName/AuthToken 与 .env CLIENT_WHITELIST 是否一致, 并重新打包 EXE";
            }
            if (uwr.downloadHandler != null && uwr.downloadHandler.data != null
                && uwr.downloadHandler.data.Length > 0)
            {
                string body = uwr.downloadHandler.text;
                try
                {
                    var err = JsonConvert.DeserializeObject<AiErrorBody>(body);
                    if (err != null && err.error != null && !string.IsNullOrEmpty(err.error.message))
                        return err.error.message;
                }
                catch (Exception)
                {
                    // 忽略, 回退原始错误
                }
                return body.Length > 300 ? body.Substring(0, 300) : body;
            }
            return uwr.error;
        }

        // ───────────────────────── DTO ─────────────────────────

        class AiHealth
        {
            [JsonProperty("status")] public string status = "";
            [JsonProperty("version")] public string version = "";
            [JsonProperty("pid")] public int pid;
        }

        class AiErrorBody
        {
            [JsonProperty("error")] public AiErrorDetail error = null;

            public class AiErrorDetail
            {
                [JsonProperty("message")] public string message = "";
                [JsonProperty("type")] public string type = "";
            }
        }

        class AiLogs
        {
            [JsonProperty("lines")] public string[] lines = new string[0];
            [JsonProperty("log_path")] public string logPath = "";
        }
    }
}
