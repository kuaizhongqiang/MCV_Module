// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-16）—— 请按需补充业务代码
using System;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    // WHY: 只抛事件不做决策——返回目标由 ContentFunctionController 处理; 项目名必须由 Controller 在绑定后经 Init 注入, 面板不读数据源。
    // WHY: 不再有 AI 入口——AI 面板由它自己的开关按钮(AiSwitchBtn → SetPanelActive)开合, 功能栏不管面板显隐
    /// <summary>内容页功能面板：项目名 / 版权条 + 「返回」入口。</summary>
    [RequireController(typeof(MCV_Module.Controllers.ContentFunctionController))]
    public class ContentFunctionPanel : PanelBase
    {
        [SerializeField] Text projectNameText;
        [SerializeField] GameObject companyText;
        [SerializeField] GameObject copyrightText;
        [SerializeField] Button backBtn;

        /// <summary>项目名节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"，静态入口静默 no-op）。</summary>
        TextComponent m_ProjectNameTextComp;

        /// <summary>返回入口点击（回菜单页）</summary>
        public event Action OnBackBtnClick;

        protected override void Awake()
        {
            // WHY: 先走基类：canvasGroup 必须就绪，否则后续 SetUIActive 会空引用
            base.Awake();

            // WHY: 尽早解析并缓存 —— TMP 形态下本组件会卸载节点上的 Legacy Text，之后 projectNameText 成了"假 null"，
            // 那时再 GetComponent 会抛、静态写入也会被静默丢弃；持有组件后赋值时机随意（未装配完会先进 pending 缓冲）。
            if (projectNameText != null) m_ProjectNameTextComp = projectNameText.GetComponent<TextComponent>();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、Text 字段变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if ((projectNameText == null && m_ProjectNameTextComp == null) || companyText == null || copyrightText == null || backBtn == null)
            {
                Log.Error("[ContentFunctionPanel] 需要手动挂载组件", this);
                return;
            }

            // WHY: 面板每次重建都是全新实例，监听随实例销毁，无需退订
            backBtn.onClick.AddListener(() => OnBackBtnClick?.Invoke());
        }

        /// <summary>装配面板：写入当前项目名（数据源 ProjectClip.displayName，由 Controller 取）。</summary>
        public void Init(string projectName)
        {
            if (m_ProjectNameTextComp != null) m_ProjectNameTextComp.SetText(projectName ?? string.Empty);
            else if (projectNameText != null) TextComponent.SetTextOn(projectNameText, projectName ?? string.Empty);
        }

        public void SetCopyright(bool ifCopyright,bool ifCompany)
        {
            companyText.SetActive(ifCompany);
            copyrightText.SetActive(ifCopyright);
        }
    }
}
