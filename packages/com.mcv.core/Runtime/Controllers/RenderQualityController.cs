using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;

namespace MCV_Module.Controllers
{
    // WHY: qualitySetted 只在用户点选档位后（OnQualitySelected）写——面板「创建 / 显示」时绝不能写，否则第一次进开始页就标记为已设置，设置面板再也不会弹
    /// <summary>画面质量控制器：连接 RenderQualityPanel 与 SystemData.renderQuality / Unity QualitySettings（读时机在 StartCanvas.OnRebuild，应用端在 GlobalDataMgr.DelayInit）。</summary>
    public class RenderQualityController : ControllerBase<RenderQualityPanel>
    {
        public override void OnViewBound()
        {
            // 先清后加：每次画布重建都会绑定一个全新面板实例
            View.OnQualitySelected -= OnQualitySelected;
            View.OnQualitySelected += OnQualitySelected;

            // 硬件信息 + 建议档位文案，并按建议档位预高亮（探测逻辑在 View 的 GetSuggestionQuality()：主要看显存）
            View.InitHardwareInfo();
        }

        public override void OnDispose()
        {
            if (View != null) View.OnQualitySelected -= OnQualitySelected;
            base.OnDispose();
        }

        /// <summary>写时机：用户点选档位 = 确认 —— 应用画质 → 写数据（renderQuality + qualitySetted = true）→ 落盘 JSON → 收起面板。</summary>
        void OnQualitySelected(RenderQualityLevel level)
        {
            GlobalDataMgr.SetRenderQuality(level);

            Log.Info($"[RenderQualityController] 画面质量已设为【{RenderQualityPanel.GetQualityDisplayName(level)}】，已写回 SystemData.json");

            // 收起面板：淡出动画播完自动 SetActive(false)（见 UIBase.SetUIActive）
            View.SetUIActive(false);
        }
    }
}
