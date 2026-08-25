using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MCV_Module.Utils
{
    /// <summary>日志级别。</summary>
    public enum LogLevel
    {
        Info,
        Success,
        Warning,
        Error,
        Verbose,
    }

    /// <summary>一条日志记录（供屏幕浮层读取）。</summary>
    public struct LogEntry
    {
        public LogLevel Level;
        public string LevelTag;   // GUI 中显示的级别短标签，如 "INFO"
        public string Time;       // "HH:mm:ss.fff"
        public string Tag;        // 模块标记，可为空
        public string Message;
    }

    /// <summary>
    /// 统一的打印系统 —— 在 Debug.Log 之上提供更丰富、可开关、可屏幕可视化的日志。
    ///
    /// Console 方向：
    ///   - 级别着色（Info/Success/Warning/Error/Verbose），富文本 <color> 便于在 Console 中区分；
    ///   - 可选时间戳前缀、模块 tag；
    ///   - Verbose 仍可整体开关（VerboseEnabled），默认行为与 Debug.Log 一致。
    ///
    /// GUI 方向（屏幕调试）：
    ///   - GuiEnabled = true 时自动挂载 LogOverlay 到 DontDestroyOnLoad 对象（零配置）；
    ///   - 上下文面板展示当前 SceneState / TaskType / 场景 / FPS 等信息；
    ///   - 日志流面板展示最近 MaxHistory 条日志。
    ///
    /// 错误/警告始终输出，不受 VerboseEnabled 影响。
    /// </summary>
    public static class Log
    {
        #region 开关与配置
        /// <summary>是否输出详细日志（Verbose）。默认为 true；发布前可置 false 以省字符串分配。</summary>
        public static bool VerboseEnabled { get; set; } = true;

        /// <summary>是否在 Console 输出时间戳前缀。默认 true。</summary>
        public static bool TimestampEnabled { get; set; } = true;

        /// <summary>是否启用 Console 富文本颜色。默认 true。</summary>
        public static bool RichTextEnabled { get; set; } = true;

        /// <summary>是否启用屏幕调试浮层（GUI）。默认 Editor 下自动开启，发布版关闭。</summary>
        public static bool GuiEnabled
        {
            get => m_GuiEnabled;
            set { m_GuiEnabled = value; ApplyGui(); }
        }

        /// <summary>GUI 日志流保留的最大条数。</summary>
        public static int MaxHistory { get; set; } = 200;

        /// <summary>日志历史版本号（每次写入 +1），供 GUI 增量重建。</summary>
        public static int HistoryVersion { get; private set; }

        static bool m_GuiEnabled;
        static LogOverlay m_Overlay;
        static readonly List<LogEntry> m_History = new List<LogEntry>(128);

        static Log()
        {
#if UNITY_EDITOR
            // Editor 下默认开启屏幕调试，便于开发期观察；发布版默认关闭。
            m_GuiEnabled = true;
#endif
        }
        #endregion

        #region 详细日志（可开关，向后兼容）
        /// <summary>详细日志（可开关）。行为与 Debug.Log 一致。</summary>
        public static void Verbose(object message)
        {
            if (!VerboseEnabled) return;
            Write(LogLevel.Verbose, null, message, null);
        }

        /// <summary>详细格式化日志（可开关）。</summary>
        public static void VerboseFormat(string format, params object[] args)
        {
            if (!VerboseEnabled) return;
            Write(LogLevel.Verbose, null, string.Format(format, args), null);
        }

        /// <summary>带模块标记的详细日志（可开关）。便于按模块过滤。</summary>
        /// <param name="tag">模块标记，如 "AddrMgr" / "StepMgr"。</param>
        public static void Tagged(string tag, object message)
        {
            if (!VerboseEnabled) return;
            Write(LogLevel.Verbose, tag, message, null);
        }
        #endregion

        #region 级别化日志
        public static void Info(object message) => Write(LogLevel.Info, null, message, null);
        public static void Info(object message, UnityEngine.Object context) => Write(LogLevel.Info, null, message, context);
        public static void InfoFormat(string format, params object[] args) => Write(LogLevel.Info, null, string.Format(format, args), null);

        public static void Success(object message) => Write(LogLevel.Success, null, message, null);
        public static void SuccessFormat(string format, params object[] args) => Write(LogLevel.Success, null, string.Format(format, args), null);

        public static void Warning(object message) => Write(LogLevel.Warning, null, message, null);
        public static void Warning(object message, UnityEngine.Object context) => Write(LogLevel.Warning, null, message, context);
        public static void WarningFormat(string format, params object[] args) => Write(LogLevel.Warning, null, string.Format(format, args), null);

        public static void Error(object message) => Write(LogLevel.Error, null, message, null);
        public static void Error(object message, UnityEngine.Object context) => Write(LogLevel.Error, null, message, context);
        public static void ErrorFormat(string format, params object[] args) => Write(LogLevel.Error, null, string.Format(format, args), null);

        /// <summary>带模块标记 + 级别的通用日志入口。</summary>
        /// <param name="tag">模块标记（可为 null）。</param>
        /// <param name="level">日志级别。</param>
        /// <param name="message">日志内容。</param>
        public static void Tag(string tag, LogLevel level, object message)
        {
            Write(level, tag, message, null);
        }
        #endregion

        #region 屏幕浮层
        /// <summary>手动启用屏幕调试浮层（Editor 下默认自动开启）。</summary>
        public static void EnableGui()
        {
            m_GuiEnabled = true;
            ApplyGui();
        }

        /// <summary>手动禁用屏幕调试浮层。</summary>
        public static void DisableGui()
        {
            m_GuiEnabled = false;
            ApplyGui();
        }

        public static IReadOnlyList<LogEntry> GetHistory() => m_History;

        public static void ClearHistory()
        {
            m_History.Clear();
            HistoryVersion++;
        }

        static void ApplyGui()
        {
            if (m_GuiEnabled)
            {
                EnsureGuiCreated();
                m_Overlay.gameObject.SetActive(true);
            }
            else if (m_Overlay != null)
            {
                m_Overlay.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 首次写日志时懒创建浮层宿主（挂到 DontDestroyOnLoad 对象）。
        /// 这样即使只通过静态构造函数把 GuiEnabled 置 true（未走 setter），
        /// 第一次打印日志也能把浮层挂载起来，无需额外初始化调用。
        /// </summary>
        static void EnsureGuiCreated()
        {
            if (m_Overlay != null) return;
            // 仅在真实运行时（Play 模式）创建浮层：
            // EditMode 测试等非 Play 环境下不挂 DontDestroyOnLoad 对象，避免测试环境副作用；
            // 屏幕浮层本身也只在运行时才有意义。
            if (!Application.isPlaying) return;
            var go = new GameObject("[LogOverlay]");
            Object.DontDestroyOnLoad(go);
            m_Overlay = go.AddComponent<LogOverlay>();
            m_Overlay.gameObject.SetActive(m_GuiEnabled);
        }
        #endregion

        #region 内部实现
        static void Write(LogLevel level, string tag, object message, UnityEngine.Object context)
        {
            // 屏幕浮层懒创建（首次日志时挂载）
            if (m_GuiEnabled) EnsureGuiCreated();

            string text = message != null ? message.ToString() : "null";
            var now = System.DateTime.Now;

            // 1) 写入历史（GUI 读取）
            var entry = new LogEntry
            {
                Level = level,
                LevelTag = LevelTag(level),
                Time = now.ToString("HH:mm:ss.fff"),
                Tag = tag,
                Message = text,
            };
            if (m_History.Count >= MaxHistory) m_History.RemoveAt(0);
            m_History.Add(entry);
            HistoryVersion++;

            // 2) 组装 Console 输出
            var sb = new StringBuilder(64);
            if (TimestampEnabled) { sb.Append('[').Append(entry.Time).Append("] "); }
            if (!string.IsNullOrEmpty(tag)) { sb.Append('(').Append(tag).Append(") "); }

            // 级别标签：富文本颜色 or 纯文本
            if (RichTextEnabled)
            {
                sb.Append("<color=").Append(LevelColorHex(level)).Append(">")
                  .Append('[').Append(entry.LevelTag).Append(']')
                  .Append("</color>");
            }
            else
            {
                sb.Append('[').Append(entry.LevelTag).Append(']');
            }
            sb.Append(' ').Append(text);

            string final = sb.ToString();

            // 3) 输出到 Console（Error/Warning 始终可见），context 透传便于 Console 点击定位
            switch (level)
            {
                case LogLevel.Warning: Debug.LogWarning(final, context); break;
                case LogLevel.Error: Debug.LogError(final, context); break;
                default: Debug.Log(final, context); break;
            }
        }

        static string LevelTag(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info: return "INFO";
                case LogLevel.Success: return "OK";
                case LogLevel.Warning: return "WARN";
                case LogLevel.Error: return "ERROR";
                case LogLevel.Verbose: return "DBG";
                default: return "INFO";
            }
        }

        static string LevelColorHex(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Info: return "#9cdcfe";      // 淡蓝
                case LogLevel.Success: return "#6fdd8b";   // 绿
                case LogLevel.Warning: return "#ffd479";   // 黄
                case LogLevel.Error: return "#ff6b6b";     // 红
                case LogLevel.Verbose: return "#b0b0b0";   // 灰
                default: return "#ffffff";
            }
        }
        #endregion
    }
}
