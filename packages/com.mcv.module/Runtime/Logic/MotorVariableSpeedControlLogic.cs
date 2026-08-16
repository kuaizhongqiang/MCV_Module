namespace MCV_Module.Logic
{
    /// <summary>
    /// 三相异步电动机变速启动控制逻辑类（双速电机变极调速）。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   手动（autoSwitch=false）：SA 选低速 → 按 SB1 低速运行；SA 选高速 → 按 SB1 高速运行。
    ///   变速启动（autoSwitch=true）：按 SB1 → 低速启动，KT 延时到 → 自动切换高速运行。
    ///   互锁：低速接触器与高速接触器互斥。
    /// 两种转速（speed1/speed2）由管理器序列化字段配置并据此驱动电机，本类只输出吸合状态。
    /// </summary>
    public class MotorVariableSpeedControlLogic : MotorStartControlLogicBase
    {
        /// <summary>变速启动开关（由管理器序列化字段喂入）：true=低速启动后 KT 延时自动切高速；false=按 SA 手动选速</summary>
        bool autoSwitch = false;

        /// <summary>手动档位：是否选择高速（变速启动开启时忽略）</summary>
        bool highSpeedSelected = false;

        /// <summary>低速接触器 KM1 是否吸合（低速运行）</summary>
        public bool LowSpeedOn { get; private set; }

        /// <summary>高速接触器 KM2 是否吸合（高速运行）</summary>
        public bool HighSpeedOn { get; private set; }

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 LowSpeedOn / HighSpeedOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">启动按钮是否按下</param>
        /// <param name="sbPressed">停止按钮是否按下</param>
        /// <param name="autoSwitch">变速启动开关：true=低速启动后 KT 延时自动切高速；false=按 SA 手动选速</param>
        /// <param name="highSpeedSelected">手动档位是否选择高速（变速启动开启时忽略）</param>
        /// <param name="delayTime">时间继电器延时时间（秒）</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sbPressed,
                            bool autoSwitch, bool highSpeedSelected, float delayTime)
        {
            this.autoSwitch = autoSwitch;
            this.highSpeedSelected = highSpeedSelected;
            SetDelayTime(delayTime);
            ComputeControlCircuit(powerOn, sb1Pressed, sbPressed);
        }

        protected override void RecomputeMainCircuit()
        {
            if (!ControlCircuitOn)
            {
                LowSpeedOn = HighSpeedOn = false;
                return;
            }
            // 变速启动：KT 延时到切高速；手动：按 SA 档位选择
            bool high = autoSwitch ? TimedOut : highSpeedSelected;
            LowSpeedOn = !high;
            HighSpeedOn = high;
        }
    }
}
