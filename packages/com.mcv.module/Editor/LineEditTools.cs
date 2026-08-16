using System.Collections.Generic;
using MCV_Module.Objects.Interactives.Elements;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 线编辑工具 —— 批量生成/清除/校验场景中的 ElementLineObj。
/// isStatic=true 生成线；isStatic=false 为运行期实例化线段，编辑期仅清除网格。
/// 注：生成的网格是运行时对象，不随场景序列化，运行期由 ElementLineObj.DelayInit 重建。
/// </summary>
public static class LineEditTools
{
    [MenuItem("MCV/线编辑/生成所有静态线", false, 60)]
    public static void GenerateAllStaticLines()
    {
        var lines = FindAllLines();
        if (lines.Count == 0)
        {
            EditorUtility.DisplayDialog("线编辑", "当前打开的场景中未找到 ElementLineObj", "确定");
            return;
        }

        int created = 0, cleared = 0;
        foreach (var line in lines)
        {
            if (line.IsStatic) { line.CreateLine(); created++; }
            else { line.DestroyLine(); cleared++; }
        }

        Debug.Log($"[LineEdit] 生成静态线 {created} 条，清除动态线 {cleared} 条");
        EditorUtility.DisplayDialog("线编辑",
            $"✔ 生成静态线 {created} 条\n✔ 清除动态线 {cleared} 条\n✔ 共处理 {lines.Count} 条",
            "确定");
    }

    [MenuItem("MCV/线编辑/清除所有线网格", false, 61)]
    public static void DestroyAllLines()
    {
        var lines = FindAllLines();
        if (lines.Count == 0)
        {
            EditorUtility.DisplayDialog("线编辑", "当前打开的场景中未找到 ElementLineObj", "确定");
            return;
        }

        foreach (var line in lines) line.DestroyLine();

        Debug.Log($"[LineEdit] 已清除 {lines.Count} 条线的网格");
        EditorUtility.DisplayDialog("线编辑", $"✔ 已清除 {lines.Count} 条线的网格", "确定");
    }

    [MenuItem("MCV/线编辑/校验线配置", false, 62)]
    public static void ValidateLines()
    {
        var lines = FindAllLines();
        if (lines.Count == 0)
        {
            EditorUtility.DisplayDialog("线校验", "当前打开的场景中未找到 ElementLineObj", "确定");
            return;
        }

        int ok = 0;
        var problems = new List<string>();
        foreach (var line in lines)
        {
            var issues = ValidateOne(line);
            if (issues.Count == 0) ok++;
            else problems.Add($"{line.name}: {string.Join("；", issues)}");
        }

        string msg = $"共 {lines.Count} 条线，{ok} 条正常，{problems.Count} 条有问题\n\n" +
                     string.Join("\n", problems);
        Debug.Log($"[LineEdit] 线校验完成\n{msg}");
        EditorUtility.DisplayDialog("线校验", msg, "确定");
    }

    /// <summary>
    /// 快捷键 Alt+L：把选中物体中的点（按选中顺序）赋给选中的线物体，并执行生成。
    /// 要求恰好选中 1 个 ElementLineObj 且 ≥2 个 ElementPointObj。
    /// 不新建任何物体，只做赋值 + 生成。
    /// </summary>
    [MenuItem("MCV/线编辑/赋值选中点并生成线 _&L", false, 63)]
    public static void AssignAndGenerateLine()
    {
        var line = GetSelectedLine();
        var points = GetSelectedPoints();
        if (line == null || points.Count < 2) return; // 已被校验函数拦截，防御性返回

        Undo.RecordObject(line, "赋值选中点并生成线");
        line.EditLinePoint(points);
        line.IsStatic = true; // 赋值生成的线视为静态，运行期自动重建
        line.CreateLine();
        EditorUtility.SetDirty(line);

        Debug.Log($"[LineEdit] 已为 {line.name} 赋值 {points.Count} 个点并生成线");
        EditorUtility.DisplayDialog("线编辑",
            $"✔ 已为 {line.name} 赋值 {points.Count} 个点并生成线\n✔ 已设为静态线",
            "确定");
    }

    // 校验：选中恰好 1 个 line 且 ≥2 个 point 时菜单才可用
    [MenuItem("MCV/线编辑/赋值选中点并生成线 _&L", true, 63)]
    public static bool ValidateAssignAndGenerateLine()
    {
        return GetSelectedLine() != null && GetSelectedPoints().Count >= 2;
    }

    // ── 工具方法 ────────────────────────────────────────────

    /// <summary> 选中物体中唯一的线物体；为 null 表示没有或不止一个 </summary>
    static ElementLineObj GetSelectedLine()
    {
        ElementLineObj line = null;
        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;
            var l = go.GetComponent<ElementLineObj>();
            if (l == null) continue;
            if (line != null) return null; // 选中了多个 line，视为无效
            line = l;
        }
        return line;
    }

    /// <summary> 选中物体中的全部点物体（保持选中顺序） </summary>
    static List<ElementPointObj> GetSelectedPoints()
    {
        var points = new List<ElementPointObj>();
        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;
            var p = go.GetComponent<ElementPointObj>();
            if (p != null) points.Add(p);
        }
        return points;
    }

    static List<string> ValidateOne(ElementLineObj line)
    {
        var issues = new List<string>();
        if (!line.IsStatic) issues.Add("非静态（运行期实例化）");

        var points = line.PointList;
        if (points == null || points.Count < 2)
            issues.Add("点少于 2 个");
        else
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null) issues.Add($"第 {i + 1} 个点引用为空");
            }
        }

        var data = line.LineDrawData;
        if (data.width <= 0 || data.sectionSegments < 1)
            issues.Add("lineDrawData 未配置完整");
        return issues;
    }

    static List<ElementLineObj> FindAllLines()
    {
        var result = new List<ElementLineObj>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<ElementLineObj>(true));
        }
        return result;
    }
}
