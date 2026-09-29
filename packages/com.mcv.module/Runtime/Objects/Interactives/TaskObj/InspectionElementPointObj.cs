using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 高亮两路任一成立就亮、都撤才熄灭——表笔只有拖拽中扫过才算，松手后即使还贴着点也要撤掉
    /// <summary>待检测点（表笔要碰的目标）：管"我是哪个元件的哪个点" + 悬停/表笔接触两路高亮</summary>
    public class InspectionElementPointObj : InteractiveBase
    {
        [SerializeField] ElementType element = ElementType.None;
        [SerializeField] ElementPointNameType point = ElementPointNameType.None;
        string pointName;

        /// <summary>鼠标悬停中（来源①）</summary>
        bool m_HoverHighlight;
        /// <summary>表笔拖拽中扫过（来源②）</summary>
        bool m_ContactHighlight;
        /// <summary>当前实际下发的高亮态（合并两路来源，只在变化时真正调高亮服务）</summary>
        bool m_HighlightOn;

        protected override void Awake()
        {
            base.Awake();  
            HandleHighlightSetting();
            HighlightInit(gameObject);
            // WHY: 点名只在 Awake 取一次——之后改 gameObject.name 不会同步，名字要在进检测任务前定好
            pointName = $"{ChnNameMap.Get(element)}_{gameObject.name}";
        }

        protected override void MoEnterEvent()
        {
            SetHighlightSource(ref m_HoverHighlight, true);
        }

        protected override void MoExitEvent()
        {
            SetHighlightSource(ref m_HoverHighlight, false);
        }

        /// <summary>表笔拖拽中碰到本点的高亮（InspectionProbeObj 每帧同步）；松手后传 false 撤掉，静止贴着不亮</summary>
        public void SetContactHighlight(bool on)
        {
            SetHighlightSource(ref m_ContactHighlight, on);
        }

        /// <summary>改一路高亮来源并重算最终状态（两路都撤了才熄灭）。</summary>
        void SetHighlightSource(ref bool source, bool on)
        {
            if (source == on) return;

            source = on;
            RefreshHighlight();
        }

        /// <summary>按两路来源的合并结果下发高亮（状态没变就不重复调服务）。</summary>
        void RefreshHighlight()
        {
            bool on = m_HoverHighlight || m_ContactHighlight;
            if (on == m_HighlightOn) return;

            m_HighlightOn = on;
            Highlight(on);
        }

        // WHY: 检测点常藏机柜/零件内部——高亮必须穿透遮挡，且须在 HighlightInit 前登记偏好才同批生效
        /// <summary>设置检测点高亮穿透遮挡；走 IHighlightService 契约，未注入则静默降级</summary>
        void HandleHighlightSetting()
        {
            IHighlightService.Instance?.SetHighlightOnTop(gameObject, true);
        }

        /// <summary>点名（显示用）：元件中文名_检测点物体名；表笔接触事件的订阅方读它</summary>
        public string GetPointName()
        {
            return pointName;
        }
    }
}
