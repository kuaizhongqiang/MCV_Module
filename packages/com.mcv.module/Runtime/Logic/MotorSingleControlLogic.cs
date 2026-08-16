namespace MCV_Module.Logic
{
    /// <summary>
    /// 三相电动机单开单控（自锁）控制逻辑工具类。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementDafaultManager 喂入元件状态并读取结果。
    /// 原理：
    ///   线圈通电 = 电源可用 && !停止按钮按下 && (启动按钮按下 || 自锁保持)
    ///   线圈通电 → 自锁置位；主回路通电 = 电源可用 && 线圈通电 → 电机运行。
    /// </summary>
    public class MotorSingleControlLogic
    {
        /// <summary>自锁保持状态（线圈通电后置位，停止按钮按下或失电时复位）</summary>
        bool selfLocked = false;

        /// <summary>控制回路（接触器线圈回路）是否通电</summary>
        public bool ControlCircuitOn { get; private set; }
        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn { get; private set; }

        /// <summary>
        /// 计算电路状态（喂入当前元件状态后，读取 ControlCircuitOn / MainCircuitOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">启动按钮是否按下</param>
        /// <param name="sbPressed">停止按钮是否按下</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sbPressed)
        {
            ControlCircuitOn = powerOn && !sbPressed && (sb1Pressed || selfLocked);
            selfLocked = ControlCircuitOn;                   // 线圈通电即自锁保持
            MainCircuitOn = powerOn && ControlCircuitOn;     // 主回路 = 有电且线圈通电
        }

        /// <summary>复位自锁与输出状态。</summary>
        public void Reset()
        {
            selfLocked = false;
            ControlCircuitOn = false;
            MainCircuitOn = false;
        }
    }
}
