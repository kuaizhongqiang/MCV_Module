using System.Text;
using MCV_Module.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCV_Module.Utils
{
    /// <summary>
    /// 屏幕调试浮层（OnGUI 实现，零依赖）。
    ///
    /// 由 Log 静态类在首次启用屏幕调试时自动创建并挂到 DontDestroyOnLoad 对象上，
    /// 无需修改任何场景/预制体。提供两块可折叠面板：
    ///   - 系统上下文：当前 SceneState / TaskType / 场景名 / FPS / 屏幕 / 时间
    ///   - 日志流：最近 N 条日志（带级别着色），可开关跟随窗口拖动
    ///
    /// 运行时快捷键：
    ///   F1 开关上下文面板；F2 开关日志流；F3 清空日志；F4/F5 增大/减小字号。
    /// </summary>
    [DisallowMultipleComponent]
    public class LogOverlay : MonoBehaviour
    {
        #region 字段
        // ── 窗口状态 ──────────────────────────────
        Rect m_ContextRect = new Rect(12, 12, 320, 450);
        Rect m_LogRect = new Rect(350, 12, 480, 280);
        bool m_ShowContext = true;
        bool m_ShowLog = true;

        // ── 外观 ──────────────────────────────────
        GUIStyle m_HeaderStyle;
        GUIStyle m_LabelStyle;
        GUIStyle[] m_LevelStyles;   // 按 LogLevel 索引的级别着色样式
        int m_FontSize = 12;
        float m_Opacity = 0.9f;
        bool m_StylesReady;   // GUI 样式是否已构建（仅在 OnGUI 内构建）

        // ── 缓存 ──────────────────────────────────
        float m_Fps = 0f;
        #endregion

        #region 生命周期
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // 平滑 FPS
            m_Fps = Mathf.Lerp(m_Fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.1f);

            // 快捷键
            if (Input.GetKeyDown(KeyCode.F1)) m_ShowContext = !m_ShowContext;
            if (Input.GetKeyDown(KeyCode.F2)) m_ShowLog = !m_ShowLog;
            if (Input.GetKeyDown(KeyCode.F3)) Log.ClearHistory();
            if (Input.GetKeyDown(KeyCode.F4)) m_FontSize = Mathf.Min(32, m_FontSize + 1);
            if (Input.GetKeyDown(KeyCode.F5)) m_FontSize = Mathf.Max(8, m_FontSize - 1);
        }

        void OnGUI()
        {
            // GUI 样式只能在 OnGUI 内创建，首次进入时懒构建一次
            if (!m_StylesReady) BuildStyles();

            GUI.color = new Color(1f, 1f, 1f, m_Opacity);

            if (m_ShowContext)
                m_ContextRect = GUI.Window(9001, m_ContextRect, DrawContextWindow, "Debug 上下文");
            if (m_ShowLog)
                m_LogRect = GUI.Window(9002, m_LogRect, DrawLogWindow, "日志流");

            GUI.color = Color.white;
        }
        #endregion

        #region 窗口绘制
        void DrawContextWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, m_ContextRect.width, 20));
            m_LabelStyle.fontSize = m_FontSize;

            GUILayout.Space(20);
            GUILayout.Label("State   : " + GetCurrentStateName(), m_LabelStyle);
            GUILayout.Label("TaskType: " + GetCurrentTaskName(), m_LabelStyle);
            GUILayout.Label("Scene   : " + SceneManager.GetActiveScene().name, m_LabelStyle);
            GUILayout.Label(string.Format("FPS     : {0:F0} ({1:F1}ms)", m_Fps, 1000f / Mathf.Max(0.0001f, m_Fps)), m_LabelStyle);
            GUILayout.Label("Size    : " + Screen.width + "x" + Screen.height, m_LabelStyle);
            GUILayout.Label("Time    : " + Time.time.ToString("F1") + "s", m_LabelStyle);

            GUILayout.Space(8);
            GUILayout.Label("—— 快捷键 ——", m_HeaderStyle);
            GUILayout.Label("F1  开关上下文面板", m_LabelStyle);
            GUILayout.Label("F2  开关日志流面板", m_LabelStyle);
            GUILayout.Label("F3  清空日志", m_LabelStyle);
            GUILayout.Label("F4 / F5  增大 / 减小字号", m_LabelStyle);
        }

        void DrawLogWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, m_LogRect.width, 20));
            m_LabelStyle.fontSize = m_FontSize;

            GUILayout.Space(20);
            GUILayout.BeginVertical();
            // 倒序绘制：最新的日志显示在最上方，旧的向下溢出被裁剪
            var entries = Log.GetHistory();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                GUILayout.Label(FormatEntry(e), StyleForLevel(e.Level));
            }
            GUILayout.EndVertical();
        }

        /// <summary>格式化一条日志为可读文本。</summary>
        static string FormatEntry(LogEntry e)
        {
            var sb = new StringBuilder(64);
            sb.Append('[');
            sb.Append(e.LevelTag);
            sb.Append("] ");
            sb.Append(e.Time);
            if (!string.IsNullOrEmpty(e.Tag))
            {
                sb.Append(' ');
                sb.Append('(');
                sb.Append(e.Tag);
                sb.Append(')');
            }
            sb.Append(' ');
            sb.Append(e.Message);
            return sb.ToString();
        }

        /// <summary>按日志级别取对应着色样式（越界安全回退到普通样式）。</summary>
        GUIStyle StyleForLevel(LogLevel level)
        {
            int idx = (int)level;
            if (m_LevelStyles != null && idx >= 0 && idx < m_LevelStyles.Length && m_LevelStyles[idx] != null)
                return m_LevelStyles[idx];
            return m_LabelStyle;
        }

        /// <summary>安全读取当前导航状态（UI 管理器未就绪时返回 "未知"）。</summary>
        string GetCurrentStateName()
        {
            if (!GlobalUIMgr.Exists || GlobalUIMgr.Instance == null) return "未知";
            return GlobalUIMgr.GetCurrentState().ToString();
        }

        /// <summary>安全读取当前任务类型（UI 管理器未就绪时返回 "未知"）。</summary>
        string GetCurrentTaskName()
        {
            if (!GlobalUIMgr.Exists || GlobalUIMgr.Instance == null) return "未知";
            return GlobalUIMgr.GetCurrentTaskType().ToString();
        }
        #endregion

        #region 样式
        void BuildStyles()
        {
            m_HeaderStyle = new GUIStyle(GUI.skin.label);
            m_HeaderStyle.fontStyle = FontStyle.Bold;
            m_HeaderStyle.normal.textColor = new Color(0.5f, 0.9f, 1f);

            m_LabelStyle = new GUIStyle(GUI.skin.label);
            m_LabelStyle.fontSize = m_FontSize;
            m_LabelStyle.wordWrap = true;

            // 各级别着色样式（与 Log 的 Console 配色保持一致）
            m_LevelStyles = new GUIStyle[5];
            m_LevelStyles[(int)LogLevel.Info] = MakeLevelStyle(Color.white);
            m_LevelStyles[(int)LogLevel.Success] = MakeLevelStyle(new Color(0.44f, 0.87f, 0.55f)); // 绿
            m_LevelStyles[(int)LogLevel.Warning] = MakeLevelStyle(new Color(1f, 0.83f, 0.47f));   // 黄
            m_LevelStyles[(int)LogLevel.Error] = MakeLevelStyle(new Color(1f, 0.42f, 0.42f));     // 红
            m_LevelStyles[(int)LogLevel.Verbose] = MakeLevelStyle(new Color(0.69f, 0.69f, 0.69f)); // 灰

            m_StylesReady = true;
        }

        GUIStyle MakeLevelStyle(Color color)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = m_FontSize;
            s.wordWrap = true;
            s.normal.textColor = color;
            return s;
        }
        #endregion
    }
}
