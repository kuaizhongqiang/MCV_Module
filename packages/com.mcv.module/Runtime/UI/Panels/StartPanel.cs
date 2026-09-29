using System;
using MCV_Module.Utils;
using MCV_Module.Controllers;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Managers;

namespace MCV_Module.UI.Panels
{
    // WHY: Start → Login 进入后不可逆; 开始按钮只抛 OnStartRequested, 由 StartController 发布 SceneStateChangeEventData(Login)。
    /// <summary>开始面板（View）：欢迎/开始界面，点击「开始」进入登录。</summary>
    [RequireController(typeof(StartController))]
    public class StartPanel : PanelBase
    {
        [SerializeField] Button startBtn;
        [SerializeField] GameObject companyImage;

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

            companyImage.SetActive(GlobalUIMgr.IfCompany);
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
            // WHY: 开始按钮点击只抛给 StartController 处理（发布状态切换事件）
            OnStartRequested?.Invoke(this);
        }
    }
}
