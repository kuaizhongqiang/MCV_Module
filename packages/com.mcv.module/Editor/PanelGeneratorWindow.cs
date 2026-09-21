using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI Panel 生成器（MCV/创建/UI Panel...）：
/// 一键生成 Panel 类 + Controller 类 + Prefab 骨架（Assets/Resources/UI/）。
/// 归属灵活：module 包（mcv-framework）/ 宿主 / 自定义路径；命名空间可覆写。
/// 不绑定 Canvas：面板是可复用资产，由各 CanvasBase 子类按自己的状态逻辑调用同一面板。
/// 设计文档：docs/panel-generator-design.md
/// </summary>
public class PanelGeneratorWindow : EditorWindow
{
    #region 输入
    string m_PanelName = "";
    int m_Ownership = 1;          // 0=module包 1=宿主(默认) 2=自定义
    string m_CustomPath = "";
    string m_NamespaceOverride = "";
    string m_ControllerNamespaceOverride = "";
    bool m_GenerateController = true;
    bool m_GeneratePrefab = true;
    #endregion

    #region 常量
    const string ModuleRepoRoot = "G:/project/mcv-framework/packages/com.mcv.module/Runtime";
    const string HostScriptsRoot = "Assets/Scripts";
    const string DefaultPanelNs = "MCV_Module.UI.Panels";
    const string DefaultControllerNs = "MCV_Module.Controllers";
    const string PrefabDir = "Assets/Resources/UI";
    static readonly string[] OwnershipLabels = { "module 包（通用，git）", "宿主（项目专属，Plastic）", "自定义路径" };
    #endregion

    #region 窗口
    [MenuItem("MCV/创建/UI Panel...")]
    static void OpenWindow()
    {
        var win = GetWindow<PanelGeneratorWindow>(true, "UI Panel 生成器");
        win.minSize = new Vector2(460, 380);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("面板名（如 TaskList → TaskListPanel / TaskListController）", EditorStyles.boldLabel);
        m_PanelName = EditorGUILayout.TextField("面板名", m_PanelName).Trim();

        EditorGUILayout.Space(6);
        m_Ownership = EditorGUILayout.Popup("代码归属", m_Ownership, OwnershipLabels);
        if (m_Ownership == 2)
        {
            m_CustomPath = EditorGUILayout.TextField("自定义目录（Assets/... 或绝对路径）", m_CustomPath);
        }
        m_NamespaceOverride = EditorGUILayout.TextField("Panel 命名空间（留空用默认）", m_NamespaceOverride);
        m_ControllerNamespaceOverride = EditorGUILayout.TextField("Controller 命名空间（留空用默认）", m_ControllerNamespaceOverride);

        EditorGUILayout.Space(6);
        m_GenerateController = EditorGUILayout.Toggle("生成 Controller（标配）", m_GenerateController);
        m_GeneratePrefab = EditorGUILayout.Toggle("生成 Prefab 骨架（Assets/Resources/UI/）", m_GeneratePrefab);

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Prefab 固定生成到 Assets/Resources/UI/（框架契约路径，与归属无关）；不绑定任何 Canvas——在任意 CanvasBase 子类的 OnRebuild 中调用 CreatePanel<XxxPanel>() 即可使用。", MessageType.Info);

        EditorGUILayout.Space(8);
        GUI.enabled = CanGenerate(out string error);
        if (!GUI.enabled)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
        if (GUILayout.Button("生成", GUILayout.Height(30)))
        {
            Generate();
        }
        GUI.enabled = true;
    }
    #endregion

    #region 校验
    bool CanGenerate(out string error)
    {
        error = "";
        if (string.IsNullOrEmpty(m_PanelName))
        {
            error = "请输入面板名";
            return false;
        }
        if (!Regex.IsMatch(m_PanelName, "^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            error = "面板名必须是合法 C# 标识符（字母/下划线开头，字母数字下划线）";
            return false;
        }
        if (m_Ownership == 2 && string.IsNullOrEmpty(m_CustomPath))
        {
            error = "自定义归属需填写目录";
            return false;
        }
        return true;
    }

    /// <summary>归一化基础名：TaskList / TaskListPanel / TaskListController → TaskList</summary>
    static string NormalizeBaseName(string input)
    {
        string name = input.Trim();
        if (name.EndsWith("Panel", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Panel".Length);
        else if (name.EndsWith("Controller", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Controller".Length);
        return name;
    }
    #endregion

    #region 路径
    string PanelNamespace => string.IsNullOrEmpty(m_NamespaceOverride) ? DefaultPanelNs : m_NamespaceOverride;
    string ControllerNamespace => string.IsNullOrEmpty(m_ControllerNamespaceOverride) ? DefaultControllerNs : m_ControllerNamespaceOverride;

    string GetPanelPath(string baseName)
    {
        string panelFile = baseName + "Panel.cs";
        switch (m_Ownership)
        {
            case 0: return $"{ModuleRepoRoot}/UI/Panels/{panelFile}";
            case 1: return $"{HostScriptsRoot}/UI/Panels/{panelFile}";
            default: return $"{m_CustomPath.TrimEnd('/', '\\')}/{panelFile}";
        }
    }

    string GetControllerPath(string baseName)
    {
        string controllerFile = baseName + "Controller.cs";
        switch (m_Ownership)
        {
            case 0: return $"{ModuleRepoRoot}/Controllers/{controllerFile}";
            case 1: return $"{HostScriptsRoot}/Controllers/{controllerFile}";
            default: return $"{m_CustomPath.TrimEnd('/', '\\')}/{controllerFile}";
        }
    }
    #endregion

    #region 生成
    void Generate()
    {
        string baseName = NormalizeBaseName(m_PanelName);
        string panelName = baseName + "Panel";
        string controllerName = baseName + "Controller";
        string panelPath = GetPanelPath(baseName);
        string controllerPath = GetControllerPath(baseName);
        string prefabPath = $"{PrefabDir}/{panelName}.prefab";
        string panelFullName = $"{PanelNamespace}.{panelName}";
        string controllerFullName = $"{ControllerNamespace}.{controllerName}";

        // 冲突检测
        if (File.Exists(panelPath))
        {
            EditorUtility.DisplayDialog("生成失败", $"Panel 文件已存在：{panelPath}", "OK");
            return;
        }
        if (m_GenerateController && File.Exists(controllerPath))
        {
            EditorUtility.DisplayDialog("生成失败", $"Controller 文件已存在：{controllerPath}", "OK");
            return;
        }
        if (m_GeneratePrefab && File.Exists(prefabPath))
        {
            EditorUtility.DisplayDialog("生成失败", $"Prefab 已存在：{prefabPath}", "OK");
            return;
        }

        // 1. Panel.cs
        WritePanelFile(panelPath, panelName, panelFullName, controllerFullName);
        // 2. Controller.cs
        if (m_GenerateController)
        {
            WriteControllerFile(controllerPath, controllerName, controllerFullName, panelFullName);
        }

        AssetDatabase.Refresh();

        // 3. Prefab 骨架（需面板类型编译完成后才能挂组件 → delayCall）
        if (m_GeneratePrefab)
        {
            EditorApplication.delayCall += () => CreatePrefabSkeleton(prefabPath, panelFullName, panelName);
        }

        EditorUtility.DisplayDialog("生成完成",
            $"已生成：\n{panelPath}\n{(m_GenerateController ? controllerPath + "\n" : "")}" +
            (m_GeneratePrefab ? $"{prefabPath}（编译完成后自动生成）\n" : "") +
            $"\n下一步：在任意 CanvasBase 子类的 OnRebuild 中调用 CreatePanel<{panelName}>()（不绑定 Canvas）。",
            "OK");
    }

    void WritePanelFile(string path, string panelName, string panelFullName, string controllerFullName)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string content =
$@"// 由 MCV/创建/UI Panel 生成器生成（{DateTime.Now:yyyy-MM-dd}）—— 请按需补充业务代码
using MCV_Module.UI;

namespace {PanelNamespace}
{{
    /// <summary>{panelName} 面板</summary>
    [RequireController(typeof({controllerFullName}))]
    public class {panelName} : PanelBase
    {{
        // 可覆写点：
        //   Awake()     —— 初始化字段
        //   OnDestroy() —— 解绑事件（EventBus 强引用务必在此 Unsubscribe）
    }}
}}
";
        File.WriteAllText(path, content);
    }

    void WriteControllerFile(string path, string controllerName, string controllerFullName, string panelFullName)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string content =
$@"// 由 MCV/创建/UI Panel 生成器生成（{DateTime.Now:yyyy-MM-dd}）—— 请按需补充业务代码
using MCV_Module.Controllers;

namespace {ControllerNamespace}
{{
    public class {controllerName} : ControllerBase<{panelFullName}>
    {{
        protected override void OnViewBound()
        {{
            // TODO: 在此注册 View 事件监听（先清后加，避免重复订阅）
        }}
    }}
}}
";
        File.WriteAllText(path, content);
    }

    /// <summary>创建 Prefab 骨架：Stretch 全屏根节点 + Panel 组件（编译完成后执行）。</summary>
    void CreatePrefabSkeleton(string prefabPath, string panelFullName, string panelName)
    {
        var panelType = ResolveType(panelFullName);
        if (panelType == null)
        {
            Debug.LogWarning($"[PanelGenerator] 面板类型尚未编译完成，请稍后手动生成 Prefab：{panelFullName}");
            return;
        }

        string dir = Path.GetDirectoryName(prefabPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var go = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer));
        go.AddComponent(panelType);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        DestroyImmediate(go);

        if (prefab != null)
        {
            Debug.Log($"[PanelGenerator] Prefab 骨架已生成：{prefabPath}");
        }
        else
        {
            Debug.LogError($"[PanelGenerator] Prefab 生成失败：{prefabPath}");
        }
    }

    /// <summary>跨程序集类型解析：先按默认程序集，再遍历已加载程序集（面板可能在 Assembly-CSharp / 包程序集）。</summary>
    static Type ResolveType(string fullName)
    {
        var type = Type.GetType(fullName);
        if (type != null) return type;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(fullName);
            if (type != null) return type;
        }
        return null;
    }
    #endregion
}
