using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interactive;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.GlobalManager
{
    public class GlobalInteractiveMgr : SingletonGlobalMgr<GlobalInteractiveMgr>
    {
        protected GlobalInteractiveMgr() { }
        [SerializeField, Header("点击阈值")] float clickThreshold = 0.2f;
        [SerializeField, Header("双击阈值")] float doubleClickThreshold = 0.2f;
        [SerializeField, Header("最远检测")] float rayMaxDistance = 300f;
        Dictionary<InteractiveBase, bool> objDict = new Dictionary<InteractiveBase, bool>();
        InteractiveBase currentObj;
        InteractiveBase previousObj;
        Ray ray;
        RaycastHit raycast;
        Camera cam;
        Mouse mouse;
        float lastClickTime;

        protected override void Awake()
        {
            base.Awake();
            mouse = Mouse.current;
        }

        void Update()
        {
            if (!isInit) return;
            if (ifUiBlockReyCast()) return;
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

        #region 注册
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
        #endregion

        #region 核心检测
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
                        previousObj = currentObj;
                        currentObj?.MoExit?.Invoke();
                        currentObj = interactive;
                        currentObj.MoEnter?.Invoke();
                    }

                    // 鼠标按下
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        currentObj.MoDown?.Invoke();
                    }

                    // 鼠标抬起
                    if (mouse.leftButton.wasReleasedThisFrame)
                    {
                        currentObj.MoUp?.Invoke();
                    }

                    // 鼠标点击（按下+抬起组合检测）
                    if (mouse.leftButton.wasReleasedThisFrame)
                    {
                        float timeSinceLast = Time.time - lastClickTime;
                        if (timeSinceLast < doubleClickThreshold)
                        {
                            currentObj.MoClickDouble?.Invoke();
                            lastClickTime = 0;
                        }
                        else
                        {
                            currentObj.MoClick?.Invoke();
                            lastClickTime = Time.time;
                        }
                    }

                    // 右键点击
                    if (mouse.rightButton.wasReleasedThisFrame)
                    {
                        currentObj.MoClickRight?.Invoke();
                    }

                    // 鼠标移动
                    if (mouse.delta.ReadValue().sqrMagnitude > 0.01f)
                    {
                        currentObj.MoMove?.Invoke(mouse.delta.ReadValue());
                    }

                    return;
                }
            }

            // 移出检测：没有击中任何交互物体
            if (currentObj != null)
            {
                currentObj.MoExit?.Invoke();
                previousObj = currentObj;
                currentObj = null;
            }
        }

        bool ifUiBlockReyCast()
        {
            // 检测鼠标是否在UI上
            var uiSystem = UnityEngine.EventSystems.EventSystem.current;
            if (uiSystem != null && uiSystem.IsPointerOverGameObject())
                return true;
            return false;
        }
        #endregion
    }
}
