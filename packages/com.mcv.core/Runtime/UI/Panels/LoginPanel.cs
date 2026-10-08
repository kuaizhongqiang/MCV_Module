
using System;
using MCV_Module.Utils;
using System.Collections.Generic;
using MCV_Module.Controllers;
using MCV_Module.Models;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    // WHY: 面板只抛 OnLoginRequested, 白名单验证与 LoginSuccessEvent 发布都在 LoginController; 游客(Unknow)只需用户名, 学生/教师必须带密码才能提交
    /// <summary>登录面板（View）：承载账号/密码输入与登录按钮，把登录请求抛给 LoginController。</summary>
    [RequireController(typeof(LoginController))]
    public class LoginPanel : PanelBase
    {
        [SerializeField] TextComponent titleText;
        [SerializeField] TextComponent userNameLabel;
        [SerializeField] TextComponent passwordLabel;
        [SerializeField] TextComponent loginButtonLabel;
        [SerializeField] TextComponent tipsTextLabel;
        [SerializeField] Button loginButton;
        [SerializeField] InputField userNameInputField;
        [SerializeField] InputField passwordInputField;
        [SerializeField] Dropdown userTypeDropdown;      // 默认Unknow 
        UserType currentType = UserType.Unknow;          // unknown 游客登录 teacher 教师登录 student 学生登录（admin 登录入口不在这里 暂不提供）
        /// <summary>下拉下标 → UserType 映射（排除 Admin，见 InitUserTypeDropdown）。</summary>
        readonly List<UserType> m_UserTypeOptions = new List<UserType>();        

        /// <summary>登录请求事件：登录按钮被点击且必填校验通过时触发，由 LoginController 订阅处理。</summary>
        public event Action<LoginPanel> OnLoginRequested;

        /// <summary>正常提示色。</summary>
        static readonly Color TipsNormalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        /// <summary>错误提示色。</summary>
        static readonly Color TipsErrorColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        /// <summary>成功提示色。</summary>
        static readonly Color TipsSuccessColor = new Color(0.3f, 0.8f, 0.4f, 1f);

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、Text 字段变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if (titleText == null || userNameLabel == null || passwordLabel == null || loginButtonLabel == null || tipsTextLabel == null
                || loginButton == null || userNameInputField == null || passwordInputField == null || userTypeDropdown == null )
            {
                Log.Error($"[LoginPanel] 缺少必要组件", this);
                return;
            }

            // 绑定登录按钮点击
            loginButton.onClick.AddListener(OnLoginButtonClick);

            // 初始化用户类型下拉，并按默认类型（Unknow/游客）应用 UI
            InitUserTypeDropdown();
            userTypeDropdown.onValueChanged.AddListener(OnUserTypeChanged);
            UpdateLoginTypeUI();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(OnLoginButtonClick);
            }
            if (userTypeDropdown != null)
            {
                userTypeDropdown.onValueChanged.RemoveListener(OnUserTypeChanged);
            }
        }
        #endregion

        #region 对外接口（供 Controller 读取/设置）
        public string UserName => userNameInputField != null ? userNameInputField.text : string.Empty;
        public string Password => passwordInputField != null ? passwordInputField.text : string.Empty;
        public UserType UserType => currentType;

        /// <summary>设置登录类型 UI（下拉/角色切换），并同步下拉显示、标题与密码栏。</summary>
        public void SetLoginTypeUI(UserType type)
        {
            currentType = type;
            if (userTypeDropdown != null)
            {
                int index = m_UserTypeOptions.IndexOf(type);
                if (index >= 0)
                {
                    userTypeDropdown.value = index;
                }
            }
            UpdateLoginTypeUI();
        }

        /// <summary>显示提示文本（默认灰色）。</summary>
        public void ShowTips(string message)
        {
            ShowTips(message, TipsNormalColor);
        }

        /// <summary>显示提示文本，可指定颜色（错误/成功等）。</summary>
        public void ShowTips(string message, Color color)
        {
            
        }

        /// <summary>显示错误提示（红色）。</summary>
        public void ShowTipsError(string message)
        {
            ShowTips(message, TipsErrorColor);
        }

        /// <summary>显示成功提示（绿色）。</summary>
        public void ShowTipsSuccess(string message)
        {
            ShowTips(message, TipsSuccessColor);
        }
        #endregion

        #region 私有方法
        void OnLoginButtonClick()
        {
            // 必填校验：游客只需用户名；学生/教师需用户名 + 密码
            if (string.IsNullOrEmpty(UserName))
            {
                ShowTipsError("请输入用户名");
                return;
            }
            if (currentType != UserType.Unknow && string.IsNullOrEmpty(Password))
            {
                ShowTipsError("请输入密码");
                return;
            }

            ShowTips("正在验证，请稍候...");
            OnLoginRequested?.Invoke(this);
        }

        void OnUserTypeChanged(int index)
        {
            // 下标经 m_UserTypeOptions 反查 UserType（下拉已排除 Admin）
            currentType = index >= 0 && index < m_UserTypeOptions.Count
                ? m_UserTypeOptions[index]
                : UserType.Unknow;
            // WHY: 切换登录类型时清空输入，避免串用上一类型的账号信息
            if (userNameInputField != null) userNameInputField.text = string.Empty;
            if (passwordInputField != null) passwordInputField.text = string.Empty;
            UpdateLoginTypeUI();
        }

        // WHY: 游客(Unknow)无需密码, 必须同时隐藏密码标签与输入框, 否则游客登录会看到空的密码栏
        /// <summary>根据当前登录类型刷新 UI：标题按类型取名，密码栏仅非游客类型显示。</summary>
        void UpdateLoginTypeUI()
        {
            titleText.SetText(GetLoginTitle(currentType));
            if (currentType == UserType.Unknow)
            {
                
            }
            else
            {
                if (currentType == UserType.Student)
                {
                    
                }
                else if (currentType == UserType.Teacher)
                {
                    
                }
            }
        }

        string GetLoginTitle(UserType type)
        {
            switch (type)
            {
                case UserType.Student: return Lang.Get("ui.login.title.student");
                case UserType.Teacher: return Lang.Get("ui.login.title.teacher");
                case UserType.Admin: return Lang.Get("ui.login.title.admin");
                default: return Lang.Get("ui.login.title.guest");
            }
        }

        // WHY: 必须跳过 Admin(登录入口不在此处), 并用 GetUserTypeDisplayName 显式给中文标签; 同时维护 m_UserTypeOptions 下标映射, 否则下拉与类型会错位。
        /// <summary>初始化用户类型下拉：按 UserType 枚举顺序填充选项（跳过 Admin），并维护下标 → UserType 映射。</summary>
        void InitUserTypeDropdown()
        {
            userTypeDropdown.options.Clear();
            m_UserTypeOptions.Clear();
            Array values = System.Enum.GetValues(typeof(UserType));
            for (int i = 0; i < values.Length; i++)
            {
                UserType type = (UserType)values.GetValue(i);
                if (type == UserType.Admin) continue; // 管理员登录入口不在这里，下拉不提供

                userTypeDropdown.options.Add(new Dropdown.OptionData(GetUserTypeDisplayName(type)));
                m_UserTypeOptions.Add(type);
            }
            userTypeDropdown.value = Math.Max(0, m_UserTypeOptions.IndexOf(currentType));
        }

        /// <summary>用户类型显示名（未知/学生/教师/管理员）。</summary>
        string GetUserTypeDisplayName(UserType type)
        {
            switch (type)
            {
                // WHY: 下拉选项标签必须在这里走 Lang —— Dropdown 只认 Legacy Text（§3 的例外节点），
                // 它们不参与 languageKey 体系，硬编码中文会让英文态的下拉与标题永远是中文。
                case UserType.Student: return Lang.Get("ui.login.type.student");
                case UserType.Teacher: return Lang.Get("ui.login.type.teacher");
                case UserType.Admin: return Lang.Get("ui.login.type.admin");
                default: return Lang.Get("ui.login.type.unknown");
            }
        }
        #endregion
    }
}
