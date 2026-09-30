using System;
using System.Collections;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Utils;

namespace MCV_Module.Steps
{
    /// <summary>弹说明面板 → 等确认 → 关面板的公共流程基类（Start / UI / Finish 共用）。</summary>
    public abstract class ConditionStepPanelBase : ConditionBase
    {
        /// <summary>说明面板控制器（由 <c>StepUIController</c> 实现 <see cref="IStepUiPanel"/>，按类型名查找）</summary>
        static IStepUiPanel ResolvePanel() =>
            GlobalControllerMgr.Instance != null
                ? GlobalControllerMgr.Instance.Find("StepUIController") as IStepUiPanel
                : null;

        /// <summary>归位：关掉可能残留的面板（跳转回来 / 上一步被打断时留下的）</summary>
        protected override void OnPrepare()
        {
            var panel = ResolvePanel();
            if (panel != null) panel.ClosePanel();
        }

        public override IEnumerator Waiting()
        {
            step.ShowAnimationsAtFirstFrame();

            var panel = ResolvePanel();
            if (panel == null)
            {
                Log.Warning($"[{GetType().Name}] {step.name} 未找到 StepUIController（是否未随 GlobalControllerMgr 创建？），跳过本步骤");
                yield break;
            }

            // 先订阅再显示：内容取不到时控制器会同步抛"已关闭"，先显示就把这个事件丢了
            bool closed = false;
            Action onClosed = () => closed = true;
            panel.OnPanelClosed += onClosed;

            panel.ShowData(step.UsingId);
            yield return WaitUntilOrForceComplete(() => closed);

            panel.OnPanelClosed -= onClosed;
            panel.ClosePanel();
        }
    }
}
