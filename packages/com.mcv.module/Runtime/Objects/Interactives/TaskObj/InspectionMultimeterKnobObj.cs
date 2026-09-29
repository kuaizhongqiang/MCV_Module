using System;
using System.Collections.Generic;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 组件必须与拾取用 Collider 同一 GameObject，否则 GlobalInteractiveMgr 射线命中的物体上没有本组件、按下去点到空白；朝向唯一源是档位表 + zeroEuler，预制体里随手转的角度运行时会覆盖。
    /// <summary>数字万用表档位旋钮：按住拖动跟手旋转、拨过某档即切换、松手吸附最近档、两端硬限位；读数不在此算，由 InspectionMultimeterObj 取 CurrentGear 决定量与超量程。</summary>
    public class InspectionMultimeterKnobObj : InteractiveBase
    {
        #region 序列化参数
        /// <summary>一档：功能 + 量程上限 + 旋钮指向角度。</summary>
        [Serializable]
        public struct GearSetting
        {
            [Tooltip("档位功能：决定读数取 InspectionData 的哪个字段（电阻/电压/电流），Off = 关机（屏幕不显示读数）")]
            public MultimeterGearType gearType;

            [Tooltip("量程上限（0 = 不判超量程）：电阻填 Ω、电压填 V、电流填 A。读数超过它屏幕显示超量程")]
            public float range;

            [Tooltip("旋钮指到本档时可视层的偏转角（度，绕 rotateAxis 正方向）")]
            public float angle;
        }

        [Header("档位表")]
        [SerializeField, Tooltip("按面板上的功能顺序排列；第 0 项就是初始档（建议配成 OFF 档）。至少要有一档")]
        List<GearSetting> gears = new List<GearSetting>();

        [SerializeField, Tooltip("进场景时旋钮停在的档位下标（越界会自动夹到有效范围）")]
        int startIndex;

        [Header("旋转表现")]
        [SerializeField, Tooltip("真正转动的可视层（旋钮模型）；留空 = 转自身")]
        Transform knobVisual;

        [SerializeField, Tooltip("旋转轴（可视层的局部空间）：面板上的旋钮用 Z（= 面板法线方向，绕自身轴转）")]
        Vector3 rotateAxis = Vector3.forward;

        [SerializeField, Tooltip("「档位角度 0」时可视层的局部欧拉角 = 你已经摆好的原始姿态（当前姿态就是 Z=0 就留 (0,0,0)）。" +
                                 "运行时朝向 = 这个姿态再绕 rotateAxis 转档位角度；预制体里随手转出来的角度会被它覆盖")]
        Vector3 zeroEuler = Vector3.zero;

        [SerializeField, Tooltip("松手吸附到最近档位 / 程序化换档的过渡时长（秒）；<= 0 = 立即到位")]
        float rotateDuration = 0.15f;

        [SerializeField, Tooltip("NextGear / PreviousGear 到两端后是否绕回（只影响这两个程序化入口；手拖到两端会顶住不越过 OFF / A=）")]
        bool wrap = true;

        [Header("拖拽旋转")]
        [SerializeField, Tooltip("圆心死区的**上限**（像素）：实际死区 = min(这个值, 旋钮屏幕半径的一半)，" +
                                 "所以旋钮在屏幕上显示得小时会按比例收紧，不会被死区整个吃掉（曾因此完全拖不动）")]
        float dragDeadRadius = 40f;

        [SerializeField, Tooltip("拖拽反向（一般留 false）：方向由脚本按「角度增大在屏幕上是逆时针还是顺时针」实测得到，只有实测仍不合手时才勾它兜底")]
        bool invertDrag;
        #endregion

        #region 运行时状态
        /// <summary>当前档位下标；-1 = 还没初始化（属性会退回 <see cref="startIndex"/>）</summary>
        int m_Index = -1;
        /// <summary>已应用的转角（度）：逐帧朝目标角靠近，避免换档瞬移</summary>
        float m_AppliedAngle;
        /// <summary>目标转角（度）</summary>
        float m_TargetAngle;
        /// <summary>松手吸附的角速度（度/秒），按"角度差 / rotateDuration"在松手时折算</summary>
        float m_RotateSpeed = float.MaxValue;
        /// <summary>实际转动的可视层（Awake 解析）</summary>
        Transform m_Visual;
        /// <summary>可视层的渲染器（Awake 取一次）：拖拽圆心用它的包围盒中心，比模型轴点可靠</summary>
        Renderer m_VisualRenderer;
        /// <summary>「取不到相机」的告警是否已打过（标定面板会逐帧调用方向实测，避免刷屏）</summary>
        bool m_WarnedNoCamera;

        /// <summary>是否正在拖拽旋转</summary>
        bool m_Dragging;
        /// <summary>按下时旋钮中心在屏幕上的位置（拖拽期间固定，避免逐帧抖动）</summary>
        Vector2 m_DragCenter;
        /// <summary>按下时旋钮在屏幕上的半径（像素）：死区按它收紧，诊断日志也用它</summary>
        float m_DragScreenRadius;
        /// <summary>本次拖拽实际使用的死区（像素）：按下时按屏幕半径算一次，全程沿用</summary>
        float m_DragDeadZone = MinDeadRadius;
        /// <summary>上一次采样的鼠标屏幕角（度）：逐帧累加差值，拖过 180° 也不会翻向</summary>
        float m_LastMouseAngle;
        /// <summary>本次拖拽累计转过多少度（屏幕角空间）</summary>
        float m_DragDelta;
        /// <summary>按下时的旋钮角度（连续角空间）</summary>
        float m_DragStartAngle;
        /// <summary>屏幕角 → 档位角 的方向（+1 = 屏幕逆时针为角度增大）</summary>
        float m_DragSign = 1f;
        /// <summary>两端硬限位（与 <see cref="m_DragStartAngle"/> 同一连续角空间）</summary>
        float m_DragMinAngle;
        float m_DragMaxAngle;
        #endregion

        #region 对外属性
        /// <summary>档位数量（0 = 未配置档位表）。</summary>
        public int GearCount => gears.Count;

        /// <summary>当前档位下标（档位表为空时为 -1）。</summary>
        public int CurrentIndex => ResolveIndex();

        /// <summary>当前档位（档位表为空时返回默认值：gearType = Off）。</summary>
        public GearSetting CurrentGear
        {
            get
            {
                int index = ResolveIndex();
                return index >= 0 && index < gears.Count ? gears[index] : default;
            }
        }

        /// <summary>当前档位的功能（读数取哪个量由它决定）。</summary>
        public MultimeterGearType CurrentGearType => CurrentGear.gearType;

        /// <summary>当前档位的量程上限（0 = 不判超量程）。</summary>
        public float CurrentRange => CurrentGear.range;

        /// <summary>当前档位显示名，如「电阻 200Ω」—— 界面提示 / 操作记录用。</summary>
        public string CurrentGearLabel
        {
            get
            {
                var gear = CurrentGear;
                string name = ChnNameMap.Get(gear.gearType);
                return gear.range > 0f ? $"{name} {gear.range:0.###}{UnitOf(gear.gearType)}" : name;
            }
        }

        /// <summary>换档后回调（参数 = 新档位下标）。万用表除了逐帧比对下标，也可以用它在第一时间刷新读数。</summary>
        public event Action<int> GearChanged;

        /// <summary>实际转动的可视层（配了 knobVisual 用它，否则用自身）—— Inspector 的角度标定工具也读它。</summary>
        public Transform Visual => knobVisual != null ? knobVisual : transform;

        // WHY: 换档与 Inspector 角度标定共用这一份算法，改了标定工具与运行时就不一致。
        /// <summary>把「档位角度」组合成可视层的局部朝向：原始姿态（zeroEuler）→ 绕旋转轴转角度。</summary>
        public Quaternion ComposeRotation(float angleDegrees)
        {
            return Quaternion.Euler(zeroEuler) * Quaternion.AngleAxis(angleDegrees, ResolveAxis());
        }

        /// <summary>归一化后的旋转轴（配成零向量时退回 Z）。</summary>
        Vector3 ResolveAxis()
        {
            return rotateAxis.sqrMagnitude > 1e-6f ? rotateAxis.normalized : Vector3.forward;
        }
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            m_Visual = Visual;
            m_VisualRenderer = m_Visual != null ? m_Visual.GetComponentInChildren<Renderer>() : null;

            HighlightPluginInit(gameObject);

            if (gears.Count == 0)
            {
                Log.Warning($"{name}: 档位表为空 —— 旋钮转不动，万用表也读不到任何档位");
                return;
            }

            m_Index = Mathf.Clamp(startIndex, 0, gears.Count - 1);
            m_TargetAngle = m_AppliedAngle = gears[m_Index].angle;
            ApplyRotation();    // 进场景直接摆到初始档角（预制体里随手转的朝向不作数）
        }

        void Update()
        {
            if (m_Dragging)
            {
                // 拖拽期间角度由鼠标直接驱动（跟手，不做过渡动画）
                UpdateDrag();
                CheckMouseReleased();
                return;
            }

            // 松手吸附：朝最近档位转过去
            if (m_Visual == null || Mathf.Approximately(m_AppliedAngle, m_TargetAngle)) return;

            m_AppliedAngle = Mathf.MoveTowardsAngle(m_AppliedAngle, m_TargetAngle, m_RotateSpeed * Time.deltaTime);
            ApplyRotation();
        }

        void OnDisable()
        {
            // 停用 / 被销毁时别把"拖拽中"的状态留到下次（那时鼠标可能早松开了）
            m_Dragging = false;
        }
        #endregion

        #region 交互
        protected override void MoEnterEvent() => Highlight(true);

        protected override void MoExitEvent() => Highlight(false);

        protected override void MoDownEvent() => BeginDrag();

        protected override void MoUpEvent() => EndDrag();

        // WHY: 框架 MoMove 只在"射线本帧命中本物体"时派发，鼠标快速划开会断开，持续跟手必须靠 Update。
        /// <summary>只做同帧提前响应；持续跟手由 Update 保证。</summary>
        protected override void MoMoveEvent(Vector2 delta)
        {
            if (m_Dragging) UpdateDrag();
        }
        #endregion

        #region 拖拽旋转
        /// <summary>圆心死区的下限（像素）：鼠标正好压在圆心时 atan2 会乱跳，必须留一点</summary>
        const float MinDeadRadius = 4f;

        /// <summary>按下开始拖拽：记下屏幕圆心、鼠标起始角与当前档角，并按各档角度端值算出硬限位；各拒绝路径打日志。</summary>
        void BeginDrag()
        {
            if (gears.Count == 0)
            {
                Log.Info($"{name}: 档位表为空，旋钮转不动（先配档位表）");
                return;
            }

            if (!TryGetDragCenter(out m_DragCenter))
            {
                Log.Warning($"{name}: 取不到旋钮的屏幕圆心（相机未就绪 / 物体在相机背面？），本次拖拽忽略");
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 dir = mouse.position.ReadValue() - m_DragCenter;
            float distance = dir.magnitude;

            // WHY: 死区 = min(配置像素上限, 屏幕半径的一半)，固定死区会把小旋钮整个吃掉，按上去完全拖不动
            m_DragScreenRadius = MeasureScreenRadius(m_DragCenter);
            m_DragDeadZone = Mathf.Min(dragDeadRadius, Mathf.Max(m_DragScreenRadius * 0.5f, MinDeadRadius));

            if (distance < m_DragDeadZone)
            {
                Log.Info($"{name}: 按下点离圆心 {distance:0}px < 死区 {m_DragDeadZone:0}px" +
                         $"（旋钮屏幕半径 {m_DragScreenRadius:0}px），本次不接拖拽");
                return;
            }

            m_Dragging = true;
            m_DragStartAngle = m_AppliedAngle;
            m_DragDelta = 0f;
            m_LastMouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 限位先算好（万一下面的方向实测出岔子，拖拽也只是方向不对，不会因为限位还是 0/0 而完全卡死）
            ResolveDragLimits(m_DragStartAngle, out m_DragMinAngle, out m_DragMaxAngle);
            m_DragSign = ResolveDragSign();
        }

        /// <summary>松手：吸附到离当前角度最近的那一档（转到位用 <see cref="rotateDuration"/> 过渡）。</summary>
        void EndDrag()
        {
            if (!m_Dragging) return;
            m_Dragging = false;

            if (gears.Count == 0) return;

            int nearest = FindNearestGearIndex(m_AppliedAngle);
            int previous = ResolveIndex();
            m_Index = nearest;
            StartRotate(gears[nearest].angle);

            if (nearest != previous) GearChanged?.Invoke(nearest);
        }

        // WHY: 逐帧累加角差而非"当前角-起始角"（拖过 180° 不被 DeltaAngle 抄近路翻向）；圆心必须用按下时缓存的，逐帧重算会因包围盒中心漂移而自激抖动。
        /// <summary>逐帧跟手：把鼠标绕旋钮中心转过的角度差累加到按下时的档角上，顶到限位一起夹回。</summary>
        void UpdateDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // 死区用按下时算好的那个（按屏幕半径收过），太靠近圆心这帧就不动，免得 atan2 乱跳
            Vector2 dir = mouse.position.ReadValue() - m_DragCenter;
            if (dir.sqrMagnitude < m_DragDeadZone * m_DragDeadZone) return;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            m_DragDelta += Mathf.DeltaAngle(m_LastMouseAngle, angle);
            m_LastMouseAngle = angle;

            float target = Mathf.Clamp(m_DragStartAngle + m_DragDelta * m_DragSign, m_DragMinAngle, m_DragMaxAngle);
            m_DragDelta = (target - m_DragStartAngle) * m_DragSign;

            SetAppliedAngle(target);
            SyncIndexToNearestGear();       // 拨过某档就切过去：万用表的读数跟着变
        }

        // WHY: 框架 MoUp 只在"松手时光标仍压在本物体上"才派发（空白处松手只发 Target=null 的 Click），不自己盯左键会卡在拖拽态。
        /// <summary>兜底结束拖拽：自己盯左键，避免松手后卡在拖拽态。</summary>
        void CheckMouseReleased()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return;

            EndDrag();
        }

        /// <summary>拖拽期间直接摆角度（跟手）：目标角一起对齐，免得 Update 的吸附动画和手指抢。</summary>
        void SetAppliedAngle(float angle)
        {
            m_AppliedAngle = angle;
            m_TargetAngle = angle;
            ApplyRotation();
        }

        /// <summary>拖拽途中经过某档就切过去（只在下标真的变了时发事件，不逐帧发）。</summary>
        void SyncIndexToNearestGear()
        {
            int nearest = FindNearestGearIndex(m_AppliedAngle);
            if (nearest == ResolveIndex()) return;

            m_Index = nearest;
            GearChanged?.Invoke(nearest);

            Log.Info($"{name}: 万用表换档 → {CurrentGearLabel}");
        }

        /// <summary>离给定角度最近的那一档（跨 ±180° 用 <c>DeltaAngle</c> 比，不会因为绕圈判错）。</summary>
        int FindNearestGearIndex(float angle)
        {
            int nearest = 0;
            float best = float.MaxValue;

            for (int i = 0; i < gears.Count; i++)
            {
                float distance = Mathf.Abs(Mathf.DeltaAngle(angle, gears[i].angle));
                if (distance >= best) continue;

                best = distance;
                nearest = i;
            }
            return nearest;
        }

        // WHY: 假设档位表跨度 < 360°，两端极值才等于旋钮能到的最远位置；跨度过大该假设不成立。
        /// <summary>两端硬限位：以按下角度为参照把各档角度换算进同一圈后取最小 / 最大。</summary>
        void ResolveDragLimits(float referenceAngle, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            for (int i = 0; i < gears.Count; i++)
            {
                float angle = referenceAngle + Mathf.DeltaAngle(referenceAngle, gears[i].angle);
                if (angle < min) min = angle;
                if (angle > max) max = angle;
            }
        }

        // WHY: 用"旋钮轴 · 相机视线"点乘推断符号会推反（表现为右拽往左转）；相机不可用时退回 +1 并告警，invertDrag 手动兜底。
        /// <summary>实测「档位角度增大 → 屏幕转向」符号（+1=屏幕逆时针 / -1=顺时针），拖拽与 Editor 标定共用；相机不可用退回 +1。</summary>
        public float MeasureAngleScreenDirection()
        {
            const float ProbeDelta = 15f;      // 探测用的角度增量：只要不为 0 就不影响符号

            var cam = ResolveCamera();
            if (cam == null)
            {
                // 标定面板会逐帧调用本方法，告警只打一次
                if (!m_WarnedNoCamera)
                {
                    m_WarnedNoCamera = true;
                    Log.Warning($"{name}: 取不到相机，旋钮角度方向按默认约定（+1）处理；拖拽方向不对请勾 invertDrag");
                }
                return 1f;
            }

            Vector3 center = GetDragCenterWorld();
            if (!TryProject(cam, center, out Vector2 centerScreen)) return 1f;

            // 圆环半径：世界半径（取不到就随便给个正值，符号与半径大小无关，只影响投影精度）
            float radius = GetWorldRadius();
            if (radius <= 1e-6f) radius = 0.1f;

            Vector3 axisWorld = ResolveWorldAxis();
            if (!TryGetRimDirection(axisWorld, cam, out Vector3 rimDir)) return 1f;

            Vector3 offset = rimDir * radius;
            Vector3 rotated = Quaternion.AngleAxis(ProbeDelta, axisWorld) * offset;

            if (!TryScreenAngleOf(cam, center + offset, centerScreen, out float before)) return 1f;
            if (!TryScreenAngleOf(cam, center + rotated, centerScreen, out float after)) return 1f;

            return Mathf.DeltaAngle(before, after) >= 0f ? 1f : -1f;
        }

        // WHY: 死区按本半径收紧（min(像素上限, 屏幕半径的一半)）——固定像素死区在旋钮显示得小时会把整个旋钮吃掉，完全拖不动。
        /// <summary>旋钮在屏幕上的半径（像素）：把「圆心 + 圆环方向 × 世界半径」投影后与圆心比距离；取不到返回 0。</summary>
        float MeasureScreenRadius(Vector2 centerScreen)
        {
            var cam = ResolveCamera();
            if (cam == null) return 0f;

            Vector3 center = GetDragCenterWorld();
            if (!TryProject(cam, center, out Vector2 projectedCenter)) return 0f;
            if (Vector2.Distance(projectedCenter, centerScreen) > 0.5f) return 0f;    // 屏幕圆心对不上：不猜

            float radius = GetWorldRadius();
            if (radius <= 1e-6f) return 0f;

            if (!TryGetRimDirection(ResolveWorldAxis(), cam, out Vector3 rimDir)) return 0f;
            if (!TryProject(cam, center + rimDir * radius, out Vector2 rimScreen)) return 0f;

            return Vector2.Distance(rimScreen, projectedCenter);
        }

        /// <summary>旋钮轴的世界方向（含层级与当前档位姿态）。</summary>
        Vector3 ResolveWorldAxis()
        {
            Transform visual = m_Visual != null ? m_Visual : Visual;
            return visual != null ? visual.rotation * ResolveAxis() : ResolveAxis();
        }

        /// <summary>圆环上的单位方向：与旋钮轴垂直、尽量落在屏幕平面内；「轴 × 视线」退化时退回相机 up / right。</summary>
        static bool TryGetRimDirection(Vector3 axisWorld, Camera cam, out Vector3 direction)
        {
            direction = Vector3.Cross(axisWorld, cam.transform.forward);
            if (direction.sqrMagnitude < 1e-8f) direction = Vector3.Cross(axisWorld, cam.transform.up);
            if (direction.sqrMagnitude < 1e-8f) direction = Vector3.Cross(axisWorld, cam.transform.right);
            if (direction.sqrMagnitude < 1e-8f) return false;

            direction.Normalize();
            return true;
        }

        /// <summary>可视层渲染器的世界半径（包围盒尺度的一半，旋钮大致就是这么快）；取不到返回 0。</summary>
        float GetWorldRadius()
        {
            Renderer renderer = ResolveRenderer();
            return renderer != null ? renderer.bounds.extents.magnitude * 0.5f : 0f;
        }

        /// <summary>可视层的渲染器：优先用 Awake 缓存的那个，Editor（没跑 Awake）时现取一次。</summary>
        Renderer ResolveRenderer()
        {
            if (m_VisualRenderer != null) return m_VisualRenderer;

            Transform visual = m_Visual != null ? m_Visual : Visual;
            return visual != null ? visual.GetComponentInChildren<Renderer>() : null;
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

        /// <summary>拖拽方向符号：实测值 + <see cref="invertDrag"/> 手动兜底。</summary>
        float ResolveDragSign()
        {
            float direction = MeasureAngleScreenDirection();

            Log.Verbose($"{name}: 旋钮方向符号 = {direction}{(invertDrag ? "（已手动反向）" : string.Empty)}");

            return invertDrag ? -direction : direction;
        }

        /// <summary>世界点相对给定屏幕圆心的屏幕角（度，y 向上 → 逆时针为正）；投影失败返回 false。</summary>
        static bool TryScreenAngleOf(Camera cam, Vector3 world, Vector2 screenCenter, out float angle)
        {
            angle = 0f;

            Vector3 point = cam.WorldToScreenPoint(world);
            if (point.z <= 0f) return false;

            angle = Mathf.Atan2(point.y - screenCenter.y, point.x - screenCenter.x) * Mathf.Rad2Deg;
            return true;
        }

        /// <summary>当前该用的相机：运行时走全局相机管理器，取不到退回 Camera.main。</summary>
        static Camera ResolveCamera()
        {
            if (GlobalCameraMgr.Exists && GlobalCameraMgr.Camera != null) return GlobalCameraMgr.Camera;
            return Camera.main;
        }

        // WHY: 模型轴点常常不在旋钮圆心，用轴点算角度会明显偏，所以优先取渲染包围盒中心。
        /// <summary>拖拽圆心（世界）：优先取可视层渲染包围盒中心，取不到渲染器退回轴点。</summary>
        Vector3 GetDragCenterWorld()
        {
            Transform visual = m_Visual != null ? m_Visual : Visual;
            if (visual == null) return Vector3.zero;

            Renderer renderer = ResolveRenderer();
            return renderer != null ? renderer.bounds.center : visual.position;
        }

        /// <summary>拖拽圆心（屏幕像素）：由 GetDragCenterWorld 投影得到；相机未就绪 / 物体在背面时返回 false。</summary>
        bool TryGetDragCenter(out Vector2 screenPos)
        {
            var cam = ResolveCamera();
            if (cam == null)
            {
                screenPos = default;
                return false;
            }

            return TryProject(cam, GetDragCenterWorld(), out screenPos);
        }
        #endregion

        #region 换档
        /// <summary>切到下一档（程序化入口，步骤 / 测试用）；已在最后一档时按 <see cref="wrap"/> 决定绕回还是不动。</summary>
        public void NextGear()
        {
            if (gears.Count == 0) return;

            int next = ResolveIndex() + 1;
            if (next >= gears.Count)
            {
                if (!wrap) return;
                next = 0;
            }
            SetGear(next);
        }

        /// <summary>切到上一档（已在第 0 档时按 <see cref="wrap"/> 决定绕到最后还是不动）。</summary>
        public void PreviousGear()
        {
            if (gears.Count == 0) return;

            int previous = ResolveIndex() - 1;
            if (previous < 0)
            {
                if (!wrap) return;
                previous = gears.Count - 1;
            }
            SetGear(previous);
        }

        /// <summary>切到指定档（越界夹取；与当前档相同则不发事件、不旋转）。</summary>
        public void SetGear(int index)
        {
            if (gears.Count == 0) return;

            index = Mathf.Clamp(index, 0, gears.Count - 1);
            if (index == ResolveIndex()) return;

            m_Index = index;
            StartRotate(gears[m_Index].angle);
            GearChanged?.Invoke(m_Index);

            Log.Info($"{name}: 万用表换档 → {CurrentGearLabel}");
        }

        /// <summary>开始朝目标角旋转：角速度按"角度差 / rotateDuration"折算（改时长不用改各档角度）。</summary>
        void StartRotate(float targetAngle)
        {
            m_TargetAngle = targetAngle;

            float delta = Mathf.Abs(Mathf.DeltaAngle(m_AppliedAngle, m_TargetAngle));
            m_RotateSpeed = rotateDuration > 0f && delta > 0f ? delta / rotateDuration : float.MaxValue;
        }

        // WHY: 先套 zeroEuler 再绕 rotateAxis 转，才不会把模型原有倾斜掰掉（「角度 0」= 摆好的原始姿态）。
        /// <summary>把当前转角写到可视层：zeroEuler 姿态 → 再绕 rotateAxis 转角度。</summary>
        void ApplyRotation()
        {
            if (m_Visual == null) return;
            m_Visual.localRotation = ComposeRotation(m_AppliedAngle);
        }

        /// <summary>有效档位下标：未初始化 / 档位表被改小时退回 <see cref="startIndex"/>（夹取后），不返回越界值。</summary>
        int ResolveIndex()
        {
            if (gears.Count == 0) return -1;
            return m_Index >= 0 && m_Index < gears.Count ? m_Index : Mathf.Clamp(startIndex, 0, gears.Count - 1);
        }

        /// <summary>档位的物理量单位（拼显示名用）。</summary>
        static string UnitOf(MultimeterGearType gear)
        {
            switch (gear)
            {
                case MultimeterGearType.Resistance:
                case MultimeterGearType.Diode:
                case MultimeterGearType.Continuity:
                    return "Ω";
                case MultimeterGearType.VoltageDC:
                case MultimeterGearType.VoltageAC:
                    return "V";
                case MultimeterGearType.CurrentDC:
                case MultimeterGearType.CurrentAC:
                    return "A";
                default:
                    return string.Empty;
            }
        }
        #endregion

        #region Editor 标定接口（仅编辑器编译）
#if UNITY_EDITOR
        /// <summary>（Editor）读第 index 档的配置；越界返回默认值。</summary>
        public GearSetting EditorGetGear(int index)
        {
            return index >= 0 && index < gears.Count ? gears[index] : default;
        }

        /// <summary>（Editor）写入某一档角度，供 Inspector 角度标定工具「对准刻度后写入」用；改的是当前档时同步已应用角度。</summary>
        public void EditorSetGearAngle(int index, float angleDegrees)
        {
            if (index < 0 || index >= gears.Count) return;

            var gear = gears[index];
            gear.angle = angleDegrees;
            gears[index] = gear;

            if (index != ResolveIndex()) return;

            m_TargetAngle = m_AppliedAngle = angleDegrees;
            ApplyRotation();
        }

        /// <summary>（Editor）整体替换档位表（按面板推荐角度一键填充用）。</summary>
        public void EditorSetGears(List<GearSetting> newGears)
        {
            gears = newGears ?? new List<GearSetting>();

            m_Index = -1;                       // 交回 startIndex 决定初始档
            m_TargetAngle = m_AppliedAngle = 0f;
        }

        /// <summary>（Editor）改初始档下标。</summary>
        public void EditorSetStartIndex(int index)
        {
            startIndex = index;
        }
#endif
        #endregion
    }
}
