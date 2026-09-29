using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // WHY: 运行时入口 —— GlobalAddressableMgr 加载此 SO 构建 id → PackageConfigSO 字典；列表缺条目会直接导致运行期加载失败。
    /// <summary>包配置主清单：持有所有 PackageConfigSO 引用的单个 .asset，Editor 打包与运行时加载共用。</summary>
    [CreateAssetMenu(
        fileName = "PackageDB_",
        menuName = "MCV/Package Database",
        order = 1)]
    public class PackageDatabaseSO : ScriptableObject
    {
        [Tooltip("所有需要管理的包配置列表\n\n" +
                 "支持 AA / AB / Default 三种类型混编\n" +
                 "Editor 打包工具会遍历此列表构建资源包\n" +
                 "运行时从此列表加载所有包数据到注册表")]
        public List<PackageConfigSO> packages = new List<PackageConfigSO>();

        /// <summary>按包类型筛选（如只取所有 AA 配置）。</summary>
        public IEnumerable<PackageConfigSO> GetByType(PackageType type)
        {
            return packages.Where(p => p != null && p.PackageType == type);
        }

        /// <summary>按 id 查找包配置，未找到返回 null。</summary>
        public PackageConfigSO FindById(string id)
        {
            return packages.Find(p => p != null && p.id == id);
        }

        /// <summary>检查列表完整性，返回引用丢失或 id 为空的条目索引。</summary>
        public List<int> Validate()
        {
            var missing = new List<int>();
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i] == null || string.IsNullOrEmpty(packages[i].id))
                    missing.Add(i);
            }
            return missing;
        }

        /// <summary>列表中包配置的总数</summary>
        public int Count => packages.Count;

#if UNITY_EDITOR
        // WHY: AssetDatabase 的类型过滤器不匹配抽象基类的派生资源（只搜 t:PackageConfigSO 收不到子类，实测 48 个 ABPackageConfigSO 在盘上而 DB 里是空表），故按「基类 + 各具体子类」各搜一遍再去重排序。
        /// <summary>Editor 工具：自动扫描并收集项目中所有 PackageConfigSO，按路径去重后依 id 排序写回。</summary>
        public void AutoCollect()
        {
            var found = new Dictionary<string, PackageConfigSO>();

            AddAssets("t:PackageConfigSO", found);
            AddAssets("t:AAPackageConfigSO", found);
            AddAssets("t:ABPackageConfigSO", found);
            AddAssets("t:DefaultPackageConfigSO", found);

            packages.Clear();
            var list = new List<PackageConfigSO>(found.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.id ?? "", b.id ?? ""));
            packages.AddRange(list);

            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>按过滤器收集资产（同一路径只收一次，防止多轮过滤重复）。</summary>
        static void AddAssets(string filter, Dictionary<string, PackageConfigSO> found)
        {
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets(filter))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || found.ContainsKey(path)) continue;

                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<PackageConfigSO>(path);
                if (config != null) found[path] = config;
            }
        }
#endif
    }
}
