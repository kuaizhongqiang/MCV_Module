using System.Text.RegularExpressions;

namespace MCV_Module.UI.Components
{
    /// <summary>中文排版的纯函数部分（NBSP 缩进的写入与还原）；标点避头依赖控件行布局，留在 <see cref="TextComponent"/>。</summary>
    internal static class TextTypography
    {
        /// <summary>不换行空格：缩进一律用它拼，普通空格会被排版还原逻辑替换掉。</summary>
        public const string Nbsp = "\u00A0";

        static readonly Regex LeadingPunctuation =
            new Regex(@"(\！|\？|\，|\。|\《|\》|\）|\：|\”|\’|\、|\；|\+|\-|\.|\?)");

        /// <summary>该字符是否是会被挪到上一行末的行首标点。</summary>
        public static bool IsLeadingPunctuation(char c) => LeadingPunctuation.IsMatch(c.ToString());

        /// <summary>原文 → 显示文（先剥旧标记保证幂等，再空格转 NBSP、换行后与首行补 8 个 NBSP）。</summary>
        public static string BuildDisplay(string source)
        {
            string s = StripIndent(source ?? string.Empty);
            s = s.Replace(" ", Nbsp);
            s = s.Replace("\n", "\n" + Eight);
            return Eight + s;
        }

        /// <summary>剥掉本组件写过的缩进标记（幂等；不碰正文里的普通空格）。</summary>
        public static string StripIndent(string s)
        {
            s = (s ?? string.Empty).Replace("\n" + Eight, "\n");
            return s.StartsWith(Eight) ? s.Substring(Eight.Length) : s;
        }

        /// <summary>把本组件写过的排版还原成原文（外部要拿原文时用）。</summary>
        public static string Remove(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace("\n\u3000\u3000", "\n");
            s = s.Replace("\u3000\u3000", "");
            return s.Replace(Nbsp, " ");
        }

        /// <summary>去掉所有换行符。</summary>
        public static string RemoveNewlines(string s) => s?.Replace("\n", string.Empty);

        /// <summary>8 个 NBSP（首行缩进与换行后缩进的同一个量）。</summary>
        static readonly string Eight = Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp;
    }
}
