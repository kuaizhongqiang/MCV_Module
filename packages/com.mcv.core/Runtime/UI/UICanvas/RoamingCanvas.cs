
using MCV_Module.Managers;
using MCV_Module.Utils;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>漫游页画布：AI 开启时挂 AI 对话面板（业务面板已移除，待重写）。</summary>
    public class RoamingCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild()
        {
            // 目标 Canvas 已由 SceneStateChangeEventData 选定，这里不再判断状态
            if (GlobalAiMgr.Instance.IsAiEnabled)
            {
                var aiPanel = GetPanel<AiDialogPanel>();
                Log.Info("RoamingCanvas.OnRebuild: " + aiPanel);
            }
        }
    }
}
