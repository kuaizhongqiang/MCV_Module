using System;
using MCV_Module.Utils;
using MCV_Module.Net;
using UnityEngine;

namespace MCV_Module.Managers
{
    /// <summary>
    /// AiServer EXE 的宿主进程管理（留源码，不进 DLL）。
    ///
    /// ⚠️ 本文件包含 #if !UNITY_WEBGL 平台宏，预编译 DLL 中宏不生效，
    ///    因此必须由 Unity 源码侧编译（留在 MCV.Runtime），不能编入 MCV.AiClient.dll。
    ///
    /// 职责:
    ///   - 拉起 StreamingAssets/AiServer/AiServer.exe（--host --port --parent-pid）
    ///   - 退出时优雅关闭 POST /v1/shutdown + 兜底 Kill
    ///   - WebGL 下全部为 no-op（无法拉起本地进程, 仅探测远程服务）
    ///
    /// 纯协议（鉴权/对话/日志）在 AiServerClient（MCV.AiClient.dll）。
    /// </summary>
    public class AiServerProcess
    {
        readonly AiServerClient _client;
#if !UNITY_WEBGL
        System.Diagnostics.Process _process;
#endif

        public AiServerProcess(AiServerClient client)
        {
            _client = client;
        }

        /// <summary>EXE 完整路径(StreamingAssets/AiServer/AiServer.exe)</summary>
        public static string ExePath
        {
            get { return Application.streamingAssetsPath + "/AiServer/AiServer.exe"; }
        }

        /// <summary>
        /// 拉起 EXE（由 AiServerClient.EnsureReadyAsync 在健康检查未通过时回调；内部保证只拉一次）。
        /// WebGL 下为空操作。
        /// </summary>
        public void TryLaunch()
        {
#if !UNITY_WEBGL
            if (_process != null && !_process.HasExited) return;

            string exe = ExePath;
            if (!System.IO.File.Exists(exe))
            {
                Log.Error($"[AiServerProcess] 未找到 AiServer EXE: {exe} (请先运行 unity-ai-server/build.bat 打包)");
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"--host {_client.Host} --port {_client.Port} --parent-pid {System.Diagnostics.Process.GetCurrentProcess().Id}",
                };
                _process = System.Diagnostics.Process.Start(psi);
                Log.Info($"[AiServerProcess] 已拉起 AiServer (port {_client.Port})");
            }
            catch (Exception e)
            {
                Log.Error($"[AiServerProcess] 启动 EXE 失败: {e.Message}");
            }
#endif
        }

        /// <summary>兜底强杀进程(WebGL 下为空操作)</summary>
        public void Kill()
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
                Log.Warning($"[AiServerProcess] 关闭进程失败: {e.Message}");
            }
            finally
            {
                _process = null;
            }
#endif
        }

        /// <summary>
        /// 同步关闭(OnApplicationQuit 时调用, 协程在退出时不会继续跑):
        /// 优雅 POST /v1/shutdown（带鉴权头）+ 兜底 Kill，并把客户端标记为未就绪。
        /// WebGL 下仅标记未就绪。
        /// </summary>
        public void ShutdownNow()
        {
#if !UNITY_WEBGL
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(_client.BaseUrl + "/v1/shutdown");
                req.Method = "POST";
                req.Timeout = 1500;
                req.Headers["X-Auth-Name"] = AiServerClient.AuthName;
                req.Headers["X-Auth-Token"] = AiServerClient.AuthToken;
                using (var resp = (System.Net.HttpWebResponse)req.GetResponse()) { }
            }
            catch (Exception)
            {
                // 服务可能已不在, 忽略
            }
            Kill();
#endif
            _client.MarkStopped();
        }
    }
}
