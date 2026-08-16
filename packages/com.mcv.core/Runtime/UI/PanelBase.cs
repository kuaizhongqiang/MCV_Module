
using System.Collections.Generic;
using MCV_Module.Managers;
using UnityEngine;

namespace MCV_Module.UI
{
    public abstract class PanelBase : UIBase
    {
        protected CanvasBase m_Canvas;
        List<ComponentBase> m_Components = new List<ComponentBase>();

        /// <summary>
        /// 生命周期绑定：面板每次初始化都是全新实例，Start 恰好触发一次 = 每次初始化绑一次。
        /// 按 1:1 名字约定（TitlePanel → TitleController）从 GlobalControllerMgr 找到对应 Controller 并绑定。
        /// </summary>
        protected virtual void Start()
        {
            BindController();
        }

        void BindController()
        {
            string typeName = GetType().Name;
            if (typeName.EndsWith("Panel"))
            {
                typeName = typeName.Substring(0, typeName.Length - "Panel".Length);
            }
            string controllerName = typeName + "Controller";

            var controller = GlobalControllerMgr.Instance.Find(controllerName);
            if (controller != null)
            {
                controller.Bind(this);
            }
            else
            {
                Debug.LogWarning($"[PanelBase] 未找到对应 Controller：{controllerName}，面板 {GetType().Name} 未绑定");
            }
        }

        public void SetCanvas(CanvasBase canvas)
        {
            m_Canvas = canvas;
        }

        public void RegisterComponent(ComponentBase component)
        {
            if (!m_Components.Contains(component))
            {
                m_Components.Add(component);
            }
        }

        public void UnregisterComponent(ComponentBase component)
        {
            if (m_Components.Contains(component))
            {
                m_Components.Remove(component);
            }
        }

        public T GetUIComponent<T> () where T : ComponentBase
        {
            foreach (var component in m_Components)
            {
                if (component is T)
                {
                    return component as T;
                }
            }
            return null;
        }
    }
}
