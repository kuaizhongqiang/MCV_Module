using MCV_Module.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    /// <summary>两形态的字重 / 对齐对照表与下发（TMP 枚举底层值不连续，只能显式对照，不能整数运算）。</summary>
    internal static class TextAlignmentMap
    {
        /// <summary>Legacy <see cref="FontStyle"/> → TMP <c>FontStyles</c>。</summary>
        public static FontStyles ToTmpFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold: return FontStyles.Bold;
                case FontStyle.Italic: return FontStyles.Italic;
                case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        /// <summary>Legacy <see cref="TextAnchor"/> → TMP <c>TextAlignmentOptions</c>（9 宫格）。</summary>
        public static TextAlignmentOptions ToTmp(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.TopLeft;
            }
        }

        /// <summary>TMP <c>TextAlignmentOptions</c> → Legacy <see cref="TextAnchor"/>（<see cref="ToTmp"/> 的反向表）。</summary>
        public static TextAnchor ToAnchor(TextAlignmentOptions alignment)
        {
            switch (alignment)
            {
                case TextAlignmentOptions.TopLeft: return TextAnchor.UpperLeft;
                case TextAlignmentOptions.Top: return TextAnchor.UpperCenter;
                case TextAlignmentOptions.TopRight: return TextAnchor.UpperRight;
                case TextAlignmentOptions.Left: return TextAnchor.MiddleLeft;
                case TextAlignmentOptions.Center: return TextAnchor.MiddleCenter;
                case TextAlignmentOptions.Right: return TextAnchor.MiddleRight;
                case TextAlignmentOptions.BottomLeft: return TextAnchor.LowerLeft;
                case TextAlignmentOptions.Bottom: return TextAnchor.LowerCenter;
                case TextAlignmentOptions.BottomRight: return TextAnchor.LowerRight;
                default: return TextAnchor.UpperLeft;
            }
        }

        /// <summary>下发 Legacy 对齐；Auto / Justified 保持节点原有值（Legacy 没有两端对齐）。</summary>
        public static void ApplyTo(Text target, OverrideAlignment alignment)
        {
            switch (alignment)
            {
                case OverrideAlignment.UpperLeft: target.alignment = TextAnchor.UpperLeft; break;
                case OverrideAlignment.UpperCenter: target.alignment = TextAnchor.UpperCenter; break;
                case OverrideAlignment.UpperRight: target.alignment = TextAnchor.UpperRight; break;
                case OverrideAlignment.MiddleLeft: target.alignment = TextAnchor.MiddleLeft; break;
                case OverrideAlignment.MiddleCenter: target.alignment = TextAnchor.MiddleCenter; break;
                case OverrideAlignment.MiddleRight: target.alignment = TextAnchor.MiddleRight; break;
                case OverrideAlignment.LowerLeft: target.alignment = TextAnchor.LowerLeft; break;
                case OverrideAlignment.LowerCenter: target.alignment = TextAnchor.LowerCenter; break;
                case OverrideAlignment.LowerRight: target.alignment = TextAnchor.LowerRight; break;
                default: break;
            }
        }

        /// <summary>下发 TMP 对齐；Auto 不写（节点原有对齐由播种值兜住）。</summary>
        public static void ApplyTo(TextMeshProUGUI target, OverrideAlignment alignment)
        {
            switch (alignment)
            {
                case OverrideAlignment.UpperLeft: target.alignment = TextAlignmentOptions.TopLeft; break;
                case OverrideAlignment.UpperCenter: target.alignment = TextAlignmentOptions.Top; break;
                case OverrideAlignment.UpperRight: target.alignment = TextAlignmentOptions.TopRight; break;
                case OverrideAlignment.MiddleLeft: target.alignment = TextAlignmentOptions.Left; break;
                case OverrideAlignment.MiddleCenter: target.alignment = TextAlignmentOptions.Center; break;
                case OverrideAlignment.MiddleRight: target.alignment = TextAlignmentOptions.Right; break;
                case OverrideAlignment.LowerLeft: target.alignment = TextAlignmentOptions.BottomLeft; break;
                case OverrideAlignment.LowerCenter: target.alignment = TextAlignmentOptions.Bottom; break;
                case OverrideAlignment.LowerRight: target.alignment = TextAlignmentOptions.BottomRight; break;
                case OverrideAlignment.Justified: target.alignment = TextAlignmentOptions.Justified; break;
                default: break;
            }
        }
    }
}
