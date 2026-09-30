using System;
using MCV_Module.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCV_Module.UI.Components;
using MCV_Module.UI.Tools;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    public class FunctionPanel : PanelBase
    {
        [SerializeField] Transform btnParent;
        [SerializeField] Button switchBtn;
        readonly List<Button> functionBtns = new List<Button>();
        readonly List<GameObject> spacingObjs = new List<GameObject>();
        readonly Vector2 layoutSpacting = new Vector2(10, -30);
        // WHY: 按钮与分隔预制体已移出 Resources 进 UI 包，这里存裸 prefab 名（UIPrefabUtil 按 ui_{name} 拼包配置 id）。
        const string FunctionBtnPrefabName = "FunctionBtn";
        const string SpacingObjPrefabName = "FunctionSpacing";
        bool isActiveNow = true;
        bool m_TargetActive = true;   // 当前动画/静止所朝向的目标状态，用于防重复触发

        // WHY: 缓存引用，避免每帧 GetComponent
        HorizontalLayoutGroup m_LayoutGroup;
        CanvasGroup m_SwitchCanvasGroup;
        RectTransform m_PanelRect;
        RectTransform m_SwitchRect;

        // 这些按钮之后插入一个分隔对象
        static readonly HashSet<string> SpacingAfter = new HashSet<string> { "BackBtn", "MuteBtn" };
        static readonly string[] DefaultBtnNames =
        {
            "ExitBtn", "BackBtn", "SettingBtn", "MuteBtn",
            "ResourcePanelBtn", "SummitBtn", "RecordBtn"
        };

        public event Action OnFunctionExitClick;
        public event Action OnFunctionBackClick;
        public event Action OnFunctionSettingClick;
        public event Action OnFunctionResourcePanelClick;
        public event Action OnFunctionSummitClick;
        public event Action OnFunctionRecordClick;
        public event Action OnFunctionMuteClick;
        public event Action<bool> OnFunctionPanelSwitch;

        protected override void Awake()
        {
            base.Awake();
            if (btnParent == null || switchBtn == null)
            {
                Log.Error("[FunctionPanel] btnParent / switchBtn 未赋值");
                return;
            }
            m_SwitchCanvasGroup = switchBtn.GetComponent<CanvasGroup>();
            m_PanelRect = GetComponent<RectTransform>();
            m_SwitchRect = switchBtn.GetComponent<RectTransform>();
            m_LayoutGroup = btnParent.GetComponent<HorizontalLayoutGroup>();

            if ( m_SwitchCanvasGroup == null)
            {
                Log.Error("[FunctionPanel] 缺少 CanvasGroup 组件（panel 或 switchBtn）");
            }

            // WHY: 让 switchBtn 忽略父级 CanvasGroup, 否则 panel 隐藏(alpha=0) 时会把子物体 switch 一起隐藏, 导致无法点击开关重新展开
            if (m_SwitchCanvasGroup != null)
            {
                m_SwitchCanvasGroup.ignoreParentGroups = true;
            }

            functionBtns.Clear();
            spacingObjs.Clear();
            ClearChildren(btnParent);

            CreateFunctionBtns(DefaultBtnNames);

            // WHY: 按钮与分隔是运行时生成的（按钮文案也要等 TextComponent 装配），布局统一等一帧 + 自下而上刷
            RequestLayoutRebuild(btnParent);

            m_TargetActive = isActiveNow;
            ActiveState(isActiveNow);
        }
        
        public void SetFunctionBtnActive(string btnName, bool isActive)
        {
            Button btn = GetBtnByName(btnName);
            if (btn == null) return;

            btn.gameObject.SetActive(isActive);
            // WHY: 隐藏时清空监听，避免隐藏按钮仍响应点击
            if (!isActive) ButtonEventClean(btn);

            // WHY: 子按钮显隐后父级 LayoutGroup 必须重排：交给统一入口（等一帧 + 自下而上），别同帧直接 ForceRebuild
            RequestLayoutRebuild(btnParent);
        }

        void CreateFunctionBtns(string[] btnNames)
        {
            for (int i = 0; i < btnNames.Length; i++)
            {
                string name = btnNames[i];
                Button btn = CreateFunctionBtn(name);
                if (btn == null) continue;

                functionBtns.Add(btn);
                if (SpacingAfter.Contains(name))
                {
                    GameObject spacing = CreateSpacingObj();
                    if (spacing != null) spacingObjs.Add(spacing);
                }
            }
        }

        Button CreateFunctionBtn(string btnName)
        {
            GameObject go = InstantiateBtn(btnName);
            if (go == null) return null;

            Button btn = go.GetComponent<Button>();
            if (btn == null)
            {
                Log.Error($"[FunctionPanel] 按钮预制体 {FunctionBtnPrefabName} 上缺少 Button 组件：{btnName}");
                return null;
            }

            Action click = GetClickEvent(btnName);
            if (click != null)
            {
                btn.onClick.AddListener(() => click.Invoke());
            }
            return btn;
        }

        GameObject InstantiateBtn(string btnName)
        {
            GameObject prefab = UIPrefabUtil.Get(FunctionBtnPrefabName);
            if (prefab == null)
            {
                Log.Error($"[FunctionPanel] 找不到按钮预制体：{FunctionBtnPrefabName}");
                return null;
            }
            GameObject go = Instantiate(prefab, btnParent);
            go.name = btnName;
            SetLabelText(go.GetComponent<Button>());
            SetBtnIco(go.GetComponent<Button>(),null);
            return go;
        }

        GameObject CreateSpacingObj()
        {
            GameObject prefab = UIPrefabUtil.Get(SpacingObjPrefabName);
            if (prefab == null)
            {
                Log.Error($"[FunctionPanel] 找不到分隔预制体：{SpacingObjPrefabName}");
                return null;
            }
            GameObject go = Instantiate(prefab, btnParent);
            go.name = "SpacingObj";
            return go;
        }

        // WHY: 按钮名 -> 事件的映射统一放这里, 避免每个按钮重复写一套创建/绑定逻辑
        Action GetClickEvent(string btnName)
        {
            switch (btnName)
            {
                case "ExitBtn":          return () => OnFunctionExitClick?.Invoke();
                case "BackBtn":          return () => OnFunctionBackClick?.Invoke();
                case "SettingBtn":       return () => OnFunctionSettingClick?.Invoke();
                case "MuteBtn":          return () => OnFunctionMuteClick?.Invoke();
                case "ResourcePanelBtn": return () => OnFunctionResourcePanelClick?.Invoke();
                case "SummitBtn":        return () => OnFunctionSummitClick?.Invoke();
                case "RecordBtn":        return () => OnFunctionRecordClick?.Invoke();
                default:                 return null;
            }
        }

        #region 工具方法
        Button GetBtnByName(string btnName)
        {
            for (int i = 0; i < functionBtns.Count; i++)
            {
                if (functionBtns[i].name == btnName) return functionBtns[i];
            }
            return null;
        }

        void ButtonEventClean(Button btn)
        {
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        void SetLabelText(Button btn)
        {
            // WHY: 标签节点上也缓存不到"字段"，只能在创建时同一处把组件取出来用——TMP 形态下 TextComponent 会卸载该节点上的
            // Legacy Text，label 随后成"假 null"，静态入口 SetTextOn 会静默 no-op（按钮文字空白）。
            // 组件经节点取（而不是经 label），因为 label 可能已经"假 null"，对它调 GetComponent 会抛。
            Transform labelNode = btn.transform.GetChild(1);
            Text label = labelNode.GetComponent<Text>();
            TextComponent labelComp = label != null ? label.GetComponent<TextComponent>() : labelNode.GetComponent<TextComponent>();
            string text = ButtonLabelText(btn.gameObject.name);
            if (labelComp != null) labelComp.SetText(text);
            else if (label != null) TextComponent.SetTextOn(label, text);
        }

        void SetBtnIco(Button btn, Sprite icoSprite)
        {
            if (icoSprite != null)
            {
                var ico = btn.transform.GetChild(0).GetComponent<Image>();
                ico.sprite = icoSprite;
            }
        }

        string ButtonLabelText(string btnName)
        {
            switch (btnName)
            {
                case "ExitBtn":          return Lang.Get("ui.function.exit");
                case "BackBtn":          return Lang.Get("ui.function.back");
                case "SettingBtn":       return Lang.Get("ui.function.setting");
                case "MuteBtn":          return Lang.Get("ui.function.mute");
                case "ResourcePanelBtn": return Lang.Get("ui.function.resource");
                case "SummitBtn":        return Lang.Get("ui.function.submit");
                case "RecordBtn":        return Lang.Get("ui.function.record");
            }
            return "";
        }
        #endregion

        #region 覆盖Active方法
        public override void SetUIActive(bool isActive)
        {
            // WHY: 已是目标状态（静止或正在动画前往）就不重复触发, 否则 switch alpha 会 0-1-0 抖动
            if (isActive == m_TargetActive) return;

            m_TargetActive = isActive;
            if (ActiveAnimCoroutine != null)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }
            ActiveAnimCoroutine = StartCoroutine(OverrideAnimCoroutine(isActive));
        }

        public override void SetUIActiveImmediately(bool isActive)
        {
            m_TargetActive = isActive;
            if (ActiveAnimCoroutine != null)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }

            ActiveState(isActive);
        }

        void ActiveState(bool isActive)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive;
                canvasGroup.alpha = isActive ? 1 : 0;
            }

            if (m_SwitchCanvasGroup != null)
            {
                // WHY: switch 必须与 panel 反向: panel 显示时 switch 隐藏(alpha=0), panel 隐藏时 switch 显示(alpha=1)
                m_SwitchCanvasGroup.interactable = !isActive;
                m_SwitchCanvasGroup.blocksRaycasts = !isActive;
                m_SwitchCanvasGroup.alpha = isActive ? 0 : 1;
            }

            if (m_LayoutGroup != null)
            {
                m_LayoutGroup.spacing = isActive? layoutSpacting.x : layoutSpacting.y;
            }

        }

        IEnumerator OverrideAnimCoroutine(bool isActive)
        {
            isAnimating = true;
            float time = 0f;
            float currentLayoutAlpha = canvasGroup != null ? canvasGroup.alpha : (isActive ? 0 : 1);
            float currentSwitchAlpha = m_SwitchCanvasGroup != null ? m_SwitchCanvasGroup.alpha : (isActive ? 1 : 0);
            Vector2 currentLayoutPos = m_PanelRect.anchoredPosition;
            Vector2 currentSwitchPos = m_SwitchRect.anchoredPosition;
            float targetLayoutAlpha = isActive ? 1 : 0;
            float targetSwitchAlpha = isActive ? 0 : 1;
            float currentSpacing = m_LayoutGroup.spacing;
            float targetSpacing = isActive ? layoutSpacting.x : layoutSpacting.y;

            while (time < animTime)
            {
                time += Time.deltaTime;
                float t = time / animTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(currentLayoutAlpha, targetLayoutAlpha, t);
                if (m_SwitchCanvasGroup != null)
                    m_SwitchCanvasGroup.alpha = Mathf.Lerp(currentSwitchAlpha, targetSwitchAlpha, t);
                m_LayoutGroup.spacing = Mathf.Lerp(currentSpacing, targetSpacing, t);
                yield return null;
            }

            ActiveState(isActive);

            ActiveAnimCoroutine = null;
            isAnimating = false;
        }
        #endregion
    }
}
