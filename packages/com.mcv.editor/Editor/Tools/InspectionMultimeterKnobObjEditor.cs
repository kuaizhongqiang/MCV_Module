using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives.TaskObj;
using UnityEditor;
using UnityEngine;

// WHY: 面板刻度是死的，旋钮能停在哪由档位表的 angle 决定：填充时先在「当前 0° 正对刻度」里选旋钮此刻正指着的刻度（默认 OFF），整张表按它平移，不必猜「Z=0 对着哪儿」。
// WHY: 预览只改可视层的 localRotation，不碰数据、不 SetDirty —— 运行时朝向由 zeroEuler 姿态 + 档位角度重算，残留预览姿态不影响运行。
/// <summary>万用表旋钮的「角度标定」面板（覆盖默认 Inspector）：按面板刻度一键填充档位表 + 逐档标定。</summary>

[CustomEditor(typeof(InspectionMultimeterKnobObj))]
public class InspectionMultimeterKnobObjEditor : Editor
{
    /// <summary>推荐档位（本模型数字表面板，从正面看**逆时针为正**；角度以「OFF = 0°」为基准）。</summary>
    struct RecommendGear
    {
        public MultimeterGearType gearType;
        public float range;         // WHY: 量程上限（0 = 不判超量程）
        public float angle;

        public RecommendGear(MultimeterGearType gearType, float range, float angle)
        {
            this.gearType = gearType;
            this.range = range;
            this.angle = angle;
        }
    }

    // WHY: 角度按本模型照片估的，实际指向对不上刻度时用下面的预览工具逐档纠正；面板上的 Hz% / °C·°F / hFE / NCV 不配就不会停在那儿。
    /// <summary>面板刻度顺序（旋钮顺时针方向）= OFF → V= → Ω(二极管/通断) → μA= → mA= → A=。</summary>
    static readonly RecommendGear[] Recommend =
    {
        new RecommendGear(MultimeterGearType.Off, 0f, 0f),
        new RecommendGear(MultimeterGearType.VoltageDC, 600f, -21f),
        new RecommendGear(MultimeterGearType.Resistance, 0f, -46f),
        new RecommendGear(MultimeterGearType.CurrentDC, 0.0002f, -134f),
        new RecommendGear(MultimeterGearType.CurrentDC, 0.2f, -153f),
        new RecommendGear(MultimeterGearType.CurrentDC, 10f, -172f),
    };

    [SerializeField] int m_GearIndex;
    [SerializeField] float m_PreviewAngle;

    /// <summary>填充时的「0° 基准」：旋钮在场景里角度为 0 时正对着的那个刻度。</summary>
    [SerializeField] int m_ZeroAnchorIndex;

    InspectionMultimeterKnobObj Knob => (InspectionMultimeterKnobObj)target;

    void OnEnable()
    {
        LoadGearAngleToPreview();
        ApplyPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);
        DrawRecommendButton();
        DrawCalibration();
    }

    #region 一键填充
    void DrawRecommendButton()
    {
        m_ZeroAnchorIndex = Mathf.Clamp(m_ZeroAnchorIndex, 0, Recommend.Length - 1);

        var anchorLabels = new string[Recommend.Length];
        for (int i = 0; i < Recommend.Length; i++) anchorLabels[i] = RecommendLabel(i);

        m_ZeroAnchorIndex = EditorGUILayout.Popup("当前 0° 正对刻度", m_ZeroAnchorIndex, anchorLabels);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("按面板刻度填充档位表")) FillFromRecommend();
            if (GUILayout.Button("整表取负", GUILayout.Width(72))) FlipAllAngles();
        }
    }

    /// <summary>按推荐刻度填充档位表：以「当前 0° 正对刻度」为基准整体平移。</summary>
    void FillFromRecommend()
    {
        bool ok = EditorUtility.DisplayDialog("填充档位表",
            $"用本模型面板上的 6 个档位替换当前档位表，并把角度以「当前 0° 正对：{RecommendLabel(m_ZeroAnchorIndex)}」为基准对齐。\n" +
            "已有档位表会被覆盖，确定继续？", "填充", "取消");
        if (!ok) return;

        var knob = Knob;
        Undo.RecordObject(knob, "填充万用表档位表");

        // WHY: 推荐角度是"以 OFF 为 0°"的相对值 —— 按基准刻度整体平移，基准档就落在 0° 上。
        float offset = Recommend[m_ZeroAnchorIndex].angle;

        var gears = new List<InspectionMultimeterKnobObj.GearSetting>();
        for (int i = 0; i < Recommend.Length; i++)
        {
            gears.Add(new InspectionMultimeterKnobObj.GearSetting
            {
                gearType = Recommend[i].gearType,
                range = Recommend[i].range,
                angle = Recommend[i].angle - offset,
            });
        }

        knob.EditorSetGears(gears);
        knob.EditorSetStartIndex(m_ZeroAnchorIndex);    // WHY: 初始档 = 基准档（旋钮当下就停在那儿）

        EditorUtility.SetDirty(knob);
        serializedObject.Update();

        m_GearIndex = m_ZeroAnchorIndex;
        m_PreviewAngle = gears[m_GearIndex].angle;      // WHY: 基准档 = 0°
        ApplyPreview();

        Debug.Log($"[万用表旋钮] {knob.name} 档位表已填充 {Recommend.Length} 档，" +
                  $"以「{RecommendLabel(m_ZeroAnchorIndex)}」为 0° 基准对齐。若还有偏差，用下面的预览工具逐档纠正。");
    }

    /// <summary>推荐档位的显示名：功能(量程) + 以 OFF 为 0° 时的角度。</summary>
    static string RecommendLabel(int index)
    {
        var rec = Recommend[index];
        string range = rec.range > 0f ? $" {rec.range:0.#####}" : string.Empty;
        return $"{MCV_Module.Utils.ChnNameMap.Get(rec.gearType)}{range}  ({rec.angle:0.##}°)";
    }

    /// <summary>所有档位角度取负 —— 整圈转反了（旋钮 +Z 背对观察者）时一键纠正。</summary>
    void FlipAllAngles()
    {
        var knob = Knob;
        if (knob.GearCount == 0) return;

        Undo.RecordObject(knob, "万用表旋钮角度取负");

        for (int i = 0; i < knob.GearCount; i++)
            knob.EditorSetGearAngle(i, -knob.EditorGetGear(i).angle);

        EditorUtility.SetDirty(knob);
        serializedObject.Update();

        LoadGearAngleToPreview();
        ApplyPreview();

        Debug.Log($"[万用表旋钮] {knob.name} 所有档位角度已取负。");
    }
    #endregion

    #region 逐档标定
    void DrawCalibration()
    {
        EditorGUILayout.LabelField("角度标定", EditorStyles.boldLabel);

        var knob = Knob;
        int count = knob.GearCount;
        if (count == 0)
        {
            EditorGUILayout.HelpBox("档位表为空：先点上面的填充按钮，或在档位表里手动加几档。", MessageType.Info);
            return;
        }

        // WHY: 实测本模型「角度增大在屏幕上往哪转」，拖拽方向按它自动定，档位角度表也应与它一致。
        float direction = knob.MeasureAngleScreenDirection();
        EditorGUILayout.LabelField(
            $"本模型实测：角度增大 → 屏幕上{(direction > 0f ? "逆时针" : "顺时针")}（拖拽方向已按它自动定）",
            EditorStyles.miniLabel);

        EditorGUILayout.HelpBox(
            "选一档 → 拨角度（旋钮跟着转）→ 对准面板刻度 → 点「写入本档角度」。\n" +
            "角度表要和上面实测的转向一致：若填充后指针与面板刻度左右镜像（往 A= 那边拨指针反着走），点「整表取负」。\n" +
            "注：上面的实测用的是当前场景相机 / 预制体预览相机，换到游戏视角若方向不同（例如相机绕到了背面），以游戏里为准。",
            MessageType.None);

        m_GearIndex = Mathf.Clamp(m_GearIndex, 0, count - 1);

        var labels = new string[count];
        for (int i = 0; i < count; i++) labels[i] = $"{i}. {GearLabel(knob, i)}";
        int picked = EditorGUILayout.Popup("档位", m_GearIndex, labels);
        if (picked != m_GearIndex)
        {
            m_GearIndex = picked;
            LoadGearAngleToPreview();
            ApplyPreview();
        }

        float angle = EditorGUILayout.Slider("预览角度", m_PreviewAngle, -180f, 180f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("-5°")) angle -= 5f;
            if (GUILayout.Button("-1°")) angle -= 1f;
            if (GUILayout.Button("+1°")) angle += 1f;
            if (GUILayout.Button("+5°")) angle += 5f;
            if (GUILayout.Button("重载本档", GUILayout.Width(72))) angle = knob.EditorGetGear(m_GearIndex).angle;
        }

        if (!Mathf.Approximately(angle, m_PreviewAngle))
        {
            m_PreviewAngle = angle;
            ApplyPreview();
        }

        float saved = knob.EditorGetGear(m_GearIndex).angle;
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"档位表里的角度：{saved:0.##}°", GUILayout.Width(180));

            using (new EditorGUI.DisabledScope(Mathf.Approximately(saved, m_PreviewAngle)))
            {
                if (GUILayout.Button($"写入第 {m_GearIndex} 档 = {m_PreviewAngle:0.##}°"))
                {
                    Undo.RecordObject(knob, "写入旋钮档位角度");
                    knob.EditorSetGearAngle(m_GearIndex, m_PreviewAngle);

                    EditorUtility.SetDirty(knob);
                    serializedObject.Update();

                    Debug.Log($"[万用表旋钮] {knob.name} 第 {m_GearIndex} 档角度已写入：{m_PreviewAngle:0.##}°");
                }
            }
        }
    }

    /// <summary>档位下拉里的显示文本：序号 + 功能(量程) + 角度。</summary>
    static string GearLabel(InspectionMultimeterKnobObj knob, int index)
    {
        var gear = knob.EditorGetGear(index);
        string range = gear.range > 0f ? $" {gear.range:0.#####}" : string.Empty;
        return $"{MCV_Module.Utils.ChnNameMap.Get(gear.gearType)}{range}  [{gear.angle:0.##}°]";
    }

    void LoadGearAngleToPreview()
    {
        if (Knob.GearCount == 0)
        {
            m_PreviewAngle = 0f;
            return;
        }

        m_GearIndex = Mathf.Clamp(m_GearIndex, 0, Knob.GearCount - 1);
        m_PreviewAngle = Knob.EditorGetGear(m_GearIndex).angle;
    }

    /// <summary>把预览角度摆到可视层上（只改朝向，不写数据、不标脏）。</summary>
    void ApplyPreview()
    {
        var visual = Knob.Visual;
        if (visual == null) return;

        visual.localRotation = Knob.ComposeRotation(m_PreviewAngle);
    }
    #endregion
}
