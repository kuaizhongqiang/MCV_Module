using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MCV_Module.EditorTools.Common;
using MCV_Module.Interfaces;
using MCV_Module.Models;
using MCV_Module.Models.Addressable;
using MCV_Module.Models.Project;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// WHY: 三条铁律（规约见 Docs/design_ai/BundlePipeline.md）—— ① 包粒度 = ProjectClip（该 clip 下 4 个 TaskData 引用的资源全部进同一个包）；② 包配置 id = ProjectData.json 里**实际写的键值**（规则 {器件}_{任务}_{资源}），不是另起的名字；③ 视频不入包（裸露在 StreamingAssets/Video）。
// WHY: **新增一类内容的唯一登记点**是 Providers —— 加一个 IContentProvider 实现即可，id 规则、配置生成、清单同步、构建、对账、旧产物清理全部复用本流程，不得另开一套。
/// <summary>内容 AB 流水线：一个 ProjectClip 一个 AssetBundle（bundle <c>Content/clip_{器件小写}</c>），一键做五件事：跑 Provider 收集 → 分级对账 → 生成/更新包配置 → 汇入 PackageDB_Master → 构建到 StreamingAssets/Content/（Temp 中转，只落 bundle 本体，不带 .manifest）。</summary>
public static class ContentBundleTools
{
    // WHY: 路径常量的唯一来源是 Assets/Editor/Common/EditorPaths.cs，本文件不得再自带路径字面量。

    #region Provider 登记
    // WHY: 数组顺序即 Provider 执行顺序，新增一类内容只需在此登记一行。
    /// <summary>Provider 列表（顺序即执行顺序）：简介图集 <c>InfoSprite</c> + 三种「一个 TaskData → 一个预制体」的模型 <c>InfoModel</c> / <c>StructureModel</c> / <c>InspectionModel</c>，后三者形态一致、共用 <see cref="ModelPrefabProvider"/>。</summary>
    static readonly IContentProvider[] Providers =
    {
        new InfoSpriteProvider(),

        new ModelPrefabProvider("InfoModel", "Assets/Prefabs/Models/InfoObjs", ContentNaming.TaskInfo,
            clip => clip.GetTaskData<TaskInfoData>(TaskType.Info)?.prefabKey),
        new ModelPrefabProvider("StructureModel", "Assets/Prefabs/Models/StructureObjs", ContentNaming.TaskStructure,
            clip => clip.GetTaskData<TaskStructureData>(TaskType.Structure)?.prefabKey),
        new ModelPrefabProvider("InspectionModel", "Assets/Prefabs/Models/InspectionObjs", ContentNaming.TaskInspection,
            clip => clip.GetTaskData<TaskInspectionData>(TaskType.Inspection)?.prefabKey),
    };
    #endregion

    #region 打包计划
    /// <summary>一个 clip 的打包计划（bundle 粒度 = clip）。</summary>
    class ClipPlan
    {
        public string clipId;          // clip_contactor
        public string device;          // contactor（id 用小驼峰）
        public string bundleName;      // Content/clip_contactor
        public readonly List<ContentResourceEntry> entries = new List<ContentResourceEntry>();
    }
    #endregion

    #region 菜单
    [MenuItem("MCV Build/内容 AB 流水线（按 ProjectClip 分包）", false, 80)]
    public static void RunAll()
    {
        Run(writeConfig: true, build: true, askBeforeBuild: true);
    }

    [MenuItem("MCV Build/内容 AB/对账（dry-run，不写盘）", false, 81)]
    public static void AuditOnly()
    {
        Run(writeConfig: false, build: false, askBeforeBuild: false);
    }

    [MenuItem("MCV Build/内容 AB/仅构建", false, 82)]
    public static void BuildOnly()
    {
        Run(writeConfig: false, build: true, askBeforeBuild: false);
    }

    [MenuItem("MCV Build/内容 AB/仅刷新包清单", false, 83)]
    public static void RefreshDatabaseOnly()
    {
        if (!EditorAssetUtil.IsScriptReady(EditorPaths.AbConfigScript))
        {
            EditorUtility.DisplayDialog("内容 AB", "ABPackageConfigSO 还没编译就绪，等编译结束再跑。", "确定");
            return;
        }

        int count = PackageDatabaseSync.Sync(EditorPaths.PackageDbAsset, null, "[ContentBundle]");
        // WHY: 只读告警 —— 本菜单的 AutoCollect 是全量重收，磁盘上的残留配置会被重新写回清单（这里不删残留，但不再静默）。
        WarnResiduePackages(ExpectedIdsFromJson());

        EditorUtility.DisplayDialog("内容 AB",
            count == 0
                ? $"清单仍为空：{EditorPaths.PackageDbAsset}\n请先跑「内容 AB 流水线」生成配置"
                : $"✔ 包清单已同步：{count} 条\n{EditorPaths.PackageDbAsset}",
            "确定");
    }

    [MenuItem("MCV Build/内容 AB/清理旧产物（Info / InfoModel）", false, 84)]
    public static void CleanLegacy()
    {
        List<string> removed = CleanLegacyArtifacts();
        if (removed.Count > 0 && EditorAssetUtil.IsScriptReady(EditorPaths.AbConfigScript))
            PackageDatabaseSync.Sync(EditorPaths.PackageDbAsset, null, "[ContentBundle]");

        EditorUtility.DisplayDialog("内容 AB",
            removed.Count > 0
                ? $"✔ 已清理旧产物并刷新包清单：\n{string.Join("\n", removed)}"
                : "没有需要清理的旧产物",
            "确定");
    }
    #endregion

    #region 主流程
    static void Run(bool writeConfig, bool build, bool askBeforeBuild)
    {
        // WHY: 前置守卫 —— 脚本没编译就绪时 CreateAsset 会产出 m_Script: {fileID: 0} 的坏配置。
        if (!EditorAssetUtil.IsScriptReady(EditorPaths.AbConfigScript))
        {
            EditorUtility.DisplayDialog("内容 AB",
                "ABPackageConfigSO 还没编译就绪（Unity 正在编译 / 域重载中）。\n\n等编译结束再跑本菜单。", "确定");
            return;
        }

        ProjectData data = ReadProjectData();
        if (data == null)
        {
            EditorUtility.DisplayDialog("内容 AB", $"读取不到 {EditorPaths.ProjectDataJson}，请看 Console", "确定");
            return;
        }

        var errors = new List<ContentResourceEntry>();
        var extras = new List<ContentResourceEntry>();
        List<ClipPlan> plans = BuildPlans(data, errors, extras);
        AuditPlans(plans, errors);

        string report = BuildReport(plans, errors, extras);
        Debug.Log($"[ContentBundle] {report}");

        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("内容 AB",
                $"对账发现 {errors.Count} 个 Error，**已中止且未改动任何产物**：\n\n" +
                $"{HeadLines(errors.Select(e => $"{e.id}：{e.note}"), 10)}\n\n完整报告见 Console。", "确定");
            return;
        }

        if (plans.Count == 0)
        {
            EditorUtility.DisplayDialog("内容 AB", "没有任何 clip 收集到资源，请检查素材与 JSON。", "确定");
            return;
        }

        // WHY: 本次流水线期望的 id 集合，之后的对账体检 / 残留告警共用这一份，勿各自重算。
        var expectedIds = new HashSet<string>(plans.SelectMany(p => p.entries).Select(e => e.id));

        if (writeConfig)
        {
            GenerateConfigs(plans);
        }
        else if (build)
        {
            // WHY: 仅构建路径只确保清单收录 —— 配置已在磁盘上，此处不重新生成。
            PackageDatabaseSync.Sync(EditorPaths.PackageDbAsset, null, "[ContentBundle]");
            // WHY: 只读告警 —— 本步的 AutoCollect 会把磁盘上的残留配置一并收回清单（本路径不删残留，只提示）。
            WarnResiduePackages(expectedIds);
        }
        // WHY: dry-run 到此为止**不碰数据库** —— AutoCollect 是全量重写，会让「不写盘」的承诺落空、也产生无意义的版本 diff。

        if (!build)
        {
            // WHY: 只读体检 —— 把「dry-run 真的不写盘」从代码注释的承诺变成弹窗里可见的数字（P1-2 / §8.2 反驳 4）。
            var audit = PackageDatabaseSync.Audit(EditorPaths.PackageDbAsset, expectedIds);
            Debug.Log($"[ContentBundle] 只读体检：{audit.Summary()}" +
                      (audit.Missing.Count > 0 ? $"\n   未收录：{string.Join(", ", audit.Missing)}" : "") +
                      (audit.Residue.Count > 0 ? $"\n   残留：{string.Join(", ", audit.Residue)}" : ""));

            EditorUtility.DisplayDialog("内容 AB",
                $"✔ 对账通过（dry-run，未写盘）\n\n{plans.Count} 个 clip 待打包，共 " +
                $"{plans.Sum(p => p.entries.Count)} 项资源\nWarnings：{extras.Count}\n\n" +
                $"清单体检（只读）：{audit.Summary()}\n\n明细见 Console。", "确定");
            return;
        }

        if (askBeforeBuild &&
            !EditorUtility.DisplayDialog("内容 AB",
                $"配置已就绪，共 {plans.Count} 个 bundle：\n\n{HeadLines(plans.Select(p => ClipLine(p)).ToList(), 12)}\n\n" +
                $"是否立即构建？\n输出：{EditorPaths.StreamingAssetsRoot}/{EditorPaths.ContentBundleDirName}/" +
                (extras.Count > 0 ? $"\n\n⚠ 另有 {extras.Count} 条 Warning（见 Console，不阻塞）" : ""),
                "构建", "取消"))
        {
            Debug.Log("[ContentBundle] 已取消构建（配置已保存，可再跑「仅构建」）");
            return;
        }

        BuildBundles(plans);
    }

    /// <summary>逐 clip 跑 Provider 收集资源；**无资源产出的 clip 不进计划（不建空包）**。</summary>
    static List<ClipPlan> BuildPlans(ProjectData data,
        List<ContentResourceEntry> errors, List<ContentResourceEntry> extras)
    {
        var plans = new List<ClipPlan>();
        if (data == null || data.clips == null) return plans;

        foreach (ProjectClip clip in data.clips)
        {
            if (clip == null || string.IsNullOrEmpty(clip.id)) continue;

            string device = ContentNaming.DeviceOf(clip.id);
            var plan = new ClipPlan { clipId = clip.id, device = device, bundleName = ContentNaming.BundleNameFor(device) };

            foreach (IContentProvider provider in Providers)
            {
                IEnumerable<ContentResourceEntry> items = provider.Collect(clip, device);
                if (items == null) continue;

                foreach (ContentResourceEntry e in items)
                {
                    if (e == null || string.IsNullOrEmpty(e.id)) continue;

                    if (e.isExtra) { extras.Add(WithSource(e, provider)); continue; }
                    if (e.isError || string.IsNullOrEmpty(e.assetPath)) { errors.Add(WithSource(e, provider)); continue; }

                    plan.entries.Add(e);
                }
            }

            if (plan.entries.Count > 0) plans.Add(plan);
            // WHY: else 分支 —— 该 clip 没有任何资源（如 clip_quiz 四步全 null）时跳过，不产出空 AssetBundleBuild。
        }

        return plans;
    }

    /// <summary>对账补充检查：id 跨 clip 重复、bundle 名末段大小写（WebGL / Linux 大小写敏感）。</summary>
    static void AuditPlans(List<ClipPlan> plans, List<ContentResourceEntry> errors)
    {
        var owner = new Dictionary<string, string>();

        foreach (ClipPlan plan in plans)
        {
            if (!ContentNaming.IsBundleFileNameLowerCase(plan.bundleName))
            {
                errors.Add(new ContentResourceEntry(plan.clipId, null,
                    $"bundle 名的文件名段必须小写：{plan.bundleName}（运行时末段会被强制小写，写盘却按原样，WebGL/Linux 会 404）"));
            }

            foreach (ContentResourceEntry e in plan.entries)
            {
                if (owner.TryGetValue(e.id, out string other))
                {
                    errors.Add(new ContentResourceEntry(e.id, e.assetPath,
                        $"id 被重复占用：{other} 与 {plan.bundleName}"));
                    continue;
                }
                owner[e.id] = plan.bundleName;
            }
        }
    }

    static ContentResourceEntry WithSource(ContentResourceEntry e, IContentProvider provider)
    {
        e.note = string.IsNullOrEmpty(e.note) ? $"[{provider.Name}]" : $"[{provider.Name}] {e.note}";
        return e;
    }

    static string BuildReport(List<ClipPlan> plans, List<ContentResourceEntry> errors, List<ContentResourceEntry> extras)
    {
        var sb = new StringBuilder();
        sb.Append($"对账完成：{plans.Count} 个 clip 有资源（共 {plans.Sum(p => p.entries.Count)} 项）/ Error {errors.Count} / Warning {extras.Count}\n");

        foreach (ClipPlan plan in plans) sb.Append("  ✔ ").Append(ClipLine(plan)).Append('\n');
        foreach (ContentResourceEntry e in errors) sb.Append("  ✘ [Error] ").Append(e.id).Append("：").Append(e.note).Append('\n');
        foreach (ContentResourceEntry e in extras) sb.Append("  ⚠ [Warning] ").Append(e.id).Append("：").Append(e.note).Append('\n');

        return sb.ToString();
    }

    static string ClipLine(ClipPlan plan)
    {
        return $"{plan.clipId} → {plan.bundleName}（{plan.entries.Count} 项）";
    }

    static string HeadLines(IEnumerable<string> lines, int max)
    {
        List<string> list = lines == null ? new List<string>() : lines.ToList();
        if (list.Count == 0) return "";
        if (list.Count <= max) return string.Join("\n", list);
        return string.Join("\n", list.Take(max)) + $"\n…… 另有 {list.Count - max} 条，见 Console";
    }

    static ProjectData ReadProjectData()
    {
        if (!File.Exists(EditorPaths.ProjectDataJson))
        {
            Debug.LogError($"[ContentBundle] 找不到 {EditorPaths.ProjectDataJson}");
            return null;
        }

        try
        {
            // WHY: 与运行时同一口径（JsonReaderWriter → Newtonsoft）—— ProjectClip 的 taskXxxData 是 [SerializeField, JsonProperty] 私有字段，JsonProperty 保证 Newtonsoft 能写入。
            return JsonConvert.DeserializeObject<ProjectData>(File.ReadAllText(EditorPaths.ProjectDataJson));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContentBundle] 解析 ProjectData.json 失败：{e.Message}");
            return null;
        }
    }
    #endregion

    #region 配置生成与清单
    static void GenerateConfigs(List<ClipPlan> plans)
    {
        EditorAssetUtil.EnsureFolder(EditorPaths.ContentConfigDir);

        var expected = new HashSet<string>();
        foreach (ClipPlan plan in plans)
            foreach (ContentResourceEntry e in plan.entries) expected.Add(e.id);

        // WHY: 必须先删掉目录里不属于本次产出的配置 —— 否则 AutoCollect（全工程按类型全量重收）会把残留 id 收回来，运行时就出现两套可解析的 id，漏改的 JSON 键变成「配置在、包不在」的隐性问题。
        List<string> removed = EditorAssetUtil.DeleteAssetsNotIn<ABPackageConfigSO>(
            EditorPaths.ContentConfigDir, expected, config => config.id, "[ContentBundle]");

        var generated = new List<ABPackageConfigSO>();
        foreach (ClipPlan plan in plans)
        {
            foreach (ContentResourceEntry e in plan.entries)
            {
                ABPackageConfigSO created = PackageConfigWriter.CreateOrUpdate(
                    EditorPaths.ContentConfigDir, e.id, "[ContentBundle]",
                    config => PackageConfigWriter.ApplyContent(config, e.id, plan.clipId, plan.bundleName, e.assetPath, e.kind));
                if (created != null) generated.Add(created);      // WHY: 创建失败时 PackageConfigWriter 已报错，这里只跳过错项
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // WHY: 自检必须回读磁盘资产确认脚本解析正常 —— 历史坑：类没写在「与类名同名」的 .cs 里、或脚本未编译完就 CreateAsset 时，m_Script 会写成 {fileID: 0}，资产加载为 null（表现为「配置在、PackageDB 里却是 None」）。
        List<string> broken = EditorAssetUtil.FindBrokenAssets<ABPackageConfigSO>(EditorPaths.ContentConfigDir, expected);
        if (broken.Count > 0)
        {
            foreach (string path in broken) AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
            Debug.LogError($"[ContentBundle] {broken.Count} 个配置资产的 m_Script 丢失，已删除：\n   " +
                           string.Join("\n   ", broken) + "\n   → 再跑一次本菜单即可重建");
        }

        PackageDatabaseSync.Sync(EditorPaths.PackageDbAsset, generated, "[ContentBundle]");

        Debug.Log($"[ContentBundle] 配置就绪：新建/更新 {generated.Count} 个，清理旧配置 {removed.Count} 个 → {EditorPaths.ContentConfigDir}");
    }

    /// <summary>当前 JSON 期望的内容 id 集合（**只读**）；读不到 JSON 时返回 null = 无法判定 → 调用方跳过残留检查。</summary>
    static HashSet<string> ExpectedIdsFromJson()
    {
        if (!File.Exists(EditorPaths.ProjectDataJson)) return null;      // WHY: 不调用 ReadProjectData，避免无谓的 LogError

        ProjectData data = ReadProjectData();
        if (data == null) return null;

        List<ClipPlan> plans = BuildPlans(data, new List<ContentResourceEntry>(), new List<ContentResourceEntry>());
        var ids = new HashSet<string>();
        foreach (ClipPlan plan in plans)
            foreach (ContentResourceEntry e in plan.entries) ids.Add(e.id);
        return ids;
    }

    // WHY: 这里**只报警、不删** —— 删残留配置只发生在 GenerateConfigs（流水线主菜单）里，「仅构建 / 仅刷新包清单」保持原有的"不删文件"语义，但不再静默。
    /// <summary>只读残留告警：<c>AutoCollect</c> 是「全工程按类型全量重收」，磁盘上残留的旧配置会被重新写回主清单，运行时就多出「配置在、包不在」的可解析 id（见审核反馈 §4.2）。</summary>
    static void WarnResiduePackages(ISet<string> expectedIds)
    {
        if (expectedIds == null || expectedIds.Count == 0) return;

        DatabaseAudit audit = PackageDatabaseSync.Audit(EditorPaths.PackageDbAsset, expectedIds);
        if (audit.Residue.Count == 0) return;

        Debug.LogWarning($"[ContentBundle] 清单里有 {audit.Residue.Count} 个不属于本次产出的残留内容包" +
                         "（AutoCollect 会把它们收回清单，运行时会多出「配置在、包不在」的 id）：\n   " +
                         string.Join("\n   ", audit.Residue) +
                         "\n   → 跑一次「MCV Build/内容 AB 流水线（按 ProjectClip 分包）」会删掉这些残留配置");
    }

    #endregion

    #region 构建与清理
    static void BuildBundles(List<ClipPlan> plans)
    {
        CleanLegacyArtifacts();          // WHY: 新旧不能并存，顺手清掉 StreamingAssets/Info、InfoModel

        var builds = plans.Select(p => new AssetBundleBuild
        {
            assetBundleName = p.bundleName,
            assetNames = p.entries.Select(e => e.assetPath).ToArray()
        }).ToArray();

        // WHY: sweepStale=true —— Temp 中转 + 只落 bundle 本体 + 清掉不属于本次产出的残留（修 P1-3：此前 Content 侧不清目标目录）。
        BuildOutcome outcome = BundleBuilder.Build(
            EditorPaths.StreamingAssetsRoot, EditorPaths.ContentBundleDirName,
            EditorPaths.TempRoot("ContentBundles"), builds,
            sweepStale: true, logTag: "[ContentBundle]");

        Debug.Log($"[ContentBundle] 构建完成（{EditorUserBuildSettings.activeBuildTarget}），合计 {outcome.TotalBytes / 1024f / 1024f:N2} MB\n{string.Join("\n", outcome.Lines)}");
        if (outcome.Removed.Count > 0)
            Debug.Log($"[ContentBundle] 已清理 {outcome.Removed.Count} 个不属于本次产出的残留产物：\n   {string.Join("\n   ", outcome.Removed)}");

        EditorUtility.DisplayDialog("内容 AB",
            outcome.Ok
                ? $"✔ 已输出 {builds.Length} 个 bundle 到 {EditorPaths.StreamingAssetsRoot}/{EditorPaths.ContentBundleDirName}/\n" +
                  "（只含 bundle 本体，无 .manifest）\n\n明细见 Console。"
                : "构建失败，请看 Console",
            "确定");
    }

    /// <summary>清理旧产物（P1 迁移用）：旧配置目录 + 旧 bundle 目录；返回被删路径。</summary>
    static List<string> CleanLegacyArtifacts()
    {
        var removed = new List<string>();

        foreach (string dir in EditorPaths.LegacyConfigDirs.Concat(EditorPaths.LegacyBundleDirs))
        {
            if (!AssetDatabase.IsValidFolder(dir)) continue;
            AssetDatabase.DeleteAsset(dir);
            removed.Add(dir);
        }

        if (removed.Count > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log($"[ContentBundle] 已清理旧产物：\n   {string.Join("\n   ", removed)}");
        }

        return removed;
    }

    // WHY: IsConfigScriptReady / EnsureFolder 已收敛到 Common/EditorAssetUtil，本文件不得再各持一份（此前两文件逐字重复）。
    #endregion
}
