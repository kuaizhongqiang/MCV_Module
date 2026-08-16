namespace MCV_Module.Logic
{
    /// <summary>
    /// 定子串电阻降压启动控制逻辑类。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   按下启动 SB1 → 控制回路通电自锁 → 主接触器 KM1 吸合（电阻串入定子降压启动），KT 开始延时；
    ///   KT 延时到 → 短接电阻接触器 KM2 吸合（全压运行）。
    /// </summary>
    public class MotorResistanceStartControlLogic : MotorStartControlLogicBase
    {
        /// <summary>主接触器 KM1 是否吸合（全程吸合）</summary>
        public bool MainContactorOn { get; private set; }

        /// <summary>短接电阻接触器 KM2 是否吸合（延时到后短接，全压运行）</summary>
        public bool BypassOn { get; private set; }

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 MainContactorOn / BypassOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">启动按钮是否按下（SB1）</param>
        /// <param name="sbPressed">停止按钮是否按下（SB）</param>
        /// <param name="delayTime">时间继电器延时时间（秒）</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sbPressed, float delayTime)
        {
            SetDelayTime(delayTime);
            ComputeControlCircuit(powerOn, sb1Pressed, sbPressed);
        }

        protected override void RecomputeMainCircuit()
        {
            if (!ControlCircuitOn)
            {
                MainContactorOn = BypassOn = false;
                return;
            }
            MainContactorOn = true;      // 主接触器全程吸合
            BypassOn = TimedOut;          // 延时前电阻在回路（降压启动），延时后短接（全压）
        }
    }
}
