namespace MCV_Module.Logic
{
    /// <summary>
    /// 延边三角形降压启动控制逻辑类。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   按下启动 SB1 → 控制回路通电自锁 → 启动接触器 KM1 吸合（延边三角形接法降压启动），KT 开始延时；
    ///   KT 延时到 → 启动接触器断开、运行接触器 KM2 吸合（全三角接法全压运行）。
    ///   互锁：启动接触器与运行接触器互斥。
    /// </summary>
    public class MotorExtendedDeltaControlLogic : MotorStartControlLogicBase
    {
        /// <summary>启动接触器 KM1 是否吸合（延边三角形，降压启动）</summary>
        public bool StartOn { get; private set; }

        /// <summary>运行接触器 KM2 是否吸合（全三角形，全压运行）</summary>
        public bool RunOn { get; private set; }

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 StartOn / RunOn）。
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
                StartOn = RunOn = false;
                return;
            }
            StartOn = !TimedOut;   // 延边三角：延时前吸合（降压启动）
            RunOn = TimedOut;      // 全三角：延时到吸合（全压）
        }
    }
}
