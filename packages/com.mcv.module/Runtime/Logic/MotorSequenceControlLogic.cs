namespace MCV_Module.Logic
{
    /// <summary>
    /// 顺序控制逻辑类 —— 两台电机顺序启动、先启先停或先启后停（逆序停止）。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   正序启动：M2 启动回路串入 M1 常开辅助触点，M1 运行后 M2 才能启动。
    ///   逆序停止（reverseStop=true）：M1 停止回路串入 M2 常闭辅助触点，M2 未停则 M1 停不了。
    /// 命名：SB1=M1 启动、SB2=M2 启动、SB3=M1 停止、SB4=M2 停止。
    /// </summary>
    public class MotorSequenceControlLogic
    {
        /// <summary>M1 自锁保持状态</summary>
        bool m1Locked = false;

        /// <summary>M2 自锁保持状态</summary>
        bool m2Locked = false;

        /// <summary>M1（先启电机）运行接触器是否吸合</summary>
        public bool M1On { get; private set; }

        /// <summary>M2（后启电机）运行接触器是否吸合</summary>
        public bool M2On { get; private set; }

        /// <summary>主回路（任一电机回路）是否通电</summary>
        public bool MainCircuitOn => M1On || M2On;

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 M1On / M2On）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">M1 启动按钮是否按下</param>
        /// <param name="sb2Pressed">M2 启动按钮是否按下</param>
        /// <param name="sb1StopPressed">M1 停止按钮是否按下</param>
        /// <param name="sb2StopPressed">M2 停止按钮是否按下</param>
        /// <param name="reverseStop">先启后停（逆序停止）：true 时 M2 运行中 M1 停不下来，须先停 M2；false 为先启先停</param>
        public void Compute(bool powerOn,
                            bool sb1Pressed, bool sb2Pressed,
                            bool sb1StopPressed, bool sb2StopPressed,
                            bool reverseStop)
        {
            // 正序启动：M2 需 M1 运行后才能吸合
            bool m2Cmd = powerOn && !sb2StopPressed && M1On && (sb2Pressed || m2Locked);
            M2On = m2Cmd;

            // M1：逆序停止时，M2 运行中 M1 的停止回路被 M2 常闭触点联锁（停不下来）
            bool m1StopBlocked = reverseStop && M2On;
            M1On = powerOn && (!sb1StopPressed || m1StopBlocked) && (sb1Pressed || m1Locked);

            m1Locked = M1On;
            m2Locked = M2On;
        }

        /// <summary>复位自锁与输出状态。</summary>
        public void Reset()
        {
            m1Locked = m2Locked = false;
            M1On = M2On = false;
        }
    }
}
