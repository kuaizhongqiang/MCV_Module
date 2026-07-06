
using System.Collections;
using MCV_Module.GlobalManager;
using UnityEngine;

namespace MCV_Module.UI
{    
    public abstract class CanvasBase : UIBase
    {
        protected override void Awake()
        {
            base.Awake();
            GlobalUiMgr.RegisterCanvas(this);
        }

        protected virtual IEnumerator Start()
        {
            bool show = false;
            ClearChildren(transform);
            OnVisible(() => show = true);
            while (!show)
                yield return null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GlobalUiMgr.UnregisterCanvas(this);
        }

        #region 核心方法
        public T GetPanel<T>() where T : PanelBase
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<T>() != null)
                    return child.GetComponent<T>();
            }

            T CreatePanel = CreatePanel<T>();
            return CreatePanel;
        }
        #endregion

        #region 工具方法
        T CreatePanel<T>() where T : PanelBase
        {
            GameObject prefab = Resources.Load<GameObject>("UI/" + typeof(T).Name);
            if (prefab == null)
                return null;
            GameObject go = Instantiate(prefab, transform);

            return go.GetComponent<T>();
        }
        #endregion
    }
}
