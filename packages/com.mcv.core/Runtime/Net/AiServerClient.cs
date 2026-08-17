using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace MCV_Module.Net
{
    /// <summary>
    /// AiServer(单 EXE) 的 Unity 客户端 —— 由 GlobalAiMgr 控制。
    ///
    /// 职责:
    ///   - 拉起 StreamingAssets/AiServer/AiServer.exe 并轮询 /health 直到就绪
    ///     (Standalone/Editor; WebGL 只能连远程, 不做拉起)
    ///   - 统一协议对话: POST /v1/chat/completions (流式 SSE / 非流式)
    ///   - 退出时优雅关闭 /v1/shutdown + 兜底 Kill
    ///
    /// 注意: py 交付物只有 AiServer.exe 一个文件, 密钥/端口/模型全部内嵌在 EXE 里,
    ///   Unity 侧不需要也不应该读取任何配置文件; 端口固定 8765(可由 --port 参数覆盖)。
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

        /// <summary>
        /// 客户端鉴权名称 —— 必须与 EXE 内嵌白名单(.env CLIENT_WHITELIST)中一组一致。
        /// 鉴权方式: 每个请求携带 X-Auth-Name + X-Auth-Token 头。
        /// </summary>
        public const string AuthName = "asdf";

        /// <summary>客户端鉴权令牌 —— 必须与 EXE 内嵌白名单(.env CLIENT_WHITELIST)中一组一致</summary>
        public const string AuthToken = "asdfghjkl";

        bool _launching;
        bool _ready;
#if !UNITY_WEBGL
        System.Diagnostics.Process _process;
#endif

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

        /// <summary>EXE 完整路径(StreamingAssets/AiServer/AiServer.exe)</summary>
        public static string ExePath
        {
            get { return Application.streamingAssetsPath + "/AiServer/AiServer.exe"; }
        }

        // ───────────────────────── 生命周期 ─────────────────────────

        /// <summary>
        /// 确保服务就绪: 已运行则直接通过; 否则拉起 EXE 并轮询 /health。
        /// 可被多处调用, 内部保证只拉起一次。WebGL 下不做拉起, 仅探测远程服务。
        /// </summary>
        public IEnumerator EnsureReadyAsync(Action<bool> onReady, float timeoutSeconds = 15f)
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
#if !UNITY_WEBGL
                    LaunchProcess();
#else
                    // WebGL 无法拉起本地进程, 仅等待远程服务
#endif
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

#if !UNITY_WEBGL
        void LaunchProcess()
        {
            string exe = ExePath;
            if (!System.IO.File.Exists(exe))
            {
                Debug.LogError($"[AiServerClient] 未找到 AiServer EXE: {exe} (请先运行 unity-ai-server/build.bat 打包)");
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"--host {Host} --port {Port} --parent-pid {System.Diagnostics.Process.GetCurrentProcess().Id}",
                };
                _process = System.Diagnostics.Process.Start(psi);
                Debug.Log($"[AiServerClient] 已拉起 AiServer (port {Port})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AiServerClient] 启动 EXE 失败: {e.Message}");
            }
        }
#endif

        /// <summary>同步关闭(OnApplicationQuit 时调用, 协程在退出时不会继续跑)</summary>
        public void ShutdownNow()
        {
#if !UNITY_WEBGL
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(BaseUrl + "/v1/shutdown");
                req.Method = "POST";
                req.Timeout = 1500;
                req.Headers["X-Auth-Name"] = AuthName;
                req.Headers["X-Auth-Token"] = AuthToken;
                using (var resp = (System.Net.HttpWebResponse)req.GetResponse()) { }
            }
            catch (Exception)
            {
                // 服务可能已不在, 忽略
            }
            KillProcess();
#endif
            _ready = false;
        }

        /// <summary>兜底强杀进程(WebGL 下为空操作)</summary>
        public void KillProcess()
        {
#if !UNITY_WEBGL
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit(2000);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AiServerClient] 关闭进程失败: {e.Message}");
            }
            finally
            {
                _process = null;
            }
#endif
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
