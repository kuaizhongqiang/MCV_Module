using System.Collections.Generic;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.System;

namespace MCV_Module.Utils
{
    /// <summary>
    /// 业务数据的英文列取值：按当前语言在中 / 英两列之间挑一个，英文列为空时**回退中文**。
    /// 业务数据不走 key 表（那些是固化数据），所以这里只做"两列挑一列"，不做查找与回退链。
    /// </summary>
    public static class Localized
    {
        /// <summary>两列里挑一个；英文列为空 → 中文列。</summary>
        public static string Pick(string zh, string en)
        {
            GlobalDataMgr mgr = GlobalDataMgr.Instance;
            if (mgr != null && mgr.GetLanguageType() != LanguageType.Chinese && !string.IsNullOrEmpty(en)) return en;
            return zh;
        }

        /// <summary>集合列（如 StepUiData.pages）：下标越界或英文为空 → 回退中文；两边都取不到返回空串。</summary>
        public static string Pick(List<string> zh, List<string> en, int index)
        {
            string fallback = zh != null && index >= 0 && index < zh.Count ? zh[index] : string.Empty;
            GlobalDataMgr mgr = GlobalDataMgr.Instance;
            bool english = mgr != null && mgr.GetLanguageType() != LanguageType.Chinese;
            if (english && en != null && index >= 0 && index < en.Count && !string.IsNullOrEmpty(en[index])) return en[index];
            return fallback;
        }

        /// <summary>DataBase 派生数据的显示名（器件名 / 任务名 / 条目名）；null 输入返回 null，便于 `?.` 调用点直接替换。</summary>
        public static string Name(DataBase data)
        {
            if (data == null) return null;
            return Pick(data.displayName, data.displayNameEn);
        }
    }
}
