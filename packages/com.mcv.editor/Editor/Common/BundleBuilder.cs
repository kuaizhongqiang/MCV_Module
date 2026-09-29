using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    // WHY: 两条铁律不可拆 —— ① 先构建到 Temp/MCV_*，只把 bundle 本体（不带 .manifest）拷进 StreamingAssets，最后删 Temp，本类不提供第二套拷贝路径；② sweepStale=true 时先删目标目录里不属于本次产出的文件（修复 P1-3：Content 侧不清、CameraBg 侧清导致的语义分叉）。
    /// <summary>把 <c>AssetBundleBuild[]</c> 构建产物落到 <c>{outputRoot}/{dirName}/</c>（Temp 中转、只拷本体、可选清残留）。</summary>
    public static class BundleBuilder
    {
        /// <summary>构建并落盘到 <c>{outputRoot}/{dirName}/</c>；bundle 名 = 相对 StreamingAssets 的路径（可含目录段）。</summary>
        public static BuildOutcome Build(string outputRoot, string dirName, string tempRoot,
                                         AssetBundleBuild[] builds, bool sweepStale, string logTag)
        {
            var outcome = new BuildOutcome
            {
                Lines = new List<string>(),
                Missing = new List<string>(),
                Removed = new List<string>(),
            };

            if (builds == null || builds.Length == 0) return outcome;

            string targetDir = Path.Combine(outputRoot, dirName);

            if (Directory.Exists(tempRoot)) FileUtil.DeleteFileOrDirectory(tempRoot);
            Directory.CreateDirectory(tempRoot);
            Directory.CreateDirectory(targetDir);

            var manifest = BuildPipeline.BuildAssetBundles(
                tempRoot, builds, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

            bool ok = manifest != null;

            long total = 0;
            foreach (AssetBundleBuild build in builds)
            {
                string src = Path.Combine(tempRoot, build.assetBundleName);
                string dst = Path.Combine(targetDir, Path.GetFileName(build.assetBundleName));

                if (!File.Exists(src))
                {
                    outcome.Missing.Add(build.assetBundleName);
                    outcome.Lines.Add($"   {build.assetBundleName}  缺失");
                    Debug.LogError($"{logTag} 构建失败：产物缺失 {src}");
                    continue;
                }

                File.Copy(src, dst, true);
                var info = new FileInfo(dst);
                total += info.Length;
                outcome.Lines.Add($"   {dirName}/{Path.GetFileName(dst)}  {info.Length / 1024f:N1} KB  ({build.assetNames.Length} 项)");
            }

            // WHY: 残留清理 gate 在「构建成功且产物齐全」之后 —— 构建失败时目标目录可能是唯一的可用产物来源，清残留等于在失败现场再动一次目录。
            if (sweepStale && ok && outcome.Missing.Count == 0)
                outcome.Removed = SweepStale(targetDir, builds.Select(b => Path.GetFileName(b.assetBundleName)));

            FileUtil.DeleteFileOrDirectory(tempRoot);
            AssetDatabase.Refresh();

            outcome.BundleCount = builds.Length - outcome.Missing.Count;
            outcome.TotalBytes = total;
            outcome.Ok = ok && outcome.Missing.Count == 0;
            return outcome;
        }

        // WHY: 多目录重载的存在理由是「同批构建」—— Unity 的「同一资产被显式分配后不再复制」只在**同一次 BuildAssetBundles 调用**内成立。
        // 实测：面板包与字体包分开构建时字体被复制进面板包（6187 KB）；同一次调用里同时声明两包才只记依赖（面板 152 KB + 字体包 6033 KB）。
        // 故调用方必须能一次传进多个不同目录段的 bundle，本重载负责「一次构建 + 各落各自目录 + 按目录各自清残留」。
        /// <summary>多目录构建：一次 <c>BuildAssetBundles</c> 构建全部 <paramref name="builds"/>，每个 bundle 本体按自己的目录段落到 <c>{outputRoot}/{目录段}/</c>，残留清理**按目录各自**进行。</summary>
        public static BuildOutcome Build(string outputRoot, string tempRoot,
                                         AssetBundleBuild[] builds, bool sweepStale, string logTag)
        {
            var outcome = new BuildOutcome
            {
                Lines = new List<string>(),
                Missing = new List<string>(),
                Removed = new List<string>(),
            };

            if (builds == null || builds.Length == 0) return outcome;

            if (Directory.Exists(tempRoot)) FileUtil.DeleteFileOrDirectory(tempRoot);
            Directory.CreateDirectory(tempRoot);

            var manifest = BuildPipeline.BuildAssetBundles(
                tempRoot, builds, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

            bool ok = manifest != null;

            long total = 0;
            // WHY: 按目录段分组，为的是「残留清理按目录各自进行」—— 拿全部 bundle 名去清某一个目录会删掉别的流水线的产物。
            var perDir = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (AssetBundleBuild build in builds)
            {
                string dirSegment = BundleDirSegment(build.assetBundleName);
                if (string.IsNullOrEmpty(dirSegment))
                {
                    outcome.Missing.Add(build.assetBundleName);
                    outcome.Lines.Add($"   {build.assetBundleName}  缺失（bundle 名不是「目录段/文件名段」，算不出落盘目录）");
                    Debug.LogError($"{logTag} 构建失败：bundle 名不合法 {build.assetBundleName}");
                    continue;
                }

                string targetDir = Path.Combine(outputRoot, dirSegment);
                Directory.CreateDirectory(targetDir);

                string src = Path.Combine(tempRoot, build.assetBundleName);
                string dst = Path.Combine(targetDir, Path.GetFileName(build.assetBundleName));

                if (!File.Exists(src))
                {
                    outcome.Missing.Add(build.assetBundleName);
                    outcome.Lines.Add($"   {build.assetBundleName}  缺失");
                    Debug.LogError($"{logTag} 构建失败：产物缺失 {src}");
                    continue;
                }

                File.Copy(src, dst, true);
                var info = new FileInfo(dst);
                total += info.Length;
                outcome.Lines.Add($"   {dirSegment}/{Path.GetFileName(dst)}  {info.Length / 1024f:N1} KB  ({build.assetNames.Length} 项)");

                if (!perDir.TryGetValue(dirSegment, out List<string> keep))
                {
                    keep = new List<string>();
                    perDir[dirSegment] = keep;
                }
                keep.Add(Path.GetFileName(build.assetBundleName));
            }

            // WHY: 残留清理与单目录版同口径 gate 在「构建成功且产物齐全」之后；这里只遍历本批真正落过盘的目录，且只带该目录自己的 bundle 文件名
            if (sweepStale && ok && outcome.Missing.Count == 0)
            {
                foreach (KeyValuePair<string, List<string>> kvp in perDir)
                    outcome.Removed.AddRange(SweepStale(Path.Combine(outputRoot, kvp.Key), kvp.Value));
            }

            FileUtil.DeleteFileOrDirectory(tempRoot);
            AssetDatabase.Refresh();

            outcome.BundleCount = builds.Length - outcome.Missing.Count;
            outcome.TotalBytes = total;
            outcome.Ok = ok && outcome.Missing.Count == 0;
            return outcome;
        }

        /// <summary>bundle 名的目录段（<c>UI/ui</c> → <c>UI</c>）；不是「目录段/文件名段」两段式时返回 null。</summary>
        static string BundleDirSegment(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName)) return null;

            string[] parts = bundleName.Split('/');
            return parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0 ? parts[0] : null;
        }

        /// <summary>清掉 <c>targetDir</c> 里不在 <c>keepFileNames</c> 内的文件（连同 <c>.meta</c>），走 <c>AssetDatabase.DeleteAsset</c> 避免孤立 .meta 告警，返回被删资产路径。</summary>
        public static List<string> SweepStale(string targetDir, IEnumerable<string> keepFileNames)
        {
            var removed = new List<string>();
            if (!Directory.Exists(targetDir)) return removed;

            var keep = new HashSet<string>(keepFileNames ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            string dir = targetDir.Replace('\\', '/');

            foreach (string file in Directory.GetFiles(dir))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;   // WHY: .meta 跟本体一起处理，跳过
                if (keep.Contains(name)) continue;

                string assetPath = dir + "/" + name;
                if (!AssetDatabase.DeleteAsset(assetPath))
                {
                    File.Delete(file);
                    string meta = file + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                }

                removed.Add(assetPath);
            }

            return removed;
        }
    }

    /// <summary>构建结果（见 <c>BundleBuilder.Build</c> 的两个重载）。</summary>
    public struct BuildOutcome
    {
        /// <summary>构建成功且产物齐全（manifest 非空、无缺失本体）。</summary>
        public bool Ok;

        /// <summary>成功拷贝的本体数。</summary>
        public int BundleCount;

        /// <summary>拷贝产物的总字节数。</summary>
        public long TotalBytes;

        /// <summary>逐包明细行（<c>目录/文件名  大小 KB  (N 项)</c>）。</summary>
        public List<string> Lines;

        /// <summary>Temp 里缺产物（构建失败）的 bundle 名。</summary>
        public List<string> Missing;

        /// <summary><c>sweepStale</c> 清掉的残留资产路径。</summary>
        public List<string> Removed;
    }
}
