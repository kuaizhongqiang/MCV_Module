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

/// <summary>
/// 内容 AB 流水线 —— **一个 ProjectClip 一个 AssetBundle**（bundle：<c>Content/clip_{器件小写}</c>）。
///
/// 三条铁律：
///   ① 包粒度 = ProjectClip（该 clip 下各 TaskData 引用的资源全部进同一个包）；
///   ② 包配置 id = <c>ProjectData.json</c> 里**实际写的键值**（规则 <c>{器件}_{任务}_{资源}</c>）；
///   ③ 视频不入包（裸露在 <c>StreamingAssets/Video</c>）。
///
/// 一键做五件事：跑 Provider 收集 → 分级对账 → 生成/更新包配置 → 汇入 <c>PackageDB_Master</c> →
/// 构建到 <c>Assets/StreamingAssets/Content/</c>（Temp 中转，只落 bundle 本体，不带 .manifest）。
///
/// **新增一类内容的唯一登记点**是 <see cref="Providers"/>：加一个 <see cref="IContentProvider"/> 实现即可，
/// id 规则、配置生成、清单同步、构建、对账、旧产物清理全部复用本流程。
///
/// ⚠ 与 LOW 的差异（本工程实情）：
///   - **不登记** <c>InfoSpriteProvider</c>（图集型，决策 F-8 不移植）；
///   - Provider 覆盖 CUR 既有 6 种任务里**单模型型**的三种（Purpose / LineConnection / Training）；
///     Equipment / Principle 是**列表型**（<c>equipmentStructs</c>/<c>principleStructs</c>），需要列表型
///     Provider 才能打包，属后续增量；Info / Structure / Measure 三类数据类尚未落地（P4c/P5b）。
/// </summary>
public static class ContentBundleTools
{
    // 路径常量的唯一来源：Assets/Editor/Common/EditorPaths.cs（本文件不再自带路径字面量）

    #region Provider 登记
    /// <summary>
    /// Provider 列表（顺序即执行顺序）—— 新增一类内容只需在此登记一行。
    ///
    /// 形态：id = 该 TaskData 的 <c>prefabKey</c>，素材 = <c>{ModelRoot}/{id}.prefab</c>。
    /// </summary>
    static readonly IContentProvider[] Providers =
    {
        new ModelPrefabProvider("PurposeModel", "Assets/Prefabs/Models/PurposeObjs", ContentNaming.TaskPurpose,
            clip => clip.GetTaskData<TaskPurposeData>(TaskType.Purpose)?.prefabKey),
        new ModelPrefabProvider("LineConnectionModel", "Assets/Prefabs/Models/LineConnectionObjs", ContentNaming.TaskLineConnection,
            clip => clip.GetTaskData<TaskLineConnectionData>(TaskType.LineConnection)?.prefabKey),
        new ModelPrefabProvider("TrainingModel", "Assets/Prefabs/Models/TrainingObjs", ContentNaming.TaskTraining,
            clip => clip.GetTaskData<TaskTrainingData>(TaskType.Training)?.prefabKey),
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
    [MenuItem("MCV/内容 AB 流水线（按 ProjectClip 分包）", false, 80)]
    public static void RunAll()
    {
        Run(writeConfig: true, build: true, askBeforeBuild: true);
    }

    [MenuItem("MCV/内容 AB/对账（dry-run，不写盘）", false, 81)]
    public static void AuditOnly()
    {
        Run(writeConfig: false, build: false, askBeforeBuild: false);
    }

    [MenuItem("MCV/内容 AB/仅构建", false, 82)]
    public static void BuildOnly()
    {
        Run(writeConfig: false, build: true, askBeforeBuild: false);
    }

    [MenuItem("MCV/内容 AB/仅刷新包清单", false, 83)]
    public static void RefreshDatabaseOnly()
    {
        if (!EditorAssetUtil.IsScriptReady(EditorPaths.AbConfigScript))
        {
            EditorUtility.DisplayDialog("内容 AB", "ABPackageConfigSO 还没编译就绪，等编译结束再跑。", "确定");
            return;
        }

        int count = PackageDatabaseSync.Sync(EditorPaths.PackageDbAsset, null, "[ContentBundle]");
        // 只读告警：本菜单的 AutoCollect 是全量重收，磁盘上的残留配置会被重新写回清单（这里不删残留，但不再静默）
        WarnResiduePackages(ExpectedIdsFromJson());

        EditorUtility.DisplayDialog("内容 AB",
            count == 0
                ? $"清单仍为空：{EditorPaths.PackageDbAsset}\n请先跑「内容 AB 流水线」生成配置"
                : $"✔ 包清单已同步：{count} 条\n{EditorPaths.PackageDbAsset}",
            "确定");
    }
    #endregion

    #region 主流程
    static void Run(bool writeConfig, bool build, bool askBeforeBuild)
    {
        // 前置守卫：脚本没编译就绪时 CreateAsset 会产出 m_Script: {fileID: 0} 的坏配置
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

        // 本次流水线期望的 id 集合（对账体检 / 残留告警共用）
        var expectedIds = new HashSet<string>(plans.SelectMany(p => p.entries).Select(e => e.id));

        if (writeConfig)
        {
            GenerateConfigs(plans);
        }
        else if (build)
        {
            // 仅构建：确保清单收录（配置已在磁盘上）
            PackageDatabaseSync.Sync(EditorPaths.PackageDbAsset, null, "[ContentBundle]");
            // 只读告警：本步的 AutoCollect 会把磁盘上的残留配置一并收回清单（本路径不删残留，只提示）
            WarnResiduePackages(expectedIds);
        }
        // dry-run 到此为止：**不碰数据库**（AutoCollect 是全量重写，会让「不写盘」的承诺落空、也产生无意义的版本 diff）

        if (!build)
        {
            // 只读体检：把「dry-run 真的不写盘」从代码注释的承诺变成弹窗里可见的数字
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
            // else：该 clip 没有任何资源（如四步全 null）→ 跳过，不产出空 AssetBundleBuild
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
            // 与运行时同一口径（JsonReaderWriter → Newtonsoft）；ProjectClip 的 taskXxxData 是
            // [SerializeField, JsonProperty] 私有字段，JsonProperty 保证 Newtonsoft 能写入
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

        // 先删掉目录里不属于本次产出的配置：否则 AutoCollect（全工程按类型全量重收）会把残留 id 收回来，
        // 运行时就出现两套可解析的 id，漏改的 JSON 键变成「配置在、包不在」的隐性问题。
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
                if (created != null) generated.Add(created);      // 创建失败时 PackageConfigWriter 已报错，这里只跳过错项
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 自检：回读磁盘资产确认脚本解析正常。
        // 历史坑：类没写在「与类名同名」的 .cs 里、或脚本未编译完就 CreateAsset 时，m_Script 会写成
        // {fileID: 0}，资产加载为 null（表现为「配置在、清单里却是 None」）。
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

    /// <summary>
    /// 当前 JSON 期望的内容 id 集合（**只读**）。读不到 JSON 时返回 null = 无法判定 → 调用方跳过残留检查。
    /// </summary>
    static HashSet<string> ExpectedIdsFromJson()
    {
        if (!File.Exists(EditorPaths.ProjectDataJson)) return null;      // 不调用 ReadProjectData，避免无谓的 LogError

        ProjectData data = ReadProjectData();
        if (data == null) return null;

        List<ClipPlan> plans = BuildPlans(data, new List<ContentResourceEntry>(), new List<ContentResourceEntry>());
        var ids = new HashSet<string>();
        foreach (ClipPlan plan in plans)
            foreach (ContentResourceEntry e in plan.entries) ids.Add(e.id);
        return ids;
    }

    /// <summary>
    /// 只读残留告警：<c>AutoCollect</c> 是「全工程按类型全量重收」，磁盘上残留的旧配置会被重新写回主清单，
    /// 运行时就多出「配置在、包不在」的可解析 id。
    ///
    /// 这里**只报警、不删**：删残留配置只发生在 <see cref="GenerateConfigs"/>（流水线主菜单）里，
    /// 「仅构建 / 仅刷新包清单」保持原有的"不删文件"语义，但不再静默。
    /// </summary>
    static void WarnResiduePackages(ISet<string> expectedIds)
    {
        if (expectedIds == null || expectedIds.Count == 0) return;

        DatabaseAudit audit = PackageDatabaseSync.Audit(EditorPaths.PackageDbAsset, expectedIds);
        if (audit.Residue.Count == 0) return;

        Debug.LogWarning($"[ContentBundle] 清单里有 {audit.Residue.Count} 个不属于本次产出的残留内容包" +
                         "（AutoCollect 会把它们收回清单，运行时会多出「配置在、包不在」的 id）：\n   " +
                         string.Join("\n   ", audit.Residue) +
                         "\n   → 跑一次「MCV/内容 AB 流水线（按 ProjectClip 分包）」会删掉这些残留配置");
    }
    #endregion

    #region 构建
    static void BuildBundles(List<ClipPlan> plans)
    {
        var builds = plans.Select(p => new AssetBundleBuild
        {
            assetBundleName = p.bundleName,
            assetNames = p.entries.Select(e => e.assetPath).ToArray()
        }).ToArray();

        // Temp 中转 + 只落 bundle 本体 + 清掉不属于本次产出的残留
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
    #endregion
}
