using System;
using System.IO;
using MCV_Module.Models.Addressable;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    /// <summary>
    /// 生成 / 更新 <see cref="ABPackageConfigSO"/> —— 「包配置怎么写」的**唯一落点**。
    ///
    /// 收敛原因：多处各写一份时，两份都能独立写错（例如
    /// <c>ContentResourceEntry.kind</c> 默认 Prefab 而 <c>ABPackageConfigSO.assetKind</c> 默认 Sprite，
    /// 靠吃默认值侥幸正确的分支迟早会分叉）。
    /// </summary>
    public static class PackageConfigWriter
    {
        /// <summary>
        /// 载入或创建 <c>{configDir}/AB_{id}.asset</c>，交给 <paramref name="fill"/> 填字段，返回资产。
        /// 目录不存在会先建；同名资产脚本丢失会先删再建（见 <see cref="EditorAssetUtil.LoadOrCreateAsset{T}"/>）。
        /// </summary>
        /// <param name="logTag">日志前缀（透传给 <see cref="EditorAssetUtil.LoadOrCreateAsset{T}"/>）</param>
        public static ABPackageConfigSO CreateOrUpdate(string configDir, string id, string logTag,
                                                      Action<ABPackageConfigSO> fill)
        {
            if (string.IsNullOrEmpty(configDir) || string.IsNullOrEmpty(id)) return null;

            EditorAssetUtil.EnsureFolder(configDir);

            string configPath = $"{configDir}/AB_{id}.asset";
            ABPackageConfigSO config = EditorAssetUtil.LoadOrCreateAsset<ABPackageConfigSO>(configPath, logTag);

            // CreateAsset 失败（父目录缺失 / 路径非法等）时不返回 null 保护就会变成一串 NRE
            if (config == null)
            {
                Debug.LogError($"{logTag} 包配置创建失败，已跳过：{configPath}");
                return null;
            }

            if (fill != null) fill(config);
            EditorUtility.SetDirty(config);
            return config;
        }

        /// <summary>
        /// 包配置统一填充：id / clipId / assetKind / displayName / bundleName / assetPath / variant。
        ///
        /// 两条不能动的语义：
        ///   ① <c>sourceAsset</c> **必须置 null** —— 它是直接引用，而配置资产在 <c>Resources/</c> 下，
        ///      一旦引用素材，Unity 会把素材一并打进 <c>resources.assets</c>（资源两份、AB 白打）；
        ///   ② <c>assetKind</c> **必须显式写** —— 运行时按它选泛型加载；不写就吃字段默认值，
        ///      而该默认值在 Provider 与 SO 两边并不一致（见类注释）。
        /// </summary>
        /// <param name="clipId">归属 clip（内容包必填）</param>
        public static void ApplyContent(ABPackageConfigSO config, string id, string clipId,
                                        string bundleName, string assetPath, ContentAssetKind kind)
        {
            if (config == null) return;

            config.id = id;
            config.clipId = clipId;
            config.assetKind = kind;
            config.displayName = Path.GetFileNameWithoutExtension(assetPath);
            // 关键：**不要**写 sourceAsset（见上方 ①）
            config.sourceAsset = null;
            config.bundleName = bundleName;
            config.assetPath = assetPath;
            config.variant = "";
        }
    }
}
