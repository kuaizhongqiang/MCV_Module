using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Objects.Interactives;
using MCV_Module.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.Managers
{
    public class GlobalInteractiveMgr : SingletonGlobalMgr<GlobalInteractiveMgr>
    {
        #region 参数
        [SerializeField, Header("双击阈值")] float doubleClickThreshold = 0.2f;
        [SerializeField, Header("最远检测")] float rayMaxDistance = 300f;
        Dictionary<InteractiveBase, bool> objDict = new Dictionary<InteractiveBase, bool>();
        InteractiveBase currentObj;
        /// <summary> 当前鼠标悬停的交互物体（供元素查询连线目标等） </summary>
        public InteractiveBase Current { get => currentObj; }
        Ray ray;
        RaycastHit raycast;
        Camera cam;
        Mouse mouse;
        float lastClickTime = -1f;
        /// <summary>鼠标是否静止（由 GlobalInputMgr 的 MouseMoveStateEventData 同步）</summary>
        bool m_MouseIdle;
        #endregion

        #region 生命周期
        protected GlobalInteractiveMgr() { }

        protected override void Awake()
        {
            base.Awake();
            mouse = Mouse.current;
            EventBus<MouseMoveStateEventData>.Subscribe(OnMouseMoveStateChanged);
        }

        protected override void OnDestroy()
        {
            EventBus<MouseMoveStateEventData>.Unsubscribe(OnMouseMoveStateChanged);
            base.OnDestroy();
        }

        void Update()
        {
            if (!isInit) return;

            // WHY: 静止判定只在 GlobalInputMgr 做，这里不再读 delta，避免两处各判一套
            // （代价：物体在静止光标下移动时，移入/移出延迟到下一次输入才触发 —— 教学场景可接受）
            if (m_MouseIdle
                && !mouse.leftButton.wasPressedThisFrame
                && !mouse.leftButton.wasReleasedThisFrame
                && !mouse.rightButton.wasPressedThisFrame
                && !mouse.rightButton.wasReleasedThisFrame) return;

            if (ifUiBlockRayCast()) return;
            CoreDetect();
        }

        protected override IEnumerator DelayInit()
        {
            while (cam == null)
            {
                cam = GlobalCameraMgr.Camera;
                yield return null;
            }
            isInit = true;
        }

        /// <summary>同步鼠标静止状态（GlobalInputMgr 发布）；恢复移动后由下一帧 Update 自然补检测。</summary>
        void OnMouseMoveStateChanged(MouseMoveStateEventData e)
        {
            if (e == null) return;
            m_MouseIdle = e.IsIdle;
        }
        #endregion

        #region 静态方法
        public static void Register(InteractiveBase interactive)
        {
            if (Instance.objDict.ContainsKey(interactive)) return;
            Instance.objDict.Add(interactive, interactive.IsInteractable);
        }

        public static void Unregister(InteractiveBase interactive)
        {
            if (!Instance.objDict.ContainsKey(interactive)) return;
            Instance.objDict.Remove(interactive);
        }

        /// <summary>按类型收集已登记的交互物（写入调用方列表，不产生 GC）；替代 FindObjectsOfType / 遍历场景。</summary>
        public static void CollectRegistered<T>(List<T> results) where T : InteractiveBase
        {
            if (results == null) return;
            results.Clear();

            var mgr = SafeInstance;   // 安全取实例：不触发创建，退出期返回 null
            if (mgr == null) return;

            foreach (var kv in mgr.objDict)
            {
                if (kv.Key is T typed) results.Add(typed);
            }
        }
        #endregion

        #region 私有方法
        /// <summary>每帧射线检测：命中即直接派发该物体的 Mo* 事件，并发布池化的全局交互事件供订阅。</summary>
        void CoreDetect()
        {
            ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out raycast, rayMaxDistance))
            {
                var interactive = raycast.collider.GetComponent<InteractiveBase>();
                if (interactive != null && interactive.IsInteractable)
                {
                    // 移入检测
                    if (currentObj != interactive)
                    {
                        if (currentObj != null)
                        {
                            currentObj.InvokeMoExit();
                            PublishInteraction(currentObj, GlobalInteractionType.Exit);
                        }
                        currentObj = interactive;
                        currentObj.InvokeMoEnter();
                        PublishInteraction(interactive, GlobalInteractionType.Enter);
                    }

                    // 鼠标按下
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        currentObj.InvokeMoDown();
                        PublishInteraction(currentObj, GlobalInteractionType.Down);
                    }

                    // 鼠标抬起与点击（按下+抬起组合检测）
                    if (mouse.leftButton.wasReleasedThisFrame)
                    {
                        currentObj.InvokeMoUp();
                        PublishInteraction(currentObj, GlobalInteractionType.Up);

                        float timeSinceLast = Time.time - lastClickTime;
                        if (timeSinceLast < doubleClickThreshold)
                        {
                            currentObj.InvokeMoClickDouble();
                            PublishInteraction(currentObj, GlobalInteractionType.ClickDouble);
                            lastClickTime = 0;
                        }
                        else
                        {
                            currentObj.InvokeMoClick();
                            PublishInteraction(currentObj, GlobalInteractionType.Click);
                            lastClickTime = Time.time;
                        }
                    }

                    // 右键点击
                    if (mouse.rightButton.wasReleasedThisFrame)
                    {
                        currentObj.InvokeMoClickRight();
                        PublishInteraction(currentObj, GlobalInteractionType.ClickRight);
                    }

                    // 鼠标移动
                    Vector2 delta = mouse.delta.ReadValue();
                    if (delta.sqrMagnitude > 0.01f)
                    {
                        currentObj.InvokeMoMove(delta);
                        PublishInteraction(currentObj, GlobalInteractionType.Move, delta);
                    }

                    return;
                }
            }

            // 移出检测：没有击中任何交互物体
            if (currentObj != null)
            {
                currentObj.InvokeMoExit();
                PublishInteraction(currentObj, GlobalInteractionType.Exit);
                currentObj = null;
            }

            // 全局点击（空白/非交互物体）：左键释放时发布 Target=null，供连线等全局逻辑判定取消
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                PublishInteraction(null, GlobalInteractionType.Click);
            }
        }

        /// <summary>发布全局交互事件（对象池复用；同步分发，Publish 返回后立即回收）。</summary>
        void PublishInteraction(InteractiveBase target, GlobalInteractionType type, Vector2 delta = default)
        {
            var e = GlobalInteractionEventData.Get(target, type, delta);
            EventBus<GlobalInteractionEventData>.Publish(e);
            e.Release();
        }
        #endregion

        #region 工具方法
        /// <summary>检测鼠标是否在 UI 上（在 UI 上时跳过场景射线交互）</summary>
        bool ifUiBlockRayCast()
        {
            var uiSystem = UnityEngine.EventSystems.EventSystem.current;
            if (uiSystem != null && uiSystem.IsPointerOverGameObject())
                return true;
            return false;
        }
        #endregion
    }
}
