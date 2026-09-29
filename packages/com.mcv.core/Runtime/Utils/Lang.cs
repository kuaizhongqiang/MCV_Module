using MCV_Module.Managers;
using MCV_Module.Models.System;

namespace MCV_Module.Utils
{
    /// <summary>
    /// 运行时取文案的唯一入口（供不走文本组件的代码使用，例如注册在别的管理器里的提示语）。
    /// 回退链的实现在 <see cref="GlobalDataMgr.PickClipText"/>，本类只做空安全与日志，**不复制那份逻辑**。
    /// </summary>
    public static class Lang
    {
        const string LogTag = "[Lang]";

        /// <summary>按 key 取当前语言文案；永不返回 null、不抛异常。顺序：当前语言 → 中文原文 → key 本身。</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            GlobalDataMgr mgr = GlobalDataMgr.Instance;
            if (mgr == null)
            {
                // WHY: 数据管理器还没起来（或退出态）时不能崩，也不能写空——直接把 key 交回去，调用方至少能看到什么。
                return key;
            }
            if (!mgr.TryGetClip(key, out LanguageClip clip))
            {
                Log.Warning($"{LogTag} 语言 key「{key}」未登记（已返回 key 本身）");
                return key;
            }
            string picked = mgr.PickClipText(clip);
            if (picked == null)
            {
                Log.Warning($"{LogTag} 语言 key「{key}」所有语言槽位均为空（已返回 key 本身）");
                return key;
            }
            return picked;
        }

        /// <summary>带格式化的取文案：<c>Lang.Get("ui.loading", 90)</c>；占位符与参数不匹配时返回未格式化文本并告警，不抛。</summary>
        public static string Get(string key, params object[] args)
        {
            string format = Get(key);
            if (args == null || args.Length == 0) return format;
            try
            {
                return string.Format(format, args);
            }
            catch (System.FormatException)
            {
                Log.Warning($"{LogTag} key「{key}」格式化失败（占位符与参数不匹配），返回未格式化文本");
                return format;
            }
        }

        /// <summary>key 是否已登记（供校验与调试）。</summary>
        public static bool Has(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            GlobalDataMgr mgr = GlobalDataMgr.Instance;
            return mgr != null && mgr.TryGetClip(key, out _);
        }
    }
}
