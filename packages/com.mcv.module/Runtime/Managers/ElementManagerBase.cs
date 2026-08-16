using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Objects.Interactives.Elements;
using MCV_Module.Objects.Tools;
using MCV_Module.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.Managers
{
    public abstract class ElementManagerBase : SingletonBase
    {
        #region 参数
        static ElementManagerBase instance;
        protected Dictionary<string, ElementObjBase> elements = new Dictionary<string, ElementObjBase>();
        protected Dictionary<string, ElementLineObj> lines = new Dictionary<string, ElementLineObj>();

        /// <summary>连线状态机状态</summary>
        DrawState state = DrawState.Idle;
        /// <summary>连线起点（当前正在连线的起始点）</summary>
        ElementPointObj startPoint;
        /// <summary>临时拖线对象</summary>
        GameObject tmpLine;

        [SerializeField, Header("连线：虚拟平面距离"), Tooltip("临时线自由端所在屏幕平行平面到相机的距离；<=0 时兜底用相机到起点距离")]
        float planeDistance = 0f;

        [SerializeField, Header("连线：临时线"), Tooltip("临时线参数")]
        LineDrawData lineDrawData = new LineDrawData();

        /// <summary>临时线端点上次位置（位移超阈值才重建网格，避免鼠标静止时每帧全量重建）</summary>
        Vector3 lastTmpEnd;
        bool hasTmpEnd;
        /// <summary>端点位移平方阈值（约 0.0001 世界单位）</summary>
        const float TmpEndEpsilonSqr = 1e-8f;
        #endregion

        #region 生命周期
        /// <summary>
        /// 在 Awake 中绑定静态 instance。
        /// 父物体 Awake 先于子物体执行，因此元素子物体自注册（RegisterElement/RegisterLine）时 instance 已可用。
        /// </summary>
        protected virtual void Awake()
        {
            instance = this;
        }

        protected override IEnumerator DelayInit()
        {
            yield return null;
            EventBus<GlobalInteractionEventData>.Subscribe(OnGlobalInteraction);
            isInit = true;
        }

        protected virtual void OnDestroy()
        {
            EventBus<GlobalInteractionEventData>.Unsubscribe(OnGlobalInteraction);
        }

        /// <summary>连线状态机每帧驱动：Drawing 状态下更新临时线端点（吸附 / 虚拟平面）。
        /// 端点位移超过阈值才重建网格，避免鼠标静止时每帧全量重建。</summary>
        void Update()
        {
            if (instance != this || !isInit) return;
            if (state != DrawState.Drawing || startPoint == null || tmpLine == null) return;

            // 悬停合法目标 → 吸附；否则 → 自由端在虚拟平面上跟随鼠标
            var hover = GlobalInteractiveMgr.Instance != null ? GlobalInteractiveMgr.Instance.Current as ElementPointObj : null;
            var data = GetTmpLineData();
            Vector3 end = hover != null && hover != startPoint ? hover.transform.position : PlaneProject();

            if (!hasTmpEnd || (end - lastTmpEnd).sqrMagnitude > TmpEndEpsilonSqr)
            {
                startPoint.UpdateTmpLine(tmpLine, end, data);
                lastTmpEnd = end;
                hasTmpEnd = true;
            }
        }
        #endregion

        #region 静态方法
        public static ElementManagerBase Instance { get => instance; set => instance = value; }

        /// <summary>
        /// 获取元器件
        /// </summary>
        /// <typeparam name="EL"> 元器件基类</typeparam>
        /// <param name="id"> 元器件id</param>
        /// <returns></returns>
        public static EL GetElement<EL>(string id) where EL : ElementObjBase
        {
            if (instance == null || !instance.elements.ContainsKey(id)) return null;
            return instance.elements[id] as EL;
        }

        /// <summary>
        /// 获取连线
        /// </summary>
        /// <typeparam name="LI"> 连线基类</typeparam>
        /// <param name="id"> 连线id</param>
        /// <returns></returns>
        public static LI GetLine<LI>(string id) where LI : ElementLineObj
        {
            if (instance == null || !instance.lines.ContainsKey(id)) return null;
            return instance.lines[id] as LI;
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 注册元器件。操作本 Manager 实例的字典（而非静态 instance），
        /// 避免调用方通过 GetComponentInParent 找到的 Manager 与静态 instance 不同步导致空引用。
        /// </summary>
        /// <param name="element"> 元器件物体 </param>
        public void RegisterElement(ElementObjBase element)
        {
            if (element == null || element.Data == null) return;
            if (elements.ContainsKey(element.Data.id)) return;
            elements.Add(element.Data.id, element);
        }

        /// <summary>
        /// 注销元器件
        /// </summary>
        /// <param name="element"></param>
        public void UnregisterElement(ElementObjBase element)
        {
            if (element == null || element.Data == null) return;
            if (!elements.ContainsKey(element.Data.id)) return;
            elements.Remove(element.Data.id);
        }

        /// <summary>
        /// 注册连线
        /// </summary>
        /// <param name="line"> 线段物体 </param>
        public void RegisterLine(ElementLineObj line)
        {
            if (line == null || line.Data == null) return;
            if (lines.ContainsKey(line.Data.id)) return;
            lines.Add(line.Data.id, line);
        }

        /// <summary>
        /// 注销连线
        /// </summary>
        /// <param name="line"></param>
        public void UnregisterLine(ElementLineObj line)
        {
            if (line == null || line.Data == null) return;
            if (!lines.ContainsKey(line.Data.id)) return;
            lines.Remove(line.Data.id);
        }

        /// <summary>供任务数据覆盖虚拟平面距离（<=0 时回到兜底逻辑）</summary>
        public void SetPlaneDistance(float distance)
        {
            planeDistance = distance;
        }

        /// <summary>取消当前临时连线（跳转/离开连线步骤时复位状态机到 Idle）</summary>
        public void CancelDrawing()
        {
            if (startPoint != null) startPoint.DestroyLine();
            startPoint = null;
            tmpLine = null;
            hasTmpEnd = false;
            state = DrawState.Idle;
        }

        /// <summary>已连接的常驻线集合（供步骤条件轮询连线完成）</summary>
        public IEnumerable<ElementLineObj> GetLines() => lines.Values;
        #endregion

        #region 私有方法
        /// <summary>
        /// 连线状态机：订阅全局交互事件，处理 Click 类型。
        /// Idle → 点击任意 point 开始连线；Drawing → 点击合法目标提交 / 点击起点、其他 collider、空白取消。
        /// </summary>
        void OnGlobalInteraction(GlobalInteractionEventData data)
        {
            if (instance != this || !isInit) return;
            if (data == null || data.Type != GlobalInteractionType.Click) return;
            var target = data.Target as ElementPointObj;

            switch (state)
            {
                case DrawState.Idle:
                    // 点击任意 point → 开始连线（临时线用管理器统一参数）
                    if (target == null) return;
                    startPoint = target;
                    tmpLine = startPoint.CreateTmpLine(GetTmpLineData());
                    hasTmpEnd = false;
                    state = DrawState.Drawing;
                    break;

                case DrawState.Drawing:
                    // 点击合法目标 → 提交；点击起点/其他 collider/空白 → 取消
                    if (target != null && target != startPoint)
                    {
                        startPoint.CreateLine(target);
                    }
                    else
                    {
                        startPoint.DestroyLine();
                    }
                    tmpLine = null;
                    startPoint = null;
                    hasTmpEnd = false;
                    state = DrawState.Idle;
                    break;
            }
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 临时线绘制参数：优先用管理器配置的 lineDrawData；未配置（width<=0 或分段<1）时回退到起点点的绘制参数。
        /// </summary>
        LineDrawData GetTmpLineData()
        {
            if (lineDrawData.width <= 0 || lineDrawData.sectionSegments < 1)
            {
                return startPoint != null ? startPoint.GetDrawData() : lineDrawData;
            }
            return lineDrawData;
        }

        /// <summary>鼠标射线与屏幕平行虚拟平面的交点（法线=相机 forward，距离由配置/任务指定，兜底=相机到起点）</summary>
        Vector3 PlaneProject()
        {
            Camera cam = GlobalCameraMgr.Camera;
            if (cam == null) return startPoint.transform.position; // 相机未就绪时临时线收缩回起点
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            float dist = planeDistance > 0f
                ? planeDistance
                : Vector3.Distance(cam.transform.position, startPoint.transform.position);
            var plane = new Plane(cam.transform.forward, cam.transform.position + cam.transform.forward * dist);
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            return ray.GetPoint(dist); // 射线与平面平行（理论上不会发生）时兜底
        }
        #endregion

        #region 其他类
        /// <summary>连线状态机状态</summary>
        enum DrawState { Idle, Drawing }
        #endregion
    }
}
