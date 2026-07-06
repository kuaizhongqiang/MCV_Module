using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controller
{
    public class SleepTipsController : ControllerBase<SleepTipsPanel>
    {
        [SerializeField] private float _idleTimeout = 120f;
        private float _idleTimer;

        private void Update()
        {
            // 检测用户活动
            if (Input.anyKeyDown || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
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
