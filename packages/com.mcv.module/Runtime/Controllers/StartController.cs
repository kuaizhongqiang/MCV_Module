using MCV_Module.Controller;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 开始控制器：编排 StartPanel 的进入登录动作。
    /// 流程：面板点击「开始」→ OnStartRequested → 发布 SceneStateChangeEventData(Login)，
    /// 由常驻的 GlobalUIMgr 监听切换 Canvas（Start → Login，进入后不可逆，状态机无返回路径）。
    /// </summary>
    public class StartController : ControllerBase<StartPanel>
    {
        protected override void OnViewBound()
        {
            // 每次绑定全新面板实例时先清后加，避免重复订阅
            View.OnStartRequested -= OnStartRequested;
            View.OnStartRequested += OnStartRequested;
        }

        void OnStartRequested(StartPanel panel)
        {
            // 进入登录界面（事件驱动，发布方只管发；监听方 GlobalUIMgr 为常驻对象）
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Login));
            Debug.Log("[StartController] 开始按钮点击，进入登录界面");
        }
    }
}
