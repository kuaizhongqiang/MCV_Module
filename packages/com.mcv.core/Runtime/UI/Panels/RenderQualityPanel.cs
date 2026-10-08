using System;
using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    // WHY: 只负责把点击翻译成档位与刷新高亮/文案, 不读不写 SystemData、不动 QualitySettings; 档位是否生效由 RenderQualityController 决定, 且预制体无确认键故点击即生效。
    /// <summary>画面质量面板（View）：三个档位按钮（标清 / 高清 / 超清）+ 硬件信息提示。</summary>
    [RequireController(typeof(MCV_Module.Controllers.RenderQualityController))]
    public class RenderQualityPanel : PanelBase
    {
        [SerializeField] Button low;
        [SerializeField] Button medium;
        [SerializeField] Button high;
        [SerializeField] TextComponent infoText;

        /// <summary>硬件信息文本节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"，静态入口静默 no-op）。</summary>
        TextComponent m_InfoTextComp;

        /// <summary>当前高亮的档位（只是显示态；数据真源是 <c>SystemData.renderQuality.renderQuality</c>）。</summary>
        RenderQualityLevel quality = RenderQualityLevel.High;

        /// <summary>档位选择事件：用户点选后抛出（参数 = 点选的档位），由 RenderQualityController 订阅处理。</summary>
        public event Action<RenderQualityLevel> OnQualitySelected;

        /// <summary>当前高亮的档位。</summary>
        public RenderQualityLevel Quality => quality;

        #region 生命周期
        protected override void Awake()
        {
            // WHY: base 必须先调, canvasGroup 在 base.Awake() 里初始化, 引用缺失时 Controller 仍要能安全地 SetUIActive
            base.Awake();

            // WHY: 必须在引用校验之前解析并缓存 —— TMP 形态下本组件会卸载节点上的 Legacy Text，之后 infoText 成了"假 null"，
            // 那时再 GetComponent 会抛、静态入口也会静默 no-op（硬件信息写不进去）。
            if (infoText != null) m_InfoTextComp = infoText.GetComponent<TextComponent>();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、infoText 变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if (low == null || medium == null || high == null || (infoText == null && m_InfoTextComp == null))
            {
                Log.Error("[RenderQualityPanel] 缺少必要引用（Low / Medium / High / InfoText），请在预制体上挂全；面板将不可用", this);
                return;
            }

            low.onClick.AddListener(OnLowClick);
            medium.onClick.AddListener(OnMediumClick);
            high.onClick.AddListener(OnHighClick);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // WHY: 面板每次重建都是新实例, 按钮随面板销毁; 这里退订只为对称、防手误复用
            if (low != null) low.onClick.RemoveListener(OnLowClick);
            if (medium != null) medium.onClick.RemoveListener(OnMediumClick);
            if (high != null) high.onClick.RemoveListener(OnHighClick);
        }
        #endregion

        #region 对外接口（供 Controller 调用）
        /// <summary>初始化面板内容：探测本机硬件填 infoText，并把建议档位预高亮（由 Controller 在绑定时调一次）。</summary>
        public void InitHardwareInfo()
        {
            RenderQualityLevel suggest = GetSuggestionQuality();
            SetInfoText(BuildHardwareInfo(suggest));
            SetBtnSelected(suggest);
        }

        /// <summary>刷新选中标记（只改高亮，不回抛事件）。</summary>
        public void SetBtnSelected(RenderQualityLevel level)
        {
            quality = level;
            SetSelected(low, level == RenderQualityLevel.Low);
            SetSelected(medium, level == RenderQualityLevel.Medium);
            SetSelected(high, level == RenderQualityLevel.High);
        }

        /// <summary>设置硬件信息 / 建议档位文案。</summary>
        public void SetInfoText(string text)
        {
            if (m_InfoTextComp != null) m_InfoTextComp.SetText(text);
            else infoText.SetText(text);
        }

        /// <summary>按本机配置给建议档位（主要看显存）：无独显或显存 ≤ 2GB → 标清；≤ 6GB → 高清；> 6GB → 超清。</summary>
        public static RenderQualityLevel GetSuggestionQuality()
        {
            bool hasIndependentGraphics = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11
                                       || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
            int vramMB = SystemInfo.graphicsMemorySize;   // MB

            if (!hasIndependentGraphics || vramMB <= 2048) return RenderQualityLevel.Low;
            if (vramMB <= 6144) return RenderQualityLevel.Medium;
            return RenderQualityLevel.High;
        }

        /// <summary>档位中文名（与预制体三个按钮的文字一致：标清 / 高清 / 超清）。</summary>
        public static string GetQualityDisplayName(RenderQualityLevel level)
        {
            switch (level)
            {
                case RenderQualityLevel.Low:    return "标清";
                case RenderQualityLevel.Medium: return "高清";
                default:                        return "超清";
            }
        }
        #endregion

        #region 点击 → 事件
        void OnLowClick()    { Select(RenderQualityLevel.Low); }
        void OnMediumClick() { Select(RenderQualityLevel.Medium); }
        void OnHighClick()   { Select(RenderQualityLevel.High); }

        /// <summary>点选档位：先点亮按钮（反馈不等 Controller），再抛事件让 Controller 去应用 / 写数据。</summary>
        void Select(RenderQualityLevel level)
        {
            SetBtnSelected(level);
            OnQualitySelected?.Invoke(level);
        }
        #endregion

        #region 私有方法
        /// <summary>档位显示名（面板内文案走 key；与 <see cref="GetQualityDisplayName"/> 分开，后者供 Log 保持中文）。</summary>
        static string QualityStepName(RenderQualityLevel level)
        {
            switch (level)
            {
                case RenderQualityLevel.Low: return Lang.Get("ui.quality.name.low");
                case RenderQualityLevel.Medium: return Lang.Get("ui.quality.name.medium");
                default: return Lang.Get("ui.quality.name.high");
            }
        }

        /// <summary>硬件信息文案：系统 / CPU / 内存 / 显卡 / 显存 + 建议档位（末行 richText 蓝色，内存与显存按 GB 保留 1 位小数）。</summary>
        string BuildHardwareInfo(RenderQualityLevel suggest)
        {
            // WHY: 这一段是用户可见文案，必须走 Lang（key 已登记），否则英文态下硬件信息永远是中文。
            // 注意**不要复用 GetQualityDisplayName**：它被 RenderQualityController 的 Log 使用，而 Log 按 §5 刻意保持中文。
            return Lang.Get("ui.quality.hw.os", SystemInfo.operatingSystem) + "\n"
                 + Lang.Get("ui.quality.hw.cpu", SystemInfo.processorType) + "\n"
                 + Lang.Get("ui.quality.hw.memory", (SystemInfo.systemMemorySize / 1024f).ToString("F1")) + "\n"
                 + Lang.Get("ui.quality.hw.gpu", SystemInfo.graphicsDeviceName) + "\n"
                 + Lang.Get("ui.quality.hw.vram", (SystemInfo.graphicsMemorySize / 1024f).ToString("F1")) + "\n"
                 + Lang.Get("ui.quality.hw.suggest", QualityStepName(suggest));
        }

        // WHY: 标记路径固定为 按钮 → BG → Frame → Color（取自 BG.prefab 层级, 改预制体结构要同步这里）; 逐级判空, 层级对不上只告警不抛异常。
        /// <summary>切换选中标记。</summary>
        void SetSelected(Button btn, bool isSelected)
        {
            if (btn == null) return;

            Transform bg = btn.transform.childCount > 0 ? btn.transform.GetChild(0) : null;
            Transform frame = bg != null && bg.childCount > 1 ? bg.GetChild(1) : null;
            Transform mark = frame != null && frame.childCount > 0 ? frame.GetChild(0) : null;

            if (mark == null)
            {
                Log.Warning($"[RenderQualityPanel] 未找到选中标记（{btn.name} → BG → Frame → Color），按钮高亮失效", this);
                return;
            }

            mark.gameObject.SetActive(isSelected);
        }
        #endregion
    }
}
