using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MCV_Module.EditorTools.Common
{
    /// <summary>
    /// <c>AssetBundleBuild[]</c> → 产物落到 <c>{outputRoot}/{dirName}/</c>。
    ///
    /// 固化两条铁律：
    ///   ① **Temp 中转**：先构建到 <c>Temp/MCV_*</c>，只把 bundle **本体** <c>File.Copy</c> 进 StreamingAssets，
    ///      不带 <c>.manifest</c>，最后删 Temp —— 本类不提供第二套拷贝路径；
    ///   ② **清残留**（<paramref name="sweepStale"/>）：目标目录里不属于本次产出的文件先删掉
    ///      （否则删掉一个 clip 后 <c>StreamingAssets/Content/clip_xxx</c> 会与「配置已删」并存且无人报告）。
    /// </summary>
    public static class BundleBuilder
    {
        /// <summary>构建并落盘。</summary>
        /// <param name="outputRoot">bundle 输出根（如 <c>Assets/StreamingAssets</c>）</param>
        /// <param name="dirName">输出目录段（如 <c>Content</c>）</param>
        /// <param name="tempRoot">Temp 中转目录（绝对路径）</param>
        /// <param name="builds">构建描述（bundle 名 = 相对 StreamingAssets 的路径，可含目录段）</param>
        /// <param name="sweepStale">true = 先清掉目标目录里不属于本次产出的残留</param>
        /// <param name="logTag">日志前缀（如 <c>[ContentBundle]</c>）</param>
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

            // 残留清理 gate 在「构建成功且产物齐全」之后：构建失败时目标目录可能是唯一的可用产物来源，
            // 此时清残留等于在失败现场再动一次目录。
            if (sweepStale && ok && outcome.Missing.Count == 0)
                outcome.Removed = SweepStale(targetDir, builds.Select(b => Path.GetFileName(b.assetBundleName)));

            FileUtil.DeleteFileOrDirectory(tempRoot);
            AssetDatabase.Refresh();

            outcome.BundleCount = builds.Length - outcome.Missing.Count;
            outcome.TotalBytes = total;
            outcome.Ok = ok && outcome.Missing.Count == 0;
            return outcome;
        }

        /// <summary>
        /// 清掉 <paramref name="targetDir"/> 里不在 <paramref name="keepFileNames"/> 内的文件（连同 <c>.meta</c>）。
        /// 走 <c>AssetDatabase.DeleteAsset</c>，避免留下孤立 <c>.meta</c> 告警。
        /// </summary>
        /// <returns>被删掉的资产路径</returns>
        public static List<string> SweepStale(string targetDir, IEnumerable<string> keepFileNames)
        {
            var removed = new List<string>();
            if (!Directory.Exists(targetDir)) return removed;

            var keep = new HashSet<string>(keepFileNames ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            string dir = targetDir.Replace('\\', '/');

            foreach (string file in Directory.GetFiles(dir))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;   // 跟本体一起处理
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

    /// <summary>构建结果（见 <see cref="BundleBuilder.Build"/>）。</summary>
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
