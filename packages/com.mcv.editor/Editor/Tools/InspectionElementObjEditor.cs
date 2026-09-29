using System.Collections.Generic;
using System.Text;
using MCV_Module.Models;
using MCV_Module.Models.ElementInspection;
using MCV_Module.Objects.Interactives.TaskObj;
using MCV_Module.Utils;
using UnityEditor;
using UnityEngine;

// WHY: 自动配对把扫描到的点两两组合补进「正确组」或「错误组」，已有条目与读数原样保留、同一对不同时落进两组；两组都没登过的接点运行时判成「未登记」（屏幕 Err）。
// WHY: 删点后配对表会留 Missing 引用，配对前先清无效条目；数据读写用 SerializedProperty（撤销/预制体覆写/多选都正常），改完立刻 Update() 重读避免绘制用旧数据。
/// <summary>InspectionElementObj 的「检测面板」（覆盖默认 Inspector）：扫描 / 自动配对 / 清理 / 逐条编辑。</summary>

[CustomEditor(typeof(InspectionElementObj))]
public class InspectionElementObjEditor : Editor
{
    const string RightPath = "inspectionData.rightCheckPointDatas";
    const string WrongPath = "inspectionData.wrongCheckPointDatas";

    /// <summary>本物体下扫描到的检测点（绘制期间复用，避免每次重画都新建列表）</summary>
    readonly List<InspectionElementPointObj> m_Points = new List<InspectionElementPointObj>();

    /// <summary>按端子对分好组的检测点：键 = 端子对下标，值 = 落在该对上的点</summary>
    readonly Dictionary<int, List<InspectionElementPointObj>> m_Groups = new Dictionary<int, List<InspectionElementPointObj>>();

    /// <summary>没配上对的点（名字不在端子对表里，或该端子对只放了一个点）</summary>
    readonly List<InspectionElementPointObj> m_Unmatched = new List<InspectionElementPointObj>();

    InspectionElementObj Element => target as InspectionElementObj;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("elementType"), new GUIContent("元件类型"));

        var element = Element;
        if (element == null) return;

        DrawScan(element);
        DrawPairingButtons(element);

        DrawPairList(true, "正确接点组（接点正确，用这里的读数）");
        DrawPairList(false, "错误接点组（接错时的读数 / 提示用）");

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "运行时：红黑表笔分别吸附到本物体下的两个检测点后，万用表按「接点对」来这两组表里查读数。\n" +
            "**没登记**的接点对 = 这两点之间没有通路 → 电阻档读 OL（∞），不需要为「接不上」专门建条目。\n" +
            "正确组命中 = IsPairCorrect true（正确但开路时屏幕仍是 OL，那也算接对）。\n" +
            "读数：R(Ω) 填 0 = 导通；电阻无穷大请勾「∞ 开路」（电阻类档位显示 OL），不要靠填个大数凑。\n" +
            "**动作态另配**：勾上后，元件已动作（本体上挂了「动作部件」且被按下，如接触器的试验按键）时改用「动作」那一行 —— " +
            "自动配对已按端子对类型把两行都填好（动合：静止 ∞ → 动作 0Ω；动断相反）；线圈 / 绕组这类阻值不变的通路不要勾。",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    #region 扫描
    /// <summary>扫描并显示检测点统计；结果填进 <see cref="m_Points"/> 供配对使用。</summary>
    void DrawScan(InspectionElementObj element)
    {
        ElementCheckPointPairing.CollectChildPoints(element.gameObject, m_Points);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"检测点扫描：{m_Points.Count} 个（含未激活）", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(m_Points.Count == 0))
            {
                if (GUILayout.Button("全选定位", GUILayout.Width(72)))
                    Selection.objects = m_Points.ToArray();
            }
        }

        if (m_Points.Count == 0)
        {
            EditorGUILayout.HelpBox("本物体下没有 InspectionElementPointObj：请把检测点挂到它下面（未激活的也会被扫到）。",
                MessageType.Warning);
        }
        else if (m_Points.Count < 2)
        {
            EditorGUILayout.HelpBox("检测点不足 2 个，无法配对。", MessageType.Warning);
        }
    }
    #endregion

    #region 配对按钮
    void DrawPairingButtons(InspectionElementObj element)
    {
        // WHY: 端子对分组先算出来给预览与按钮共用，保证「上面写几对、点下去就配几对」。
        ElementCheckPointPairing.CollectTerminalGroups(m_Points, element.ElementType, m_Groups, m_Unmatched);

        DrawTerminalSummary(element);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(m_Groups.Count == 0))
            {
                if (GUILayout.Button("按端子对配对 → 正确组")) GenerateTerminalPairs(element, intoRight: true);
                if (GUILayout.Button("按端子对配对 → 错误组")) GenerateTerminalPairs(element, intoRight: false);
            }

            if (GUILayout.Button("清空两组", GUILayout.Width(72))) ClearAll(element);
        }

        EditorGUILayout.Space(2);
        if (GUILayout.Button("全部组合（候选池，不填默认读数）", GUILayout.Width(220))) GenerateAllCombinations(element);
    }

    /// <summary>识别到哪些端子对（按表顺序）+ 哪些点没配上 —— 面板上的"预警"，比生成完再数条数可靠。</summary>
    void DrawTerminalSummary(InspectionElementObj element)
    {
        if (m_Points.Count < 2) return;

        if (m_Groups.Count == 0)
        {
            EditorGUILayout.HelpBox($"按【{ChnNameMap.Get(element.ElementType)}】的端子对表没识别到任何一对：检测点请按端子号命名，" +
                                    "名字里的空格 / 括号 / 连字符会被忽略。表在 ElementCheckPointPairing（按元件类型各一张），缺标号就往里加。",
                MessageType.Warning);
            return;
        }

        var pairs = ElementCheckPointPairing.GetTerminalPairs(element.ElementType);
        var sb = new StringBuilder();
        for (int i = 0; i < pairs.Length; i++)
        {
            if (!m_Groups.TryGetValue(i, out var bucket)) continue;

            if (sb.Length > 0) sb.Append("、");
            sb.Append(ElementCheckPointPairing.GetTerminalPairLabel(element.ElementType, i));
            if (bucket.Count > 2) sb.Append($"×{bucket.Count}");
        }

        EditorGUILayout.HelpBox(
            $"按【{ChnNameMap.Get(element.ElementType)}】的端子对表识别到 {m_Groups.Count} 组（会生成 {m_Groups.Count} 条配对）：{sb}",
            MessageType.Info);

        if (m_Unmatched.Count == 0) return;

        EditorGUILayout.HelpBox($"这 {m_Unmatched.Count} 个点不参与配对（名字不在本元件的端子对表里，或该端子对只放了一个点）：" +
                                $"{JoinNames(m_Unmatched)}", MessageType.Warning);
    }

    // WHY: 先清掉两组里的无效条目；已存在的组合（含对面那组里的）跳过，已有条目与读数不会被覆盖。
    /// <summary>按端子对自动配对：同组两个端子配成一对（A1-A2、1L1-2T1…）补进目标组，新条目按「测量状态」+ 端子对类型填默认读数（动合静止=开路 OL、动断静止=0Ω；线圈绕组留 0 待填）。</summary>
    void GenerateTerminalPairs(InspectionElementObj element, bool intoRight)
    {
        Undo.RecordObject(element, "按端子对配对检测点");

        int cleaned = CleanInvalid(element);
        int added = ElementCheckPointPairing.BuildTerminalPairs(
            m_Points, element.ElementType, GetList(element, intoRight), GetList(element, !intoRight),
            m_Unmatched);

        FinishGenerate(element, cleaned, added, intoRight ? "正确组" : "错误组");
    }

    /// <summary>全部两两组合（n 个点 → n(n−1)/2 条）：端子命名不规范时的兜底，条目会很多，先弹确认。</summary>
    void GenerateAllCombinations(InspectionElementObj element)
    {
        int total = m_Points.Count * (m_Points.Count - 1) / 2;
        bool ok = EditorUtility.DisplayDialog("全部组合（候选池）",
            $"会把 {m_Points.Count} 个检测点**两两组合**成 {total} 条，全部补进正确组。\n" +
            "条目多起来后很难维护，一般只在端子号不规则的元件上用。确定继续？", "继续", "取消");
        if (!ok) return;

        Undo.RecordObject(element, "生成全部检测点组合");

        int cleaned = CleanInvalid(element);
        int added = ElementCheckPointPairing.BuildAllPairs(
            m_Points, element.Data.rightCheckPointDatas, element.Data.wrongCheckPointDatas);

        FinishGenerate(element, cleaned, added, "正确组（全部组合）");
    }

    /// <summary>改完数据后的收口：标脏 + 重读序列化对象（下面的绘制与 Apply 才不会用旧数据）+ 日志。</summary>
    void FinishGenerate(InspectionElementObj element, int cleaned, int added, string group)
    {
        EditorUtility.SetDirty(element);
        serializedObject.Update();

        Debug.Log($"[检测配对] {element.name}：{group} 新增 {added} 条（清理无效 {cleaned} 条），" +
                  $"当前 正确组 {element.Data.rightCheckPointDatas.Count} / 错误组 {element.Data.wrongCheckPointDatas.Count} 条");
    }

    /// <summary>清掉两组里因删点而留下空引用的条目。</summary>
    static int CleanInvalid(InspectionElementObj element)
    {
        return ElementCheckPointPairing.RemoveInvalidPairs(element.Data.rightCheckPointDatas)
             + ElementCheckPointPairing.RemoveInvalidPairs(element.Data.wrongCheckPointDatas);
    }

    /// <summary>把点名拼成一行（太长就截断，避免 HelpBox 撑爆）。</summary>
    static string JoinNames(List<InspectionElementPointObj> points, int max = 12)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < points.Count && i < max; i++)
        {
            if (sb.Length > 0) sb.Append("、");
            sb.Append(points[i] != null ? points[i].name : "Missing");
        }

        if (points.Count > max) sb.Append($" 等 {points.Count} 个");
        return sb.ToString();
    }

    void ClearAll(InspectionElementObj element)
    {
        bool ok = EditorUtility.DisplayDialog("清空检测点配对",
            $"确定清空「{element.name}」的两组配对？已填的电阻 / 电流 / 电压会一起丢掉。", "清空", "取消");
        if (!ok) return;

        Undo.RecordObject(element, "清空检测点配对");
        element.Data.rightCheckPointDatas.Clear();
        element.Data.wrongCheckPointDatas.Clear();

        EditorUtility.SetDirty(element);
        serializedObject.Update();
    }

    static List<CheckPointData> GetList(InspectionElementObj element, bool right)
    {
        return right ? element.Data.rightCheckPointDatas : element.Data.wrongCheckPointDatas;
    }
    #endregion

    #region 配对列表绘制
    void DrawPairList(bool right, string title)
    {
        var listProp = serializedObject.FindProperty(right ? RightPath : WrongPath);
        if (listProp == null) return;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"{title} —— {listProp.arraySize} 条", EditorStyles.boldLabel);

        if (listProp.arraySize == 0)
        {
            EditorGUILayout.LabelField("（空）", EditorStyles.miniLabel);
            return;
        }

        for (int i = 0; i < listProp.arraySize; i++)
        {
            DrawPairItem(listProp, listProp.GetArrayElementAtIndex(i), i);
        }
    }

    /// <summary>画一条配对（删除时直接 ExitGUI 结束本轮绘制，避免边遍历边改数组）。</summary>
    void DrawPairItem(SerializedProperty listProp, SerializedProperty itemProp, int index)
    {
        var pointsProp = itemProp.FindPropertyRelative("checkPoints");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{index + 1}. {BuildPairLabel(pointsProp)}", EditorStyles.boldLabel);

                if (GUILayout.Button("定位", GUILayout.Width(48))) PingPair(pointsProp);

                if (GUILayout.Button("×", GUILayout.Width(24)))
                {
                    listProp.DeleteArrayElementAtIndex(index);
                    GUIUtility.ExitGUI();   // WHY: 元素已删，本轮绘制立即收尾。
                }
            }

            EditorGUILayout.PropertyField(pointsProp, new GUIContent("接点对"), true);

            var openProp = itemProp.FindPropertyRelative("openCircuit");
            bool open = openProp != null && openProp.boolValue;

            var hasActuatedProp = itemProp.FindPropertyRelative("hasActuated");
            bool hasActuated = hasActuatedProp != null && hasActuatedProp.boolValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("静止", EditorStyles.miniLabel, GUILayout.Width(26));

                // WHY: 开路时电阻没有数字可填（∞），灰掉 R 免得填了个数还被显示成 OL。
                DrawValueField(itemProp.FindPropertyRelative("resistance"), "R", "Ω", !open);
                DrawValueField(itemProp.FindPropertyRelative("current"), "I", "A");
                DrawValueField(itemProp.FindPropertyRelative("voltage"), "U", "V");

                if (openProp != null)
                {
                    openProp.boolValue = EditorGUILayout.ToggleLeft(
                        new GUIContent("∞ 开路", "静止状态下该接点对电阻无穷大：电阻类档位屏幕显示 OL（真数字表行为）"),
                        openProp.boolValue, GUILayout.Width(76));
                }

                if (hasActuatedProp != null)
                {
                    hasActuatedProp.boolValue = EditorGUILayout.ToggleLeft(
                        new GUIContent("动作态另配", "勾上后：元件已动作（本体上挂了动作部件且被按下，如试验按键）时改用下面那一行；不勾 = 动作前后读数不变"),
                        hasActuatedProp.boolValue, GUILayout.Width(88));
                }
            }

            if (hasActuated)
            {
                var actOpenProp = itemProp.FindPropertyRelative("actuatedOpenCircuit");
                bool actOpen = actOpenProp != null && actOpenProp.boolValue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("动作", EditorStyles.miniLabel, GUILayout.Width(26));

                    DrawValueField(itemProp.FindPropertyRelative("actuatedResistance"), "R", "Ω", !actOpen);
                    DrawValueField(itemProp.FindPropertyRelative("actuatedCurrent"), "I", "A");
                    DrawValueField(itemProp.FindPropertyRelative("actuatedVoltage"), "U", "V");

                    if (actOpenProp != null)
                    {
                        actOpenProp.boolValue = EditorGUILayout.ToggleLeft(
                            new GUIContent("∞ 开路", "动作后该接点对断开：电阻类档位屏幕显示 OL"),
                            actOpenProp.boolValue, GUILayout.Width(76));
                    }
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    static void DrawValueField(SerializedProperty prop, string label, string unit, bool editable = true)
    {
        if (prop == null) return;

        using (new EditorGUI.DisabledScope(!editable))
        {
            prop.floatValue = EditorGUILayout.FloatField(new GUIContent($"{label}({unit})"), prop.floatValue);
        }
    }

    /// <summary>接点对的显示名：用检测点自己的点名（元件中文名_物体名）。</summary>
    static string BuildPairLabel(SerializedProperty pointsProp)
    {
        if (pointsProp == null || !pointsProp.isArray || pointsProp.arraySize == 0) return "（未分配接点）";

        var sb = new StringBuilder();
        for (int i = 0; i < pointsProp.arraySize; i++)
        {
            if (i > 0) sb.Append("  ↔  ");

            var point = pointsProp.GetArrayElementAtIndex(i).objectReferenceValue as InspectionElementPointObj;
            sb.Append(point != null ? point.GetPointName() : "Missing");
        }
        return sb.ToString();
    }

    /// <summary>定位一条配对：选中并 Ping 它的两个检测点。</summary>
    static void PingPair(SerializedProperty pointsProp)
    {
        var objects = new List<Object>();
        for (int i = 0; i < pointsProp.arraySize; i++)
        {
            var point = pointsProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (point != null) objects.Add(point);
        }
        if (objects.Count == 0) return;

        EditorGUIUtility.PingObject(objects[0]);
        Selection.objects = objects.ToArray();
    }
    #endregion
}
