

using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.InputController;
using MCV_Module.Models;
using MCV_Module.Singleton;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.Managers
{
    /// <summary>全局输入管理器：输入控制器注册表 + 鼠标静止/移动判定（仅状态翻转时发事件）。</summary>
    public class GlobalInputMgr : SingletonGlobalMgr<GlobalInputMgr>
    {
        #region 参数
        [SerializeField, Header("静止判定窗口(秒)")] float idleJudgeTime = 0.1f;
        [SerializeField, Header("静止判定累计位移阈值(像素)")] float idleMoveThreshold = 0.5f;

        Dictionary<string, InputControllerBase> m_ControllerDict = new Dictionary<string, InputControllerBase>();

        Mouse mouse;
        float idleTimer;        // 累计位移不足阈值已持续的时间
        float moveDistance;     // 本次判定窗口内累计位移
        bool isIdle;            // 当前状态：false = 移动中
        #endregion

        protected override IEnumerator DelayInit()
        {
            yield return null;
            isInit = true;
        }

        void Update()
        {
            if (!isInit) return;

            if (mouse == null) mouse = Mouse.current;
            if (mouse == null) return;   // 无鼠标设备（纯触屏等）

            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > 0f) moveDistance += delta.magnitude;

            if (moveDistance >= idleMoveThreshold)
            {
                // 窗口内累计位移超阈值 → 移动中
                moveDistance = 0f;
                idleTimer = 0f;
                SetIdle(false);
            }
            else
            {
                // 位移不足阈值：累计静止时长，超过判定窗口即判定静止
                idleTimer += Time.unscaledDeltaTime;
                if (idleTimer >= idleJudgeTime)
                {
                    idleTimer = 0f;
                    SetIdle(true);
                }
            }
        }

        /// <summary>状态翻转时才发事件（订阅方只关心变化，不逐帧发）。</summary>
        void SetIdle(bool idle)
        {
            if (isIdle == idle) return;

            isIdle = idle;
            EventBus<MouseMoveStateEventData>.Publish(
                new MouseMoveStateEventData(idle ? MouseMoveState.Idle : MouseMoveState.Moving));
        }

        public static void RegisterController(string name, InputControllerBase controller)
        {
            if (!Instance.m_ControllerDict.ContainsKey(name))
            {
                Instance.m_ControllerDict[name] = controller;
            }
        }

        public static void UnregisterController(string name)
        {
            if (Instance.m_ControllerDict.ContainsKey(name))
            {
                Instance.m_ControllerDict.Remove(name);
            }
        }

        /// <summary>按类型获取已注册的 Controller（T 的类型名作为查找 key）。</summary>
        public static T GetController<T>() where T : InputControllerBase
        {
            string key = typeof(T).Name;
            if (Instance.m_ControllerDict.TryGetValue(key, out var controller))
                return controller as T;
            return null;
        }
    }
}
