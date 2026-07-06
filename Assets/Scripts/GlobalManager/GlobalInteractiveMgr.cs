
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interactive;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.GlobalManager
{
    public class GlobalInteractiveMgr : SingletonGlobalMgr<GlobalInteractiveMgr>
    {
        protected GlobalInteractiveMgr(){}
        [SerializeField,Header("点击阈值")] float clickThreshold = 0.2f;
        [SerializeField,Header("双击阈值")] float doubleClickThreshold = 0.2f;
        [SerializeField,Header("最远检测")] float rayMaxDistance = 300f;
        Dictionary<InteractiveBase,bool> objDict = new Dictionary<InteractiveBase,bool>();
        InteractiveBase currentObj;   
        Ray ray;
        RaycastHit raycast;
        Camera cam;
        Mouse mouse;

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
            while(cam == null)
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
            Instance.objDict.Add(interactive,interactive.IsInteractable);
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
            // 只能返回一个物体的事件
            // 需要检测物体的 移入/移出/左键点击/右键点击/双击/鼠标按下/鼠标抬起/鼠标移动
        }

        bool ifUiBlockReyCast()
        {
            // 检测鼠标是否在UI上
            return false;
        }
        #endregion
    }
}
