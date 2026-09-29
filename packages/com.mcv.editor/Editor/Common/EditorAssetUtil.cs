using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    // WHY: 共享内核，禁止再抄一份 —— EnsureFolder（9 行 ×2）与 IsConfigScriptReady（5 行 ×2）曾在 ContentBundleTools 与 CameraBgBundleTools 逐字重复，「脚本丢失的坏资产」处理也只在此收敛（§4.1 铁律③）。
    /// <summary>Editor 侧「资产 / 编译就绪」基础设施的共享内核。</summary>
    public static class EditorAssetUtil
    {
        /// <summary>递归确保 Assets 内目录存在（父级不存在会先建父级）。</summary>
        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // WHY: 编译中 / 域重载中 GetClass() 返回 null，此时 CreateAsset 会写出坏资产 —— 所有「会 CreateAsset 的菜单」入口第一步都要过这道守卫。
        /// <summary>编译就绪守卫：MonoScript 资产存在且能解析出类时返回 true。</summary>
        public static bool IsScriptReady(string scriptPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            return script != null && script.GetClass() != null;
        }

        // WHY: 历史坑 —— 类没写在「与类名同名」的 .cs 里、或脚本没编译完就 CreateAsset，资产会加载为 null（表现为「配置在、PackageDB 里却是 None」），故同名但加载为 null 时先删再建。
        /// <summary>载入或创建 <c>T</c> 资产；同名资产存在但加载为 null 时先删再建。logTag/createdLog 为 null 则不打对应日志。</summary>
        public static T LoadOrCreateAsset<T>(string assetPath, string logTag, string createdLog = null)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset == null && File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                if (logTag != null) Debug.Log($"{logTag} 已删除脚本丢失的旧配置并重建：{assetPath}");
            }

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
                if (createdLog != null) Debug.Log($"{logTag}{createdLog}：{assetPath}");
            }

            return asset;
        }

        // WHY: 必须删残留 —— PackageDatabaseSO.AutoCollect 是全工程按类型全量重收，残留 id 会被收回清单，运行时出现两套可解析 id（漏改的 JSON 键变成「配置在、包不在」）；加载为 null 的坏资产也在此清掉。
        /// <summary>删除 <c>dir</c> 下 id 不在 <c>keepIds</c> 内的资产（残留配置清理），返回被删资产路径。</summary>
        public static List<string> DeleteAssetsNotIn<T>(string dir, ISet<string> keepIds,
                                                       Func<T, string> idOf, string logTag)
            where T : UnityEngine.Object
        {
            var removed = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && keepIds != null && keepIds.Contains(idOf(asset))) continue;

                AssetDatabase.DeleteAsset(path);
                removed.Add(path);
                if (logTag != null) Debug.Log($"{logTag} 已删除不属于本次产出的旧配置：{path}");
            }

            return removed;
        }

        /// <summary>回读自检：返回 <c>dir</c> 下 <c>AB_{id}.asset</c> 加载为 null（m_Script 丢失）的资产路径；调用方负责删除并提示重跑本菜单。</summary>
        public static List<string> FindBrokenAssets<T>(string dir, IEnumerable<string> expectedIds)
            where T : UnityEngine.Object
        {
            var broken = new List<string>();
            if (expectedIds == null) return broken;

            foreach (string id in expectedIds)
            {
                string path = $"{dir}/AB_{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<T>(path) == null) broken.Add(path);
            }

            return broken;
        }
    }
}
