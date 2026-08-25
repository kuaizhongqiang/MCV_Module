// 由 MCV/创建/UI Panel 生成器生成（2026-08-18）—— 请按需补充业务代码
using System.Collections;
using MCV_Module.Utils;
using System.Collections.Generic;
using MCV_Module.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>StepProcessingPanel 面板</summary>
    [RequireController(typeof(StepProcessingController))]
    public class StepProcessingPanel : PanelBase
    {
        [SerializeField] Transform btnParent;
        [SerializeField] Text showText;
        List<Button> buttons = new List<Button>();
        List<GameObject> spacings = new List<GameObject>();
        const string ButtonPath = "UI/StepProcessingBtn";
        const string SpacingPath = "UI/StepProcessingSpacing";    
        HorizontalLayoutGroup m_LayoutGroup;    
        bool isActiveNow = true;
        bool m_TargetActive = true;   // 当前动画/静止所朝向的目标状态，用于防重复触发
        int spacingHide = -8;
        int spacingShow = 13;
        int parentShow = 5;
        int parentHide = -30;
        Button currentBtn;

        protected override void Awake()
        {
            base.Awake();
            if (btnParent == null || showText == null)
            {
                Log.Error("需要手动挂载组件");
                return;
            }

            m_LayoutGroup = btnParent.GetComponent<HorizontalLayoutGroup>();

            ClearChildren(btnParent);

            buttons.Clear();
            spacings.Clear();

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

        public void Init(List<string> btnNames)
        {
            ClearChildren(btnParent);

            buttons.Clear();
            spacings.Clear();

            CreateButtons(btnNames);

            SetButtonCurrentState(btnNames[0]);
        }

        public string currentBtnName()
        {
            return currentBtn.name;
        }

        public void SetButtonActive(string btnName)
        {
            SetButtonCurrentState(btnName);
        }

        #region 创建按钮
        void CreateButtons(List<string> btnNames)
        {
            for (int i = 0; i < btnNames.Count; i++)
            {
                Button btn = CreateButton(btnNames[i]);
                if (btn == null) continue;
                btn.onClick.AddListener(() =>
                {
                    Log.Info("点击了按钮：" + btn.name);
                });
                buttons.Add(btn);
                if (i < btnNames.Count - 1)
                {
                    GameObject spacing = CreateSpacing();
                    if (spacing != null) spacings.Add(spacing);
                }
            }
        }
        #endregion

        #region 工具方法
        Button CreateButton(string name)
        {
            GameObject prefab = Resources.Load<GameObject>(ButtonPath);
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, btnParent);
            Button btn = go.GetComponent<Button>();
            btn.name = name;
            Text text = btn.GetComponentInChildren<Text>();
            if (text != null) text.text = name;
            return btn;
        }

        GameObject CreateSpacing()
        {
            GameObject prefab = Resources.Load<GameObject>(SpacingPath);
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, btnParent);
            return go;
        }

        void SetSpacingLayoutSpacing(GameObject obj, int spacing)
        {
            var HorizontalLayout = obj.GetComponent<HorizontalLayoutGroup>();
            if (HorizontalLayout != null)
            {
                HorizontalLayout.spacing = spacing;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(obj.GetComponent<RectTransform>());
        }

        void SetAllSpacingsLayoutSpacing(int spacing)
        {
            for (int i = 0; i < spacings.Count; i++)
            {
                SetSpacingLayoutSpacing(spacings[i], spacing);
            }
        }
        // 让激活样式唯一
        void SetButtonCurrentState(string btnName)
        {
            currentBtn = btnParent.Find(btnName).GetComponent<Button>();
            for (int i = 0; i < buttons.Count; i++)
            {
                Button btn = buttons[i];
                bool isCurrent = btn.name == btnName;
                SetButtonShow(btn, isCurrent);
            }
        }
        // 考虑样式问题
        void SetButtonShow(Button btn, bool isCurrent)
        {
            
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

            m_LayoutGroup.spacing = isActive ? parentShow : parentHide;
            int targetSpacing = isActive ? spacingShow : spacingHide;
            SetAllSpacingsLayoutSpacing(targetSpacing);
        }

        IEnumerator OverrideAnimCoroutine(bool isActive)
        {
            isAnimating = true;
            float time = 0f;
            float currentLayoutAlpha = canvasGroup != null ? canvasGroup.alpha : (isActive ? 0 : 1);
            float targetAlpha = isActive ? 1 : 0;
            float currentLayoutSpacing = m_LayoutGroup.spacing;
            int targetSpacing = isActive ? parentShow : parentHide;
            float currentSpacingSpacing = spacings[0].GetComponent<HorizontalLayoutGroup>().spacing;
            int targetSpacingSpacing = isActive ? spacingShow : spacingHide;

            while (time < animTime)
            {
                time += Time.deltaTime;
                float t = time / animTime;
                float alpha = Mathf.Lerp(currentLayoutAlpha, targetAlpha, t);
                float spacing = Mathf.Lerp(currentLayoutSpacing, targetSpacing, t);
                float spacingSpacing = Mathf.Lerp(currentSpacingSpacing, targetSpacingSpacing, t);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = alpha;
                }
                m_LayoutGroup.spacing = spacing;
                SetAllSpacingsLayoutSpacing((int)spacingSpacing);

                yield return null;
            }
            

            ActiveState(isActive);

            ActiveAnimCoroutine = null;
            isAnimating = false;
        }
        #endregion
    }
}
