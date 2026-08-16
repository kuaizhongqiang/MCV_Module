using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MCV_Module.Controller;
using MCV_Module.Interfaces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller 占位挂载工具 —— 在 1_Content 的 ControllerRoot 下，为每个
/// MCV_Module.Controllers 命名空间的 Controller 脚本创建子物体并挂组件。
///
/// 幂等设计：已挂载的同名物体自动跳过，新增 Controller 后重复执行即可补齐。
/// 通过类型 API 挂组件，不依赖脚本 guid，因此不生成 .meta 也不会出问题。
/// </summary>
public static class ControllerPlaceholderTools
{
    private const string SCENE_PATH = "Assets/Scenes/1_Content.unity";
    private const string ROOT_NAME = "ControllerRoot";
    private const string CONTROLLER_NS = "MCV_Module.Controllers";

    [MenuItem("MCV/挂载 Controller 占位到场景", false, 52)]
    public static void MountControllers()
    {
        // ── ① 定位 1_Content 场景 ────────────────────────────
        Scene scene;
        if (SceneManager.GetActiveScene().path == SCENE_PATH)
        {
            scene = SceneManager.GetActiveScene();
        }
        else
        {
            // 若当前场景有未保存改动，Unity 会弹窗询问
            scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        var controllerRoot = FindRootObject(scene, ROOT_NAME);
        if (controllerRoot == null)
        {
            EditorUtility.DisplayDialog("Controller 挂载", $"在 {SCENE_PATH} 中未找到 {ROOT_NAME}", "确定");
            return;
        }

        // ── ② 扫描全部 Controller 类型 ────────────────────────
        var types = FindControllerTypes();
        if (types.Count == 0)
        {
            EditorUtility.DisplayDialog("Controller 挂载", "未扫描到任何 Controller 类型", "确定");
            return;
        }

        // 已挂载的同名物体跳过（幂等）
        var mounted = new HashSet<string>(controllerRoot.GetComponentsInChildren<IController>(true)
            .Select(c => c.ControllerName));
        var toCreate = types.Where(t => !mounted.Contains(t.Name)).ToList();

        if (toCreate.Count == 0)
        {
            EditorUtility.DisplayDialog("Controller 挂载",
                $"所有 {types.Count} 个 Controller 均已挂载，无需处理", "确定");
            return;
        }

        // ── ③ 创建子物体并挂组件 ──────────────────────────────
        foreach (var type in toCreate)
        {
            var go = new GameObject(type.Name);
            go.transform.SetParent(controllerRoot.transform, false);
            go.AddComponent(type);
            Debug.Log($"[ControllerMount] 已创建 {type.Name} 并挂载组件");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[ControllerMount] 完成：新建 {toCreate.Count} 个，跳过 {mounted.Count} 个，共 {types.Count} 个");
        EditorUtility.DisplayDialog("Controller 挂载",
            $"✔ 新建 {toCreate.Count} 个 Controller 物体\n" +
            $"✔ 已存在跳过 {mounted.Count} 个\n" +
            "✔ 场景已保存",
            "确定");
    }

    // ── 工具方法 ────────────────────────────────────────────

    private static GameObject FindRootObject(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }

    private static List<Type> FindControllerTypes()
    {
        var baseType = typeof(ControllerBase<>);
        var result = new List<Type>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            foreach (var type in SafeGetTypes(asm))
            {
                if (type.IsAbstract || type.IsGenericType) continue;
                if (type.Namespace != CONTROLLER_NS) continue;
                if (type.BaseType != null && type.BaseType.IsGenericType &&
                    type.BaseType.GetGenericTypeDefinition() == baseType)
                    result.Add(type);
            }
        }

        return result.OrderBy(t => t.Name).ToList();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
    }
}
