namespace MCV_Module.Logic
{
    /// <summary>
    /// 正反转控制逻辑类 —— 两个方向的自锁回路 + 接触器/按钮双重互锁。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   正转/反转各自自锁；正转回路串入反转按钮常闭 + 反转接触器常闭（反之亦然），
    ///   因此运行中可直接按对方启动按钮换向，且正反转互斥。
    /// 命名：SB1=正转启动（常开）、SB2=反转启动（常开）、SB=停止（常闭）。
    /// </summary>
    public class MotorForwardReverseControlLogic
    {
        /// <summary>正转自锁保持状态</summary>
        bool forwardLocked = false;

        /// <summary>反转自锁保持状态</summary>
        bool reverseLocked = false;

        /// <summary>正转接触器 KM1 是否吸合</summary>
        public bool ForwardOn { get; private set; }

        /// <summary>反转接触器 KM2 是否吸合</summary>
        public bool ReverseOn { get; private set; }

        /// <summary>控制回路（任一方向）是否通电</summary>
        public bool ControlCircuitOn => ForwardOn || ReverseOn;

        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn => ControlCircuitOn;

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 ForwardOn / ReverseOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">正转启动按钮是否按下（SB1）</param>
        /// <param name="sb2Pressed">反转启动按钮是否按下（SB2）</param>
        /// <param name="sbPressed">停止按钮是否按下（SB）</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sb2Pressed, bool sbPressed)
        {
            // 双重互锁：本方启动需对方按钮未按下 + 对方接触器未吸合
            ForwardOn = powerOn && !sbPressed && !sb2Pressed && (sb1Pressed || forwardLocked);
            ReverseOn = powerOn && !sbPressed && !sb1Pressed && (sb2Pressed || reverseLocked);

            // 接触器互锁：任一方向吸合时排除另一方向（理论上上面已保证互斥，此处兜底）
            ForwardOn = ForwardOn && !ReverseOn;
            ReverseOn = ReverseOn && !ForwardOn;

            forwardLocked = ForwardOn;
            reverseLocked = ReverseOn;
        }

        /// <summary>复位自锁与输出状态。</summary>
        public void Reset()
        {
            forwardLocked = reverseLocked = false;
            ForwardOn = ReverseOn = false;
        }
    }
}
