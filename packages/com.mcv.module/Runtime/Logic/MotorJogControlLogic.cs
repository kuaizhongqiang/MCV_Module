namespace MCV_Module.Logic
{
    /// <summary>
    /// 点动控制逻辑类 —— 按下运转、松开停转，无自锁。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 线圈通电 = 电源可用 && !停止按钮按下 && 点动按钮按下（无自锁保持）
    /// </summary>
    public class MotorJogControlLogic
    {
        /// <summary>控制回路（接触器线圈回路）是否通电</summary>
        public bool ControlCircuitOn { get; private set; }

        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn => ControlCircuitOn;

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 ControlCircuitOn / MainCircuitOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">点动按钮是否按下（按住运转、松开停转）</param>
        /// <param name="sbPressed">停止按钮是否按下（无停止按钮时传 false）</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sbPressed)
        {
            ControlCircuitOn = powerOn && !sbPressed && sb1Pressed;
        }

        /// <summary>复位输出状态。</summary>
        public void Reset()
        {
            ControlCircuitOn = false;
        }
    }
}
