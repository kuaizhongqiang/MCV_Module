using MCV_Module.Utils;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 场景切换由 GlobalUIMgr 订阅 LoginSuccessEvent 处理(见 OnLoginSuccess), 本类只做验证 + 写数据 + 发事件
    /// <summary>登录控制器：编排 LoginPanel（View）与 GlobalDataMgr（数据）的联动。</summary>
    public class LoginController : ControllerBase<LoginPanel>
    {
        public override void OnViewBound()
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

            // WHY: 必须在 SetUserData 之后调用 —— 档案文件名取自用户数据; 按当前用户档案名(学号 | Anonymous)读回已有成绩接续, 并记本轮起点(供累计用时)
            GlobalDataMgr.BeginScoreSession();

            // 发布登录通过事件（场景切换由 GlobalUIMgr 订阅处理）
            var user = GlobalDataMgr.Instance.UserData;
            EventBus<LoginSuccessEvent>.Publish(new LoginSuccessEvent(user));

            panel.ShowTipsSuccess("登录成功");
        }
    }
}
