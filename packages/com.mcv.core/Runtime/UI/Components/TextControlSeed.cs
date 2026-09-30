using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    /// <summary>被换掉的 Legacy 控件的"节点既有设置"快照：可原样回挂（兜底），也可换算后播种到 TMP。</summary>
    internal struct LegacySeed
    {
        public bool wrap;
        public VerticalWrapMode verticalOverflow;
        public bool raycast;
        public bool richText;
        public TextAnchor alignment;

        /// <summary>抄出既有设置；必须在禁用 / 卸载之前调用（Destroy 帧末生效，之后引用即"假 null"）。</summary>
        public static LegacySeed Capture(Text t)
        {
            return new LegacySeed
            {
                wrap = t.horizontalOverflow == HorizontalWrapMode.Wrap,
                verticalOverflow = t.verticalOverflow,
                raycast = t.raycastTarget,
                richText = t.supportRichText,
                alignment = t.alignment,
            };
        }

        /// <summary>回挂兜底：原样写回 Legacy 控件。</summary>
        public void ApplyTo(Text t)
        {
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = verticalOverflow;
            t.raycastTarget = raycast;
            t.supportRichText = richText;
            t.alignment = alignment;
        }

        /// <summary>换向成功：换算成 TMP 设置（raycastTarget 与 richText 必须搬）。</summary>
        public void ApplyTo(TextMeshProUGUI t)
        {
            t.enableWordWrapping = wrap;
            t.overflowMode = verticalOverflow == VerticalWrapMode.Truncate ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
            t.raycastTarget = raycast;
            t.richText = richText;
            t.alignment = TextAlignmentMap.ToTmp(alignment);
        }
    }

    /// <summary>被换掉的 TMP 控件的"节点既有设置"快照（与 <see cref="LegacySeed"/> 对称）。</summary>
    internal struct TmpSeed
    {
        public bool wrap;
        public TextOverflowModes overflow;
        public bool raycast;
        public bool richText;
        public TextAlignmentOptions alignment;

        /// <summary>抄出既有设置；必须在禁用 / 卸载之前调用。</summary>
        public static TmpSeed Capture(TextMeshProUGUI t)
        {
            return new TmpSeed
            {
                wrap = t.enableWordWrapping,
                overflow = t.overflowMode,
                raycast = t.raycastTarget,
                richText = t.richText,
                alignment = t.alignment,
            };
        }

        /// <summary>回挂兜底：原样写回 TMP 控件。</summary>
        public void ApplyTo(TextMeshProUGUI t)
        {
            t.enableWordWrapping = wrap;
            t.overflowMode = overflow;
            t.raycastTarget = raycast;
            t.richText = richText;
            t.alignment = alignment;
        }

        /// <summary>换向成功：换算成 Legacy 设置（对齐 / 换行 / 溢出 / 点击 / 富文本；字体不搬）。</summary>
        public void ApplyTo(Text t)
        {
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = overflow == TextOverflowModes.Truncate ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            t.raycastTarget = raycast;
            t.supportRichText = richText;
            t.alignment = TextAlignmentMap.ToAnchor(alignment);
        }
    }
}
