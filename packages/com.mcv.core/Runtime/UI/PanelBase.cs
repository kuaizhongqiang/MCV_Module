
using System.Collections.Generic;
using System.Reflection;
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
        /// 优先按 [RequireController] 特性指定类型绑定（编译期安全）；
        /// 无特性时回退 1:1 名字约定（TitlePanel → TitleController）。
        /// </summary>
        protected virtual void Start()
        {
            BindController();
        }

        void BindController()
        {
            // 优先 [RequireController] 强绑定（新面板由生成器自动写入）
            var attr = GetType().GetCustomAttribute<RequireControllerAttribute>(false);
            if (attr != null && attr.ControllerType != null)
            {
                string controllerName = attr.ControllerType.Name;
                var controller = GlobalControllerMgr.Instance.Find(controllerName);
                if (controller != null)
                {
                    controller.Bind(this);
                }
                else
                {
                    Debug.LogError($"[PanelBase] 未找到 [RequireController] 指定 Controller：{controllerName}，面板 {GetType().Name} 未绑定");
                }
                return;
            }

            // 回退：字符串命名约定（兼容历史面板）
            string typeName = GetType().Name;
            if (typeName.EndsWith("Panel"))
            {
                typeName = typeName.Substring(0, typeName.Length - "Panel".Length);
            }
            string controllerNameLegacy = typeName + "Controller";

            var controllerLegacy = GlobalControllerMgr.Instance.Find(controllerNameLegacy);
            if (controllerLegacy != null)
            {
                controllerLegacy.Bind(this);
            }
            else
            {
                Debug.LogWarning($"[PanelBase] 未找到对应 Controller：{controllerNameLegacy}，面板 {GetType().Name} 未绑定");
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
