using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;

namespace MCV_Module.Controllers
{
    // WHY: 面板只负责播放与显示、不读数据也不认 PrincipleStruct，视频名必须由本控制器取好再传，别让 View 反向依赖 Model
    /// <summary>实验原理控制器：取当前 ProjectClip 的原理视频名，交给面板播放（Controller → View 单向）。</summary>
    public class TaskPrincipleController : ControllerBase<TaskPrinciplePanel>
    {
        public override void OnViewBound()
        {
            string videoName = ResolveVideoName();
            if (string.IsNullOrEmpty(videoName)) return;

            View.LoadVideo(videoName);
        }

        /// <summary>解析当前项目的原理视频名：当前 ProjectClip → TaskPrinciple 数据（每个任务只有一条原理视频）。</summary>
        string ResolveVideoName()
        {
            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                Log.Warning("[TaskPrincipleController] 当前没有 ProjectClip 数据");
                return "";
            }

            var data = clip.GetTaskData(TaskType.Principle) as TaskPrincipleData;
            if (data == null || data.principleStructs == null || data.principleStructs.Count == 0)
            {
                Log.Warning("[TaskPrincipleController] 当前 ProjectClip 没有实验原理数据");
                return "";
            }

            return data.principleStructs[0].videoName ?? "";
        }
    }
}
