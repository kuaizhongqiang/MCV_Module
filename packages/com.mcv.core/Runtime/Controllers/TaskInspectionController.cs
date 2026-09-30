// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-14）—— 请按需补充业务代码

using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Objects.Interactives;
using MCV_Module.Objects.Interactives.TaskObj;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 不用接触事件（InspectionProbeEventData）驱动提示 —— 接触在拖拽途中一路翻转，而提示要的是"鼠标指到哪个物体"与"真正贴住了哪个点"
    /// <summary>检测（测量）面板调度：浮动框走悬停（GlobalInteractionEventData Enter/Exit）、操作记录走吸附（InspectionProbeSnapEventData），开/关与文案全在控制器。</summary>
    public class TaskInspectionController : ControllerBase<MCV_Module.UI.Panels.TaskInspectionPanel>
    {
        // WHY: 原 [SerializeField] InspectionManager mgr 在全类从未被读取（死字段）——控制器已不挂场景，序列化引用一并去掉；
        //      将来真要用检测管理器，走 InspectionManager.Instance（InstManagerBase 的静态单例），不要再回到场景引用。

        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();

            // Controller 由 GlobalControllerMgr 常驻持有 → 一次订阅即可（EventBus 内部去重）
            EventBus<GlobalInteractionEventData>.Subscribe(OnGlobalInteraction);
            EventBus<InspectionProbeSnapEventData>.Subscribe(OnProbeSnapped);
            EventBus<AllStepsCompletedEvent>.Subscribe(OnAllStepsCompleted);
        }

        public override void OnDispose()
        {
            EventBus<GlobalInteractionEventData>.Unsubscribe(OnGlobalInteraction);
            EventBus<InspectionProbeSnapEventData>.Unsubscribe(OnProbeSnapped);
            EventBus<AllStepsCompletedEvent>.Unsubscribe(OnAllStepsCompleted);
            base.OnDispose();
        }

        public override void OnViewBound()
        {
            // WHY: View 是 Unity 对象，销毁后要按伪 null（== null）判断，不能用 ?.（会漏判）；面板随 Canvas 重建，悬停态不可继承
            if (View == null) return;

            View.CloseTips();
            View.ClearOpRecord();
        }
        #endregion

        #region 浮动框：悬停驱动
        // WHY: 判定放在控制器而不是物体自己 —— MVC 里 View 不认场景对象，"指到哪个物体"统一由控制器解析（同 TaskStructureController）
        /// <summary>悬停驱动浮动框：鼠标移入检测点 / 表笔就显示它的名字，移出就收起。</summary>
        void OnGlobalInteraction(GlobalInteractionEventData e)
        {
            // 事件载荷是池化的、同步分发，出了回调就归还：这里只取名字，不留引用
            if (e == null || View == null) return;
            if (e.Type != GlobalInteractionType.Enter && e.Type != GlobalInteractionType.Exit) return;

            string name = ResolveHoverName(e.Target);
            if (name == null) return;    // 不是本任务关心的物体：不动浮动框（别把正在显示的提示关掉）

            if (e.Type == GlobalInteractionType.Enter) View.ShowTips(name);
            else View.CloseTips();
        }

        /// <summary>悬停物体的显示名：只认检测点与表笔，其它返回 null（= 不处理）。</summary>
        static string ResolveHoverName(InteractiveBase target)
        {
            switch (target)
            {
                case InspectionElementPointObj point: return point.GetPointName();
                case InspectionProbeObj probe: return probe.ProbeName;
                default: return null;
            }
        }
        #endregion

        #region 操作记录：吸附驱动
        /// <summary>吸附住 → 写一条操作记录；脱离吸附不动记录（表笔失败回弹时文字不该变）。</summary>
        void OnProbeSnapped(InspectionProbeSnapEventData e)
        {
            if (e == null || !e.IsSnapped || e.Point == null) return;
            if (View == null) return;

            View.SetOpRecord(BuildOpRecordLine(e.ProbeType, e.Point));
        }

        /// <summary>操作记录文案：带动作的整句 —— 如 <c>"红表笔"接入接触器_A1点</c>。</summary>
        static string BuildOpRecordLine(InspectionProbeType probeType, InspectionElementPointObj point)
        {
            return $"\"{ChnNameMap.Get(probeType)}\"接入{point.GetPointName()}点";
        }
        #endregion

        #region 链尾收尾：上报成绩 + 返回菜单
        // WHY: 挂在链尾事件而不是 Finish 按钮回调 —— Finish 没配 usingId（直接过）时也要收尾，且导航与成绩属 C 层、不塞进步骤条件
        /// <summary>步骤链走完：上报本次测量计分单元 -> 返回菜单。</summary>
        void OnAllStepsCompleted(AllStepsCompletedEvent e)
        {
            ReportMeasureScore();
            ReturnToMenu();
        }

        /// <summary>上报本次测量计分单元：completed=true 二值口径（半途退出 0 分），单价由 ScoreCalculator 按类别均分算（见 design_ai/Score.md §7）。</summary>
        static void ReportMeasureScore()
        {
            var clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                Log.Warning("[TaskInspectionController] 没有当前 ProjectClip，测量成绩无法上报");
                return;
            }

            var data = GlobalDataMgr.GetTaskData(TaskType.Inspection) as TaskInspectionData;
            if (data == null)
            {
                Log.Warning($"[TaskInspectionController] {clip.id} 没有检测任务数据，测量成绩无法上报");
                return;
            }

            var task = GlobalDataMgr.ReportScoredUnit(clip.id, clip.displayName, data.id, data.displayName,
                TaskType.Inspection, completed: true);

            if (task == null)
            {
                Log.Warning($"[TaskInspectionController] {clip.displayName} 的测量成绩上报被忽略（该任务类型不计分？）");
                return;
            }

            Log.Info($"[TaskInspectionController] {clip.displayName}·{data.displayName} 完成，已上报计分单元：{task.score:0.##}/{task.fullScore:0.##} 分");
        }

        // WHY: 换 Canvas 归 GlobalUIMgr，这里只发状态事件、不要自己 SetActive Canvas
        /// <summary>回菜单：只发 SceneStateChangeEventData(Menu)，由 GlobalUIMgr 统一换 Canvas（同 ContentFunctionController 的返回入口）。</summary>
        static void ReturnToMenu()
        {
            Log.Info("[TaskInspectionController] 测量链结束，返回菜单页");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Menu));
        }
        #endregion
    }
}
