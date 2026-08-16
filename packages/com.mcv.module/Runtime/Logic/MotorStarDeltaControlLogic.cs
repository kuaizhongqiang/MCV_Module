namespace MCV_Module.Logic
{
    /// <summary>
    /// 三相异步电动机星三角（Y-Δ）降压启动控制逻辑类。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   按下启动 SB1 → 控制回路通电自锁 → 主接触器 KM1 + 星形接触器 KM2 吸合（降压启动），KT 开始延时；
    ///   KT 延时到 → 星形接触器 KM2 断开、三角接触器 KM3 吸合（全压运行）。
    ///   互锁：星形接触器与三角接触器互斥，电机运行期间 KM1 始终吸合。
    /// </summary>
    public class MotorStarDeltaControlLogic : MotorStartControlLogicBase
    {
        /// <summary>主接触器 KM1 是否吸合（运行期间始终吸合）</summary>
        public bool MainContactorOn { get; private set; }

        /// <summary>星形接触器 KM2 是否吸合（启动降压阶段）</summary>
        public bool StarOn { get; private set; }

        /// <summary>三角接触器 KM3 是否吸合（全压运行阶段）</summary>
        public bool DeltaOn { get; private set; }

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 MainContactorOn / StarOn / DeltaOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">启动按钮是否按下</param>
        /// <param name="sbPressed">停止按钮是否按下</param>
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
                MainContactorOn = StarOn = DeltaOn = false;
                return;
            }
            MainContactorOn = true;      // 主接触器运行期间始终吸合
            StarOn = !TimedOut;          // 星形：延时前（降压启动）
            DeltaOn = TimedOut;          // 三角：延时到（全压运行）
        }
    }
}
