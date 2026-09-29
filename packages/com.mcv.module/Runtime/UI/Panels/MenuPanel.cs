using System;
using System.Collections.Generic;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{    
    public class MenuPanel : PanelBase
    {
        #region 序列化参数
        [SerializeField] Transform btnsParent;
        [SerializeField,Tooltip("用来对应json，也用来实现对应menuBtn是否显示")] string[] btnIds = new string[9] 
        {
            "menu_contactor",
            "menu_thermalRelay",
            "menu_timeRelay",
            "menu_masterAppliance",
            "menu_fuse",
            "menu_breaker",
            "menu_speedRelay",
            "menu_combinationSwitch",
            "menu_quiz"
        };
        [SerializeField] Button roamingBtn;
        [SerializeField] Button quitBtn;
        [SerializeField] Button resultBtn;
        [SerializeField] GameObject companyImage;
        [SerializeField] GameObject copyrightText;
        [SerializeField] List<Image> breathImages = new List<Image>();
        MenuClip currentClip;
        List<Button> menuBtns = new List<Button>();
        /// <summary>与 menuBtns 一一对应的菜单数据（显隐/选中/点击都按同一份下标取，避免错位）</summary>
        readonly List<MenuClip> menuClips = new List<MenuClip>();
        readonly string animToggleName = "active";
        Animator anim;
        /// <summary>当前是否显示：本面板走 Animator 显隐、GameObject 恒激活，显隐只能由本字段跟踪。</summary>
        bool m_IsShowing;
        #endregion

        #region 公开事件
        public event Action OnRoamingBtnClick;
        public event Action OnQuitBtnClick;
        public event Action OnResultBtnClick;
        public event Action<MenuClip> OnMenuBtnClick;
        #endregion
        #region 生命周期
        protected override void Awake()
        {
            SetBtnsActive();
            BindBtns();
            base.Awake();
            anim = GetComponent<Animator>();
            // WHY: Animator 的 Bool 参数默认 false（实例化即进 Hide 态），以它为准初始化，避免"已显示却记为隐藏"
            m_IsShowing = anim != null && anim.GetBool(animToggleName);
        }

        protected override void OnDestroy()
        {
            
            base.OnDestroy();
        }
        #endregion

        #region 公开方法
        /// <summary>是否处于显示状态（Animator 的 "active" 参数）：面板走 Animator 显隐、GameObject 恒激活，调用方须用它而非 activeSelf 判断。</summary>
        public bool IsShowing => m_IsShowing;

        /// <summary>由 Controller 装配：设置当前层级列表并居中定位到 selectedIndex（每次层级切换/面板重建后调用）。</summary>
        public void Init()
        {
           
        }

        public void SetCopyright(bool ifCopyright,bool ifCompany)
        {
            companyImage.SetActive(ifCompany);
            copyrightText.SetActive(ifCopyright);
        }
        #endregion

        #region 事件方法
        
        #endregion

        #region 覆盖方法
        public override void SetUIActive(bool isActive, Action onHidden)
        {
            // WHY: 本类只用 Animator 控制显隐，不 SetActive、不跑基类淡入淡出，GameObject 恒为激活态；
            //      显隐状态必须自己记，否则调用方读 gameObject.activeSelf 恒为 true，弹层只能收起一次
            m_IsShowing = isActive;
            if (anim == null) return;
            anim.SetBool(animToggleName, isActive);

            if (isActive)
            {
                StartCoroutine(BreathLightenAnim(breathImages,2f));
            }
        }
        #endregion

        #region 私有方法
        // WHY: 面板每次重建都是全新实例, 绑定随实例销毁, 无需退订。
        /// <summary>绑定入口按钮的点击 → 面板事件（业务由 MenuController 订阅处理）。</summary>
        void BindBtns()
        {
            if (roamingBtn != null) roamingBtn.onClick.AddListener(() => OnRoamingBtnClick?.Invoke());
            if (quitBtn != null)    quitBtn.onClick.AddListener(() => OnQuitBtnClick?.Invoke());
            if (resultBtn != null)  resultBtn.onClick.AddListener(() => OnResultBtnClick?.Invoke());
        }

        // WHY: 可以通过移除对应 json 数据来控制软件运行后是否显示子菜单按钮
        // TOOD: 实现复杂 需要优化
        void SetBtnsActive()
        {
            // WHY: 数据未就绪时直接返回, 本方法在 Awake 最早期调用, 不能假设 json 已加载
            var menuData = GlobalDataMgr.Exists ? GlobalDataMgr.GetMenuData() : null;
            if (menuData == null || menuData.clips == null)
            {
                Log.Warning("[MenuPanel] 菜单数据未就绪，跳过器件按钮显隐装配");
                return;
            }

            menuBtns.Clear();
            menuClips.Clear();

            for (int i = 0; i < btnIds.Length; i++)
            {
                if (btnsParent == null || i >= btnsParent.childCount) break;

                // WHY: 菜单数据里没有该 id → 该按钮不显示（json 即显隐开关）
                MenuClip clip = menuData.GetClip(btnIds[i]);
                if (clip == null)
                {
                    btnsParent.GetChild(i).gameObject.SetActive(false);
                    continue;
                }

                Button btn = btnsParent.GetChild(i).GetComponent<Button>();
                if (btn == null)
                {
                    Log.Error($"[MenuPanel] {btnsParent.GetChild(i).name} 缺少 Button 组件，点击不会生效");
                    continue;
                }

                menuBtns.Add(btn);
                menuClips.Add(clip);

                // WHY: 循环变量不能在闭包里直接用, 需捕获当下下标（与 menuClips 同序）
                int index = menuBtns.Count - 1;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnMenuBtnClicked(index));
            }

            // WHY: roamingBtn 常驻 MenuPanel、语义固定为「进入漫游」; 从漫游弹层回内容页走器件按钮(MenuController.OnMenuBtnClick 会顺带卸载房间场景)。
        }

        /// <summary>器件按钮点击：刷新选中态并向上抛事件（业务由 MenuController 订阅处理）。</summary>
        void OnMenuBtnClicked(int index)
        {
            if (index < 0 || index >= menuClips.Count) return;

            MenuClip clip = menuClips[index];
            Log.Info($"[MenuPanel] 点击菜单入口：{clip.id}（{clip.displayName}）");
            SetBtnsSelected(clip);
            OnMenuBtnClick?.Invoke(clip);
        }

        // WHY: 对按钮切换每次只能有一个是选中的的
        void SetBtnsSelected(MenuClip clip)
        {
            if (clip == null) return;

            currentClip = clip;
            for (int i = 0; i < menuBtns.Count; i++)
            {
                SetBtnSelected(menuBtns[i], menuClips[i] == clip);
            }
        }
        // 具体的按钮样式方法
        void SetBtnSelected(Button btn,bool isSelected)
        {
            btn.transform.GetChild(1).GetComponent<Image>().color = isSelected ? Color.white : Color.clear;
        }
        #endregion
    }
}
