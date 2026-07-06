using System.Collections.Generic;
using MCV_Module.Data.Project;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class MenuController : ControllerBase<MenuPanel>
    {
        protected override void OnViewBound()
        {
            var clip = GlobalDataMgr.GetProjectClip();
            if (clip != null && clip.Tasks.Count > 0)
            {
                var taskNames = clip.Tasks.ConvertAll(t => t.displayName);
                View.CreateExperimentButtons(taskNames.ToArray(), OnExperimentSelected);
            }
        }

        private void OnExperimentSelected(int index)
        {
            var clip = GlobalDataMgr.GetProjectClip();
            if (clip == null || index >= clip.Tasks.Count) return;

            var task = clip.Tasks[index];
            // TODO: 跳转到对应 Task 场景
        }
    }
}
