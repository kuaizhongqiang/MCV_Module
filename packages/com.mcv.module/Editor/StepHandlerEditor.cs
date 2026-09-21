using MCV_Module.Models;
using MCV_Module.Steps;
using UnityEditor;
using UnityEngine;

/// <summary>
/// StepHandler 的 Inspector 自定义绘制 —— 按 conditionType 显示不同字段。
/// 常显：id / displayName / description / conditionType / showObjs / hideObjs / animations / tipsId / audioId
/// type 相关：Click→targetObj，Drag→targetObj+dragObj，Tool→usingId+targetObj，UI/Question→usingId，LineConnect→lines
/// </summary>
[CustomEditor(typeof(StepHandler))]
public class StepHandlerEditor : Editor
{
    private static readonly string[] AlwaysFields =
    {
        "id", "displayName", "description", "conditionType",
        "showObjs", "hideObjs", "animations", "tipsId", "audioId",
    };
    private static readonly string[] AlwaysLabels =
    {
        "ID", "显示名称", "描述", "条件类型",
        "显示对象", "隐藏对象", "动画列表", "提示ID", "音频ID",
    };

    /// <summary>conditionType → 需要额外显示的字段名列表</summary>
    private static string[] FieldsByType(ConditionType type)
    {
        switch (type)
        {
            case ConditionType.Click: return new[] { "targetObj" };
            case ConditionType.Drag: return new[] { "targetObj", "dragObj" }; // 从 dragObj 拖到 targetObj 的检测位置
            case ConditionType.Tool: return new[] { "usingId", "targetObj" }; // 拖出 UI 工具到 targetObj 上检测
            case ConditionType.UI:
            case ConditionType.Question: return new[] { "usingId" };
            case ConditionType.LineConnect: return new[] { "lines" };
            default: return System.Array.Empty<string>(); // None / Default / Finish 无额外参数
        }
    }

    private static string FieldLabel(string field)
    {
        switch (field)
        {
            case "targetObj": return "目标物体";
            case "dragObj": return "拖拽物体";
            case "usingId": return "使用ID（Tool/UI/Question）";
            case "lines": return "连线模板（ElementLineObj）";
            default: return field;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var type = (ConditionType)serializedObject.FindProperty("conditionType").enumValueIndex;
        EditorGUILayout.LabelField($"{target.name}  [{type}]", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 常显字段
        for (int i = 0; i < AlwaysFields.Length; i++)
            EditorGUILayout.PropertyField(serializedObject.FindProperty(AlwaysFields[i]), new GUIContent(AlwaysLabels[i]), true);

        // 按 conditionType 显示的额外字段
        foreach (var extra in FieldsByType(type))
            EditorGUILayout.PropertyField(serializedObject.FindProperty(extra), new GUIContent(FieldLabel(extra)), true);

        serializedObject.ApplyModifiedProperties();
    }
}
