using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCV_Module.Interfaces;
using MCV_Module.Models;
using MCV_Module.Models.Addressable;
using MCV_Module.Models.Project;

// WHY: 产出以 JSON 的 taskInfoData.images 为准、逐位配对（JSON 要而素材缺 → 空 assetPath + Error；素材有而 JSON 未引用 → isExtra + Warning）；id 拼法 {器件}_info_{NN} 见 ContentNaming；规则见 Docs/design_ai/BundlePipeline.md §4。
/// <summary>简介图集 Provider —— <c>Assets/Sprites/Origin/Content/Info/{序号}{器件中文名}/*.png</c>（每器件 6 张）。</summary>
public class InfoSpriteProvider : IContentProvider
{
    const string InfoRoot = "Assets/Sprites/Origin/Content/Info";

    public string Name => "InfoSprite";

    public IEnumerable<ContentResourceEntry> Collect(ProjectClip clip, string device)
    {
        var list = new List<ContentResourceEntry>();

        // WHY: ProjectClip 的 taskXxxData 是 [SerializeField] 私有字段，Editor 程序集读不到 —— 必须走公开的 GetTaskData<T>() / GetTask<T>()（它们在同程序集内读私有字段）。
        TaskInfoData info = clip.GetTaskData<TaskInfoData>(TaskType.Info);
        List<string> requiredIds = info != null && info.images != null ? info.images : new List<string>();

        string folder = FindClipFolder(clip);
        string[] files = string.IsNullOrEmpty(folder)
            ? new string[0]
            : Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (files.Length == 0 && requiredIds.Count == 0) return list;

        // WHY: 逐位配对 —— 第 i 张图 ↔ JSON 的第 i 个 id。
        int count = Math.Max(files.Length, requiredIds.Count);
        for (int i = 0; i < count; i++)
        {
            string expectedId = ContentNaming.InfoSpriteId(device, i + 1);
            string path = i < files.Length ? files[i] : null;
            string jsonId = i < requiredIds.Count ? requiredIds[i] : null;

            if (jsonId == null)
            {
                if (path == null) continue;
                var extra = new ContentResourceEntry(expectedId, path,
                    $"素材有而 JSON 未引用：{Path.GetFileName(path)}", ContentAssetKind.Sprite) { isExtra = true };
                list.Add(extra);
                continue;
            }

            string note;
            bool isError = false;
            if (path == null)
            {
                note = "JSON 要求但素材缺失";
                isError = true;
            }
            else if (jsonId != expectedId)
            {
                note = $"id 不符合命名规则（应为 {expectedId}）";
                isError = true;
            }
            else
            {
                note = null;
            }

            var entry = new ContentResourceEntry(jsonId, path, note, ContentAssetKind.Sprite) { isError = isError };
            list.Add(entry);
        }

        return list;
    }

    /// <summary>按 displayName 找素材目录（目录名形如 "1接触器"，包含中文器件名即可）。</summary>
    static string FindClipFolder(ProjectClip clip)
    {
        if (clip == null || string.IsNullOrEmpty(clip.displayName)) return null;
        if (!Directory.Exists(InfoRoot)) return null;

        return Directory.GetDirectories(InfoRoot)
            .FirstOrDefault(dir => Path.GetFileName(dir).Contains(clip.displayName));
    }
}
