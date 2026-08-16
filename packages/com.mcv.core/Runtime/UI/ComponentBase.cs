

using MCV_Module.Models;

namespace MCV_Module.UI
{
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
