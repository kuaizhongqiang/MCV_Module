using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MCV_Module.Models;
using MCV_Module.Models.System;
using MCV_Module.UI.Components;
using MCV_Module.Utils;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCV_Module.EditorTools.Tools
{
    /// <summary>
    /// 文本 key 工具链（§1 创建 / §3 登记 / §4 校验 / §5 保存后同步 / §8 清理与改名 / §20 触发点）。
    ///
    /// <para><b>口径</b>：key = <c>{prefab 资产相对路径}/{节点路径}</c>。**同步阶段不生成 key** —— key 只由右键创建 / 批量挂载 /
    /// 手动登记写入；保存后同步只把"已有 key 的节点"的中文录入值增量搬进 SO 并**单目标导出**（禁用 ExportAll）。</para>
    ///
    /// <para><b>存量不迁移</b>：`LanguageData.json` 里已有的 65 条语义式 `ui.*` key 不动 —— 它们 `prefabPath` / `prefabGuid`
    /// 均为空，所有反查与清理都按"未登记来源"跳过，不参与自动注册、也不会被清成孤儿。</para>
    /// </summary>
    public static class TextKeyTools
    {
        const string KeyRoot = "MCV Editor/文字/key/";
        const string SyncRoot = "MCV Editor/文字/同步/";
        const string MountMenu = "MCV Editor/文字/挂载 TextComponent";
        const string CreateMenu = "GameObject/UI/TextComponent";
        const string LogTag = "[TextKey]";

        /// <summary>报告目录（<c>Logs/TextSync</c>；§8 明确不用 Temp）。</summary>
        public static string ReportDir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/TextSync"));

        /// <summary>Unity 去重后缀 <c>" (1)"</c>。剥离它才能让同一节点的 key 不随复制次数变化。</summary>
        static readonly Regex DuplicateSuffix = new Regex(@"\s*\(\d+\)$");
        /// <summary>空白与控制字符（<c>/</c> 是分隔符，必须保留）。</summary>
        static readonly Regex IllegalChars = new Regex(@"[\s\u0000-\u001F\u007F]");

        #region 路径与 key
        /// <summary>同步 / 自动注册的触发器白名单：只认工程内的 <c>.prefab</c>（排除 StreamingAssets、Plugins 与包内资产）。</summary>
        public static bool IsSyncablePrefab(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) return false;
            if (path.StartsWith("Assets/StreamingAssets/")) return false;
            if (path.Contains("/Plugins/")) return false;
            if (path.StartsWith("Packages/")) return false;
            return true;
        }

        /// <summary>prefabKey 前缀 = prefab 的**资产相对路径**（相对 <c>Assets/</c>、去 <c>.prefab</c>）。</summary>
        public static string PrefabPrefix(string assetPath)
        {
            string p = (assetPath ?? string.Empty).Replace('\\', '/');
            const string assetsRoot = "Assets/";
            if (p.StartsWith(assetsRoot)) p = p.Substring(assetsRoot.Length);
            if (p.EndsWith(".prefab")) p = p.Substring(0, p.Length - ".prefab".Length);
            return p;
        }

        /// <summary>该组件的 prefab 资产路径；不在 prefab 里（场景节点）返回 null。</summary>
        static string PrefabAssetPathOf(Component comp)
        {
            if (comp == null) return null;

            // Prefab Mode（隔离编辑）：场景里没有实例根，资产路径在 PrefabStage 上。
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.prefabContentsRoot != null && comp.transform.IsChildOf(stage.prefabContentsRoot.transform))
                return stage.assetPath;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(comp.gameObject);
            if (root == null) return null;

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            return IsSyncablePrefab(path) ? path : null;
        }

        /// <summary>组件所在的 prefab 根（节点路径的起点，**不含根名**）。</summary>
        static Transform PrefabRootOf(Component comp)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.prefabContentsRoot != null && comp.transform.IsChildOf(stage.prefabContentsRoot.transform))
                return stage.prefabContentsRoot.transform;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(comp.gameObject);
            if (root != null) return root.transform;

            Transform cur = comp.transform;
            while (cur.parent != null) cur = cur.parent;
            return cur;
        }

        /// <summary>节点路径：从根的直接子节点拼到该节点，<c>/</c> 分隔；根节点本身返回空串。</summary>
        public static string NodePath(Transform node, Transform root)
        {
            if (node == null || root == null || node == root) return string.Empty;

            var parts = new List<string>();
            Transform cur = node;
            while (cur != null && cur != root)
            {
                parts.Add(Segment(cur));
                cur = cur.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>单层段名：剥离 Unity 去重后缀 → 空白 / 控制字符替换成 <c>_</c>；同层同名再加 <c>#siblingIndex</c> 消歧。</summary>
        static string Segment(Transform t)
        {
            string baseName = SegmentBase(t);

            int same = 0;
            Transform parent = t.parent;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    if (SegmentBase(parent.GetChild(i)) == baseName) same++;
                }
            }
            return same > 1 ? baseName + "#" + t.GetSiblingIndex() : baseName;
        }

        static string SegmentBase(Transform t)
        {
            string name = DuplicateSuffix.Replace(t.name, string.Empty);
            name = IllegalChars.Replace(name, "_");
            return string.IsNullOrEmpty(name) ? "_" : name;
        }

        /// <summary>key 合法性（§4）：非空、无首尾空白、无空白与控制字符。</summary>
        public static bool IsValidKey(string key, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(key)) { error = "key 为空"; return false; }
            if (key != key.Trim()) { error = $"key 首尾有空白：「{key}」"; return false; }
            if (IllegalChars.IsMatch(key)) { error = $"key 含空白或控制字符：「{key}」"; return false; }
            return true;
        }

        /// <summary>为一个组件算 key；失败时给出人读的原因。</summary>
        public static bool TryBuildKey(TextComponent comp, out string assetPath, out string key, out string error)
        {
            assetPath = null;
            key = null;
            error = null;

            if (comp == null) { error = "组件为空"; return false; }

            assetPath = PrefabAssetPathOf(comp);
            if (string.IsNullOrEmpty(assetPath))
            {
                // WHY: 场景内文本不参与自动注册（§17）——工具须识别并跳过、不报错。
                error = $"「{comp.name}」不在 prefab 资产内（场景内文本不参与自动注册）";
                return false;
            }

            string nodePath = NodePath(comp.transform, PrefabRootOf(comp));
            if (string.IsNullOrEmpty(nodePath))
            {
                error = $"「{comp.name}」就是 prefab 根节点本身，没有可用的节点路径（请挂到子节点上）";
                return false;
            }

            key = PrefabPrefix(assetPath) + "/" + nodePath;
            return IsValidKey(key, out error);
        }
        #endregion

        #region 唯一 SO 与中文录入面
        /// <summary>取唯一的 LanguageDataSO；缺失或存在多份都算前置错误（§4"多处 SO 前置校验报错"）。</summary>
        static bool TryGetLanguageSO(out LanguageDataSO so, out string error)
        {
            so = null;
            error = null;

            string[] guids = AssetDatabase.FindAssets("t:LanguageDataSO");
            if (guids.Length == 0)
            {
                error = "工程里没有 LanguageDataSO（先建：Assets → Create → MCV → Data → LanguageData）";
                return false;
            }
            if (guids.Length > 1)
            {
                var sb = new StringBuilder("工程里存在多份 LanguageDataSO —— key 表必须唯一，请先只留一份：");
                for (int i = 0; i < guids.Length; i++) sb.Append("\n  ").Append(AssetDatabase.GUIDToAssetPath(guids[i]));
                error = sb.ToString();
                return false;
            }

            so = AssetDatabase.LoadAssetAtPath<LanguageDataSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (so == null) { error = "LanguageDataSO 载入失败"; return false; }
            return true;
        }

        static string[] CreateEmptyClips()
        {
            int count = Enum.GetNames(typeof(LanguageType)).Length;
            var clips = new string[count];
            for (int i = 0; i < count; i++) clips[i] = string.Empty;
            return clips;
        }

        /// <summary>写组件的 languageKey（走 SerializedObject，保证进 Undo 与脏标记）。</summary>
        static void WriteLanguageKey(TextComponent comp, string key)
        {
            var so = new SerializedObject(comp);
            SerializedProperty prop = so.FindProperty("languageKey");
            if (prop == null) { Log.Warning($"{LogTag} 找不到 languageKey 字段"); return; }
            prop.stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>把 SO 单独导出成 JSON（§6：自动路径**禁用 ExportAll**，它会把空 SO 一起覆盖）。</summary>
        static void ExportSingle(LanguageDataSO so)
        {
            so.Export();
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region §3 登记 / §4 校验
        /// <summary>登记一个组件：算 key → 建条目（中文进 clips[0]）→ 写 languageKey → 单目标导出。重名一律阻断。</summary>
        public static bool TryRegister(TextComponent comp, out string key, out string error)
        {
            key = null;
            if (!TryBuildKey(comp, out string assetPath, out key, out error)) return false;
            if (!TryGetLanguageSO(out LanguageDataSO so, out error)) return false;

            so.data ??= new LanguageData();
            so.data.languageClips ??= new List<LanguageClip>();

            // WHY: 先落到局部变量 —— `key` 是 out 参数，编译器不允许它出现在 lambda 里。
            string newKey = key;
            LanguageClip conflict = so.data.languageClips.Find(c => c != null && c.id == newKey);
            if (conflict != null)
            {
                // WHY（§4）：重名**报错并阻断**，不覆盖、不自动加后缀 —— 加后缀会让 key 与节点名悄悄脱钩。
                error = $"key「{key}」已被登记（displayName={conflict.displayName}）；请改**节点名**后重新登记。";
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var clip = new LanguageClip
            {
                id = key,
                displayName = comp.gameObject.name,
                clips = CreateEmptyClips(),
                prefabPath = assetPath,
                prefabGuid = guid,
            };

            // WHY（B2）：中文录入面是组件的**序列化字段 text**（不是运行时 RawText —— 编辑期它恒为空，这正是旧登记链路写不进中文的原因）。
            string literal = comp.LiteralText;
            if (!string.IsNullOrEmpty(literal)) clip.clips[0] = literal;

            so.data.languageClips.Add(clip);
            WriteLanguageKey(comp, key);
            ExportSingle(so);

            Log.Info($"{LogTag} 已登记：{key}（displayName={clip.displayName}，中文 {clip.clips[0].Length} 字）");
            return true;
        }

        [MenuItem(KeyRoot + "登记选中节点的 key", false, 81)]
        public static void RegisterSelected()
        {
            TextComponent[] targets = Selection.GetFiltered<TextComponent>(SelectionMode.Editable | SelectionMode.Deep);
            if (targets.Length == 0) { Log.Warning($"{LogTag} 没有选中 TextComponent"); return; }

            int ok = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                if (TryRegister(targets[i], out string key, out string error)) ok++;
                else Log.Error($"{LogTag} 登记失败：{error}");
            }
            Log.Info($"{LogTag} 登记结束：成功 {ok} / 共 {targets.Length}");
        }

        /// <summary>§4 校验：多处 SO、重名、非法 key、prefabPath 为空但带 GUID 之类的半截登记。</summary>
        [MenuItem(KeyRoot + "校验全部 key（重名 / 非法 / 多处 SO）", false, 80)]
        public static void ValidateAll()
        {
            if (!TryGetLanguageSO(out LanguageDataSO so, out string error)) { Log.Error($"{LogTag} {error}"); return; }

            List<LanguageClip> clips = so.data?.languageClips ?? new List<LanguageClip>();
            var seen = new Dictionary<string, int>();
            var problems = new List<string>();
            int pathKeys = 0;

            for (int i = 0; i < clips.Count; i++)
            {
                LanguageClip c = clips[i];
                if (c == null) { problems.Add($"第 {i} 条为空条目"); continue; }

                if (!IsValidKey(c.id, out string keyError)) problems.Add($"第 {i} 条：{keyError}");
                if (seen.TryGetValue(c.id ?? string.Empty, out int first)) problems.Add($"重名：{c.id}（第 {first} 条与第 {i} 条）");
                else seen[c.id ?? string.Empty] = i;

                bool hasSource = !string.IsNullOrEmpty(c.prefabGuid) || !string.IsNullOrEmpty(c.prefabPath);
                if (!hasSource) continue;   // 存量语义式 key：不参与反查（§3）

                pathKeys++;
                if (string.IsNullOrEmpty(c.prefabGuid)) problems.Add($"{c.id}：有 prefabPath 但缺 prefabGuid（改名/Moved 后无法反查）");
                if (string.IsNullOrEmpty(c.prefabPath)) problems.Add($"{c.id}：有 prefabGuid 但缺 prefabPath");
                if (string.IsNullOrEmpty(c.clips?[0])) problems.Add($"{c.id}：中文槽（clips[0]）为空 —— PickClipText 会退回到字面量或 key 本身");
            }

            if (problems.Count == 0)
            {
                Log.Info($"{LogTag} 校验通过：{clips.Count} 条（其中路径式 {pathKeys} 条，存量语义式 {clips.Count - pathKeys} 条不参与反查）");
                return;
            }

            for (int i = 0; i < problems.Count; i++) Log.Error($"{LogTag} {problems[i]}");
            Log.Error($"{LogTag} 校验未通过：{problems.Count} 个问题（共 {clips.Count} 条）");
        }
        #endregion

        #region §8 孤儿清理
        [MenuItem(KeyRoot + "清理孤儿条目 dry-run（只报告）", false, 90)]
        public static void CleanDryRun() => Clean(false);

        [MenuItem(KeyRoot + "清理孤儿条目（落盘，删前写报告）", false, 91)]
        public static void CleanApply()
        {
            bool ok = EditorUtility.DisplayDialog("清理孤儿语言条目",
                "按「GUID 反查 → 路径刷新 → 判孤儿」的顺序处理：\n"
                + "· GUID 命中 ⇒ 只刷新路径，不删\n"
                + "· GUID 与路径都为空 ⇒ 视为未登记来源，跳过不删\n"
                + "· GUID 查不到且路径不存在 ⇒ 判孤儿并删除\n\n"
                + "删除前会写一份报告到 Logs/TextSync/。",
                "开始清理", "取消");
            if (!ok) return;
            Clean(true);
        }

        /// <summary>执行孤儿清理；<paramref name="apply"/>=false 只出报告（公开以便 MCP / 批处理驱动）。</summary>
        public static void Clean(bool apply)
        {
            if (!TryGetLanguageSO(out LanguageDataSO so, out string error)) { Log.Error($"{LogTag} {error}"); return; }

            List<LanguageClip> clips = so.data?.languageClips;
            if (clips == null) { Log.Warning($"{LogTag} 没有语言条目"); return; }

            var orphans = new List<LanguageClip>();
            var refreshed = new List<string>();
            int skipped = 0;

            for (int i = 0; i < clips.Count; i++)
            {
                LanguageClip c = clips[i];
                if (c == null) continue;

                // ② GUID / 路径均为空 ⇒ 未登记来源（存量语义式 key），跳过删除。
                if (string.IsNullOrEmpty(c.prefabGuid) && string.IsNullOrEmpty(c.prefabPath)) { skipped++; continue; }

                // ① GUID 反查命中 ⇒ **非孤儿**，顺手把路径刷新成最新（改名 / 移动后的收敛）。
                if (!string.IsNullOrEmpty(c.prefabGuid))
                {
                    string livePath = AssetDatabase.GUIDToAssetPath(c.prefabGuid);
                    if (!string.IsNullOrEmpty(livePath))
                    {
                        if (livePath != c.prefabPath)
                        {
                            refreshed.Add($"{c.id}: {c.prefabPath} → {livePath}");
                            if (apply) c.prefabPath = livePath;
                        }
                        continue;
                    }
                }

                // ③ GUID 查不到且路径确不存在 ⇒ 判孤儿。
                if (string.IsNullOrEmpty(c.prefabPath) || !File.Exists(AbsolutePath(c.prefabPath)))
                {
                    orphans.Add(c);
                    continue;
                }
                skipped++;
            }

            string report = WriteRemoveReport(orphans, refreshed, skipped, apply);
            if (apply && orphans.Count > 0)
            {
                for (int i = 0; i < orphans.Count; i++) clips.Remove(orphans[i]);
                ExportSingle(so);
            }
            else if (apply && refreshed.Count > 0)
            {
                ExportSingle(so);
            }

            Log.Info($"{LogTag} {(apply ? "清理完成" : "dry-run")}：孤儿 {orphans.Count} 条，路径刷新 {refreshed.Count} 条，"
                     + $"跳过（未登记来源 / 路径仍存在）{skipped} 条。报告：{report}");
        }

        static string AbsolutePath(string assetRelativeOrAbsolute)
        {
            if (Path.IsPathRooted(assetRelativeOrAbsolute)) return assetRelativeOrAbsolute;
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../" + assetRelativeOrAbsolute));
        }

        static string WriteRemoveReport(List<LanguageClip> orphans, List<string> refreshed, int skipped, bool apply)
        {
            Directory.CreateDirectory(ReportDir);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string path = Path.Combine(ReportDir, $"removed-{stamp}.json");

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
            sb.AppendLine($"  \"applied\": {(apply ? "true" : "false")},");
            sb.AppendLine($"  \"skipped\": {skipped},");
            sb.AppendLine("  \"refreshedPaths\": [");
            for (int i = 0; i < refreshed.Count; i++) sb.AppendLine($"    \"{Escape(refreshed[i])}\"{(i < refreshed.Count - 1 ? "," : string.Empty)}");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"removed\": [");
            for (int i = 0; i < orphans.Count; i++)
            {
                LanguageClip c = orphans[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"id\": \"{Escape(c.id)}\",");
                sb.AppendLine($"      \"displayName\": \"{Escape(c.displayName)}\",");
                sb.AppendLine($"      \"prefabPath\": \"{Escape(c.prefabPath)}\",");
                sb.AppendLine($"      \"prefabGuid\": \"{Escape(c.prefabGuid)}\",");
                sb.AppendLine($"      \"clips\": [\"{Escape(c.clips?[0])}\"]");
                sb.AppendLine($"    }}{(i < orphans.Count - 1 ? "," : string.Empty)}");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        static string Escape(string s) => (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        #endregion

        #region §8 prefab 改名 / 移动
        /// <summary>
        /// prefab 改名 / 移动后：① 重写 prefab 内部所有 <c>languageKey</c> 的前缀；② 刷新 SO 侧 <c>prefabPath</c>。
        /// 不做这两件事，运行期就会查不到 key（静默回退字面量），SO 侧也会留下失效路径。
        /// </summary>
        class PrefabMoveHook : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (movedAssets == null || movedAssets.Length == 0) return;

                var touchedPrefixes = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    string newPath = movedAssets[i];
                    if (!IsSyncablePrefab(newPath)) continue;

                    string oldPrefix = PrefabPrefix(i < movedFromAssetPaths.Length ? movedFromAssetPaths[i] : null);
                    string newPrefix = PrefabPrefix(newPath);
                    if (string.IsNullOrEmpty(oldPrefix) || oldPrefix == newPrefix) continue;

                    string guid = AssetDatabase.AssetPathToGUID(newPath);
                    RewritePrefabKeys(newPath, oldPrefix, newPrefix, guid);
                    touchedPrefixes.Add(new KeyValuePair<string, string>(guid, newPath));
                }

                if (touchedPrefixes.Count > 0) RefreshClipPaths(touchedPrefixes);
            }
        }

        /// <summary>重写 prefab 内部 key 的前缀（只动**带前缀**的 key，存量语义式 key 不碰）。</summary>
        static void RewritePrefabKeys(string assetPath, string oldPrefix, string newPrefix, string guid)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
            if (contents == null) return;

            int rewritten = 0;
            try
            {
                TextComponent[] comps = contents.GetComponentsInChildren<TextComponent>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    TextComponent comp = comps[i];
                    string key = comp.LanguageKey;
                    if (string.IsNullOrEmpty(key)) continue;
                    if (!key.StartsWith(oldPrefix + "/")) continue;

                    string next = newPrefix + key.Substring(oldPrefix.Length);
                    WriteLanguageKey(comp, next);
                    rewritten++;
                }

                if (rewritten > 0) PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            if (rewritten > 0) Log.Info($"{LogTag} prefab 改名：{assetPath} 重写 {rewritten} 条 languageKey（{oldPrefix} → {newPrefix}，guid={guid}）");
        }

        /// <summary>把 SO 里这些 GUID 对应的条目路径刷新成新路径。</summary>
        static void RefreshClipPaths(List<KeyValuePair<string, string>> guidToPath)
        {
            if (!TryGetLanguageSO(out LanguageDataSO so, out string error)) { Log.Warning($"{LogTag} 跳过路径刷新：{error}"); return; }

            List<LanguageClip> clips = so.data?.languageClips;
            if (clips == null) return;

            int changed = 0;
            for (int i = 0; i < clips.Count; i++)
            {
                LanguageClip c = clips[i];
                if (c == null || string.IsNullOrEmpty(c.prefabGuid)) continue;

                for (int j = 0; j < guidToPath.Count; j++)
                {
                    if (c.prefabGuid != guidToPath[j].Key) continue;
                    if (c.prefabPath == guidToPath[j].Value) continue;
                    c.prefabPath = guidToPath[j].Value;
                    changed++;
                    break;
                }
            }

            if (changed > 0)
            {
                ExportSingle(so);
                Log.Info($"{LogTag} prefab 移动：刷新 {changed} 条条目的 prefabPath");
            }
        }
        #endregion

        #region §5 保存后同步
        /// <summary>
        /// 保存 prefab 后：登记待处理路径 → 延后（编辑器空闲）增量同步 → **单目标导出**。
        ///
        /// <para><b>防环三件套</b>：① 触发器白名单（只 <c>.prefab</c>，排除 StreamingAssets / <c>*.json</c> / <c>*.asset</c> —— 导出 JSON 引发的导入因此不会再排队）；
        /// ② 重入锁（同步自身引发的导入不再排期）；③ **同步期只读 prefab**（不生成 key、不写 prefab，diff 短路：无变化不写盘、不导出）。</para>
        /// </summary>
        class AssetSaveHook : AssetPostprocessor
        {
            static readonly HashSet<string> Pending = new HashSet<string>();
            static bool scheduled;
            static bool syncing;

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (syncing) return;

                for (int i = 0; i < importedAssets.Length; i++)
                {
                    if (IsSyncablePrefab(importedAssets[i])) Pending.Add(importedAssets[i]);
                }
                if (Pending.Count == 0 || scheduled) return;

                // WHY: 不在导入期做 IO —— 排到编辑器空闲再做（§5"登记待处理路径 → 延后执行"）。
                scheduled = true;
                EditorApplication.delayCall += Flush;
            }

            static void Flush()
            {
                scheduled = false;
                if (syncing || Pending.Count == 0) return;

                syncing = true;
                try
                {
                    var paths = new string[Pending.Count];
                    Pending.CopyTo(paths);
                    Pending.Clear();

                    bool anyChange = false;
                    for (int i = 0; i < paths.Length; i++) anyChange |= SyncPrefab(paths[i]);

                    // WHY: **单目标 Export()**，绝不 ExportAll —— 自动路径用 ExportAll 会在存在空 SO 时把 SystemData.json 等整体覆盖。
                    if (anyChange && TryGetLanguageSO(out LanguageDataSO so, out _)) ExportSingle(so);
                }
                catch (Exception e)
                {
                    Log.Error($"{LogTag} 保存后同步失败：{e.Message}");
                }
                finally
                {
                    syncing = false;
                }
            }
        }

        /// <summary>
        /// 增量同步一个 prefab：**只读**地把"已有 key 的节点"的中文录入值搬进 SO；返回是否真的改了数据。
        /// 放在外层是为了让手动菜单与保存钩子共用同一份实现（两处实现必漂移）。
        /// </summary>
        public static bool SyncPrefab(string assetPath)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (root == null) return false;

            TextComponent[] comps = root.GetComponentsInChildren<TextComponent>(true);
            if (comps.Length == 0) return false;

            if (!TryGetLanguageSO(out LanguageDataSO so, out string error)) { Log.Warning($"{LogTag} 跳过 {assetPath}：{error}"); return false; }

            List<LanguageClip> clips = so.data?.languageClips;
            if (clips == null) return false;

            bool changed = false;
            for (int i = 0; i < comps.Length; i++)
            {
                TextComponent comp = comps[i];
                string key = comp.LanguageKey;
                // WHY（§3）：同步阶段**不生成 key** —— 没 key 的节点不参与自动注册。
                if (string.IsNullOrEmpty(key)) continue;

                LanguageClip clip = clips.Find(c => c != null && c.id == key);
                if (clip == null)
                {
                    Log.Warning($"{LogTag} {assetPath}：节点「{comp.name}」的 key「{key}」未登记，本次跳过（登记是显式动作）");
                    continue;
                }

                if (clip.clips == null || clip.clips.Length == 0) clip.clips = CreateEmptyClips();

                // WHY: 中文录入面 = 序列化字段 text；**只写 clips[0]，其余语言槽不动**（英文由人后填）。
                string literal = comp.LiteralText;
                if (string.IsNullOrEmpty(literal)) continue;
                if (clip.clips[0] == literal) continue;   // diff 短路：无变化不写盘、不导出

                clip.clips[0] = literal;
                changed = true;
            }

            if (changed) Log.Info($"{LogTag} 保存后同步：{assetPath}");
            return changed;
        }
        #endregion

        #region §1 创建 / 挂载
        [MenuItem(CreateMenu, false, 10)]
        static void CreateTextComponent(MenuCommand command)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI), typeof(TextComponent));
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create TextComponent");
            Selection.activeGameObject = go;
        }

        /// <summary>批量挂载：把选中范围内（含子节点）的 Text / TMP 节点补挂 TextComponent 并搬属性、登记 key。幂等。</summary>
        [MenuItem(MountMenu, false, 76)]
        public static void MountSelected()
        {
            GameObject[] roots = Selection.gameObjects;
            if (roots == null || roots.Length == 0) { Log.Warning($"{LogTag} 没有选中任何节点"); return; }

            int mounted = 0, skipped = 0, registered = 0;
            for (int r = 0; r < roots.Length; r++)
            {
                GameObject root = roots[r];
                if (root == null) continue;

                TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                var mountedHere = new List<TextComponent>();

                for (int i = 0; i < tmps.Length; i++)
                {
                    // WHY: 遍历只处理**本资产自有节点**、排除嵌套 prefab 实例内部节点（§17）—— 嵌套内的组件由其自身资产登记。
                    if (IsNestedInsideOtherPrefab(root, tmps[i].gameObject)) { skipped++; continue; }
                    if (tmps[i].GetComponent<TextComponent>() != null) { skipped++; continue; }

                    mountedHere.Add(Mount(tmps[i].gameObject, tmps[i].fontSize, tmps[i].color, tmps[i].text));
                    mounted++;
                }

                for (int i = 0; i < mountedHere.Count; i++)
                {
                    if (TryRegister(mountedHere[i], out _, out string error)) registered++;
                    else Log.Warning($"{LogTag} 已挂载但未登记：「{mountedHere[i].name}」：{error}");
                }
            }

            AssetDatabase.SaveAssets();
            Log.Info($"{LogTag} 挂载结束：新增 {mounted} 个 TextComponent，登记 {registered} 条，跳过（已有组件 / 嵌套实例）{skipped} 个节点。");
        }

        /// <summary>给一个 TMP 节点补挂 TextComponent 并把显示属性搬进它（文案进 <c>text</c> 录入面）。</summary>
        static TextComponent Mount(GameObject go, float fontSize, Color color, string literal)
        {
            var comp = Undo.AddComponent<TextComponent>(go);
            var so = new SerializedObject(comp);

            SerializedProperty prop = so.FindProperty("fontSize");
            if (prop != null) prop.intValue = Mathf.RoundToInt(fontSize);
            prop = so.FindProperty("color");
            if (prop != null) prop.colorValue = color;
            prop = so.FindProperty("text");
            if (prop != null) prop.stringValue = literal ?? string.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(comp);
            return comp;
        }

        /// <summary>该节点是否属于某个"更外层、且不是本次处理根"的 prefab 实例内部。</summary>
        static bool IsNestedInsideOtherPrefab(GameObject root, GameObject node)
        {
            GameObject instRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(node);
            return instRoot != null && instRoot != root;
        }
        #endregion

        #region 手动同步（§20 手动菜单）
        [MenuItem(SyncRoot + "同步选中 prefab（SO ← 中文录入面）", false, 92)]
        public static void SyncSelectedPrefabs()
        {
            GameObject[] roots = Selection.gameObjects;
            if (roots == null || roots.Length == 0) { Log.Warning($"{LogTag} 没有选中任何 prefab / 节点"); return; }

            var paths = new HashSet<string>();
            for (int i = 0; i < roots.Length; i++)
            {
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(roots[i]);
                if (!string.IsNullOrEmpty(path)) paths.Add(path);
            }
            if (paths.Count == 0) { Log.Warning($"{LogTag} 选中的不是 prefab 资产或其实例"); return; }

            bool any = false;
            foreach (string path in paths) any |= SyncPrefab(path);
            if (any && TryGetLanguageSO(out LanguageDataSO so, out _)) ExportSingle(so);
            Log.Info($"{LogTag} 手动同步结束：{paths.Count} 个 prefab，{(any ? "有更新" : "无变化（diff 短路）")}");
        }
        #endregion
    }
}