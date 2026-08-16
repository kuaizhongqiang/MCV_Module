using UnityEngine;

namespace MCV_Module.Utils
{
    /// <summary>
    /// 日志工具 —— 提供可开关的详细日志（Verbose）。
    ///
    /// 用于替换运行路径上的高频/过程性 Debug.Log（场景加载、资源加载等）：
    ///   - 默认开启，行为与 Debug.Log 一致；
    ///   - 发布版可在启动早期设置 VerboseEnabled = false（或按需加宏裁剪），
    ///     避免无谓的字符串插值分配与日志输出。
    /// 错误（Debug.LogError）与警告（Debug.LogWarning）不经过此开关，保持始终可见。
    /// </summary>
    public static class Log
    {
        /// <summary>是否输出详细日志。默认为 true；发布前可置为 false。</summary>
        public static bool VerboseEnabled { get; set; } = true;

        /// <summary>详细日志（可开关）。</summary>
        public static void Verbose(object message)
        {
            if (VerboseEnabled) Debug.Log(message);
        }

        /// <summary>详细格式化日志（可开关）。</summary>
        public static void VerboseFormat(string format, params object[] args)
        {
            if (VerboseEnabled) Debug.LogFormat(format, args);
        }
    }
}
