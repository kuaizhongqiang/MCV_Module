using System;
using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.Models
{
    // WHY: 只存**路径字符串**、不存 Font / TMP_FontAsset 引用 —— 本资产住在 Resources/Config 下（运行期 Resources.Load 取用），
    // 一旦在这里直连字体资产，Unity 就会把字体一并打进 resources.assets，正是「字体进 AB、default 包不含字体」要避免的事
    // （旧 Localization 分支的 FontCatalogSO 存引用，才不得不额外造一套字体脱钩 / 回挂机制）。
    /// <summary>字体清单条目：fontId → 两种形态的资产路径 + 所属全局包名（全是字符串，不持引用）。</summary>
    [Serializable]
    public class FontCatalogEntry
    {
        /// <summary>稳定 id（TextComponent.fontId 就是它）。</summary>
        public string id = "ui";

        /// <summary>Legacy 字体资产路径（Assets/Fonts/xxx.ttf）。Legacy 形态由 prefab 引用该资产，这里只作登记与校验。</summary>
        public string legacyFontPath = "";

        /// <summary>TMP 字体资产路径（Assets/Fonts/xxx SDF.asset）；TMP 形态没有 prefab 引用，运行期按此路径取。</summary>
        public string tmpFontAssetPath = "";

        /// <summary>该字体所在的全局包名（bundle 名段全小写）。</summary>
        public string bundleName = "";
    }

    /// <summary>字体清单：fontId → 两种形态的资产路径与所属包；加字体只改这份数据，零代码。</summary>
    [CreateAssetMenu(menuName = "MCV/Data/FontCatalog", fileName = "FontCatalogSO")]
    public class FontCatalogSO : ScriptableObject
    {
        [SerializeField] List<FontCatalogEntry> entries = new List<FontCatalogEntry>();

        /// <summary>全部条目（只读视图）。</summary>
        public IReadOnlyList<FontCatalogEntry> Entries => entries;

        /// <summary>按 fontId 取条目；不存在返回 null。</summary>
        public FontCatalogEntry Find(string fontId)
        {
            if (string.IsNullOrEmpty(fontId) || entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].id == fontId) return entries[i];
            }
            return null;
        }
    }
}
