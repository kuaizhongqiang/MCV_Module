using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;



namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 本组件与光标拾取用的 Collider 必须同挂一个 GameObject（长条检测区也建议放根节点），否则 Physics.Raycast 首个命中体上没有 InteractiveBase，既拾取不到又会清掉悬停态；吸附缩放只作用于可视层，检测区/拾取体不缩。
    /// <summary>检测笔（红/黑表笔）：拖拽沿过笔尖、法线取与相机视线最接近世界轴的平面做二维移动，检测区与检测点真实重叠或落入即接触并发布接触/吸附事件，拖拽未接触时复位回上次接触位置。</summary>
    public class InspectionProbeObj : InteractiveBase
    {
        #region 序列化参数
        [Header("表笔")]
        [SerializeField, Tooltip("红/黑表笔，随接触事件一起发给订阅方")]
        InspectionProbeType probeType = InspectionProbeType.Red;

        [SerializeField, Tooltip("笔尖：多个目标同时被接触时取离它最近的一个；留空则用自身位置")]
        Transform probePoint;

        [SerializeField, Tooltip("沿相机视线延伸的长条检测区（建议用 Box/Capsule 这类凸碰撞体）；未赋值则不做接触判定")]
        Collider areaCollider;

        [Header("拖拽")]
        [SerializeField, Tooltip("拖拽平面法线。Auto（默认）= 与相机视线最接近的世界轴：表笔在该轴上不漂移，始终停在面板所在的深度（相机 22° 俯视时 = Z）。CameraFacing = 严格屏幕平行面：贴光标最准，但会顺着视线方向漂到面板里/外")]
        MovePlaneNormal movePlaneNormal = MovePlaneNormal.Auto;

        [Header("复位")]
        [SerializeField, Tooltip("拖拽失败后回到「上一个接触位置 / 初始位置」的过渡时长（秒）")]
        float resetDuration = 0.3f;

        [Header("吸附表现")]
        [SerializeField, Tooltip("吸附（贴住检测点且未拖拽）时的缩放系数，相对可视层的初始缩放；拖拽中 / 未吸附固定为 1")]
        float snapScaleFactor = 0.8f;

        [SerializeField, Tooltip("缩到吸附尺寸的过渡时长（秒）")]
        float snapScaleDuration = 0.2f;
        #endregion

        #region 运行时状态
        /// <summary>初始位置：从未接触过时的复位目标</summary>
        Vector3 m_HomePos;
        /// <summary>复位目标：接触成功时更新为接触位置，供"失败后回到上次到位点"</summary>
        Vector3 m_ReturnPos;

        /// <summary>拖拽平面：过表笔当前位置，法线由拖拽平面模式决定；每次按下时重建</summary>
        Plane m_MovePlane;
        bool m_PlaneReady;

        /// <summary>可视层（Awake 按约定层级自动取到的那一层）：吸附缩放只作用于它</summary>
        Transform m_VisualRoot;
        /// <summary>可视层的基准缩放（Awake 时记下，通常是 1）</summary>
        Vector3 m_VisualBaseScale = Vector3.one;
        /// <summary>当前缩放系数：1 = 基准；吸附时趋向 <see cref="snapScaleFactor"/></summary>
        float m_Scale = 1f;

        bool m_Dragging;
        /// <summary>按下时「物体位置 - 平面交点」，拖拽全程保留，避免抓起瞬间跳变</summary>
        Vector3 m_GrabOffset;

        /// <summary>当前接触中的点（null = 未接触）</summary>
        InspectionElementPointObj m_ContactPoint;
        Coroutine m_ResetCoroutine;

        /// <summary>上一帧的吸附态（只在翻转时发 <see cref="InspectionProbeSnapEventData"/>，不逐帧发）</summary>
        bool m_Snapped;
        /// <summary>吸附中的点（脱离吸附时事件里要带上它 —— 那时 m_ContactPoint 可能已经变空）</summary>
        InspectionElementPointObj m_SnapPoint;

        /// <summary>正被拖拽扫过而高亮的点（null = 没有）；只认拖拽中的接触</summary>
        InspectionElementPointObj m_HighlightedPoint;

        /// <summary>候选点缓存（逐帧复用，避免 GC）</summary>
        readonly List<InspectionElementPointObj> m_Candidates = new List<InspectionElementPointObj>();
        /// <summary>检测区物理重叠缓冲区（逐帧复用）</summary>
        readonly Collider[] m_OverlapHits = new Collider[16];
        /// <summary>本帧被检测区重叠到的待检测点（逐帧复用）</summary>
        readonly List<InspectionElementPointObj> m_Touched = new List<InspectionElementPointObj>();
        bool m_WarnedNoCandidates;
        #endregion

        #region 对外属性
        /// <summary>本表笔类型（红 / 黑）。</summary>
        public InspectionProbeType ProbeType => probeType;

        /// <summary>本表笔中文名（红表笔 / 黑表笔）—— 界面提示文案用。</summary>
        public string ProbeName => ChnNameMap.Get(probeType);

        /// <summary>当前吸附的检测点（未吸附为 null）；吸附 = 贴住检测点且未拖拽，供步骤条件在进入等待时补读一次当前状态（吸附事件只在翻转时发）。</summary>
        public InspectionElementPointObj SnappedPoint => IsSnapped ? m_ContactPoint : null;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            // 预制体实例化时位姿已落定，这里记的初始位置就是复位落点，不必等相机。
            m_HomePos = transform.position;
            m_ReturnPos = m_HomePos;

            InitVisualRoot();

            if (areaCollider == null)
                Log.Warning($"{name}: areaCollider 未赋值，表笔不会有任何接触判定");

            HighlightInit(gameObject);
        }

        void Update()
        {
            if (m_Dragging)
            {
                // WHY: 只靠 MoMove 派发会在光标移出本物体 Collider、光标在 UI 上、鼠标静止门控时直接断掉（物体停住、松手回弹），所以拖拽期间逐帧自算平面交点。
                FollowMouse();
                CheckMouseReleased();
            }

            UpdateContact();
            UpdateContactHighlight();   // 拖拽中扫到的点才高亮（放好就不亮）
            UpdateSnap();       // 接触 + 拖拽都算完了再判吸附态（吸附 = 接触且未拖拽）
            UpdateScale();      // 放在接触判定之后：本帧的吸附状态定了再决定缩放
        }

        void OnDisable()
        {
            ClearState();
        }

        protected override void OnDestroy()
        {
            ClearState();
            base.OnDestroy();
        }
        #endregion

        protected override void MoEnterEvent()
        {
            Highlight(true);
        }

        protected override void MoExitEvent()
        {
            Highlight(false);
        }


        #region 拖拽（二维移动）
        protected override void MoDownEvent()
        {
            if (!RefreshMovePlane()) return;                // 相机未就绪：先不接受拖拽
            if (!TryGetMousePlanePoint(out var point)) return;

            StopReset();
            m_Dragging = true;
            m_GrabOffset = transform.position - point;
        }

        protected override void MoMoveEvent(Vector2 delta)
        {
            // 只做同帧提前响应；持续跟随由 Update 保证（见 Update 注释），delta 不参与计算
            if (!m_Dragging) return;
            FollowMouse();
        }

        /// <summary>让表笔跟着鼠标在拖拽平面上走（贴光标 + 保留按下时的抓取偏移）。</summary>
        void FollowMouse()
        {
            if (!TryGetMousePlanePoint(out var point)) return;   // 射线与平面平行等极端情况：保持不动

            transform.position = point + m_GrabOffset;

            // WHY: 本项目 Physics.autoSyncTransforms 关着，不显式同步则本帧接触查询与射线仍看上一物理步的位置（快速拖动会错帧）。
            Physics.SyncTransforms();
        }

        protected override void MoUpEvent()
        {
            if (!m_Dragging) return;
            EndDrag();
        }

        /// <summary>结束一次拖拽：松手时不在接触态就过渡回「上一个接触位置 / 初始位置」，接触态下松手不动（要移开才解除）。</summary>
        void EndDrag()
        {
            m_Dragging = false;
            if (m_ContactPoint != null) return;
            StartReset();
        }

        /// <summary>兜底结束拖拽：框架 MoUp 只在松手瞬间光标仍压在本物体上才派发（空白处只发 Target=null 的 Click），所以自己盯左键避免卡在拖拽态。</summary>
        void CheckMouseReleased()
        {
            if (!m_Dragging) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return;
            EndDrag();
        }
        #endregion

        #region 接触判定
        /// <summary>接触状态机：每帧找出被检测区碰到的点，只在状态翻转时发布事件，多个命中时只保留离笔尖最近的一个。</summary>
        void UpdateContact()
        {
            // 检测区没引用时视为未接触：接触中被打断也要走解除，不能静默保持
            var touched = AreaUsable ? FindTouchedPoint() : null;
            if (touched == m_ContactPoint) return;

            if (m_ContactPoint != null)
            {
                PublishContact(m_ContactPoint, isContact: false);
                m_ContactPoint = null;
            }

            if (touched != null)
            {
                m_ContactPoint = touched;
                m_ReturnPos = transform.position;   // 接触位置留作复位目标
                PublishContact(touched, isContact: true);
            }
        }

        /// <summary>在已登记的待检测点里找出被检测区碰到的那一个：碰撞体真实重叠，或点落在检测区形状内，多个命中取离笔尖最近的一个。</summary>
        InspectionElementPointObj FindTouchedPoint()
        {
            GlobalInteractiveMgr.CollectRegistered(m_Candidates);
            if (m_Candidates.Count == 0)
            {
                // 静默失败点：未激活的物体会连 Awake 都不跑，也就不会注册进 GlobalInteractiveMgr
                if (!m_WarnedNoCandidates)
                {
                    m_WarnedNoCandidates = true;
                    Log.Warning($"{name}: 场景里没有已登记的 InspectionElementPointObj（待检测点是否处于未激活状态？未激活的物体不会注册，表笔永远判不到接触）");
                }
                return null;
            }

            CollectOverlappedPoints();

            Vector3 tip = TipPosition;
            InspectionElementPointObj nearest = null;
            float nearestSqr = float.MaxValue;

            for (int i = 0; i < m_Candidates.Count; i++)
            {
                var point = m_Candidates[i];
                if (point == null) continue;

                Vector3 position = point.transform.position;
                if (!m_Touched.Contains(point) && !IsInsideArea(position)) continue;

                float sqr = (position - tip).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = point;
                }
            }

            return nearest;
        }

        /// <summary>把检测区的物理重叠命中体反查成待检测点（命中的碰撞体可以挂在点的子物体上）。</summary>
        void CollectOverlappedPoints()
        {
            m_Touched.Clear();

            int count = OverlapArea(m_OverlapHits);
            for (int i = 0; i < count; i++)
            {
                var hit = m_OverlapHits[i];
                if (hit == null) continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;   // 表笔自己

                var point = hit.GetComponentInParent<InspectionElementPointObj>();
                if (point == null || m_Touched.Contains(point)) continue;
                m_Touched.Add(point);
            }
        }

        /// <summary>用检测区形状做一次物理重叠查询（触发器也计入），命中写进缓冲区并返回数量；只处理 BoxCollider，其它形状返回 0 由几何判定兜底。</summary>
        int OverlapArea(Collider[] buffer)
        {
            if (areaCollider is not BoxCollider box) return 0;

            var t = box.transform;
            Vector3 scale = t.lossyScale;
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

            return Physics.OverlapBoxNonAlloc(
                t.TransformPoint(box.center), halfExtents, buffer, t.rotation,
                ~0, QueryTriggerInteraction.Collide);
        }

        /// <summary>点是否落在检测区形状内（点落在碰撞体内时 ClosestPoint 返回点本身，距离≈0）。</summary>
        bool IsInsideArea(Vector3 position)
        {
            Vector3 closest = areaCollider.ClosestPoint(position);
            return (closest - position).sqrMagnitude <= ContactTolerance;
        }

        void PublishContact(InspectionElementPointObj point, bool isContact)
        {
            EventBus<InspectionProbeEventData>.Publish(new InspectionProbeEventData(probeType, point, isContact));
        }

        /// <summary>拖拽扫过高亮：把当前接触点同步给点自己，只有拖拽中才给、放好立即撤掉；逐帧比对，变了就撤旧点亮新点。</summary>
        void UpdateContactHighlight()
        {
            // 不在拖拽就一律视为"没有要亮的点"（放好之后表笔还贴着点也不亮）
            var target = m_Dragging ? m_ContactPoint : null;
            if (target == m_HighlightedPoint) return;

            if (m_HighlightedPoint != null) m_HighlightedPoint.SetContactHighlight(false);

            m_HighlightedPoint = target;

            if (m_HighlightedPoint != null) m_HighlightedPoint.SetContactHighlight(true);
        }

        /// <summary>笔尖位置（未配置 probePoint 时用自身位置）</summary>
        Vector3 TipPosition => probePoint != null ? probePoint.position : transform.position;

        /// <summary>检测区可用：只要赋了值就参与判定（它只提供形状，不需要触发器/Rigidbody 之类物理配置）</summary>
        bool AreaUsable => areaCollider != null;

        /// <summary>接触容差（平方距离）：点落在体内时 ClosestPoint 返回点本身，留一点浮点余量</summary>
        const float ContactTolerance = 1e-6f;
        #endregion

        #region 复位
        void StartReset()
        {
            StopReset();

            if (resetDuration <= 0f)
            {
                transform.position = m_ReturnPos;
                return;
            }
            m_ResetCoroutine = StartCoroutine(ResetCoroutine());
        }

        IEnumerator ResetCoroutine()
        {
            Vector3 from = transform.position;
            float time = 0f;
            while (time < resetDuration)
            {
                time += Time.deltaTime;
                transform.position = Vector3.Lerp(from, m_ReturnPos, Mathf.Clamp01(time / resetDuration));
                Physics.SyncTransforms();   // 与 FollowMouse 同理：让回弹途中的位置当帧就能被判定到
                yield return null;
            }
            transform.position = m_ReturnPos;
            Physics.SyncTransforms();
            m_ResetCoroutine = null;
        }

        void StopReset()
        {
            if (m_ResetCoroutine == null) return;
            StopCoroutine(m_ResetCoroutine);
            m_ResetCoroutine = null;
        }

        /// <summary>清拖拽/接触状态；吸附中或接触中就补发解除（停用、销毁时调用）。</summary>
        void ClearState()
        {
            StopReset();
            m_Dragging = false;
            ResetScale();               // 停用/销毁后别把缩小的状态留到下次

            // 把"拖拽扫过"的点高亮撤掉，别让点停在亮着
            if (m_HighlightedPoint != null)
            {
                m_HighlightedPoint.SetContactHighlight(false);
                m_HighlightedPoint = null;
            }

            // 收口时吸附、接触都算解除：两条事件各补发一次，订阅方（操作记录等）不至于停在"还在吸附"上
            if (m_Snapped)
            {
                m_Snapped = false;
                PublishSnap(m_SnapPoint, isSnapped: false);
                m_SnapPoint = null;
            }

            if (m_ContactPoint == null) return;
            PublishContact(m_ContactPoint, isContact: false);
            m_ContactPoint = null;
        }
        #endregion

        #region 吸附表现（缩放）
        /// <summary>吸附态：接触且不在拖拽中；拖拽中一律不算吸附（拖过去蹭到只算接触），目前吸附就等同于接触态。</summary>
        bool IsSnapped => m_ContactPoint != null && !m_Dragging;

        /// <summary>吸附态翻转检测：每帧比一次，翻转才发吸附事件（不逐帧发）；抓起表笔或移开检测点都会解除吸附。</summary>
        void UpdateSnap()
        {
            bool snapped = IsSnapped;
            if (snapped == m_Snapped) return;

            m_Snapped = snapped;
            if (snapped) m_SnapPoint = m_ContactPoint;      // 吸附住：记住是哪个点，解除时事件里要带上

            PublishSnap(m_SnapPoint, snapped);

            if (!snapped) m_SnapPoint = null;
        }

        /// <summary>发布吸附事件（<c>IsSnapped</c> true = 吸附住 / false = 脱离吸附）。</summary>
        void PublishSnap(InspectionElementPointObj point, bool isSnapped)
        {
            if (point == null) return;      // 兜底：没有点就不发（正常不会走到）

            EventBus<InspectionProbeSnapEventData>.Publish(new InspectionProbeSnapEventData(probeType, point, isSnapped));
        }

        /// <summary>可视层：按约定层级取「根 → 子0 → 子1」并记下基准缩放（层级固定，改了要同步改这里）；顺带校验检测区不在可视层里，否则缩放会连改检测区大小导致判定抖动。</summary>
        void InitVisualRoot()
        {
            Transform first = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (first != null && first.childCount > 1) m_VisualRoot = first.GetChild(1);

            if (m_VisualRoot == null)
            {
                Log.Warning($"{name}: 层级不是「根 → 子0 → 子1」，找不到可视层，吸附缩放不会生效");
                return;
            }

            m_VisualBaseScale = m_VisualRoot.localScale;

            if (areaCollider != null
                && (areaCollider.transform == m_VisualRoot || areaCollider.transform.IsChildOf(m_VisualRoot)))
            {
                Log.Warning($"{name}: areaCollider 挂在可视层（{m_VisualRoot.name}）下面 —— 吸附缩放会连着改变检测区大小，接触判定会在缩放过程中反复翻转（抖动）。请把检测区移到不受缩放影响的位置（建议与本组件同一 GameObject）");
            }
        }

        /// <summary>吸附缩放：吸附时用 snapScaleDuration 从基准平滑缩到 snapScaleFactor，开始拖拽 / 脱离接触立即回基准；只缩可视层，根节点不动故不影响判定。</summary>
        void UpdateScale()
        {
            if (IsSnapped)
            {
                // 速率按"1 → snapScaleFactor 恰好走 snapScaleDuration 秒"折算，改系数不用改时长
                float speed = snapScaleDuration > 0f
                    ? Mathf.Abs(1f - snapScaleFactor) / snapScaleDuration
                    : float.MaxValue;
                m_Scale = Mathf.MoveTowards(m_Scale, snapScaleFactor, speed * Time.deltaTime);
            }
            else
            {
                m_Scale = 1f;
            }

            ApplyScale();
        }

        /// <summary>立即回到基准缩放</summary>
        void ResetScale()
        {
            m_Scale = 1f;
            ApplyScale();
        }

        /// <summary>把缩放系数写到可视层（值没变就不写，避免每帧弄脏子物体变换）</summary>
        void ApplyScale()
        {
            if (m_VisualRoot == null) return;

            Vector3 scale = m_VisualBaseScale * m_Scale;
            if (m_VisualRoot.localScale == scale) return;
            m_VisualRoot.localScale = scale;
        }
        #endregion

        #region 平面移动工具
        /// <summary>按表笔当前位置与当前相机朝向重建拖拽平面并返回相机是否就绪；只在按下时调用一次，保证同一次拖拽期间平面不随相机变化。</summary>
        bool RefreshMovePlane()
        {
            if (!GlobalCameraMgr.Exists) return false;      // 管理器尚未就绪：不主动创建

            var cam = GlobalCameraMgr.Camera;
            if (cam == null) return false;

            // 平面过表笔当前位置 —— m_GrabOffset 因此落在平面内，抓起瞬间不会跳变
            m_MovePlane = new Plane(ResolvePlaneNormal(cam.transform.forward), transform.position);
            m_PlaneReady = true;
            return true;
        }

        /// <summary>拖拽平面法线：Auto 取与相机视线夹角最小的世界轴（表笔在该轴不漂移，稳在面板深度），CameraFacing 为严格屏幕平行面。</summary>
        Vector3 ResolvePlaneNormal(Vector3 viewForward)
        {
            switch (movePlaneNormal)
            {
                case MovePlaneNormal.X: return Vector3.right;
                case MovePlaneNormal.Y: return Vector3.up;
                case MovePlaneNormal.Z: return Vector3.forward;
                case MovePlaneNormal.CameraFacing: return viewForward;
                default:
                    float x = Mathf.Abs(viewForward.x);
                    float y = Mathf.Abs(viewForward.y);
                    float z = Mathf.Abs(viewForward.z);
                    if (x >= y && x >= z) return Vector3.right;
                    return y >= z ? Vector3.up : Vector3.forward;
            }
        }

        /// <summary>鼠标射线与拖拽平面的交点</summary>
        bool TryGetMousePlanePoint(out Vector3 point)
        {
            point = default;

            if (!m_PlaneReady || !GlobalCameraMgr.Exists) return false;

            var mouse = Mouse.current;
            if (mouse == null) return false;

            var cam = GlobalCameraMgr.Camera;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (!m_MovePlane.Raycast(ray, out float enter)) return false;   // 射线与平面平行
            point = ray.GetPoint(enter);
            return true;
        }
        #endregion
    }
}
