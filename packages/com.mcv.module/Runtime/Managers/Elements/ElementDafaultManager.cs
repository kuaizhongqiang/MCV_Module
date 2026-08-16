using System.Collections;
using MCV_Module.Event;
using MCV_Module.Logic;
using MCV_Module.Objects.Interactives.Elements;
using UnityEngine;
using UnityEngine.Serialization;

namespace MCV_Module.Managers.Elements
{
    /// <summary>
    /// 默认元件管理器 —— 三相电动机单开单控电路（自锁接触器控制）。
    /// 元件状态变化（断路器切换、按钮按下/抬起）经 ElementStateChangeEventData 驱动电路重算：
    /// 正常：闭合QS → 按下SB1 → 线圈通电接触器吸合自锁 → 主回路通电电机转动。
    /// 分支1：按下SB → 线圈断电接触器分离 → 主回路断电电机停止。
    /// 分支2：打开QS → 线圈断电接触器分离 → 主回路断电电机停止。
    /// </summary>
    public class ElementDafaultManager : ElementManagerBase
    {
        #region 参数
        protected ElementDafaultManager() { }

        [SerializeField] ElementButtonSwitchObj sb1;   // 启动按钮（常开）
        [SerializeField, FormerlySerializedAs("sb2")] ElementButtonSwitchObj sb;   // 停止按钮（常闭）
        [SerializeField] ElementBreakerObj qs;         // 断路器
        [SerializeField] ElementMotorObj motor;        // 电机
        [SerializeField] ElementContactorObj contractor; // 接触器

        [SerializeField, Header("电机转速"), Tooltip("电机运行转速（度/秒），可经 SetMotorSpeed 从外部指定")]
        float motorSpeed = 200f;

        /// <summary>单开单控控制逻辑（一般类工具，维护自锁与回路状态）</summary>
        readonly MotorSingleControlLogic controlLogic = new MotorSingleControlLogic();
        #endregion

        #region 生命周期
        protected override IEnumerator DelayInit()
        {
            yield return base.DelayInit();
            EventBus<ElementStateChangeEventData>.Subscribe(OnElementStateChanged);
        }

        protected override void OnDestroy()
        {
            EventBus<ElementStateChangeEventData>.Unsubscribe(OnElementStateChanged);
            base.OnDestroy();
        }
        #endregion

        #region 私有方法
        /// <summary>元件状态变化：仅当本管理器为当前活动实例且是相关元件时重算电路。</summary>
        void OnElementStateChanged(ElementStateChangeEventData data)
        {
            if (Instance != this) return;
            if (data == null || (data.Element != sb1 && data.Element != sb && data.Element != qs)) return;
            RecomputeCircuit();
        }

        /// <summary>
        /// 重算电路状态：喂入元件状态给控制逻辑类（单开单控），读取回路结果并应用到元件。
        /// </summary>
        void RecomputeCircuit()
        {
            controlLogic.Compute(!qs.IsOpen, sb1.IsPressed, sb.IsPressed);

            SetContractor(controlLogic.ControlCircuitOn);
            SetMotor(controlLogic.MainCircuitOn);
        }

        void SetMotor(bool isOn)
        {
            if (isOn)
            {
                // 显式传转速启动，避免依赖动画默认转速（Stop 后可能被采集成 0）
                motor.MotorRun(motorSpeed);
            }
            else
            {
                motor.MotorStop();
            }
        }

        /// <summary>供外部（任务数据等）设置电机转速，下次启动生效。</summary>
        public void SetMotorSpeed(float speed)
        {
            motorSpeed = speed;
        }

        void SetContractor(bool isOn)
        {
            contractor.coilConnected = isOn;
        }

        void SetQS(bool isOn)
        {
            qs.IsOpen = isOn;
            RecomputeCircuit();
        }
        #endregion
    }
}
