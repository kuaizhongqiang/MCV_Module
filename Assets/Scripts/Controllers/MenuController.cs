using MCV_Module.Data.Project;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controller
{
    public class MenuController : ControllerBase<MenuPanel>
    {
        [SerializeField] private string _experimentScenePrefix = "N_";

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

            // 跳转到对应实验场景
            string sceneName = $"{_experimentScenePrefix}{clip.id}_{index:D2}";
            GlobalSceneMgr.Instance.LoadSceneSingle(sceneName);
        }
    }
}
