
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.GlobalManager
{
    public class GlobalCameraMgr : SingletonGlobalMgr<GlobalCameraMgr>
    {
        protected GlobalCameraMgr(){}
        public static Camera Camera
        {
            get
            {
                if (Instance.m_Camera == null)
                {
                    Instance.m_Camera = GetCamera();
                }
                return Instance.m_Camera;
            }
        }
        Camera m_Camera;
        

        protected override void Awake()
        {
            base.Awake();
        }

        protected override IEnumerator DelayInit()
        {
            while(!Camera)
            {
                yield return null;
            }

            isInit = true;
        }

        #region 核心方法
        static Camera GetCamera()
        {
            Camera[] camsAll = Camera.allCameras;
            List<Camera> mainCams = new List<Camera>();
            for (int i = 0; i < camsAll.Length; i++)
            {
                if (camsAll[i].tag == "MainCamera")
                {
                    mainCams.Add(camsAll[i]);
                }
            }
            if (mainCams.Count == 1) return mainCams[0];
            else if (mainCams.Count == 0) return CreateMain();
            else
            {
                for (int i = 1; i < mainCams.Count ; i++)
                {
                    Destroy(mainCams[i].gameObject);
                }
                return mainCams[0];
            }
        }

        static Camera CreateMain()
        {
            var camPrefab = Resources.Load<GameObject>("MainCamera");
            var cam = Instantiate(camPrefab,Instance.transform).GetComponent<Camera>();

            return cam;
        }
        #endregion
    }
}