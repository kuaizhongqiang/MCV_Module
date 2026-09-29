using System;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models.Project;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 面板按需创建且随 Canvas 重建被清理，首次创建时 Start（→ View 绑定）还没跑，故本类不依赖 View 属性、直接用取到的面板实例；找不到说明 / 取不到面板时必须告警并直接抛"已关闭"，否则把步骤流程卡死
    /// <summary>步骤 UI 说明调度（C）—— 实现 IStepUiPanel 契约，供步骤条件 ConditionUI 经 GlobalControllerMgr.Find("StepUIController") 调用：按 id 取多态条目里的 StepUiData，翻页并回抛 OnPanelClosed。</summary>
    public class StepUIController : ControllerBase<StepUIPanel>, IStepUiPanel
    {
        StepUiData currentData;   // 当前说明（业务状态放控制器，不放面板）
        int currentPage;          // 当前第几页

        /// <summary>用户点了「确认」：步骤条件订阅此事件后结束 Waiting（<see cref="IStepUiPanel"/> 契约）</summary>
        public event Action OnPanelClosed;

        #region 绑定
        public override void OnViewBound()
        {
            if (View == null) return;
            Subscribe(View);
        }

        public override void OnDispose()
        {
            if (View != null) Unsubscribe(View);
            base.OnDispose();
        }
        #endregion

        #region IStepUiPanel（只由 ConditionUI 调用）
        /// <summary>按说明 id 取出内容并显示面板（页码复位到第 1 页）。</summary>
        public void ShowData(string uiId)
        {
            currentData = FindInfo(uiId);
            if (currentData == null || currentData.pages == null || currentData.pages.Count == 0)
            {
                Log.Warning($"[StepUIController] 找不到 UI 说明 id={uiId}（检查 StreamingAssets/Data/StepContentData.json 里 contentType=UI 的条目），跳过 UI 步骤");
                OnPanelClosed?.Invoke();
                return;
            }

            var panel = ResolvePanel(true);
            if (panel == null)
            {
                Log.Warning("[StepUIController] 当前激活 Canvas 上取不到 StepUIPanel，跳过 UI 步骤");
                OnPanelClosed?.Invoke();
                return;
            }

            Subscribe(panel);
            currentPage = 0;
            // WHY: 必须先激活再填内容——ShowPage 末尾重建布局（协程），面板未激活时那段会直接跳过，复用时文本变了尺寸却不刷新
            panel.SetUIActive(true);
            ShowCurrentPage(panel);
        }

        /// <summary>关闭说明面板（步骤条件在 Prepare / Complete / 跳转归位时调用）。</summary>
        public void ClosePanel()
        {
            currentData = null;
            currentPage = 0;

            // 收起路径绝不新建面板（FindPanel）：新建实例的 Awake 会先亮一帧，又是一次闪
            var panel = ResolvePanel(false);
            if (panel == null) return;

            Unsubscribe(panel);

            // WHY: 失活判断不能省——本控制器协程已挪到 GlobalControllerMgr，但 SetUIActive(false) 仍会驱动面板自身协程；这里被步骤条件（ConditionStart / UI / Finish 的 OnPrepare）调用，异常会打断 Prepare 协程 → 整条步骤链卡死
            if (!panel.gameObject.activeInHierarchy) return;

            panel.SetUIActive(false);
        }
        #endregion

        #region 翻页 / 收尾
        /// <summary>把当前页写给面板（页码越界自动夹取，面板据此决定翻页按钮的显隐与可用）。</summary>
        void ShowCurrentPage(StepUIPanel panel)
        {
            int count = currentData.pages.Count;
            currentPage = Mathf.Clamp(currentPage, 0, count - 1);
            panel.ShowPage(Localized.Pick(currentData.title, currentData.titleEn), Localized.Pick(currentData.pages, currentData.pagesEn, currentPage), currentPage, count);
        }

        void OnPrevClicked()
        {
            if (currentData == null || currentPage <= 0) return;

            currentPage--;
            var panel = ResolvePanel(false);
            if (panel != null) ShowCurrentPage(panel);
        }

        void OnNextClicked()
        {
            if (currentData == null) return;
            if (currentPage >= currentData.pages.Count - 1) return;

            currentPage++;
            var panel = ResolvePanel(false);
            if (panel != null) ShowCurrentPage(panel);
        }

        /// <summary>点「确认」= 完成本步骤：抛给步骤条件，条件随后走 Complete → ClosePanel 收面板。</summary>
        void OnConfirmClicked() => OnPanelClosed?.Invoke();
        #endregion

        #region 订阅 / 数据 / 面板
        void Subscribe(StepUIPanel panel)
        {
            // 先清后加（幂等）：同一面板实例上只保留一份回调
            panel.OnPrevClick -= OnPrevClicked;
            panel.OnNextClick -= OnNextClicked;
            panel.OnConfirmClick -= OnConfirmClicked;
            panel.OnPrevClick += OnPrevClicked;
            panel.OnNextClick += OnNextClicked;
            panel.OnConfirmClick += OnConfirmClicked;
        }

        void Unsubscribe(StepUIPanel panel)
        {
            panel.OnPrevClick -= OnPrevClicked;
            panel.OnNextClick -= OnNextClicked;
            panel.OnConfirmClick -= OnConfirmClicked;
        }

        /// <summary>按 id 取 UI 说明条目（数据源 StepContentData.json；容器多态，这里只挑 StepUiData，取不到返回 null）。</summary>
        static StepUiData FindInfo(string uiId)
        {
            if (string.IsNullOrEmpty(uiId)) return null;
            // 退出阶段 / 管理器未就绪时不触发创建
            if (!GlobalDataMgr.Exists || GlobalDataMgr.Instance == null) return null;

            var data = GlobalDataMgr.Instance.StepContentData;
            if (data == null || data.contents == null) return null;

            for (int i = 0; i < data.contents.Count; i++)
            {
                if (data.contents[i] is StepUiData ui && ui.id == uiId) return ui;
            }
            return null;
        }

        // WHY: 收起路径必须传 false（走 FindPanel）——否则会"先建一个再立刻关掉"
        /// <summary>取当前激活 Canvas 上的说明面板；createIfMissing = true 时不存在就创建。</summary>
        static StepUIPanel ResolvePanel(bool createIfMissing)
        {
            var canvas = GlobalUIMgr.GetActiveCanvas();
            if (canvas == null) return null;

            return createIfMissing ? canvas.GetPanel<StepUIPanel>() : canvas.FindPanel<StepUIPanel>();
        }
        #endregion
    }
}
