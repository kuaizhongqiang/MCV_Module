
namespace MCV_Module.UI
{
    public abstract class PanelBase : UIBase
    {
        CanvasBase canvas;
        public CanvasBase Canvas
        {
            get
            {
                if (canvas == null)
                {
                    canvas = GetComponentInParent<CanvasBase>();
                }
                return canvas;
            }
        }
    }
}