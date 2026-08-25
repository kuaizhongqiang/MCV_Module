using MCV_Module.Controller;
using MCV_Module.Utils;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 登录控制器：编排 LoginPanel（View）与 GlobalDataMgr（数据）的联动。
    /// 流程：面板点击登录 → OnLoginRequested → 白名单验证（暂空→通过）→ 写用户数据 → 发布 LoginSuccessEvent。
    /// 「登录通过后的执行」暂为空：LoginSuccessEvent 暂无订阅方，后续在此做场景切换等处理。
    /// </summary>
    public class LoginController : ControllerBase<LoginPanel>
    {
        protected override void OnViewBound()
        {
            // 每次绑定全新面板实例时先清后加，避免重复订阅
            View.OnLoginRequested -= OnLoginRequested;
            View.OnLoginRequested += OnLoginRequested;
        }

        void OnLoginRequested(LoginPanel panel)
        {
            string userName = panel.UserName;
            string password = panel.Password;
            var userType = panel.UserType;

            // 白名单验证（暂空 → 直接通过）
            bool verified = GlobalDataMgr.VerifyLogin(userName, password, userType);
            if (!verified)
            {
                Log.Warning($"[LoginController] 登录验证未通过：{userName}");
                panel.ShowTipsError("账号或密码错误");
                return;
            }

            // 写入登录用户数据
            GlobalDataMgr.SetUserData(userName, password, userType);

            // 发布登录通过事件（执行暂为空，后续订阅处理）
            var user = GlobalDataMgr.Instance.UserData;
            EventBus<LoginSuccessEvent>.Publish(new LoginSuccessEvent(user));

            panel.ShowTipsSuccess("登录成功");
        }
    }
}
