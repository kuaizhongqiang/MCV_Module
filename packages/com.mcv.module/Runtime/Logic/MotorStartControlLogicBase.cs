using System.Collections;
using UnityEngine;

namespace MCV_Module.Logic
{
    /// <summary>
    /// 电动机启动控制逻辑基类 —— 通用自锁控制回路 + 时间继电器（KT）延时。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 控制回路：线圈通电 = 电源可用 && !停止按钮按下 && (启动按钮按下 || 自锁保持)
    /// 时间继电器：控制回路通电后由管理器 StartCoroutine(RunTimer()) 启动计时协程，
    ///             延时到后置 TimedOut 并重算主回路（星形→三角 / 低速→高速）。
    ///             计时期间停止或失电 → 协程自动中断，KT 复位，不产生切换。
    /// </summary>
    public abstract class MotorStartControlLogicBase
    {
        /// <summary>自锁保持状态（线圈通电后置位，停止按钮按下或失电时复位）</summary>
        bool selfLocked = false;

        /// <summary>时间继电器延时时间（秒）；&lt;=0 视为立即延时到</summary>
        float delayTime = 3f;

        /// <summary>时间继电器是否正在计时</summary>
        public bool Timing { get; private set; }

        /// <summary>控制回路（接触器线圈回路）是否通电</summary>
        public bool ControlCircuitOn { get; private set; }

        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn => ControlCircuitOn;

        /// <summary>时间继电器是否已延时到（可切换到运行状态）</summary>
        public bool TimedOut { get; private set; }

        /// <summary>是否需要启动 KT 计时（控制回路通电且尚未延时到、且未在计时）</summary>
        public bool NeedsTimer => ControlCircuitOn && !TimedOut && !Timing;

        /// <summary>设置时间继电器延时时间（&lt;=0 视为立即延时到）。</summary>
        protected void SetDelayTime(float seconds)
        {
            delayTime = Mathf.Max(0f, seconds);
        }

        /// <summary>计算控制回路公共状态（自锁逻辑）并重算主回路；派生类 Compute 中调用。</summary>
        protected void ComputeControlCircuit(bool powerOn, bool sb1Pressed, bool sbPressed)
        {
            ControlCircuitOn = powerOn && !sbPressed && (sb1Pressed || selfLocked);
            selfLocked = ControlCircuitOn;
            if (!ControlCircuitOn) TimedOut = false;   // 失电/停止 → 时间继电器复位
            RecomputeMainCircuit();
        }

        /// <summary>
        /// 时间继电器计时协程：管理器在 NeedsTimer 时 StartCoroutine 启动。
        /// 逐帧检查控制回路（而非 WaitForSeconds），停止/失电即刻中断，保证"停止后重启"能重新计时；
        /// 延时到且仍通电 → 置 TimedOut 并重算主回路。
        /// </summary>
        public IEnumerator RunTimer()
        {
            if (Timing) yield break;       // 已在计时则跳过（去重）
            Timing = true;
            float remain = delayTime;
            while (remain > 0f && ControlCircuitOn)
            {
                yield return null;
                remain -= Time.deltaTime;
            }
            Timing = false;
            if (ControlCircuitOn)          // 未被停止/失电中断 → 延时到切换
            {
                TimedOut = true;
                RecomputeMainCircuit();
            }
        }

        /// <summary>复位自锁、计时与输出状态。</summary>
        public virtual void Reset()
        {
            selfLocked = false;
            Timing = false;
            TimedOut = false;
            ControlCircuitOn = false;
            RecomputeMainCircuit();
        }

        /// <summary>重算主回路接触器输出（派生类根据 TimedOut / 手动档位决定吸合状态）。</summary>
        protected abstract void RecomputeMainCircuit();
    }
}
