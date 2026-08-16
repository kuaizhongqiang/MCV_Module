using System.Collections;

namespace MCV_Module.Logic
{
    /// <summary>
    /// 双速电机自动变速配合制动控制逻辑类 —— 变速启动 + 能耗制动 的组合。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   按 SB1 → 低速启动（KM1），KT 延时到 → 自动切高速（KM2）；
    ///   按下停止 SB → 运行断开 → 制动接触器 KM3 吸合（能耗制动），KT2 计时到 → 释放。
    ///   复用 MotorVariableSpeedControlLogic（变速）与 MotorDynamicBrakingControlLogic（制动）。
    /// 用法：管理器在 NeedsSpeedTimer / NeedsBraking 时分别 StartCoroutine(RunSpeedTimer() / RunBrakingTimer())，
    ///       均先启动协程再应用元件。
    /// </summary>
    public class MotorVariableSpeedBrakingControlLogic
    {
        /// <summary>能耗制动 KT 制动时间（秒），Compute 喂入后供协程使用</summary>
        float brakeTime = 2f;

        readonly MotorVariableSpeedControlLogic speedLogic = new MotorVariableSpeedControlLogic();
        readonly MotorDynamicBrakingControlLogic brakeLogic = new MotorDynamicBrakingControlLogic();

        /// <summary>低速接触器 KM1 是否吸合</summary>
        public bool LowSpeedOn => speedLogic.LowSpeedOn;

        /// <summary>高速接触器 KM2 是否吸合</summary>
        public bool HighSpeedOn => speedLogic.HighSpeedOn;

        /// <summary>制动接触器 KM3 是否吸合</summary>
        public bool BrakingOn => brakeLogic.BrakingOn;

        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn => LowSpeedOn || HighSpeedOn;

        /// <summary>是否需要启动变速计时协程</summary>
        public bool NeedsSpeedTimer => speedLogic.NeedsTimer;

        /// <summary>是否需要启动制动计时协程</summary>
        public bool NeedsBraking => brakeLogic.NeedsBraking;

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 LowSpeedOn / HighSpeedOn / BrakingOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">启动按钮是否按下（SB1）</param>
        /// <param name="sbPressed">停止按钮是否按下（SB）</param>
        /// <param name="speedDelayTime">变速切换 KT 延时时间（秒）</param>
        /// <param name="brakeTime">能耗制动 KT 制动时间（秒）</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sbPressed, float speedDelayTime, float brakeTime)
        {
            this.brakeTime = brakeTime;
            speedLogic.Compute(powerOn, sb1Pressed, sbPressed, true, false, speedDelayTime);
            brakeLogic.Compute(powerOn, sb1Pressed, sbPressed);
        }

        /// <summary>变速切换计时协程（转交 MotorVariableSpeedControlLogic）。</summary>
        public IEnumerator RunSpeedTimer() => speedLogic.RunTimer();

        /// <summary>制动计时协程（转交 MotorDynamicBrakingControlLogic）。</summary>
        public IEnumerator RunBrakingTimer() => brakeLogic.RunBraking(brakeTime);

        /// <summary>复位两个内部逻辑。</summary>
        public void Reset()
        {
            speedLogic.Reset();
            brakeLogic.Reset();
        }
    }
}
