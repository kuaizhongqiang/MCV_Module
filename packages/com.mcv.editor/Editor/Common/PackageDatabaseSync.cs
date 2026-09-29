using System;
using System.Collections.Generic;
using System.Linq;
using MCV_Module.Models.Addressable;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    // WHY: 关键边界 —— AutoCollect() 是全工程按类型全量重收、会重写整个 packages 列表，只允许在明确「要写」时调用（Sync 是唯一写入口）；dry-run 一律走 Audit（绝不写盘，P0-3「dry-run 会写盘」的根因）。
    /// <summary>包清单（<see cref="PackageDatabaseSO"/>）同步与只读体检。</summary>
    public static class PackageDatabaseSync
    {
        /// <summary>同步主清单：全量 <c>AutoCollect</c> + 本次产出兜底补入（AutoCollect 可能漏收）；会写盘（<c>SaveAssets</c>），返回同步后的清单条数。</summary>
        public static int Sync(string dbAssetPath, IEnumerable<ABPackageConfigSO> justGenerated, string logTag)
        {
            var db = EditorAssetUtil.LoadOrCreateAsset<PackageDatabaseSO>(dbAssetPath, logTag, "已创建包清单");

            db.AutoCollect();

            int added = 0;
            if (justGenerated != null)
            {
                foreach (ABPackageConfigSO config in justGenerated)
                {
                    // WHY: config == null 是隐式保护，别去掉 —— 上游自检删掉坏资产后列表中仍持有被删对象的引用。
                    if (config == null || string.IsNullOrEmpty(config.id) || db.FindById(config.id) != null) continue;
                    db.packages.Add(config);
                    added++;
                }
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            if (added > 0) Debug.LogWarning($"{logTag} AutoCollect 漏收 {added} 个配置，已兜底补入");
            if (db.Count == 0) Debug.LogError($"{logTag} 包清单为空：{dbAssetPath}");
            else Debug.Log($"{logTag} 包清单已同步：{dbAssetPath}（{db.Count} 条）");

            return db.Count;
        }

        // WHY: 落实 Docs/design_ai/BundlePipeline.md「清单中不属于本次产出的 AB 配置要有报告」的承诺（P1-2）—— 此前只在删除残留配置时打一条 Log，反向残留（配置已删、bundle 仍在）任何菜单都不报告。
        /// <summary>只读体检（绝不写盘、绝不调用 AutoCollect）：与预期 id 集合求差集，返回 <see cref="DatabaseAudit"/>。</summary>
        public static DatabaseAudit Audit(string dbAssetPath, ISet<string> expectedIds)
        {
            var audit = new DatabaseAudit
            {
                Missing = new List<string>(),
                Residue = new List<string>(),
                NullRefs = new List<string>(),
            };

            var db = AssetDatabase.LoadAssetAtPath<PackageDatabaseSO>(dbAssetPath);
            if (db == null)
            {
                if (expectedIds != null) audit.Missing.AddRange(expectedIds.OrderBy(id => id, StringComparer.Ordinal));
                return audit;
            }

            audit.TotalCount = db.Count;

            var present = new HashSet<string>();
            for (int i = 0; i < db.packages.Count; i++)
            {
                PackageConfigSO package = db.packages[i];
                if (package == null || string.IsNullOrEmpty(package.id))
                {
                    audit.NullRefs.Add($"packages[{i}]（{(package == null ? "引用丢失" : "id 为空")}）");
                    continue;
                }

                present.Add(package.id);
            }

            if (expectedIds != null)
            {
                foreach (string id in expectedIds)
                    if (!present.Contains(id)) audit.Missing.Add(id);
            }

            // WHY: 反向残留只看「内容包」（AB 且 clipId 非空）—— AA / CameraBg 等全局包不属于本流水线产出，一律报出只会淹没有效信息。
            foreach (PackageConfigSO package in db.packages)
            {
                var ab = package as ABPackageConfigSO;
                if (ab == null || string.IsNullOrEmpty(ab.id) || string.IsNullOrEmpty(ab.clipId)) continue;
                if (expectedIds != null && expectedIds.Contains(ab.id)) continue;
                audit.Residue.Add(ab.id);
            }

            audit.Missing.Sort(StringComparer.Ordinal);
            audit.Residue.Sort(StringComparer.Ordinal);
            return audit;
        }
    }

    /// <summary>只读体检结果（见 <see cref="PackageDatabaseSync.Audit"/>）。</summary>
    public struct DatabaseAudit
    {
        /// <summary>清单当前条数。</summary>
        public int TotalCount;

        /// <summary>预期有、清单里没有。</summary>
        public List<string> Missing;

        /// <summary>清单里有、不属于本次产出的**内容包**（配置未清干净 / bundle 已成无主残留）。</summary>
        public List<string> Residue;

        /// <summary><c>packages[i]</c> 引用丢失或 id 为空（等价 <c>PackageDatabaseSO.Validate()</c> 的索引集合）。</summary>
        public List<string> NullRefs;

        /// <summary>一行摘要，供弹窗 / Console 直接使用。</summary>
        public string Summary()
        {
            return $"清单 {TotalCount} 条 ／ 未收录 {Count(Missing)} ／ 残留 {Count(Residue)} ／ 空引用 {Count(NullRefs)}";
        }

        static int Count(List<string> list)
        {
            return list == null ? 0 : list.Count;
        }
    }
}
