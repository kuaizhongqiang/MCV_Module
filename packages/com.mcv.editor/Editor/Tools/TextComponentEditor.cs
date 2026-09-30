using MCV_Module.EditorTools.Tools;
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
/// TextComponent 的 Inspector 扩展：编辑期把 <c>text</c> 单向下发到控件；按 §3 的**路径式 key** 登记 / 删除语言条目，
/// 两种操作都会把 SO 单目标重新导出成 JSON。
/// </summary>
[CustomEditor(typeof(TextComponent))]
public class TextComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        var comp = (TextComponent)target;
        // WHY: text 是中文录入面、控件文本只是它的下游显示 —— 编辑期改完立刻下发，设计师才看得见自己写的字（§2）。
        if (changed) PushLiteralToControl(comp);

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
            string suggested = TextKeyTools.TryBuildKey(comp, out _, out string candidate, out string reason)
                ? candidate
                : null;

            EditorGUILayout.HelpBox(suggested == null
                ? $"本节点未填 languageKey，且无法自动生成：{reason}"
                : $"本节点未填 languageKey（当前显示字面量）。\n将要生成：{suggested}",
                suggested == null ? MessageType.Warning : MessageType.Info);

            if (suggested != null && GUILayout.Button($"登记：{suggested}"))
            {
                if (!TextKeyTools.TryRegister(comp, out _, out string error)) Log.Error($"[TextComponentEditor] {error}");
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
            RegisterWithExistingKey(so, comp, key);
            Repaint();
        }
    }

    /// <summary>编辑期单向下发：序列化字段 <c>text</c> → 节点控件（控件文本是它的下游显示）。</summary>
    static void PushLiteralToControl(TextComponent comp)
    {
        string literal = comp.LiteralText ?? string.Empty;
        var tmp = comp.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null) tmp.text = literal;
        var legacy = comp.GetComponent<UnityEngine.UI.Text>();
        if (legacy != null) legacy.text = literal;
    }

    /// <summary>查找项目中的 LanguageDataSO；多个时用第一个并告警（登记路径由 TextKeyTools 前置阻断）。</summary>
    LanguageDataSO FindLanguageSO()
    {
        string[] guids = AssetDatabase.FindAssets("t:LanguageDataSO");
        if (guids.Length == 0) return null;
        if (guids.Length > 1)
            Log.Warning($"[TextComponentEditor] 存在多个 LanguageDataSO，使用第一个：{AssetDatabase.GUIDToAssetPath(guids[0])}");
        return AssetDatabase.LoadAssetAtPath<LanguageDataSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    /// <summary>已有 key 但条目缺失：按该 key 补登记（中文录入面取组件序列化字段 <c>text</c>）。</summary>
    void RegisterWithExistingKey(LanguageDataSO so, TextComponent comp, string key)
    {
        so.data ??= new LanguageData();
        so.data.languageClips ??= new List<LanguageClip>();

        so.data.languageClips.Add(new LanguageClip
        {
            id = key,
            displayName = comp.gameObject.name,
            clips = CreateClipsWithLiteral(comp),
        });

        so.Export(); // WHY: SO → JSON，运行时按 key 反查才拿得到
        EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        Log.Info($"[TextComponentEditor] 已按既有 key 登记：{key}（displayName={comp.gameObject.name}）");
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

    /// <summary>每个语言一个空槽位；中文槽取组件序列化字段 <c>text</c>（**不是**运行时 RawText —— 编辑期它恒为空）。</summary>
    static string[] CreateClipsWithLiteral(TextComponent comp)
    {
        int count = Enum.GetNames(typeof(LanguageType)).Length;
        var clips = new string[count];
        for (int i = 0; i < count; i++) clips[i] = string.Empty;

        string literal = comp.LiteralText;
        if (!string.IsNullOrEmpty(literal)) clips[0] = literal;
        return clips;
    }
}
