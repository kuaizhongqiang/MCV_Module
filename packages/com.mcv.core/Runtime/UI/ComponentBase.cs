

using MCV_Module.Models;

namespace MCV_Module.UI
{
    /// <summary>UI 最小单元基类：找到所属面板并自注册，面板据此按类型取组件。</summary>
    public abstract class ComponentBase : UIBase
    {
        public virtual ComponentType ComponentType{get;}
        protected PanelBase panelBase;        
        protected override void Awake()
        {
            base.Awake();
            panelBase = GetComponentInParent<PanelBase>();
        }

        protected void Start()
        {            
            if (panelBase != null)
            {
                panelBase.RegisterComponent(this);
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (panelBase != null)
            {
                panelBase.UnregisterComponent(this);
            }
        }
    }
}
