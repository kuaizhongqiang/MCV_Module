using System;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>步骤 UI 说明面板（View）—— 只做展示与按键上报，不做任何决策；分页内容与流程全在控制器。</summary>
    [RequireController(typeof(MCV_Module.Controllers.StepUIController))]
    public class StepUIPanel : PanelBase
    {
        #region 序列化参数
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;
        [SerializeField] Button confirmBtn;
        [SerializeField] Button nextBtn;
        [SerializeField] Button prevBtn;
        #endregion

        // WHY: 标题/正文节点上的组件要提前缓存 —— TMP 形态下组件会卸载节点上的 Legacy Text，字段随后成"假 null"，
        // 静态入口 SetTextOn 会静默 no-op（换页后标题与正文不更新）。
        /// <summary>标题节点上的组件（缓存见上）。</summary>
        TextComponent m_TitleTextComp;
        /// <summary>正文节点上的组件（缓存见上）。</summary>
        TextComponent m_ContentTextComp;

        #region 事件
        /// <summary>点「上一条」</summary>
        public event Action OnPrevClick;
        /// <summary>点「下一条」</summary>
        public event Action OnNextClick;
        /// <summary>点「确认」（控制器据此收尾并通知步骤条件）</summary>
        public event Action OnConfirmClick;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();   // WHY: canvasGroup 在 UIBase.Awake 里初始化，先调它，别把判空的 return 写在前面

            // WHY: 判空 return 之前先把组件解析出来，否则引用缺失时提前返回就再也缓存不到（换形态后字段成"假 null"）。
            if (titleText != null) m_TitleTextComp = titleText.GetComponent<TextComponent>();
            if (contentText != null) m_ContentTextComp = contentText.GetComponent<TextComponent>();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、Text 字段变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if ((titleText == null && m_TitleTextComp == null) || (contentText == null && m_ContentTextComp == null)
                || confirmBtn == null || nextBtn == null || prevBtn == null)
            {
                Log.Error("[StepUIPanel] 需要手动挂载组件");
                return;
            }
            confirmBtn.onClick.AddListener(ConfirmClick);
            nextBtn.onClick.AddListener(NextClick);
            prevBtn.onClick.AddListener(PrevClick);
        }

        protected override void OnDestroy()
        {
            // WHY: 布局重建协程由 PanelBase.OnDestroy 统一收尾，这里只管自己的按钮监听
            if (confirmBtn != null) confirmBtn.onClick.RemoveListener(ConfirmClick);
            if (nextBtn != null) nextBtn.onClick.RemoveListener(NextClick);
            if (prevBtn != null) prevBtn.onClick.RemoveListener(PrevClick);
            base.OnDestroy();
        }
        #endregion

        #region 对外接口（只由控制器调用）
        /// <summary>展示一页：写标题/正文，并按「第几页 + 共几页」切翻页按钮（只有 1 页时翻页按钮整体隐藏）。</summary>
        public void ShowPage(string title, string content, int pageIndex, int pageCount)
        {
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title ?? string.Empty);
            else TextComponent.SetTextOn(titleText, title ?? string.Empty);
            if (m_ContentTextComp != null) m_ContentTextComp.SetText(content ?? string.Empty);
            else TextComponent.SetTextOn(contentText, content ?? string.Empty);

            bool showPager = pageCount > 1;
            SetPagerVisible(showPager);
            if (showPager)
            {
                if (prevBtn != null) prevBtn.interactable = pageIndex > 0;
                if (nextBtn != null) nextBtn.interactable = pageIndex < pageCount - 1;
            }

            // WHY: 标题/正文挂在 ContentSizeFitter 上，翻页按钮还会整体显隐 —— 不刷布局父级会按旧尺寸排版。
            //      统一走 RequestLayoutRebuild（等一帧 + 按深度自下而上 + 防重入），别在这里同帧直接 ForceRebuild。
            RequestLayoutRebuild();
        }
        #endregion

        #region 私有方法（纯表现）
        void SetPagerVisible(bool visible)
        {
            if (prevBtn != null) prevBtn.gameObject.SetActive(visible);
            if (nextBtn != null) nextBtn.gameObject.SetActive(visible);
        }

        void ConfirmClick() => OnConfirmClick?.Invoke();
        void NextClick() => OnNextClick?.Invoke();
        void PrevClick() => OnPrevClick?.Invoke();
        #endregion
    }
}
