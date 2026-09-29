using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Models.System
{
    /// <summary>
    /// 字体清单的取用门面：从 <c>Resources/Config/FontCatalog</c> 取 <see cref="FontCatalogSO"/>，按 fontId 解析条目、包名与两种形态的资产路径。
    /// 纯查表（含一次 Resources 缓存），**不做任何加载** —— 字体资产由 GlobalAssetsMgr 从字体包取。
    /// </summary>
    public static class FontCatalog
    {
        /// <summary>清单资产路径（相对 Resources，不带扩展名）。</summary>
        public const string ResourcePath = "Config/FontCatalog";

        static FontCatalogSO s_Asset;
        static bool s_Resolved;

        /// <summary>清单资产；缺失时告警一次并返回 null（调用方据此降级：不动节点上已有的字体）。</summary>
        public static FontCatalogSO Asset
        {
            get
            {
                if (!s_Resolved)
                {
                    s_Resolved = true;
                    s_Asset = Resources.Load<FontCatalogSO>(ResourcePath);
                    if (s_Asset == null)
                        Log.Warning($"[FontCatalog] 未找到 Resources/{ResourcePath}.asset：字体清单缺失，字体相关功能降级（不影响已有字体引用）");
                }
                return s_Asset;
            }
        }

        /// <summary>清缓存：Editor 工具改完清单后调用，让下次取用重新加载。</summary>
        public static void Invalidate()
        {
            s_Asset = null;
            s_Resolved = false;
        }

        /// <summary>按 fontId 取条目；清单缺失或 id 不存在返回 null。</summary>
        public static FontCatalogEntry Find(string fontId)
        {
            FontCatalogSO asset = Asset;
            return asset != null ? asset.Find(fontId) : null;
        }

        /// <summary>该 fontId 所属的全局包名；取不到返回空串。</summary>
        public static string BundleNameOf(string fontId)
        {
            FontCatalogEntry entry = Find(fontId);
            return entry != null ? entry.bundleName : string.Empty;
        }

        /// <summary>该 fontId 的 Legacy 字体资产路径（Assets/Fonts/…）；取不到返回空串。</summary>
        public static string LegacyFontPathOf(string fontId)
        {
            FontCatalogEntry entry = Find(fontId);
            return entry != null ? entry.legacyFontPath : string.Empty;
        }

        /// <summary>该 fontId 的 TMP 字体资产路径；取不到返回空串。</summary>
        public static string TmpFontAssetPathOf(string fontId)
        {
            FontCatalogEntry entry = Find(fontId);
            return entry != null ? entry.tmpFontAssetPath : string.Empty;
        }
    }
}
