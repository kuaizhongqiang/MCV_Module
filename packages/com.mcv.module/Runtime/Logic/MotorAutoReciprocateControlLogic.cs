namespace MCV_Module.Logic
{
    /// <summary>
    /// 自动往返控制逻辑类 —— 行程开关驱动正反转自动往返。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   按 SB1 启动（默认正转）→ 撞到前限位 SQ1 → 切反转 → 撞到后限位 SQ2 → 切正转 → 往复；
    ///   按下停止 SB → 全部断开。
    /// </summary>
    public class MotorAutoReciprocateControlLogic
    {
        /// <summary>正转接触器 KM1 是否吸合</summary>
        public bool ForwardOn { get; private set; }

        /// <summary>反转接触器 KM2 是否吸合</summary>
        public bool ReverseOn { get; private set; }

        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn => ForwardOn || ReverseOn;

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 ForwardOn / ReverseOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">启动按钮是否按下（SB1，默认正转启动）</param>
        /// <param name="sbPressed">停止按钮是否按下（SB）</param>
        /// <param name="sq1Pressed">前限位开关是否被撞到（→ 切反转）</param>
        /// <param name="sq2Pressed">后限位开关是否被撞到（→ 切正转）</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sbPressed, bool sq1Pressed, bool sq2Pressed)
        {
            // 自锁保持：任一方向运行即保持，SB1 首次按下启动
            bool controlOn = powerOn && !sbPressed && (sb1Pressed || ForwardOn || ReverseOn);
            if (!controlOn)
            {
                ForwardOn = ReverseOn = false;
                return;
            }

            // 行程开关优先换向；无行程信号且尚无方向时默认正转启动
            if (sq1Pressed) { ForwardOn = false; ReverseOn = true; }
            else if (sq2Pressed) { ForwardOn = true; ReverseOn = false; }
            else if (!ForwardOn && !ReverseOn) { ForwardOn = true; }
        }

        /// <summary>复位输出状态。</summary>
        public void Reset()
        {
            ForwardOn = ReverseOn = false;
        }
    }
}
