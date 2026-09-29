using MCV_Module.Utils;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: Start → Login 单向不可逆、状态机无返回路径——发布方只管发，切换 Canvas 由常驻 GlobalUIMgr 监听执行
    /// <summary>开始控制器：编排 StartPanel 的进入登录动作（点「开始」→ 发布 SceneStateChangeEventData(Login)）。</summary>
    public class StartController : ControllerBase<StartPanel>
    {
        public override void OnViewBound()
        {
            // 每次绑定全新面板实例时先清后加，避免重复订阅
            View.OnStartRequested -= OnStartRequested;
            View.OnStartRequested += OnStartRequested;
        }

        void OnStartRequested(StartPanel panel)
        {
            // 进入登录界面（事件驱动，发布方只管发；监听方 GlobalUIMgr 为常驻对象）
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Login));
            Log.Info("[StartController] 开始按钮点击，进入登录界面");
        }
    }
}
