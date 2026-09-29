using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Models.Addressable;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    // WHY: B1.5 —— CameraBg 与 RoomOne 两个专用工具曾各持一份「编译就绪守卫 → 素材校验 → 写包配置 → 回读自检 → 同步主清单 → 构建到 StreamingAssets」流程（8 段几乎逐行重复）；Docs/design_ai/BundlePipeline.md:230 已定「再加第三个全局包时应当先补这个抽象，而不是再抄一个工具」，本类就是那个抽象的唯一执行器。
    // WHY: 依赖方向单向 —— 本类只认运行时程序集里的 MCV_Module.Interfaces.IGlobalBundleProvider 与 Common 内核，provider 实现住在 BuildTools/GlobalProviders；Common 绝不反向引用 BuildTools（AGENT.md 红线）。
    /// <summary>全局包（不属任何 clip、<c>clipId</c> 一律留空）AB 流水线的唯一执行器：一个 bundle 一组资源，由 <see cref="IGlobalBundleProvider"/> 驱动，产物目录 = bundle 名首段。</summary>
    public static class GlobalBundleRunner
    {
        /// <summary>非交互核心：编译守卫 → 收集 → 素材校验 → 残留配置清理 → 写包配置 → 回读自检 → 同步主清单 →（可选）构建到 <c>StreamingAssets/{bundle 首段}/</c>；<paramref name="interactive"/> = false 时不弹任何对话框，供自动化 / 测试直接调用。</summary>
        public static void Run(IGlobalBundleProvider provider, bool build, bool interactive)
        {
            if (provider == null)
            {
                Debug.LogError("[GlobalBundle] provider 为 null，未做任何改动");
                return;
            }

            string name = provider.Name;
            string logTag = $"[{name}AB]";
            string dialogTitle = $"{name} AB";
            string bundleName = provider.BundleName;
            string configDir = provider.ConfigDir;

            // WHY: 不变量 —— 产物目录 = bundle 名的首段（与 Content/CameraBg/RoomOne 同口径）；末段还必须全小写，否则运行时拼 URL 会被强制小写而在 WebGL / Linux 上 404。
            string dirName = BundleDirName(bundleName);
            if (string.IsNullOrEmpty(dirName) || !ContentNaming.IsBundleFileNameLowerCase(bundleName))
            {
                Debug.LogError($"{logTag} bundle 名不合法：{bundleName}（须形如「目录段/小写文件名段」，如 UI/ui）");
                return;
            }

            // ① 编译就绪守卫 —— 脚本没编译就绪时 CreateAsset 会把 m_Script 写成 {fileID: 0}，产出加载为 null 的坏配置
            if (!EditorAssetUtil.IsScriptReady(EditorPaths.AbConfigScript))
            {
                if (interactive)
                    EditorUtility.DisplayDialog(dialogTitle,
                        "ABPackageConfigSO 还没编译就绪（Unity 正在编译 / 域重载中）。\n\n等编译结束再跑本菜单。", "确定");
                return;
            }

            // ② 收集条目（从固定表还是从目录扫描由 provider 决定）
            List<GlobalResourceEntry> entries = Collect(provider);
            if (entries.Count == 0)
            {
                string msg = $"{logTag} 没有收集到任何资源条目（目录不存在 / 为空 / 条目全被跳过），已中止：不产出空包、也未改动任何配置\n" +
                             $"   → 素材就位后再跑：{provider.MenuPath}";
                Debug.LogError(msg);
                if (interactive) EditorUtility.DisplayDialog(dialogTitle, msg, "确定");
                return;
            }

            // ③ 素材存在性 + 导入类型校验（按 kind 选口径），另查 id 重复
            List<string> problems = Validate(entries, provider.AbortOnFirstAssetProblem);
            if (problems.Count > 0)
            {
                foreach (string p in problems) Debug.LogError($"{logTag} {p}");

                if (interactive)
                    EditorUtility.DisplayDialog(dialogTitle,
                        provider.AbortOnFirstAssetProblem
                            ? problems[0]
                            : $"素材校验未通过（{problems.Count} 条，见 Console）：\n\n" + HeadLines(problems, 6),
                        "确定");
                return;
            }

            var ids = new List<string>(entries.Count);
            foreach (GlobalResourceEntry e in entries) ids.Add(e.id);
            var expectedIds = new HashSet<string>(ids);

            // ③ 残留清理（策略由 provider 决定）：先删掉目录里不属于本次产出的配置 —— 否则 AutoCollect（全工程按类型全量重收）会把残留 id 收回主清单，运行时就多出「配置在、包不在」的可解析 id
            if (provider.CleanStaleConfigs)
            {
                List<string> removed = EditorAssetUtil.DeleteAssetsNotIn<ABPackageConfigSO>(
                    configDir, expectedIds, config => config.id, logTag);
                if (removed.Count > 0)
                    Debug.Log($"{logTag} 已清理 {removed.Count} 个不属于本次产出的旧配置：\n   " + string.Join("\n   ", removed));
            }

            // ③ 写包配置：clipId 一律 null —— 全局包不参与按 clip 的装卸链路（GlobalAddressableMgr.GetConfigsByClip 对空 clipId 返回空表）
            var generated = new List<ABPackageConfigSO>();
            foreach (GlobalResourceEntry e in entries)
            {
                ABPackageConfigSO created = PackageConfigWriter.CreateOrUpdate(
                    configDir, e.id, logTag,
                    // WHY: assetKind 必须显式写 —— 不写就吃 SO 字段默认值，而该默认值与 Provider 侧默认值并不一致（P1-8）
                    config => PackageConfigWriter.ApplyContent(config, e.id, null, bundleName, e.assetPath, e.kind));
                if (created != null) generated.Add(created);      // WHY: 创建失败时 PackageConfigWriter 已报错，这里只跳过错项
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ④ 回读自检：脚本丢失的配置回读为 null（历史坑），删掉并提示重跑一次
            List<string> broken = EditorAssetUtil.FindBrokenAssets<ABPackageConfigSO>(configDir, ids);
            if (broken.Count > 0)
            {
                foreach (string path in broken) AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
                Debug.LogError($"{logTag} {broken.Count} 个配置资产的 m_Script 丢失，已删除：\n   " +
                               string.Join("\n   ", broken) + "\n   → 再跑一次本菜单即可重建");
            }

            // ⑤ 同步主清单（唯一写入口；AutoCollect 是全量重收，只允许在这里被调用）
            PackageDatabaseSync.Sync(EditorPaths.PackageDbAsset, generated, logTag);

            int ready = generated.Count - broken.Count;

            // ⑥ 仅生成配置入口
            if (!build)
            {
                Debug.Log($"{logTag} 配置就绪：{generated.Count} 条 → {configDir}（未构建）");
                if (interactive)
                    EditorUtility.DisplayDialog(dialogTitle,
                        $"✔ 配置已就绪：{ready} 条\nbundle：{bundleName}\n\n（未构建，可再跑主菜单构建）", "确定");
                return;
            }

            // ⑦ 规划构建批次：本包 + BuildWith 声明的依赖包（必须在**同一次** BuildAssetBundles 里构建，理由见 IGlobalBundleProvider.BuildWith）
            var batchBuilds = new List<AssetBundleBuild>();
            var batchNames = new List<string>();

            batchBuilds.Add(PlanBuild(bundleName, entries));
            batchNames.Add(bundleName);

            foreach (IGlobalBundleProvider dep in ResolveDependencies(provider, logTag))
            {
                // WHY: 依赖包的 bundle 名同样受「目录段/小写文件名段」不变量约束 —— 落盘目录由它算出来，算不出就不能进这一批
                string depDir = BundleDirName(dep.BundleName);
                if (string.IsNullOrEmpty(depDir) || !ContentNaming.IsBundleFileNameLowerCase(dep.BundleName))
                {
                    Debug.LogError($"{logTag} 依赖包 bundle 名不合法：{dep.BundleName}（须形如「目录段/小写文件名段」，如 Fonts/font），已跳过该依赖包");
                    continue;
                }

                List<GlobalResourceEntry> depEntries = Collect(dep);
                if (depEntries.Count == 0)
                {
                    // WHY: 依赖包没条目时只告警、不中止 —— 主包的配置与构建仍应照常完成（例如字体清单还没建好，不该连带 UI 包也建不出来）
                    Debug.LogWarning($"{logTag} 同批依赖包 {dep.BundleName} 没有收集到条目，已跳过该包（主包仍按本批构建）");
                    continue;
                }

                batchBuilds.Add(PlanBuild(dep.BundleName, depEntries));
                batchNames.Add(dep.BundleName);
            }

            if (batchNames.Count > 1)
                Debug.Log($"{logTag} 本次同批构建：{string.Join(" + ", batchNames)}" +
                          "（同一次 BuildAssetBundles —— 分开构建会把被引用的资源复制进本包，而不是记依赖）");

            // ⑥ 构建前确认（interactive=false 直接构建，不弹任何窗）
            if (interactive &&
                !EditorUtility.DisplayDialog(dialogTitle,
                    $"配置已就绪：{ready} 条\nbundle：{bundleName}\n\n是否立即构建 AssetBundle？\n" +
                    $"本次同批构建：{string.Join(" + ", batchNames)}\n" +
                    $"输出：{string.Join("、", batchNames.ConvertAll(n => EditorPaths.StreamingAssetsRoot + "/" + BundleDirName(n) + "/"))}",
                    "构建", "取消"))
            {
                Debug.Log($"{logTag} 已取消构建（配置已保存，可再跑本菜单只做构建）");
                return;
            }

            // ⑦ 构建：一次 BuildAssetBundles 构建整批；Temp 中转 + 各包本体只落自己目录 + 残留清理按目录各自进行
            // （sweepStale=true —— 每个目标目录里只该有本次产出的本体，目录独立于 Content/，两条流水线互不清理）
            BuildOutcome outcome = BundleBuilder.Build(
                EditorPaths.StreamingAssetsRoot,
                EditorPaths.TempRoot(name + "Bundle"),
                batchBuilds.ToArray(),
                sweepStale: true, logTag: logTag);

            if (outcome.Ok)
            {
                Debug.Log($"{logTag} 构建完成（{EditorUserBuildSettings.activeBuildTarget}）：" +
                          $"{outcome.TotalBytes / 1024f:N1} KB / {outcome.BundleCount} 个包 → {EditorPaths.StreamingAssetsRoot}/\n" +
                          string.Join("\n", outcome.Lines));
            }

            if (interactive)
                EditorUtility.DisplayDialog(dialogTitle,
                    outcome.Ok
                        ? $"✔ 已输出 {outcome.BundleCount} 个包：{string.Join(" + ", batchNames)}\n{EditorPaths.StreamingAssetsRoot}/"
                        : "构建失败，请看 Console", "确定");
        }

        // ── 内部 ─────────────────────────────────────────────────────

        /// <summary>把条目列表折成一条 <see cref="AssetBundleBuild"/>（assetNames 保序 —— 顺序只影响包内顺序，不影响按 id 取件）。</summary>
        static AssetBundleBuild PlanBuild(string bundleName, List<GlobalResourceEntry> entries)
        {
            var assetNames = new List<string>(entries.Count);
            foreach (GlobalResourceEntry e in entries) assetNames.Add(e.assetPath);

            return new AssetBundleBuild { assetBundleName = bundleName, assetNames = assetNames.ToArray() };
        }

        /// <summary>取 provider 声明的同批构建依赖包：跳过空项、按 bundle 名去重（含主包自身），保持声明顺序。</summary>
        static List<IGlobalBundleProvider> ResolveDependencies(IGlobalBundleProvider provider, string logTag)
        {
            var list = new List<IGlobalBundleProvider>();

            IEnumerable<IGlobalBundleProvider> deps = provider.BuildWith;
            if (deps == null) return list;

            // WHY: 主包自己的 bundle 名先入 seen —— 依赖表里重复声明自己会导致同一 bundle 出现两条 AssetBundleBuild（构建报重复 / 落盘互相覆盖）
            var seen = new HashSet<string> { provider.BundleName };

            foreach (IGlobalBundleProvider dep in deps)
            {
                if (dep == null) continue;
                if (string.IsNullOrEmpty(dep.BundleName))
                {
                    Debug.LogWarning($"{logTag} 依赖表里有一个 bundle 名为空的 provider，已跳过");
                    continue;
                }
                if (!seen.Add(dep.BundleName))
                {
                    // WHY: 同一个依赖包被声明两次（或声明了自己）只构建一次
                    continue;
                }
                list.Add(dep);
            }

            return list;
        }

        /// <summary>跑 provider 收集并剔除 id 为空的条目（id 为空无法写配置、也无法按 id 取件，与内容流水线 BuildPlans 的口径一致）。</summary>
        static List<GlobalResourceEntry> Collect(IGlobalBundleProvider provider)
        {
            var list = new List<GlobalResourceEntry>();

            IEnumerable<GlobalResourceEntry> raw = provider.Collect();
            if (raw == null) return list;

            foreach (GlobalResourceEntry e in raw)
            {
                if (e == null || string.IsNullOrEmpty(e.id)) continue;
                list.Add(e);
            }

            return list;
        }

        /// <summary>素材校验：按 <c>kind</c> 选口径（Sprite → Texture2D + TextureImporter；Font / TmpFont → 主资产非空；其余 → GameObject），再查 id 重复，返回问题清单（空 = 通过）。</summary>
        static List<string> Validate(List<GlobalResourceEntry> entries, bool abortOnFirst)
        {
            var problems = new List<string>();

            foreach (GlobalResourceEntry e in entries)
            {
                if (string.IsNullOrEmpty(e.assetPath))
                {
                    problems.Add($"条目 {e.id} 的 assetPath 为空（provider 未能定出素材路径）");
                    if (abortOnFirst) return problems;
                    continue;
                }

                if (e.kind == ContentAssetKind.Sprite)
                {
                    // WHY: 贴图必须按 Sprite 导入 —— 运行时按 Sprite 泛型加载后取 .texture，导入类型不对会拿到 null
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(e.assetPath) == null)
                    {
                        problems.Add(WithNote(e, $"找不到贴图：{e.assetPath}"));
                        if (abortOnFirst) return problems;
                        continue;
                    }

                    var importer = AssetImporter.GetAtPath(e.assetPath) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.Sprite)
                    {
                        problems.Add($"{e.assetPath} 的 Texture Type 不是 Sprite（当前 {importer.textureType}），" +
                                     "运行期按 Sprite 加载会拿到 null，请改成 Sprite (2D and UI)");
                        if (abortOnFirst) return problems;
                    }
                }
                else if (e.kind == ContentAssetKind.Font || e.kind == ContentAssetKind.TmpFont)
                {
                    // WHY: 字体两种形态（Font / TMP_FontAsset）都只做「主资产非空」校验 —— 用 LoadMainAssetAtPath 取 Object，
                    // 不必引 TMPro 类型进共享内核（Assets/Editor/Common 只依赖 Interfaces + Models，AGENT.md 红线）；
                    // 是否存在 / 路径是否写错已由上面的 assetPath 与这里的 null 判定覆盖。
                    if (AssetDatabase.LoadMainAssetAtPath(e.assetPath) == null)
                    {
                        problems.Add(WithNote(e, $"找不到字体资产：{e.assetPath}"));
                        if (abortOnFirst) return problems;
                    }
                }
                else
                {
                    // WHY: 面板 / 碎片是 Prefab —— 运行时按 Prefab 泛型加载（GameObject）
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(e.assetPath) == null)
                    {
                        problems.Add(WithNote(e, $"找不到预制体：{e.assetPath}"));
                        if (abortOnFirst) return problems;
                    }
                }
            }

            // WHY: id 重复会让后一条覆盖前一条（配置路径 = AB_{id}.asset 同名），故自检；放在素材校验之后，保证问题清单的先后顺序稳定
            var seen = new HashSet<string>();
            foreach (GlobalResourceEntry e in entries)
                if (!seen.Add(e.id)) problems.Add($"包配置 id 重复：{e.id}");

            return problems;
        }

        /// <summary>把条目备注（note）作为括号补充附在问题文案后 —— RoomOne 靠它保留「表中文件名必须逐字一致」的提示。</summary>
        static string WithNote(GlobalResourceEntry entry, string message)
        {
            return string.IsNullOrEmpty(entry.note) ? message : $"{message}（{entry.note}）";
        }

        /// <summary>bundle 名的首段 = 产物目录（相对 StreamingAssets）；不是「目录段/文件名段」两段式时返回 null（调用方报错中止）。</summary>
        static string BundleDirName(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName)) return null;

            string[] parts = bundleName.Split('/');
            return parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0 ? parts[0] : null;
        }

        /// <summary>只列前 <paramref name="max"/> 条，其余折成一行提示（弹窗不撑爆）。</summary>
        static string HeadLines(List<string> lines, int max)
        {
            if (lines.Count <= max) return string.Join("\n", lines);
            return string.Join("\n", lines.GetRange(0, max)) + $"\n…… 另有 {lines.Count - max} 条，见 Console";
        }
    }
}
