
using System.Collections;
using MCV_Module.UI.Panels;

namespace MCV_Module.UI.Canvas
{
    public class StartCanvas : CanvasBase
    {
        protected override IEnumerator Start()
        {
            yield return base.Start();

            var startPanel = GetPanel<StartPanel>();
            startPanel.SetUIActive(true);
        }
    }
}

