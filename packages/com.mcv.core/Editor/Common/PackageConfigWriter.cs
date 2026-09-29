using System;
using System.IO;
using MCV_Module.Models.Addressable;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    // WHY: 共享内核 —— 此前 ContentBundleTools 与 CameraBgBundleTools 各写一份包配置（29 行 ×2、其中 20 行逐字相同），两份能独立写错（P1-8：ContentResourceEntry.kind 默认 Prefab 而 ABPackageConfigSO.assetKind 默认 Sprite，CameraBg 侧靠吃默认值侥幸正确）。
    /// <summary>生成 / 更新 <see cref="ABPackageConfigSO"/> —— 「包配置怎么写」的唯一落点。</summary>
    public static class PackageConfigWriter
    {
        /// <summary>载入或创建 <c>{configDir}/AB_{id}.asset</c>（目录不存在先建、同名资产脚本丢失先删再建），交给 <c>fill</c> 填字段后返回资产。</summary>
        public static ABPackageConfigSO CreateOrUpdate(string configDir, string id, string logTag,
                                                      Action<ABPackageConfigSO> fill)
        {
            if (string.IsNullOrEmpty(configDir) || string.IsNullOrEmpty(id)) return null;

            EditorAssetUtil.EnsureFolder(configDir);

            string configPath = $"{configDir}/AB_{id}.asset";
            ABPackageConfigSO config = EditorAssetUtil.LoadOrCreateAsset<ABPackageConfigSO>(configPath, logTag);

            // WHY: CreateAsset 失败（父目录缺失 / 路径非法等）时不返回 null 保护就会变成一串 NRE。
            if (config == null)
            {
                Debug.LogError($"{logTag} 包配置创建失败，已跳过：{configPath}");
                return null;
            }

            if (fill != null) fill(config);
            EditorUtility.SetDirty(config);
            return config;
        }

        // WHY: 两条不能动的语义 —— ① sourceAsset 必须置 null：它是直接引用，而配置资产在 Resources/ 下，一旦引用素材 Unity 会把素材一并打进 resources.assets（资源两份、AB 白打）；② assetKind 必须显式写：运行时按它选泛型加载，不写就吃字段默认值，而该默认值在 Provider 与 SO 两边并不一致。clipId 为归属 clip（内容包必填），非内容资源传 null。
        /// <summary>包配置统一填充：id / clipId / assetKind / displayName / bundleName / assetPath / variant。</summary>
        public static void ApplyContent(ABPackageConfigSO config, string id, string clipId,
                                        string bundleName, string assetPath, ContentAssetKind kind)
        {
            if (config == null) return;

            config.id = id;
            config.clipId = clipId;
            config.assetKind = kind;
            config.displayName = Path.GetFileNameWithoutExtension(assetPath);
            // WHY: 关键 —— 不要写 sourceAsset（见上方 ①），否则 Unity 会把素材一并打进 resources.assets。
            config.sourceAsset = null;
            config.bundleName = bundleName;
            config.assetPath = assetPath;
            config.variant = "";
        }
    }
}
