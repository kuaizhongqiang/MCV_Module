using System;
using MCV_Module.Controllers;
using MCV_Module.Event;
using MCV_Module.Models;
using UnityEngine;
using MCV_Module.UI.Components;
using MCV_Module.Utils;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    // WHY: 纯展示 + 交互, 不持有业务逻辑; 按钮显隐完全由 DialogRequestEvent 决定(都显示/只确认/都不显示), 全链路是 EventBus<DialogRequestEvent> -> DialogController -> Show -> OnConfirm/OnCancel -> EventBus<DialogResultEvent>。
    /// <summary>对话框面板 —— 纯展示 + 交互，不持有任何业务逻辑。</summary>
    [RequireController(typeof(DialogController))]
    public class DialogPanel : PanelBase
    {
        [Header("文本")]
        [SerializeField] Text contentText;

        [Header("按钮")]
        [SerializeField] Button confirmBtn;
        [SerializeField] Text confirmBtnText;
        [SerializeField] Button cancelBtn;
        [SerializeField] Text cancelBtnText;

        // WHY: 四个文本节点上的组件都要提前缓存 —— TMP 形态下组件会卸载节点上的 Legacy Text，字段随后成"假 null"，
        // 静态入口 SetTextOn / ReadRaw 会静默 no-op（读回空串），只有持有组件才读写得到。
        /// <summary>正文节点上的组件（缓存见上）。</summary>
        TextComponent m_ContentTextComp;
        /// <summary>确认按钮文本节点上的组件（缓存见上）。</summary>
        TextComponent m_ConfirmBtnTextComp;
        /// <summary>取消按钮文本节点上的组件（缓存见上）。</summary>
        TextComponent m_CancelBtnTextComp;

        /// <summary>当前对话框身份（Show 时写入，DialogController 用它给结果事件打标）。</summary>
        DialogId m_CurrentId;

        /// <summary>当前对话框身份。</summary>
        public DialogId CurrentId => m_CurrentId;

        /// <summary>确认按钮点击</summary>
        public event Action OnConfirm;
        /// <summary>取消按钮点击</summary>
        public event Action OnCancel;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            // WHY: 在调用之前解析一次（Awake 时节点上的 Legacy Text 还在，GetComponent 稳定可用）—— TMP 形态下换过形态后
            // 字段成"假 null"，那时再解析会抛，静态写入也会被静默丢弃。
            if (contentText != null) m_ContentTextComp = contentText.GetComponent<TextComponent>();
            if (confirmBtnText != null) m_ConfirmBtnTextComp = confirmBtnText.GetComponent<TextComponent>();
            if (cancelBtnText != null) m_CancelBtnTextComp = cancelBtnText.GetComponent<TextComponent>();

            if (confirmBtn != null)
                confirmBtn.onClick.AddListener(HandleConfirm);
            if (cancelBtn != null)
                cancelBtn.onClick.AddListener(HandleCancel);
        }

        protected override void OnDestroy()
        {
            if (confirmBtn != null)
                confirmBtn.onClick.RemoveListener(HandleConfirm);
            if (cancelBtn != null)
                cancelBtn.onClick.RemoveListener(HandleCancel);

            OnConfirm = null;
            OnCancel = null;
            base.OnDestroy();
        }
        #endregion

        #region 显示控制
        /// <summary>显示一个对话框：由 Controller 调用，按请求渲染标题/文字并控制按钮显隐。</summary>
        public void Show(DialogRequestEvent request)
        {
            m_CurrentId = request.Id;

            if (m_ContentTextComp != null) m_ContentTextComp.SetText(request.Content ?? "");
            else TextComponent.SetTextOn(contentText, request.Content ?? "");

            string confirmLabel = string.IsNullOrEmpty(request.ConfirmLabel) ? Lang.Get("ui.dialog.confirm") : request.ConfirmLabel;
            string cancelLabel = string.IsNullOrEmpty(request.CancelLabel) ? Lang.Get("ui.dialog.cancel") : request.CancelLabel;
            if (m_ConfirmBtnTextComp != null) m_ConfirmBtnTextComp.SetText(confirmLabel);
            else TextComponent.SetTextOn(confirmBtnText, confirmLabel);
            if (m_CancelBtnTextComp != null) m_CancelBtnTextComp.SetText(cancelLabel);
            else TextComponent.SetTextOn(cancelBtnText, cancelLabel);

            if (confirmBtn != null) confirmBtn.gameObject.SetActive(request.ShowConfirm);
            if (cancelBtn != null) cancelBtn.gameObject.SetActive(request.ShowCancel);

            SetUIActive(true);
        }

        /// <summary>关闭并隐藏（结果已派发后由 Controller 调用）</summary>
        public void Hide()
        {
            SetUIActive(false);
        }

        // WHY: 必须等收起动画播完、面板仍 active 时再回调发布结果事件, 立即隐藏会让收起协程报错
        /// <summary>关闭并隐藏，收起动画播放完成后回调 onHidden（用于先播完收起动画再发布结果事件）。</summary>
        public void Hide(Action onHidden)
        {
            SetUIActive(false, onHidden);
        }
        #endregion

        #region 交互处理
        void HandleConfirm()
        {
            OnConfirm?.Invoke();
        }

        void HandleCancel()
        {
            OnCancel?.Invoke();
        }
        #endregion
    }
}
