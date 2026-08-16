namespace MCV_Module.Logic
{
    /// <summary>
    /// 反接制动控制逻辑类 —— 停止时反接电源制动（单向或双向），速度继电器 KS 判定制动结束。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   正常运行：SB1/SB2 正反转自锁；按下停止 → 运行接触器断开 → 反接制动接触器吸合（电源反接减速），
    ///   速度继电器 KS 在转速近零时断开 → 制动接触器释放（KS 由管理器按电机转速喂入，无需协程计时）。
    ///   双向（twoDirection=true）：制动方向取原运行方向的相反方向。
    /// 命名：SB1=正转启动、SB2=反转启动、SB=停止。
    /// </summary>
    public class MotorPluggingControlLogic
    {
        /// <summary>电源状态</summary>
        bool powerOn = false;

        /// <summary>速度继电器状态（true=转速高于阈值，电机仍在转）</summary>
        bool ksClosed = false;

        /// <summary>制动请求锁存（停止边沿置位，KS 断开/重启/失电复位）</summary>
        bool brakeLatch = false;

        /// <summary>正转自锁保持状态</summary>
        bool forwardLocked = false;

        /// <summary>反转自锁保持状态</summary>
        bool reverseLocked = false;

        /// <summary>正转运行接触器 KM1 是否吸合</summary>
        public bool ForwardRunOn { get; private set; }

        /// <summary>反转运行接触器 KM2 是否吸合</summary>
        public bool ReverseRunOn { get; private set; }

        /// <summary>反接制动是否进行中（制动接触器吸合）</summary>
        public bool BrakingOn { get; private set; }

        /// <summary>制动是否作用于反转接触器（原为正转 → 反接反转制动；单向恒为 true）</summary>
        public bool BrakingReverse { get; private set; }

        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn => ForwardRunOn || ReverseRunOn;

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 ForwardRunOn / ReverseRunOn / BrakingOn / BrakingReverse）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">正转启动按钮是否按下（SB1）</param>
        /// <param name="sb2Pressed">反转启动按钮是否按下（SB2）</param>
        /// <param name="sbPressed">停止按钮是否按下（SB）</param>
        /// <param name="ksClosed">速度继电器是否闭合（转速高于阈值，反接制动进行中电机未停）</param>
        /// <param name="twoDirection">是否双向（可逆反接制动）；false 为单向</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sb2Pressed, bool sbPressed, bool ksClosed, bool twoDirection)
        {
            this.powerOn = powerOn;
            this.ksClosed = ksClosed;

            bool wasRunning = ForwardRunOn || ReverseRunOn;
            bool wasForward = ForwardRunOn;

            // 正反转运行回路（互锁）
            ForwardRunOn = powerOn && !sbPressed && !sb2Pressed && (sb1Pressed || forwardLocked);
            ReverseRunOn = powerOn && !sbPressed && !sb1Pressed && (sb2Pressed || reverseLocked);
            ForwardRunOn = ForwardRunOn && !ReverseRunOn;
            ReverseRunOn = ReverseRunOn && !ForwardRunOn;
            forwardLocked = ForwardRunOn;
            reverseLocked = ReverseRunOn;

            // 停止边沿锁存制动请求，并记录原方向以决定反接方向
            if (wasRunning && !(ForwardRunOn || ReverseRunOn) && powerOn)
            {
                brakeLatch = true;
                BrakingReverse = twoDirection ? wasForward : true;
            }
            // 解除：重新启动 / 失电 / 转速降到近零（KS 断开）
            if (ForwardRunOn || ReverseRunOn || !powerOn || !ksClosed) brakeLatch = false;

            BrakingOn = powerOn && brakeLatch && ksClosed;
        }

        /// <summary>复位自锁、制动与输出状态。</summary>
        public void Reset()
        {
            powerOn = ksClosed = brakeLatch = forwardLocked = reverseLocked = false;
            ForwardRunOn = ReverseRunOn = BrakingOn = BrakingReverse = false;
        }
    }
}
