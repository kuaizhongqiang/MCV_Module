using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MCV_Module.Data.Addressable
{
    /// <summary>
    /// 包配置主清单 — 单个 .asset 文件，持有所有 PackageConfigSO 的引用列表
    ///
    /// 用途：
    ///   - Editor 打包工具：遍历此列表 → 收集所有需要打包的资源 → 构建 AA Group / AB Bundle
    ///   - 运行时入口：加载此 SO → 遍历列表 → 填充 PackageDataRepository 运行时注册表
    ///
    /// 建议：项目中只需创建一个 Database 实例（如 PackageDB_Master.asset）
    /// 将所有需要管理的包配置拖入列表即可。
    ///
    /// 快捷功能：在 Inspector 中点 AutoCollect() 可自动收集项目中所有 PackageConfigSO。
    /// </summary>
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

        /// <summary>
        /// 按包类型筛选（如只获取所有 AA 配置）
        /// </summary>
        /// <param name="type">要筛选的包类型</param>
        /// <returns>该类型的所有包配置</returns>
        public IEnumerable<PackageConfigSO> GetByType(PackageType type)
        {
            return packages.Where(p => p != null && p.PackageType == type);
        }

        /// <summary>
        /// 按 id 查找包配置
        /// </summary>
        /// <param name="id">包的唯一标识</param>
        /// <returns>找到的包配置，未找到则返回 null</returns>
        public PackageConfigSO FindById(string id)
        {
            return packages.Find(p => p != null && p.id == id);
        }

        /// <summary>
        /// 检查列表中所有包配置的完整性
        /// 返回那些引用丢失或 id 为空的条目索引
        /// </summary>
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
        /// <summary>
        /// Editor 工具方法：自动扫描并收集项目中所有 PackageConfigSO
        ///
        /// 使用 AssetDatabase.FindAssets 搜索全部 PackageConfigSO 类型的 .asset 文件，
        /// 排除了自身后添加到列表中。适用于初始化或同步清单。
        /// </summary>
        public void AutoCollect()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:PackageConfigSO");
            packages.Clear();
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<PackageConfigSO>(path);
                if (so != null && so != this)
                    packages.Add(so);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
