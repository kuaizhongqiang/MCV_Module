using MCV_Module.UI.Panels;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.Controller
{
    public class SleepTipsController : ControllerBase<SleepTipsPanel>
    {
        [SerializeField] private float _idleTimeout = 120f;
        private float _idleTimer;
        private Mouse _mouse;
        private Keyboard _keyboard;

        protected override void OnViewBound()
        {
            _mouse = Mouse.current;
            _keyboard = Keyboard.current;
        }

        private void Update()
        {
            if (_keyboard == null || _mouse == null) return;

            // 检测用户活动（新 InputSystem）
            if (_keyboard.anyKey.wasPressedThisFrame ||
                _mouse.delta.ReadValue().sqrMagnitude > 0.01f)
            {
                _idleTimer = 0f;
                return;
            }

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _idleTimeout)
            {
                _idleTimer = 0f;
                View.ShowSleepTip("检测到长时间未操作，系统将进入待机状态...");
            }
        }
    }
}
