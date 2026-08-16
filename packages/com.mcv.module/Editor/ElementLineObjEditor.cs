using MCV_Module.Objects.Interactives.Elements;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
    ElementLineObj 编辑器 + 全局自动刷新。
    - 全局轮询（[InitializeOnLoad]）：不依赖选中，任一静态线相关的
      点位置/数量/参数/线变换变化都会自动重建网格 —— 拖动中间点即时刷新。
    - Inspector 提供手动 生成/清除 按钮。
*/

[CustomEditor(typeof(ElementLineObj))]
public class ElementLineObjEditor : Editor
{
    ElementLineObj Line => target as ElementLineObj;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);
        EditorGUI.BeginDisabledGroup(!Line.IsStatic);
        if (GUILayout.Button("生成线 (CreateLine)"))
        {
            Line.CreateLine();
        }
        if (GUILayout.Button("清除线 (DestroyLine)"))
        {
            Line.DestroyLine();
        }
        EditorGUI.EndDisabledGroup();
    }
}

/// <summary>
/// 全局静态线自动刷新：每帧对比所有已打开场景中 ElementLineObj 的状态快照，
/// 有变化即重建。与选中无关，拖动中间点也能实时更新。
/// </summary>
[InitializeOnLoad]
public static class ElementLineSceneUpdater
{
    static readonly Dictionary<ElementLineObj, string> lastKeys = new();

    static ElementLineSceneUpdater()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (Application.isPlaying) return;

        // 清理已销毁的线（避免字典无界增长）
        if (lastKeys.Count > 0)
        {
            var dead = lastKeys.Keys.Where(k => k == null).ToList();
            if (dead.Count > 0)
                foreach (var k in dead) lastKeys.Remove(k);
        }

        foreach (var line in FindAllLines())
        {
            // 既有场景线补挂 MeshCollider（RequireComponent 不会回补已存在的物体）
            if (line.GetComponent<MeshCollider>() == null)
            {
                var mc = line.gameObject.AddComponent<MeshCollider>();
                var mf = line.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) mc.sharedMesh = mf.sharedMesh;
            }

            string key = BuildStateKey(line);
            if (lastKeys.TryGetValue(line, out var last) && last == key) continue;

            lastKeys[line] = key;
            if (!line.IsStatic)
            {
                line.DestroyLine();
                continue;
            }
            var data = line.LineDrawData;
            if (data.width > 0 && data.sectionSegments >= 1) line.CreateLine();
            else line.DestroyLine();
        }
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

    /// <summary>
    /// 参与重建判定的状态快照：isStatic、线的变换、绘制参数、点列表数量及每个点的世界位置。
    /// 只要其中任一变化（点被移动、点增删、参数改动、线整体移动）key 就会变，从而触发重建。
    /// </summary>
    static string BuildStateKey(ElementLineObj line)
    {
        var sb = new StringBuilder();
        sb.Append(line.IsStatic ? '1' : '0');

        var t = line.transform;
        sb.Append('|').Append(t.position).Append('|').Append(t.rotation).Append('|').Append(t.lossyScale);

        var data = line.LineDrawData;
        sb.Append('|').Append(data.width)
          .Append('|').Append(data.sectionSegments)
          .Append('|').Append(data.RadialSegments)
          .Append('|').Append(data.material != null ? data.material.name : "null")
          .Append('|').Append(data.bazierOffsetDistance)
          .Append('|').Append(data.bazierOffsetDirection);

        var points = line.PointList;
        sb.Append('|').Append(points == null ? -1 : points.Count);
        if (points != null)
        {
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p == null) { sb.Append("|null"); continue; }
                sb.Append('|').Append(p.transform.position);
            }
        }
        return sb.ToString();
    }
}
