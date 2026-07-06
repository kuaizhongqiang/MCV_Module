
using System.Collections;
using System.Collections.Generic;
using MCV_Module.UI;

namespace MCV_Module.GlobalManager
{
    public class GlobalUiMgr : SingletonGlobalMgr<GlobalUiMgr>
    {
        protected GlobalUiMgr(){}
        List<CanvasBase> m_CanvasList = new List<CanvasBase>();

        protected override void Awake()
        {
            base.Awake();
        }

        protected override IEnumerator DelayInit()
        {
            yield break;
        }
        
        #region 静态方法
        public static void RegisterCanvas(CanvasBase canvas)
        {
            if (Instance.m_CanvasList.Contains(canvas)) return;
            Instance.m_CanvasList.Add(canvas);
        }

        public static void UnregisterCanvas(CanvasBase canvas)
        {
            if (!Instance.m_CanvasList.Contains(canvas)) return;
            Instance.m_CanvasList.Remove(canvas);
        }
        #endregion

        #region 工具方法
        // 获取Canvas
        public static T GetCanvas<T>() where T : CanvasBase
        {
            return Instance.m_CanvasList.Find(x => x is T) as T;
        }
        // 获取指定Canvas的Panel
        public static T GetPanel<T>(CanvasBase canvas) where T : PanelBase
        {
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var child = canvas.transform.GetChild(i);
                if (child.GetComponent<T>() != null)
                    return child.GetComponent<T>();
            }
            return null;
        }
        // 获取第一个Panel
        public static T GetPanel<T>() where T : PanelBase
        {
            for (int i = 0; i < Instance.m_CanvasList.Count; i++)
            {
                var canvas = Instance.m_CanvasList[i];
                var panel = GetPanel<T>(canvas);
                if (panel != null)
                    return panel;
            }
            return null;
        }
        #endregion
    }
}