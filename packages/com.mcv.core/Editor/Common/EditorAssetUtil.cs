using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    /// <summary>
    /// Editor 侧「资产 / 编译就绪」基础设施。
    ///
    /// 把「脚本丢失的坏资产」处理收敛到一处：类没写在「与类名同名」的 .cs 里、
    /// 或脚本没编译完就 <c>CreateAsset</c>，资产会加载为 null（表现为「配置在、清单里却没有」）。
    /// </summary>
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

        /// <summary>
        /// 编译就绪守卫：MonoScript 资产存在**且**能解析出类。
        /// 编译中 / 域重载中 <c>GetClass()</c> 返回 null，此时 <c>CreateAsset</c> 会写出坏资产 ——
        /// 所有「会 CreateAsset 的菜单」入口第一步都要过这道守卫。
        /// </summary>
        public static bool IsScriptReady(string scriptPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            return script != null && script.GetClass() != null;
        }

        /// <summary>
        /// 载入或创建 ScriptableObject 资产。同名资产存在但**加载为 null**（<c>m_Script</c> 丢失）时先删再建。
        /// </summary>
        /// <param name="assetPath">资产路径（<c>Assets/...</c>）</param>
        /// <param name="logTag">日志前缀（如 <c>[ContentBundle]</c>）；null 则不打日志</param>
        /// <param name="createdLog">新建时的日志正文（null 表示新建不打日志）</param>
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

        /// <summary>
        /// 删除 <paramref name="dir"/> 下 id 不在 <paramref name="keepIds"/> 内的资产（残留配置清理）。
        ///
        /// 为什么必须删：<c>PackageDatabaseSO.AutoCollect</c> 是**全工程按类型全量重收**，
        /// 残留 id 会被收回清单，运行时就出现两套可解析的 id（漏改的 JSON 键变成「配置在、包不在」）。
        /// 加载为 null 的坏资产同样在此被清掉。
        /// </summary>
        /// <returns>被删掉的资产路径</returns>
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

        /// <summary>
        /// 回读自检：返回 <paramref name="dir"/> 下 <c>AB_{id}.asset</c> 加载为 null（<c>m_Script</c> 丢失）的资产路径。
        /// 调用方负责删除并提示重跑本菜单。
        /// </summary>
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
