using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// AgentCanvas UI Toolkit 元件预览场景设置工具。
/// 用 3 种布局引擎分别展示对应场景，模拟真实页面效果。
/// 菜单: AgentCanvas → Create Preview Scene
/// </summary>
public class AgentCanvasPreviewSetup
{
    [MenuItem("AgentCanvas/Create Preview Scene")]
    static void CreatePreviewScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        var scenePath = "Assets/Scenes/AgentCanvasPreview.unity";

        var uiDocGO = new GameObject("AgentCanvasUIDocument");
        var uiDoc = uiDocGO.AddComponent<UIDocument>();
        uiDoc.panelSettings = GetOrCreatePanelSettings();

        var root = uiDoc.rootVisualElement;
        root.style.flexGrow = 1;

        // 加载 USS 样式表
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Resources/UI/AgentCanvas/AgentCanvasStyles.uss");
        if (styleSheet != null)
            root.styleSheets.Add(styleSheet);
        else
            Debug.LogWarning("[Preview] AgentCanvasStyles.uss not found, using fallback inline styles.");

        // ── 页面标题 ──
        var header = new Label("AgentCanvas UI Toolkit 布局与元件预览");
        header.AddToClassList("agent-title-text");
        header.style.unityTextAlign = TextAnchor.MiddleCenter;
        header.style.marginTop = 16;
        header.style.marginBottom = 4;
        root.Add(header);

        var sub = new Label("三种布局引擎 × 十种元件类型");
        sub.style.fontSize = 14;
        sub.style.color = new Color(0.42f, 0.42f, 0.42f); // #6B6B6B
        sub.style.marginBottom = 16;
        sub.style.unityTextAlign = TextAnchor.MiddleCenter;
        root.Add(sub);

        // ── 三个布局卡片并排 ──
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexGrow = 1;
        row.style.marginLeft = 8;
        row.style.marginRight = 8;
        row.style.marginBottom = 16;
        root.Add(row);

        // free_stack 页面
        var freeCol = new VisualElement();
        freeCol.style.width = Length.Percent(33);
        freeCol.style.marginRight = 6;
        row.Add(freeCol);
        BuildFreeStackPage(freeCol);

        // waterfall 页面
        var waterCol = new VisualElement();
        waterCol.style.width = Length.Percent(33);
        waterCol.style.marginLeft = 6;
        waterCol.style.marginRight = 6;
        row.Add(waterCol);
        BuildWaterfallPage(waterCol);

        // three_column 页面
        var threeCol = new VisualElement();
        threeCol.style.width = Length.Percent(33);
        threeCol.style.marginLeft = 6;
        row.Add(threeCol);
        BuildThreeColumnPage(threeCol);

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[AgentCanvas Preview] 场景已创建: {scenePath}");
    }

    // ═══════════════════════════════════════════════════════════════
    // 页面 1: free_stack — 知识点展示
    // ═══════════════════════════════════════════════════════════════
    static void BuildFreeStackPage(VisualElement parent)
    {
        var card = MakePageCard("free_stack — 自由堆积", 300);
        parent.Add(card);

        var area = new VisualElement();
        area.style.position = Position.Relative;
        area.style.flexGrow = 1;
        area.style.minHeight = 450;
        area.style.backgroundColor = new Color(0.961f, 0.941f, 0.910f); // #F5F0E8

        // title 元件
        var title = new Label("光学显微镜的构造");
        title.style.position = Position.Absolute;
        title.style.left = 16;
        title.style.top = 16;
        title.AddToClassList("agent-title-text");
        area.Add(title);

        // image 元件
        var imgBox = new VisualElement();
        imgBox.style.position = Position.Absolute;
        imgBox.style.left = 16;
        imgBox.style.top = 56;
        imgBox.style.width = 140;
        imgBox.style.height = 120;
        imgBox.AddToClassList("agent-model-viewport");
        var imgLabel = new Label("🔬");
        imgLabel.style.fontSize = 32;
        imgLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        imgLabel.style.marginTop = 40;
        imgBox.Add(imgLabel);
        area.Add(imgBox);

        // subtitle_text 元件
        var st = new Label("实验目的");
        st.style.position = Position.Absolute;
        st.style.left = 170;
        st.style.top = 56;
        st.AddToClassList("agent-subtitle-title");
        area.Add(st);

        var stc = new Label("了解光学显微镜的基本构造,\n掌握正确的使用方法和维护要点。");
        stc.style.position = Position.Absolute;
        stc.style.left = 170;
        stc.style.top = 82;
        stc.style.whiteSpace = WhiteSpace.Normal;
        stc.style.width = 180;
        stc.AddToClassList("agent-subtitle-content");
        area.Add(stc);

        // button 元件
        var btn = new Button(() => { }) { text = "开始学习" };
        btn.style.position = Position.Absolute;
        btn.style.left = 170;
        btn.style.top = 150;
        btn.AddToClassList("agent-button-element");
        area.Add(btn);

        // image_text 元件
        var it = new VisualElement();
        it.style.position = Position.Absolute;
        it.style.left = 16;
        it.style.top = 190;
        it.style.flexDirection = FlexDirection.Row;
        it.style.width = 340;
        var itImg = new VisualElement();
        itImg.style.width = 80;
        itImg.style.height = 60;
        itImg.AddToClassList("agent-model-viewport");
        it.Add(itImg);
        var itTxt = new Label("显微镜由目镜、物镜、载物台等组成。");
        itTxt.AddToClassList("agent-image-text-body");
        itTxt.style.marginLeft = 6;
        it.Add(itTxt);
        area.Add(it);

        card.Add(area);
    }

    // ═══════════════════════════════════════════════════════════════
    // 页面 2: waterfall — 步骤/清单
    // ═══════════════════════════════════════════════════════════════
    static void BuildWaterfallPage(VisualElement parent)
    {
        var card = MakePageCard("waterfall — 瀑布流", 300);
        parent.Add(card);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.style.minHeight = 450;
        scroll.style.backgroundColor = new Color(0.961f, 0.941f, 0.910f); // #F5F0E8

        var container = scroll.contentContainer;
        container.style.paddingLeft = 12;
        container.style.paddingRight = 12;
        container.style.paddingTop = 12;
        container.style.paddingBottom = 12;

        // title 元件
        var title = new Label("显微镜使用步骤");
        title.AddToClassList("agent-title-text");
        title.style.marginBottom = 12;
        container.Add(title);

        // 步骤列表（waterfall 典型用法）
        var steps = new[] {
            ("取镜与安放", "将显微镜从柜中取出，置于实验台上。"),
            ("对光", "转动转换器，使低倍物镜对准通光孔。"),
            ("放置标本", "将玻片标本放在载物台上，用压片夹压住。"),
            ("调焦观察", "转动粗准焦螺旋，使镜筒缓慢下降至物镜接近标本。"),
            ("收镜整理", "实验结束后，将显微镜擦拭干净并放回原处。"),
        };
        foreach (var (titleText, desc) in steps)
        {
            var item = new VisualElement();
            item.style.marginBottom = 10;
            item.style.paddingBottom = 10;
            item.style.borderBottomColor = new Color(0.9f, 0.9f, 0.92f);
            item.style.borderBottomWidth = 1;

            // subtitle_text 元件
            var st = new Label(titleText);
            st.AddToClassList("agent-subtitle-title");
            st.style.marginBottom = 2;
            item.Add(st);

            var sd = new Label(desc);
            sd.AddToClassList("agent-subtitle-content");
            sd.style.whiteSpace = WhiteSpace.Normal;
            item.Add(sd);

            container.Add(item);
        }

        // button_list 元件
        var blTitle = new Label("操作：");
        blTitle.AddToClassList("agent-button-list-title");
        blTitle.style.marginTop = 4;
        container.Add(blTitle);

        foreach (var act in new[] { "重播步骤", "查看原理", "返回目录" })
        {
            var b = new Button(() => { }) { text = act };
            b.AddToClassList("agent-button-list-item");
            b.style.marginBottom = 4;
            container.Add(b);
        }

        card.Add(scroll);
    }

    // ═══════════════════════════════════════════════════════════════
    // 页面 3: three_column — 图文对照 + 答题
    // ═══════════════════════════════════════════════════════════════
    static void BuildThreeColumnPage(VisualElement parent)
    {
        var card = MakePageCard("three_column — 左中右", 300);
        parent.Add(card);

        var area = new VisualElement();
        area.style.flexDirection = FlexDirection.Row;
        area.style.flexGrow = 1;
        area.style.minHeight = 450;
        area.style.backgroundColor = new Color(0.961f, 0.941f, 0.910f); // #F5F0E8

        // ── 左栏: title + subtitle_text ──
        var left = new VisualElement();
        left.style.width = Length.Percent(33);
        left.style.paddingLeft = 8;
        left.style.paddingRight = 4;
        left.style.paddingTop = 12;

        var lh = new Label("实验原理");
        lh.AddToClassList("agent-markdown-h1");
        lh.style.marginBottom = 8;
        left.Add(lh);

        var lc = new Label("显微镜利用凸透镜的放大成像原理，将微小的物体放大至肉眼可观察的尺寸。\n\n物镜：将标本放大形成倒立实像。\n目镜：将实像进一步放大形成虚像。");
        lc.AddToClassList("agent-markdown-paragraph");
        lc.style.whiteSpace = WhiteSpace.Normal;
        left.Add(lc);

        // model 元件
        var modelBox = new VisualElement();
        modelBox.AddToClassList("agent-model-viewport");
        modelBox.style.minHeight = 100;
        modelBox.style.marginTop = 12;
        var ml = new Label("📦 3D 模型视口");
        ml.style.unityTextAlign = TextAnchor.MiddleCenter;
        ml.style.marginTop = 40;
        modelBox.Add(ml);
        left.Add(modelBox);

        area.Add(left);

        // ── 中栏: image + video ──
        var center = new VisualElement();
        center.style.width = Length.Percent(33);
        center.style.paddingLeft = 4;
        center.style.paddingRight = 4;
        center.style.paddingTop = 12;

        var ci = new VisualElement();
        ci.AddToClassList("agent-model-viewport");
        ci.style.height = 100;
        var cil = new Label("🔬 显微镜结构图");
        cil.style.unityTextAlign = TextAnchor.MiddleCenter;
        cil.style.marginTop = 38;
        ci.Add(cil);
        center.Add(ci);

        // video 元件
        var vb = new VisualElement();
        vb.AddToClassList("agent-video-player");
        vb.style.minHeight = 80;
        vb.style.marginTop = 8;
        var vl = new Label("▶ 使用教程视频");
        vl.style.unityTextAlign = TextAnchor.MiddleCenter;
        vl.style.marginTop = 30;
        vb.Add(vl);
        center.Add(vb);

        area.Add(center);

        // ── 右栏: 答题交互区 ──
        var right = new VisualElement();
        right.style.width = Length.Percent(33);
        right.style.paddingLeft = 4;
        right.style.paddingRight = 8;
        right.style.paddingTop = 12;

        var rh = new Label("小测验");
        rh.AddToClassList("agent-markdown-h1");
        rh.style.marginBottom = 8;
        right.Add(rh);

        // choice 元件
        var cq = new Label("显微镜的光源位于？");
        cq.AddToClassList("agent-choice-question");
        cq.style.marginBottom = 6;
        right.Add(cq);

        foreach (var opt in new[] { "A. 目镜上方", "B. 载物台下方", "C. 物镜内部" })
        {
            var rb = new RadioButton(opt);
            rb.style.marginBottom = 3;
            rb.style.fontSize = 13;
            right.Add(rb);
        }

        var submitBtn = new Button(() => { }) { text = "提交答案" };
        submitBtn.AddToClassList("agent-choice-submit");
        submitBtn.style.marginTop = 6;
        submitBtn.style.marginBottom = 12;
        right.Add(submitBtn);

        // fill 元件
        var fq = new Label("放大倍数 = 目镜 × ____");
        fq.AddToClassList("agent-fill-question");
        fq.style.marginBottom = 4;
        right.Add(fq);

        var fi = new TextField();
        fi.style.marginBottom = 6;
        fi.style.fontSize = 13;
        right.Add(fi);

        var fbtn = new Button(() => { }) { text = "提交" };
        fbtn.AddToClassList("agent-fill-submit");
        right.Add(fbtn);

        area.Add(right);
        card.Add(area);
    }

    // ═══════════════════════════════════════════════════════════════
    // 辅助方法
    // ═══════════════════════════════════════════════════════════════

    static VisualElement MakePageCard(string title, float minH)
    {
        var card = new VisualElement();
        card.style.minHeight = minH;

        var tl = new Label(title);
        tl.style.fontSize = 14;
        tl.style.unityFontStyleAndWeight = FontStyle.Bold;
        tl.style.color = new Color(0.35f, 0.35f, 0.42f);
        tl.style.marginBottom = 6;
        tl.style.paddingLeft = 4;
        card.Add(tl);

        return card;
    }

    static Button MakeVEButton(string text, Color bg)
    {
        var btn = new Button(() => { }) { text = text };
        btn.style.backgroundColor = bg;
        btn.style.color = Color.white;
        SetBorderRadius(btn, 4);
        btn.style.paddingLeft = 16;
        btn.style.paddingRight = 16;
        btn.style.paddingTop = 6;
        btn.style.paddingBottom = 6;
        btn.style.fontSize = 13;
        btn.style.alignSelf = Align.FlexStart;
        btn.style.whiteSpace = WhiteSpace.NoWrap;
        return btn;
    }

    static void SetBorder(VisualElement ve, Color color, float width)
    {
        ve.style.borderTopColor = color;
        ve.style.borderRightColor = color;
        ve.style.borderBottomColor = color;
        ve.style.borderLeftColor = color;
        ve.style.borderTopWidth = width;
        ve.style.borderRightWidth = width;
        ve.style.borderBottomWidth = width;
        ve.style.borderLeftWidth = width;
    }

    static void SetBorderRadius(VisualElement ve, float radius)
    {
        ve.style.borderTopLeftRadius = radius;
        ve.style.borderTopRightRadius = radius;
        ve.style.borderBottomLeftRadius = radius;
        ve.style.borderBottomRightRadius = radius;
    }

    static PanelSettings GetOrCreatePanelSettings()
    {
        var guids = AssetDatabase.FindAssets("t:PanelSettings");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        }

        var dir = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var ps = ScriptableObject.CreateInstance<PanelSettings>();
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0.5f;

        var assetPath = $"{dir}/AgentCanvasPanelSettings.asset";
        AssetDatabase.CreateAsset(ps, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Preview] 已创建 PanelSettings: {assetPath}");
        return ps;
    }
}
