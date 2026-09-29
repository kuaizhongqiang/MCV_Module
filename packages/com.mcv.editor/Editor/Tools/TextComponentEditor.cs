using MCV_Module.Models;
using MCV_Module.Models.System;
using MCV_Module.UI.Components;
using MCV_Module.Utils;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// WHY: 运行时 TextComponent 按 languageKey 从 LanguageData 反查最新文案，故 SO 改动后必须重新导出 JSON 才生效。
/// <summary>
/// TextComponent 的 Inspector 扩展（key 驱动）：显示当前 languageKey 是否已在 LanguageDataSO 登记；
/// 未登记可生成（中文原文进 clips[0]，英文留空），已登记可删除；两种情况都会把 SO 重新导出成 JSON。
/// </summary>
[CustomEditor(typeof(TextComponent))]
public class TextComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var comp = (TextComponent)target;
        GUILayout.Space(8);
        EditorGUILayout.LabelField("多语言 key", EditorStyles.boldLabel);

        LanguageDataSO so = FindLanguageSO();
        if (so == null)
        {
            EditorGUILayout.HelpBox("未找到 LanguageDataSO。请先创建：Assets → Create → MCV → Data → LanguageData",
                MessageType.Error);
            return;
        }

        string key = comp.LanguageKey;
        if (string.IsNullOrEmpty(key))
        {
            EditorGUILayout.HelpBox("本节点未填 languageKey（当前显示字面量）。可以先生成一个可读的 key，再按需改名。",
                MessageType.Info);
            if (GUILayout.Button($"生成 key：ui.{comp.gameObject.name}"))
            {
                SetKey($"ui.{comp.gameObject.name}");
                Repaint();
            }
            return;
        }

        LanguageData data = so.data ??= new LanguageData();
        LanguageClip clip = data.languageClips?.Find(c => c.id == key);
        if (clip != null)
        {
            EditorGUILayout.HelpBox($"「{key}」已登记（displayName：{clip.displayName}）。", MessageType.Info);
            if (GUILayout.Button("删除该语言条目"))
            {
                if (EditorUtility.DisplayDialog("删除语言条目",
                        $"确定删除「{key}」？\n将同时从 SO 与 LanguageData.json 移除，并清空本组件的 key。",
                        "删除", "取消"))
                {
                    DeleteClip(so, key);
                    Repaint();
                }
            }
            return;
        }

        EditorGUILayout.HelpBox($"「{key}」尚未登记。", MessageType.Warning);
        if (GUILayout.Button("登记该语言条目"))
        {
            GenerateClip(so, comp, key);
            Repaint();
        }
    }

    /// <summary>查找项目中的 LanguageDataSO；多个时用第一个并告警。</summary>
    LanguageDataSO FindLanguageSO()
    {
        string[] guids = AssetDatabase.FindAssets("t:LanguageDataSO");
        if (guids.Length == 0) return null;
        if (guids.Length > 1)
            Log.Warning($"[TextComponentEditor] 存在多个 LanguageDataSO，使用第一个：{AssetDatabase.GUIDToAssetPath(guids[0])}");
        return AssetDatabase.LoadAssetAtPath<LanguageDataSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    /// <summary>登记一条：id = languageKey，displayName = 物体名（人可读），中文原文进 clips[0]，其余语言留空。</summary>
    void GenerateClip(LanguageDataSO so, TextComponent comp, string key)
    {
        so.data ??= new LanguageData();
        so.data.languageClips ??= new List<LanguageClip>();

        string[] clips = CreateEmptyClips();
        if (!string.IsNullOrEmpty(comp.RawText)) clips[0] = comp.RawText;

        so.data.languageClips.Add(new LanguageClip
        {
            id = key,
            displayName = comp.gameObject.name,
            clips = clips,
        });

        so.Export(); // WHY: SO → JSON，运行时按 key 反查才拿得到
        EditorUtility.SetDirty(so);
        Log.Info($"[TextComponentEditor] 已登记语言条目：{key}（displayName={comp.gameObject.name}）");
    }

    /// <summary>按 id 移除条目，清空组件 key，并把 SO 重新导出。</summary>
    void DeleteClip(LanguageDataSO so, string key)
    {
        if (so.data?.languageClips == null) return;
        int removed = so.data.languageClips.RemoveAll(c => c.id == key);
        if (removed <= 0)
        {
            Log.Warning($"[TextComponentEditor] 未找到可删除的条目：{key}");
            return;
        }
        so.Export();
        EditorUtility.SetDirty(so);
        SetKey(string.Empty);
        Log.Info($"[TextComponentEditor] 已删除语言条目：{key}（移除 {removed} 条）");
    }

    /// <summary>写组件的 languageKey 字段（走 SerializedProperty，保证进 Undo 与脏标记）。</summary>
    void SetKey(string value)
    {
        var prop = serializedObject.FindProperty("languageKey");
        if (prop == null)
        {
            Log.Warning("[TextComponentEditor] 找不到 languageKey 字段");
            return;
        }
        prop.stringValue = value;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    /// <summary>每个语言各一个空槽位（数量按 LanguageType 枚举）。</summary>
    static string[] CreateEmptyClips()
    {
        int count = Enum.GetNames(typeof(LanguageType)).Length;
        var clips = new string[count];
        for (int i = 0; i < count; i++) clips[i] = string.Empty;
        return clips;
    }
}
