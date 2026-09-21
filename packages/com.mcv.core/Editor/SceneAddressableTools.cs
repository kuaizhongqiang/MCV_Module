using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCV_Module.Models.Addressable;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// AA 场景一键工具 —— 三合一流水线。
/// </summary>
public static class SceneAddressableTools
{
    private const string CONFIG_PATH = "Assets/Resources/Config/SceneAAConfig.asset";
    private const string GROUP_NAME = "Scenes";

    [MenuItem("MCV/AA 场景三合一流水线", false, 50)]
    public static void AAPipeline()
    {
        // ── ① 注册场景到 AA ────────────────────────────────
        var config = LoadOrCreateConfig();
        if (config == null) return;

        var settings = GetOrCreateSettings();
        if (settings == null) return;

        EditorPrefs.SetBool("Addressables.DebugBuildLayout", true);

        var group = GetOrCreateGroup(settings);
        if (group == null) return;

        int added = 0;
        foreach (var entry in config.scenes)
        {
            if (entry.sceneAsset == null)
            {
                Debug.LogWarning($"[SceneAA] 跳过 {entry.sceneName}，sceneAsset 未赋值");
                continue;
            }

            var scenePath = AssetDatabase.GetAssetPath(entry.sceneAsset);
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning($"[SceneAA] 跳过 {entry.sceneName}，无法获取场景路径");
                continue;
            }

            var guid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[SceneAA] 跳过 {entry.sceneName}，无法获取 GUID");
                continue;
            }

            var aaEntry = settings.CreateOrMoveEntry(guid, group, false, true);
            if (aaEntry != null)
            {
                aaEntry.address = entry.address;
                added++;
                Debug.Log($"[SceneAA] 添加: {entry.sceneName} → address: {entry.address}");
            }
        }

        RemoveFromBuildSettings(config);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        if (added == 0)
        {
            EditorUtility.DisplayDialog("AA 流水线", "没有新场景需要处理", "确定");
            return;
        }

        // ── ② 构建 AA 内容包 ───────────────────────────────
        if (EditorUtility.DisplayDialog("AA 场景流水线",
                $"已注册 {added} 个场景到 Addressables\n\n" +
                "是否立即构建 AA 内容包？\n" +
                "（构建输出到 Addressables 本地包目录，打包 Player 时随 StreamingAssets/aa 一起进包）",
                "构建", "取消"))
        {
            // 前置检查：编辑器若停留在「脚本编译失败」状态（上一次编译/构建被中断后很常见），
            // SBP 的 BuildPlayerScripts 会直接抛 "Error building Player because scripts have
            // compile errors in the editor" 并退 —— 构建 0.x 秒结束就是这个征兆。
            // 这里提前拦下并给出可操作提示，而不是让人对着一句报错猜。
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("[SceneAA] 编辑器处于脚本编译失败状态，已中止 AA 构建");
                EditorUtility.DisplayDialog("AA 场景流水线",
                    "编辑器当前停留在「脚本编译失败」状态，Addressables 无法构建。\n\n" +
                    "常见原因：上一次编译/构建被中断，失败状态残留。\n" +
                    "注意：此时 Console 里可能已经看不到任何红色报错了，但状态依然卡住。\n\n" +
                    "处理方法：\n" +
                    "1) 先把 Console 中的脚本报错清理干净；\n" +
                    "2) 若已无报错，执行菜单 Assets > Reimport（或随便改一下某个 .cs 触发重新编译）\n" +
                    "   来清除残留的失败状态；\n" +
                    "3) 再重跑本流水线。",
                    "确定");
                return;
            }

            AddressablesPlayerBuildResult result;
            AddressableAssetSettings.BuildPlayerContent(out result);
            // 构建失败必须显式报错：旧写法不看返回值，无论成败都打印「构建完成」并弹成功框，
            // 会把 "scripts have compile errors" 这类失败伪装成成功。
            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                var err = result == null ? "未返回构建结果" : result.Error;
                Debug.LogError($"[SceneAA] AA 内容构建失败: {err}");
                EditorUtility.DisplayDialog("AA 场景流水线失败",
                    $"✘ AA 内容构建失败，未产出有效资源包\n\n{err}\n\n请查看 Console 完整报错后重试。",
                    "确定");
                return;
            }
            Debug.Log($"[SceneAA] AA 内容构建完成，用时 {result.Duration:F1}s → {result.OutputPath}");
            EditorUtility.DisplayDialog("AA 流水线完成",
                $"✔ 已注册 {added} 个场景\n" +
                $"✔ AA 内容已构建（用时 {result.Duration:F0}s）\n" +
                "✔ 已从 Build Settings 移除",
                "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("AA 流水线完成",
                $"✔ 已注册 {added} 个场景到 Addressables\n" +
                "⚠ 尚未构建 AA 内容，请在打包前执行",
                "确定");
        }
    }

    [MenuItem("MCV/清理 AA 场景", false, 51)]
    public static void CleanAAScenes()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorUtility.DisplayDialog("清理 AA 场景", "Addressables 设置不存在，无需清理", "确定");
            return;
        }

        var group = settings.FindGroup(GROUP_NAME);
        if (group == null)
        {
            EditorUtility.DisplayDialog("清理 AA 场景", "场景组不存在，无需清理", "确定");
            return;
        }

        int removed = group.entries.Count;
        var entries = group.entries.ToArray();
        foreach (var entry in entries)
            settings.RemoveAssetEntry(entry.guid, false);

        settings.RemoveGroup(group);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SceneAA] 清理完成，移除了 {removed} 个场景 AA 条目");
        EditorUtility.DisplayDialog("清理 AA 场景", $"移除了 {removed} 个场景 AA 条目", "确定");
    }

    // ── 配置加载 ─────────────────────────────────────────────────

    private static SceneAddressableConfig LoadOrCreateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<SceneAddressableConfig>(CONFIG_PATH);
        if (config != null) return config;

        var dir = Path.GetDirectoryName(CONFIG_PATH).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(dir))
        {
            var parts = dir.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        config = ScriptableObject.CreateInstance<SceneAddressableConfig>();
        AssetDatabase.CreateAsset(config, CONFIG_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SceneAA] 已创建新配置: {CONFIG_PATH}");
        EditorUtility.DisplayDialog("AA 场景配置",
            $"已创建新配置，请编辑:\n{CONFIG_PATH}\n\n添加场景后再执行流水线", "确定");

        Selection.activeObject = config;
        return null;
    }

    // ── Addressables 基础设施 ────────────────────────────────────

    private static AddressableAssetSettings GetOrCreateSettings()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null) return settings;

        var path = "Assets/AddressableAssetsData";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", "AddressableAssetsData");

        settings = AddressableAssetSettings.Create(
            path, "AddressableAssetSettings", true, true);
        AddressableAssetSettingsDefaultObject.Settings = settings;

        return settings;
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings)
    {
        var group = settings.FindGroup(GROUP_NAME);
        if (group == null)
            group = settings.CreateGroup(GROUP_NAME, false, false, false, null);

        if (group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>() == null)
            group.AddSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();

        if (group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>() == null)
            group.AddSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();

        return group;
    }

    // ── Build Settings 管理 ─────────────────────────────────────

    private static void RemoveFromBuildSettings(SceneAddressableConfig config)
    {
        var existingScenes = EditorBuildSettings.scenes.ToList();
        var configPaths = new HashSet<string>();

        foreach (var entry in config.scenes)
        {
            if (entry.sceneAsset == null) continue;
            var path = AssetDatabase.GetAssetPath(entry.sceneAsset);
            if (!string.IsNullOrEmpty(path))
                configPaths.Add(path);
        }

        var updated = existingScenes
            .Where(s => !configPaths.Contains(s.path))
            .ToArray();

        EditorBuildSettings.scenes = updated;

        Debug.Log($"[SceneAA] Build Settings 已更新，移除了 {existingScenes.Count - updated.Length} 个场景");
    }
}
