using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace MCV_Module.GlobalManager.CLI
{
    /// <summary>
    /// CLI 进程管理器 — 负责 mcp.exe 子进程的启动、监控和停止。
    /// </summary>
    public class CLIProcessManager
    {
        private Process _cliProcess;

        /// <summary>进程是否正在运行</summary>
        public bool IsRunning => _cliProcess != null && !_cliProcess.HasExited;

        /// <summary>进程退出时触发</summary>
        public event Action OnProcessExited;

        /// <summary>日志回调（接收方负责线程安全）</summary>
        public event Action<string> OnLog;
        public event Action<string> OnLogWarning;
        public event Action<string> OnLogError;

        /// <summary>
        /// 启动 mcp.exe 子进程。
        /// </summary>
        /// <param name="mcpExePath">mcp.exe 的完整路径</param>
        public void Start(string mcpExePath)
        {
            if (!File.Exists(mcpExePath))
            {
                OnLogWarning?.Invoke($"[CLIProcessManager] mcp.exe not found at {mcpExePath}");
                return;
            }

            try
            {
                _cliProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = mcpExePath,
                        WorkingDirectory = Path.GetDirectoryName(mcpExePath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    },
                    EnableRaisingEvents = true,
                };

                _cliProcess.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        OnLog?.Invoke($"[CLI stdout] {e.Data}");
                };
                _cliProcess.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        OnLogWarning?.Invoke($"[CLI stderr] {e.Data}");
                };
                _cliProcess.Exited += (_, _) =>
                {
                    OnLogWarning?.Invoke($"[CLIProcessManager] CLI process exited");
                    _cliProcess = null;
                    OnProcessExited?.Invoke();
                };

                _cliProcess.Start();
                _cliProcess.BeginOutputReadLine();
                _cliProcess.BeginErrorReadLine();

                OnLog?.Invoke($"[CLIProcessManager] CLI process started (PID: {_cliProcess.Id})");
            }
            catch (Exception e)
            {
                OnLogError?.Invoke($"[CLIProcessManager] Failed to start CLI process: {e.Message}");
            }
        }

        /// <summary>
        /// 停止 CLI 子进程。
        /// </summary>
        public void Stop()
        {
            if (_cliProcess != null && !_cliProcess.HasExited)
            {
                try
                {
                    _cliProcess.Kill();
                    _cliProcess.WaitForExit(5000);
                }
                catch (Exception e)
                {
                    OnLogWarning?.Invoke($"[CLIProcessManager] Error stopping CLI: {e.Message}");
                }
                _cliProcess.Close();
                _cliProcess = null;
            }
        }
    }
}
