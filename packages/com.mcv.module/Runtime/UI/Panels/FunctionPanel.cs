using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class FunctionPanel : PanelBase
    {
        [SerializeField] Transform btnParent;
        [SerializeField] Button switchBtn;
        readonly List<Button> functionBtns = new List<Button>();
        readonly List<GameObject> spacingObjs = new List<GameObject>();
        // readonly Vector2 moveLimit = new Vector2(-50, 300);
        // readonly Vector2 switchMoveLimit = new Vector2(0, -350);
        readonly Vector2 layoutSpacting = new Vector2(10, -30);
        const string FunctionBtnPath = "UI/FunctionBtn";
        const string SpacingObjPath = "UI/FunctionSpacing";
        bool isActiveNow = true;
        bool m_TargetActive = true;   // 当前动画/静止所朝向的目标状态，用于防重复触发

        // 缓存引用，避免每帧 GetComponent
        HorizontalLayoutGroup m_LayoutGroup;
        CanvasGroup m_SwitchCanvasGroup;
        RectTransform m_PanelRect;
        RectTransform m_SwitchRect;

        // 这些按钮之后插入一个分隔对象
        static readonly HashSet<string> SpacingAfter = new HashSet<string> { "BackBtn", "MuteBtn" };
        static readonly string[] DefaultBtnNames =
        {
            "BackBtn", "ExitBtn", "SettingBtn", "MuteBtn",
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
                Debug.LogError("[FunctionPanel] btnParent / switchBtn 未赋值");
                return;
            }
            m_SwitchCanvasGroup = switchBtn.GetComponent<CanvasGroup>();
            m_PanelRect = GetComponent<RectTransform>();
            m_SwitchRect = switchBtn.GetComponent<RectTransform>();
            m_LayoutGroup = btnParent.GetComponent<HorizontalLayoutGroup>();

            if ( m_SwitchCanvasGroup == null)
            {
                Debug.LogError("[FunctionPanel] 缺少 CanvasGroup 组件（panel 或 switchBtn）");
            }

            // 让 switchBtn 不受 panel 自身 CanvasGroup 的 alpha/interactable 影响：
            // 否则 panel 隐藏(alpha=0) 时会把子物体 switch 也一起隐藏，导致无法点击开关重新展开
            if (m_SwitchCanvasGroup != null)
            {
                m_SwitchCanvasGroup.ignoreParentGroups = true;
            }

            functionBtns.Clear();
            spacingObjs.Clear();
            ClearChildren(btnParent);

            CreateFunctionBtns(DefaultBtnNames);

            var rect = btnParent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            m_TargetActive = isActiveNow;
            ActiveState(isActiveNow);
        }
        // 测试方法
        // void Update()
        // {
        //     if (Keyboard.current.escapeKey.wasPressedThisFrame)
        //     {
        //         SetUIActive(!isActiveNow);
        //         isActiveNow = !isActiveNow;
        //     }
        // }
        
        public void SetFunctionBtnActive(string btnName, bool isActive)
        {
            Button btn = GetBtnByName(btnName);
            if (btn == null) return;

            btn.gameObject.SetActive(isActive);
            // 隐藏时清空监听，避免隐藏按钮仍响应点击
            if (!isActive) ButtonEventClean(btn);
            var rect = btnParent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
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
                Debug.LogError($"[FunctionPanel] 按钮预制体 {FunctionBtnPath} 上缺少 Button 组件：{btnName}");
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
            GameObject prefab = Resources.Load<GameObject>(FunctionBtnPath);
            if (prefab == null)
            {
                Debug.LogError($"[FunctionPanel] 找不到按钮预制体：{FunctionBtnPath}");
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
            GameObject prefab = Resources.Load<GameObject>(SpacingObjPath);
            if (prefab == null)
            {
                Debug.LogError($"[FunctionPanel] 找不到分隔预制体：{SpacingObjPath}");
                return null;
            }
            GameObject go = Instantiate(prefab, btnParent);
            go.name = "SpacingObj";
            return go;
        }

        // 按钮名 -> 对应事件，统一在此映射，避免每个按钮重复写一套创建/绑定逻辑
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
            var label = btn.transform.GetChild(1).GetComponent<Text>();
            string text = ButtonLabelText(btn.gameObject.name);
            if (label != null) label.text = text;
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
                case "ExitBtn":          return "退出";
                case "BackBtn":          return "返回";
                case "SettingBtn":       return "设置";
                case "MuteBtn":          return "静音";
                case "ResourcePanelBtn": return "资源";
                case "SummitBtn":        return "提交";
                case "RecordBtn":        return "记录";
            }
            return "";
        }
        #endregion

        #region 覆盖Active方法
        public override void SetUIActive(bool isActive)
        {
            // 已是目标状态（静止或正在动画前往），不重复触发，避免 switch alpha 出现 0-1-0 抖动
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
                // panel 显示时 switch 隐藏(alpha=0)；panel 隐藏时 switch 显示(alpha=1)
                m_SwitchCanvasGroup.interactable = !isActive;
                m_SwitchCanvasGroup.blocksRaycasts = !isActive;
                m_SwitchCanvasGroup.alpha = isActive ? 0 : 1;
            }

            if (m_LayoutGroup != null)
            {
                m_LayoutGroup.spacing = isActive? layoutSpacting.x : layoutSpacting.y;
            }

            // float layoutTargetPosX = isActive ? moveLimit.x : moveLimit.y;
            // Vector2 currentLayoutPos = m_PanelRect.anchoredPosition;
            // m_PanelRect.anchoredPosition = new Vector2(layoutTargetPosX, currentLayoutPos.y);

            // float switchTargetPosX = isActive ? switchMoveLimit.x : switchMoveLimit.y;
            // Vector2 currentSwitchPos = m_SwitchRect.anchoredPosition;
            // m_SwitchRect.anchoredPosition = new Vector2(switchTargetPosX, currentSwitchPos.y);
        }

        IEnumerator OverrideAnimCoroutine(bool isActive)
        {
            isAnimating = true;
            float time = 0f;
            float currentLayoutAlpha = canvasGroup != null ? canvasGroup.alpha : (isActive ? 0 : 1);
            float currentSwitchAlpha = m_SwitchCanvasGroup != null ? m_SwitchCanvasGroup.alpha : (isActive ? 1 : 0);
            Vector2 currentLayoutPos = m_PanelRect.anchoredPosition;
            Vector2 currentSwitchPos = m_SwitchRect.anchoredPosition;
            // float targetLayoutX = isActive ? moveLimit.x : moveLimit.y;
            // float targetSwitchX = isActive ? switchMoveLimit.x : switchMoveLimit.y;
            float targetLayoutAlpha = isActive ? 1 : 0;
            float targetSwitchAlpha = isActive ? 0 : 1;
            // Vector2 targetLayoutPos = new Vector2(targetLayoutX, currentLayoutPos.y);
            // Vector2 targetSwitchPos = new Vector2(targetSwitchX, currentSwitchPos.y);
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
                // m_PanelRect.anchoredPosition = Vector2.Lerp(currentLayoutPos, targetLayoutPos, t);
                // m_SwitchRect.anchoredPosition = Vector2.Lerp(currentSwitchPos, targetSwitchPos, t);
                yield return null;
            }

            ActiveState(isActive);

            ActiveAnimCoroutine = null;
            isAnimating = false;
        }
        #endregion
    }
}
