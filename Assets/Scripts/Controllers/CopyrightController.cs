using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class CopyrightController : ControllerBase<CopyrightPanel>
    {
        protected override void OnViewBound()
        {
            View.SetCopyright("Copyright © 2026 MCV_Module");
        }
    }
}
