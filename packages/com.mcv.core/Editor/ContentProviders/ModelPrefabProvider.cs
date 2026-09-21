using System;
using System.Collections.Generic;
using System.IO;
using MCV_Module.Interfaces;
using MCV_Module.Models.Addressable;
using MCV_Module.Models.Project;

/// <summary>
/// 「一个 TaskData → 一个预制体」型 Provider 的通用实现（info / structure / measure 三种任务都是这个形态）。
///
/// 产出 id = JSON 里写的 <c>taskXxxData.prefabKey</c>（形如 <c>contactor_structure_model</c>），
/// 素材路径 = <c>{ModelRoot}/{id}.prefab</c> —— **文件名必须与 id 逐字符一致**。
/// <c>prefabKey</c> 为空视为「该 clip 不需要这个模型」，静默跳过（JSON 是唯一源）。
///
/// 新增同类模型只需在 <c>ContentBundleTools.Providers</c> 里再 <c>new</c> 一行（给目录、任务段、取值委托），
/// 不需要新建类。
/// </summary>
public class ModelPrefabProvider : IContentProvider
{
    readonly string name;
    readonly string modelRoot;
    readonly string taskSegment;
    readonly Func<ProjectClip, string> keySelector;

    /// <param name="name">对账报告里的来源名（如 <c>StructureModel</c>）</param>
    /// <param name="modelRoot">模型素材目录（<c>Assets/Prefabs/Models/...</c>）</param>
    /// <param name="taskSegment">任务段（<see cref="ContentNaming.TaskInfo"/> / <c>TaskStructure</c> / <c>TaskMeasure</c>）</param>
    /// <param name="keySelector">从 ProjectClip 取该任务 prefabKey 的委托（走公开的 <c>GetTaskData&lt;T&gt;</c>）</param>
    public ModelPrefabProvider(string name, string modelRoot, string taskSegment, Func<ProjectClip, string> keySelector)
    {
        this.name = name;
        this.modelRoot = modelRoot;
        this.taskSegment = taskSegment;
        this.keySelector = keySelector;
    }

    public string Name => name;

    public IEnumerable<ContentResourceEntry> Collect(ProjectClip clip, string device)
    {
        var list = new List<ContentResourceEntry>();
        if (clip == null || keySelector == null) return list;

        string id = keySelector(clip);
        if (string.IsNullOrEmpty(id)) return list;      // 未配置 = 该 clip 不需要这个模型

        string expectedId = ContentNaming.ConfigId(device, taskSegment, ContentNaming.ResourceModel);
        string path = $"{modelRoot}/{id}.prefab";

        if (id != expectedId)
        {
            list.Add(new ContentResourceEntry(id, path,
                $"id 不符合命名规则（应为 {expectedId}）", ContentAssetKind.Prefab) { isError = true });
            return list;
        }

        list.Add(File.Exists(path)
            ? new ContentResourceEntry(id, path, null, ContentAssetKind.Prefab)
            : new ContentResourceEntry(id, null, $"找不到预制体：{path}（文件名须与 prefabKey 一致）",
                ContentAssetKind.Prefab) { isError = true });

        return list;
    }
}
