
using System.Collections;
using MCV_Module.UI.Panels;

namespace MCV_Module.UI.Canvas
{
    public class LoadingCanvas : CanvasBase
    {
        protected override IEnumerator Start()
        {
            yield return base.Start();

            var loading = GetPanel<LoadingPanel>();

            loading.SetUIActive(true);
        }
    }
}

