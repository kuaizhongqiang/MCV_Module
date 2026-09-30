
using MCV_Module.Models;
using MCV_Module.Utils;
using MCV_Module.UI.Panels;
using UnityEngine;
using MCV_Module.Managers;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>开始页画布：装配开始面板；本期首次启动（未设置过画质）时再弹画质设置面板。</summary>
    public class StartCanvas : CanvasBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnRebuild()
        {
            // 目标 Canvas 已由 SceneStateChangeEventData 选定，这里不再判断状态
            var startPanel = GetPanel<StartPanel>();
            // WHY: 画质应用端不在这里（DelayInit 已按 JSON 应用过）；这里只读 qualitySetted 决定弹不弹设置面板，不写任何数据
            if (!GlobalDataMgr.IsRenderQualitySetted())
            {
                GetPanel<RenderQualityPanel>();
                Log.Info("[StartCanvas] 尚未设置过画面质量，已弹出设置面板");
            }

            Log.Info("StartCanvas.OnRebuild: " + startPanel + " ");
        }
    }
}
