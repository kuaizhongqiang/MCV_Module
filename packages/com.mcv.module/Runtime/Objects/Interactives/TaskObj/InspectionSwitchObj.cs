using System;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 本组件必须与拾取用 Collider 在同一个 GameObject —— GlobalInteractiveMgr 射线命中的物体上取不到本组件，按下去等于点到空白。
    /// <summary>检测页里「元器件上可点击的部件」——接触器的试验按钮、断路器的操作手柄这类本体之外、学生要动手操作的部位：沿某个局部轴在「抬起位 ↔ 按下位」之间移动。</summary>
    public class InspectionSwitchObj : InteractiveBase
    {
        #region 序列化参数
        [Header("移动部件")]
        [SerializeField, Tooltip("真正移动的部件（按钮 / 手柄模型）；留空 = 移动本物体")]
        Transform moveObj;

        [SerializeField, Tooltip("移动轴（部件的**局部**空间）：按钮一般用 Z（模型正面法线方向）")]
        ObjAxis moveAxis = ObjAxis.Z;

        [SerializeField, Tooltip("抬起位：该轴上的局部坐标（松开时停这里；按住手势的初始位）")]
        float upLimit;

        [SerializeField, Tooltip("按下位：该轴上的局部坐标（按下后停这里）")]
        float downLimit = 1f;

        [SerializeField, Tooltip("移动速度（局部单位/秒）；<= 0 = 立即到位，不做过渡")]
        float moveSpeed = 0.5f;

        [Header("交互")]
        [SerializeField, Tooltip("手势：按住（松手弹回）/ 点击切换（自锁）/ 拖拽跟手")]
        SwitchGesture gesture = SwitchGesture.Press;

        [SerializeField, Tooltip("进场景时是否已经是按下态（真按钮一般 false）")]
        bool startPressed;

        [SerializeField, Tooltip("拖拽跟手的像素死区：鼠标压在起点附近时不动，避免抖动")]
        float dragDeadZone = 3f;
        #endregion

        #region 运行时状态
        /// <summary>实际移动的层（Awake 解析：配了 moveObj 用它，否则用自身）</summary>
        Transform m_Visual;
        /// <summary>已应用的位置（<see cref="moveAxis"/> 上的局部坐标）</summary>
        float m_Applied;
        /// <summary>目标位置（朝它过渡）</summary>
        float m_Target;
        /// <summary>是否处于按下态</summary>
        bool m_Pressed;

        /// <summary>是否正在拖拽（仅 <see cref="SwitchGesture.Drag"/> 用）</summary>
        bool m_Dragging;
        /// <summary>按下时的局部坐标（拖拽起点）</summary>
        float m_DragStartPos;
        /// <summary>按下时的鼠标屏幕坐标（拖拽起点）</summary>
        Vector2 m_DragStartMouse;
        /// <summary>移动轴在屏幕上的单位方向（拖拽期间固定：相机与部件都不动，缓存是安全的）</summary>
        Vector2 m_DragScreenDir;
        /// <summary>屏幕 1 像素对应多少局部单位（按下时标定一次）</summary>
        float m_DragUnitsPerPixel;
        #endregion

        #region 对外属性
        /// <summary>是否处于按下态（true = 部件停在 <see cref="downLimit"/>）。</summary>
        public bool IsPressed => m_Pressed;

        /// <summary>实际移动的层（配了 moveObj 用它，否则用自身）—— 编辑器标定工具也读它。</summary>
        public Transform Visual => moveObj != null ? moveObj : transform;

        /// <summary>状态翻转回调（参数 = 新的按下态）。松开 / 按下各抛一次，不会逐帧重复。</summary>
        public event Action<bool> PressedChanged;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            m_Visual = Visual;

            if (m_Visual == null)
            {
                Log.Warning($"{name}: moveObj 与自身都取不到，部件不会移动");
                return;
            }

            if (Mathf.Approximately(upLimit, downLimit))
            {
                Log.Warning($"{name}: 抬起位与按下位相同（{upLimit}）—— 部件不会动，检查 upLimit / downLimit");
            }

            // 射线命中的物体上必须同时有 Collider 与本组件，否则点上去等于点到空白
            if (GetComponent<Collider>() == null)
            {
                Log.Warning($"{name}: 本物体上没有 Collider —— 拾取射线命不中，这个部件点不到" +
                            "（Collider 必须与本组件挂在同一个 GameObject 上）");
            }

            m_Pressed = startPressed;
            m_Applied = m_Target = PositionOf(m_Pressed);
            ApplyPosition();
        }

        void Update()
        {
            if (m_Dragging)
            {
                // 拖拽期间位置由鼠标直接驱动（跟手，不做过渡动画）
                UpdateDrag();
                CheckMouseReleased();
                return;
            }

            if (Mathf.Approximately(m_Applied, m_Target)) return;

            m_Applied = moveSpeed > 0f
                ? Mathf.MoveTowards(m_Applied, m_Target, moveSpeed * Time.deltaTime)
                : m_Target;
            ApplyPosition();
        }

        void OnDisable()
        {
            // 停用 / 被销毁时别把"拖拽中"留到下次（那时鼠标可能早松开了）
            m_Dragging = false;
        }
        #endregion

        #region 交互
        protected override void MoDownEvent()
        {
            if (gesture == SwitchGesture.Drag) BeginDrag();
            else if (gesture == SwitchGesture.Press) SetPressed(true);
        }

        protected override void MoUpEvent()
        {
            if (gesture == SwitchGesture.Drag) EndDrag();
            else if (gesture == SwitchGesture.Press) SetPressed(false);
        }

        protected override void MoClickEvent()
        {
            if (gesture == SwitchGesture.Toggle) Toggle();
        }

        /// <summary>只做同帧提前响应；持续跟手由 <see cref="Update"/> 保证（框架 MoMove 仅在射线本帧命中本物体时派发，快速划开会断开）。</summary>
        protected override void MoMoveEvent(Vector2 delta)
        {
            if (m_Dragging) UpdateDrag();
        }
        #endregion

        #region 状态（程序化入口）
        /// <summary>置为按下 / 弹回：状态没变就什么都不做（不重复抛事件、不重复播过渡）。</summary>
        public void SetPressed(bool pressed)
        {
            if (m_Pressed == pressed) return;

            m_Pressed = pressed;
            StartMove(PositionOf(m_Pressed));
            PressedChanged?.Invoke(m_Pressed);

            Log.Info($"{name}: {(m_Pressed ? "按下" : "弹回")}");
        }

        /// <summary>按下 / 弹回翻转一次（自锁开关、步骤脚本用）。</summary>
        public void Toggle() => SetPressed(!m_Pressed);

        /// <summary>立刻到位（不做过渡）：初始化 / 跳转归位 / 自测用；与 <see cref="SetPressed"/> 一样只在状态翻转时抛事件。</summary>
        public void SetPressedImmediate(bool pressed)
        {
            bool changed = m_Pressed != pressed;

            m_Pressed = pressed;
            m_Applied = m_Target = PositionOf(m_Pressed);
            ApplyPosition();

            if (!changed) return;

            PressedChanged?.Invoke(m_Pressed);
            Log.Info($"{name}: {(m_Pressed ? "按下" : "弹回")}（立即到位）");
        }
        #endregion

        #region 位置
        /// <summary>某个状态对应的位置：按下取 <see cref="downLimit"/>，否则取 <see cref="upLimit"/>。</summary>
        float PositionOf(bool pressed) => pressed ? downLimit : upLimit;

        /// <summary>开始朝目标位置移动（速度按 <see cref="moveSpeed"/>；&lt;= 0 表示立即到位）。</summary>
        void StartMove(float target)
        {
            m_Target = target;

            if (moveSpeed > 0f) return;

            m_Applied = m_Target;
            ApplyPosition();
        }

        /// <summary>把已应用位置写到部件上（只动 <see cref="moveAxis"/> 那一个分量）。</summary>
        void ApplyPosition()
        {
            if (m_Visual == null) return;

            Vector3 pos = m_Visual.localPosition;
            switch (moveAxis)
            {
                case ObjAxis.X:
                    pos.x = m_Applied;
                    break;
                case ObjAxis.Y:
                    pos.y = m_Applied;
                    break;
                case ObjAxis.Z:
                    pos.z = m_Applied;
                    break;
            }
            m_Visual.localPosition = pos;
        }

        /// <summary>某轴对应的单位向量（部件局部空间）。</summary>
        static Vector3 AxisVector(ObjAxis axis)
        {
            switch (axis)
            {
                case ObjAxis.X: return Vector3.right;
                case ObjAxis.Y: return Vector3.up;
                default: return Vector3.forward;
            }
        }

        /// <summary>拖拽范围：两端取 <see cref="upLimit"/> / <see cref="downLimit"/> 的较小 / 较大者（配置写反也能用）。</summary>
        void ResolveDragLimits(out float min, out float max)
        {
            min = Mathf.Min(upLimit, downLimit);
            max = Mathf.Max(upLimit, downLimit);
        }

        /// <summary>离给定位置较近的那个状态（拖拽松手吸附 / 途中判定共用一个口径）。</summary>
        bool PressedOfPosition(float position)
        {
            return Mathf.Abs(position - downLimit) < Mathf.Abs(position - upLimit);
        }
        #endregion

        #region 拖拽跟手
        /// <summary>按下开始拖拽：标定"移动轴在屏幕上的方向"与"1 像素 = 多少局部单位"；轴几乎正对相机（屏幕上没有位移分量）时拒绝本次拖拽并告警。</summary>
        void BeginDrag()
        {
            if (m_Visual == null) return;

            var mouse = Mouse.current;
            if (mouse == null)
            {
                Log.Warning($"{name}: 取不到鼠标设备，本次拖拽忽略");
                return;
            }

            var cam = ResolveCamera();
            if (cam == null)
            {
                Log.Warning($"{name}: 取不到相机，本次拖拽忽略");
                return;
            }

            // 移动轴的世界方向（含层级与部件当前姿态）
            Vector3 axisWorld = m_Visual.rotation * AxisVector(moveAxis);
            if (axisWorld.sqrMagnitude < 1e-8f)
            {
                Log.Warning($"{name}: 移动轴为零向量，本次拖拽忽略");
                return;
            }
            axisWorld.Normalize();

            Vector3 origin = m_Visual.position;
            if (!TryProject(cam, origin, out Vector2 screenOrigin)
                || !TryProject(cam, origin + axisWorld, out Vector2 screenAhead))
            {
                Log.Warning($"{name}: 部件在相机背面（投影失败），本次拖拽忽略");
                return;
            }

            Vector2 screenAxis = screenAhead - screenOrigin;
            if (screenAxis.magnitude < 1e-4f)
            {
                Log.Warning($"{name}: 移动轴几乎正对相机（屏幕上没有位移分量），沿该轴拖不动 —— " +
                            "换个视角，或把 moveAxis 配成屏幕上能看到的那个轴");
                return;
            }

            m_Dragging = true;
            m_DragStartPos = m_Applied;
            m_DragStartMouse = mouse.position.ReadValue();
            m_DragScreenDir = screenAxis.normalized;
            m_DragUnitsPerPixel = 1f / screenAxis.magnitude;   // |screenAxis| = 1 个局部单位有多少像素
        }

        /// <summary>逐帧跟手：把"鼠标位移在轴屏幕方向上的投影"换算成局部单位，加到起点位置并夹在两端之间（死区先扣掉再跟手，顶到端点不回弹）。</summary>
        void UpdateDrag()
        {
            if (m_Visual == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.position.ReadValue() - m_DragStartMouse;
            float along = Vector2.Dot(delta, m_DragScreenDir);      // 轴上分量（像素，带符号）
            float dead = Mathf.Abs(dragDeadZone);
            if (Mathf.Abs(along) <= dead) return;                   // 还在死区内：不动，免得抖动

            float moved = (along - Mathf.Sign(along) * dead) * m_DragUnitsPerPixel;

            ResolveDragLimits(out float min, out float max);
            float position = Mathf.Clamp(m_DragStartPos + moved, min, max);

            m_Applied = m_Target = position;   // 跟手：目标一起对齐，免得 Update 的过渡和手指抢
            ApplyPosition();

            // 拖过中点就翻转状态（只在真的翻转时抛事件，不逐帧抛）
            SetPressedFromDrag(PressedOfPosition(position));
        }

        /// <summary>松手：吸附到较近的一端（按 <see cref="moveSpeed"/> 过渡过去）。</summary>
        void EndDrag()
        {
            if (!m_Dragging) return;
            m_Dragging = false;

            bool pressed = PressedOfPosition(m_Applied);
            SetPressedFromDrag(pressed);

            StartMove(PositionOf(pressed));
        }

        /// <summary>拖拽途中 / 松手时的状态同步：与 <see cref="SetPressed"/> 同口径，但没有日志刷屏。</summary>
        void SetPressedFromDrag(bool pressed)
        {
            if (m_Pressed == pressed) return;

            m_Pressed = pressed;
            PressedChanged?.Invoke(m_Pressed);

            Log.Info($"{name}: 拖拽 → {(m_Pressed ? "按下" : "弹回")}");
        }

        /// <summary>兜底结束拖拽：框架 MoUp 只在松手时仍压在本物体上才派发，故自己盯一下左键，避免松手后卡在拖拽态。</summary>
        void CheckMouseReleased()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return;

            EndDrag();
        }

        /// <summary>世界点 → 屏幕点（y 向上）；在相机背面返回 false。</summary>
        static bool TryProject(Camera cam, Vector3 world, out Vector2 screen)
        {
            screen = default;

            Vector3 point = cam.WorldToScreenPoint(world);
            if (point.z <= 0f) return false;

            screen = new Vector2(point.x, point.y);
            return true;
        }

        /// <summary>当前该用的相机：运行时走全局相机管理器，取不到（如 Editor 里没在播放）退回 Camera.main。</summary>
        static Camera ResolveCamera()
        {
            if (GlobalCameraMgr.Exists && GlobalCameraMgr.Camera != null) return GlobalCameraMgr.Camera;
            return Camera.main;
        }
        #endregion

        #region Editor 标定接口（仅编辑器编译）
#if UNITY_EDITOR
        /// <summary>（Editor）把部件当前姿态记成抬起位 / 按下位 —— 免去手填坐标。</summary>
        public void EditorCaptureLimit(bool pressed)
        {
            Transform visual = Visual;
            if (visual == null) return;

            Vector3 pos = visual.localPosition;
            float value = moveAxis == ObjAxis.X ? pos.x : moveAxis == ObjAxis.Y ? pos.y : pos.z;

            if (pressed) downLimit = value;
            else upLimit = value;

            m_Applied = m_Target = PositionOf(m_Pressed);
        }
#endif
        #endregion
    }
}
