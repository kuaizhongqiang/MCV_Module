using MCV_Module.Managers;
using MCV_Module.Models.Addressable;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.UI.Tools
{
    // WHY: UI prefab 出 Resources 后一律按「包配置 id」取件；把 id 拼装与失败日志收在一处，避免 8 个调用点各自拼字符串、各自写日志。
    /// <summary>UI 包取件唯一入口：prefab 名 → 包配置 id → 同步取已预加载的预制体。</summary>
    public static class UIPrefabUtil
    {
        // WHY: 刻意只取缓存、不触发加载 —— 面板与碎片的创建路径是同步的（CanvasBase.CreatePanel 在 Rebuild 内直接返回面板），
        // 因此 UI 包必须已在 Setup 启动阶段预加载完；这里若走异步加载，调用点拿不到实例。
        /// <summary>按 prefab 名取已预加载的 UI 预制体；未就绪返回 null 并打 Error（不触发加载）。</summary>
        public static GameObject Get(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                Log.Error("[UIPrefabUtil] prefabName 为空");
                return null;
            }

            string packageId = ContentNaming.UIPrefabId(prefabName);
            GameObject prefab = GlobalAssetsMgr.GetPrefabByPackageId(packageId);
            if (prefab == null)
            {
                Log.Error($"[UIPrefabUtil] UI prefab 未就绪：{prefabName}（包配置 id={packageId}）" +
                          "—— UI 包应在 Setup 启动阶段预加载；请确认已跑 MCV Build/UI prefab AB，且 StreamingAssets/UI/ui 存在");
            }
            return prefab;
        }
    }
}
