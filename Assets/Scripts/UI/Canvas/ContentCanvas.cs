
using System.Collections;
using MCV_Module.UI.Panels;

namespace MCV_Module.UI.Canvas
{
    public class ContentCanvas : CanvasBase
    {
        protected override IEnumerator Start()
        {
            yield return base.Start();

            var title = GetPanel<TitlePanel>();
            var function1unction = GetPanel<FunctionPanel>();
            var copyright = GetPanel<CopyrightPanel>();

            title.SetUIActive(true);
            function1unction.SetUIActive(true);
            copyright.SetUIActive(true);
        }
    }
}

