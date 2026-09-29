// 由 MCV Editor/创建/UI Panel 生成器生成（2026-08-18）—— 请按需补充业务代码
using System.Collections;
using MCV_Module.Utils;
using System.Collections.Generic;
using MCV_Module.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;
using MCV_Module.UI.Components;
using MCV_Module.UI.Tools;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>StepProcessingPanel 面板</summary>
    [RequireController(typeof(StepProcessingController))]
    public class StepProcessingPanel : PanelBase
    {
        [SerializeField] Transform btnParent;
        [SerializeField] Text showText;

        /// <summary>说明文本节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"）。</summary>
        TextComponent m_ShowTextComp;
        List<Button> buttons = new List<Button>();
        List<GameObject> spacings = new List<GameObject>();
        // WHY: 按钮与分隔预制体已移出 Resources 进 UI 包，这里存裸 prefab 名（UIPrefabUtil 按 ui_{name} 拼包配置 id）。
        const string ButtonPrefabName = "StepProcessingBtn";
        const string SpacingPrefabName = "StepProcessingSpacing";    
        HorizontalLayoutGroup m_LayoutGroup;    
        bool isActiveNow = true;
        bool m_TargetActive = true;   // WHY: 当前动画/静止所朝向的目标状态，用于防重复触发
        int spacingHide = -8;
        int spacingShow = 13;
        int parentShow = 5;
        int parentHide = -30;
        Button currentBtn;

        protected override void Awake()
        {
            base.Awake();

            // WHY: 尽早解析一次（此时节点上的 Legacy Text 还在，GetComponent 稳定可用）；换形态后再解析会抛。
            if (showText != null) m_ShowTextComp = showText.GetComponent<TextComponent>();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、showText 变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if (btnParent == null || (showText == null && m_ShowTextComp == null))
            {
                Log.Error("需要手动挂载组件");
                return;
            }

            m_LayoutGroup = btnParent.GetComponent<HorizontalLayoutGroup>();

            ClearChildren(btnParent);

            buttons.Clear();
            spacings.Clear();

        }

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
            GameObject prefab = UIPrefabUtil.Get(ButtonPrefabName);
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, btnParent);
            Button btn = go.GetComponent<Button>();
            btn.name = name;
            Text text = btn.GetComponentInChildren<Text>();
            // WHY: 按钮文本来自运行时实例化的预制体、没有可缓存的字段，只能在创建时同一处把组件取出来用 —— TMP 形态下
            // TextComponent 会卸载该节点上的 Legacy Text，text 随后成"假 null"，静态入口 SetTextOn 会静默 no-op（按钮无字）。
            // 组件优先经 text 所在节点取；text 已"假 null"时退回在子级里找组件（对它调 GetComponent 会抛）。
            TextComponent textComp = text != null ? text.GetComponent<TextComponent>() : btn.GetComponentInChildren<TextComponent>();
            if (textComp != null) textComp.SetText(name);
            else if (text != null) TextComponent.SetTextOn(text, name);
            return btn;
        }

        GameObject CreateSpacing()
        {
            GameObject prefab = UIPrefabUtil.Get(SpacingPrefabName);
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
            // WHY: 已是目标状态（静止或正在动画前往），不重复触发，避免 switch alpha 出现 0-1-0 抖动
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
