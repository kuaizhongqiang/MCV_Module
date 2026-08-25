using System;
using MCV_Module.Utils;
using MCV_Module.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>
    /// 开始面板（View）：欢迎/开始界面，点击「开始」进入登录。
    /// 与 StartController 联动：开始按钮 → OnStartRequested → Controller 发布
    /// SceneStateChangeEventData(Login) 切换状态（Start → Login，进入后不可逆）。
    /// </summary>
    [RequireController(typeof(StartController))]
    public class StartPanel : PanelBase
    {
        [SerializeField] Button startBtn;

        /// <summary>开始请求事件：点击「开始」时触发，由 StartController 订阅处理（进入登录）。</summary>
        public event Action<StartPanel> OnStartRequested;

        protected override void Awake()
        {
            base.Awake();

            if (startBtn != null)
            {
                startBtn.onClick.AddListener(HandleStart);
            }
            else
            {
                Log.Error("[StartPanel] startBtn 未赋值", this);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (startBtn != null)
            {
                startBtn.onClick.RemoveListener(HandleStart);
            }
        }

        void HandleStart()
        {
            // 开始按钮点击：抛给 StartController 处理（发布状态切换事件）
            OnStartRequested?.Invoke(this);
        }
    }
}
