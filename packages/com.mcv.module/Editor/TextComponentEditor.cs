using MCV_Module.Models;
using MCV_Module.Models.System;
using MCV_Module.UI.Components;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/*
    TextComponent 的 Inspector 扩展（SO 驱动）：
    1. 检查当前物体名是否已作为 LanguageClip.displayName 注册进 LanguageDataSO。
    2. 未注册时「生成语言 Clip」：写入 SO（id 用新 GUID）、同步组件字段，并导出 JSON。
    3. 已注册时「删除语言 Clip」：从 SO 移除、清空组件字段，并导出 JSON。
    运行时 TextComponent 按字段 id 从 JSON 反向找回最新内容显示。
*/
[CustomEditor(typeof(TextComponent))]
public class TextComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var comp = (TextComponent)target;
        string clipName = comp.gameObject.name;

        GUILayout.Space(8);
        EditorGUILayout.LabelField("语言 Clip 注册", EditorStyles.boldLabel);

        LanguageDataSO so = FindLanguageSO();
        if (so == null)
        {
            EditorGUILayout.HelpBox("未找到 LanguageDataSO。请先创建：Assets → Create → MCV → Data → LanguageData",
                MessageType.Error);
            return;
        }

        LanguageData data = so.data ??= new LanguageData();
        bool exists = data.languageClips != null
                      && data.languageClips.Exists(c => c.displayName == clipName);

        if (exists)
        {
            EditorGUILayout.HelpBox($"「{clipName}」已注册为语言 Clip。", MessageType.Info);
            if (GUILayout.Button("删除语言 Clip"))
            {
                if (EditorUtility.DisplayDialog("删除语言 Clip",
                        $"确定删除「{clipName}」？\n将同时从 SO/JSON 移除并清空组件字段。",
                        "删除", "取消"))
                {
                    DeleteClip(so, clipName);
                    Repaint(); // 立即刷新存在性状态
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"「{clipName}」未注册语言 Clip。", MessageType.Warning);
            if (GUILayout.Button("生成语言 Clip"))
            {
                GenerateClip(so, clipName);
                Repaint(); // 立即刷新存在性状态
            }
        }
    }

    /// <summary>查找项目中的 LanguageDataSO 资产；多个时使用第一个并提示。</summary>
    LanguageDataSO FindLanguageSO()
    {
        var guids = AssetDatabase.FindAssets("t:LanguageDataSO");
        if (guids.Length == 0) return null;
        if (guids.Length > 1)
            Debug.LogWarning($"[TextComponentEditor] 存在多个 LanguageDataSO，使用第一个：{AssetDatabase.GUIDToAssetPath(guids[0])}");
        return AssetDatabase.LoadAssetAtPath<LanguageDataSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    void GenerateClip(LanguageDataSO so, string clipName)
    {
        so.data ??= new LanguageData();
        so.data.languageClips ??= new List<LanguageClip>();

        // 每次生成新的 GUID 作 id，避免重复；displayName 仍用物体名（人可读）
        string clipId = Guid.NewGuid().ToString("N");
        // 组件字段里已填的文本优先沿用，否则按语言数开空槽
        string[] clips = ReadComponentClips() ?? CreateEmptyClips();
        so.data.languageClips.Add(new LanguageClip
        {
            id = clipId,
            displayName = clipName,
            clips = clips,
        });

        so.Export(); // SO → JSON，运行时即生效
        EditorUtility.SetDirty(so);
        AssignToComponent(clipId, clipName, clips);
        Debug.Log($"[TextComponentEditor] 已生成语言 Clip：{clipName}（id={clipId}）");
    }

    /// <summary>从 SO 移除 displayName 匹配的 Clip，清空组件字段，并导出 JSON。</summary>
    void DeleteClip(LanguageDataSO so, string clipName)
    {
        if (so.data == null || so.data.languageClips == null) return;
        int removed = so.data.languageClips.RemoveAll(c => c.displayName == clipName);
        if (removed <= 0)
        {
            Debug.LogWarning($"[TextComponentEditor] 未找到可删除的 Clip：{clipName}");
            return;
        }
        so.Export();
        EditorUtility.SetDirty(so);
        ClearComponentClip();
        Debug.Log($"[TextComponentEditor] 已删除语言 Clip：{clipName}（移除 {removed} 条）");
    }

    /// <summary>把 Clip 同步进组件的 languageClip 字段（id/displayName + clips）。</summary>
    void AssignToComponent(string clipId, string clipName, string[] clips)
    {
        var prop = serializedObject.FindProperty("languageClip");
        var idProp = prop?.FindPropertyRelative("id");
        var displayProp = prop?.FindPropertyRelative("displayName");
        var clipsProp = prop?.FindPropertyRelative("clips");
        if (idProp == null || displayProp == null || clipsProp == null)
        {
            Debug.LogWarning("[TextComponentEditor] languageClip 字段无法写入，仅写入了 SO/JSON");
            return;
        }
        idProp.stringValue = clipId;
        displayProp.stringValue = clipName;
        clipsProp.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
            clipsProp.GetArrayElementAtIndex(i).stringValue = clips[i];
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    /// <summary>
    /// 把组件的 languageClip 字段重置为默认态：id/displayName 置空（运行时走静态文本），
    /// clips 恢复为按语言数量开空槽。
    /// </summary>
    void ClearComponentClip()
    {
        var prop = serializedObject.FindProperty("languageClip");
        var idProp = prop?.FindPropertyRelative("id");
        var displayProp = prop?.FindPropertyRelative("displayName");
        var clipsProp = prop?.FindPropertyRelative("clips");
        if (idProp == null || displayProp == null || clipsProp == null) return;
        idProp.stringValue = string.Empty;
        displayProp.stringValue = string.Empty;
        string[] emptyClips = CreateEmptyClips();
        clipsProp.arraySize = emptyClips.Length;
        for (int i = 0; i < emptyClips.Length; i++)
            clipsProp.GetArrayElementAtIndex(i).stringValue = emptyClips[i];
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    /// <summary>读取组件 languageClip 字段里已填的 clips；未填或全空返回 null。</summary>
    string[] ReadComponentClips()
    {
        var prop = serializedObject.FindProperty("languageClip");
        var clipsProp = prop?.FindPropertyRelative("clips");
        if (clipsProp == null || clipsProp.arraySize == 0) return null;
        var clips = new string[clipsProp.arraySize];
        bool anyFilled = false;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i] = clipsProp.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrEmpty(clips[i])) anyFilled = true;
        }
        return anyFilled ? clips : null;
    }

    /// <summary>每个语言各一个空文本槽位，后续在 SO/JSON 中填写。</summary>
    static string[] CreateEmptyClips()
    {
        int count = Enum.GetNames(typeof(LanguageType)).Length;
        var clips = new string[count];
        for (int i = 0; i < count; i++) clips[i] = string.Empty;
        return clips;
    }
}
