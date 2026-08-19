using System;
using MCV_Module.Controller;
using MCV_Module.Event;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>
    /// 对话框面板 —— 纯展示 + 交互，不持有任何业务逻辑。
    ///
    /// 结构：标题 + 文字 + 两个按钮（确认 / 取消）。
    /// 按钮显隐由请求控制：两个都显示 / 只显示确认 / 都不显示（纯提示框）。
    ///
    /// 事件流：
    ///   外部系统 -> EventBus&lt;DialogRequestEvent&gt; 发布 -> DialogController 订阅 ->
    ///   DialogController 调用 Show() -> 本面板渲染 -> 用户点击 ->
    ///   OnConfirm / OnCancel 事件 -> DialogController -> EventBus&lt;DialogResultEvent&gt; 发布
    /// </summary>
    [RequireController(typeof(DialogController))]
    public class DialogPanel : PanelBase
    {
        [Header("文本")]
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;

        [Header("按钮")]
        [SerializeField] Button confirmBtn;
        [SerializeField] Text confirmBtnText;
        [SerializeField] Button cancelBtn;
        [SerializeField] Text cancelBtnText;

        /// <summary>确认按钮点击</summary>
        public event Action OnConfirm;
        /// <summary>取消按钮点击</summary>
        public event Action OnCancel;

        /// <summary>当前对话框标题（供 Controller 回填结果事件标识）</summary>
        public string GetTitle()
        {
            return titleText != null ? titleText.text : "";
        }

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

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
        /// <summary>
        /// 显示一个对话框。由 Controller 调用，按请求渲染标题/文字并控制按钮显隐。
        /// </summary>
        public void Show(DialogRequestEvent request)
        {
            if (titleText != null) titleText.text = request.Title ?? "";
            if (contentText != null) contentText.text = request.Content ?? "";

            if (confirmBtnText != null) confirmBtnText.text = request.ConfirmLabel ?? "确认";
            if (cancelBtnText != null) cancelBtnText.text = request.CancelLabel ?? "取消";

            if (confirmBtn != null) confirmBtn.gameObject.SetActive(request.ShowConfirm);
            if (cancelBtn != null) cancelBtn.gameObject.SetActive(request.ShowCancel);

            SetUIActive(true);
        }

        /// <summary>关闭并隐藏（结果已派发后由 Controller 调用）</summary>
        public void Hide()
        {
            SetUIActive(false);
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
