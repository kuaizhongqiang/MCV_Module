namespace MCV_Module.Logic
{
    /// <summary>
    /// 多地控制逻辑类 —— 多处启动/停止，带自锁。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 启动按钮并联（任一按下即启动），停止按钮串联（任一按下即停止）。
    /// 线圈通电 = 电源可用 && 无停止按钮按下 && (任一启动按钮按下 || 自锁保持)
    /// </summary>
    public class MotorMultiLocationControlLogic
    {
        /// <summary>自锁保持状态（线圈通电后置位，停止按钮按下或失电时复位）</summary>
        bool selfLocked = false;

        /// <summary>控制回路（接触器线圈回路）是否通电</summary>
        public bool ControlCircuitOn { get; private set; }

        /// <summary>主回路（电机回路）是否通电</summary>
        public bool MainCircuitOn { get; private set; }

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 ControlCircuitOn / MainCircuitOn）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">各启动按钮是否按下（数组，任一为 true 即启动）</param>
        /// <param name="sbPressed">各停止按钮是否按下（数组，任一为 true 即停止）</param>
        public void Compute(bool powerOn, bool[] sb1Pressed, bool[] sbPressed)
        {
            bool anyStart = Any(sb1Pressed);
            bool anyStop = Any(sbPressed);
            ControlCircuitOn = powerOn && !anyStop && (anyStart || selfLocked);
            selfLocked = ControlCircuitOn;
            MainCircuitOn = powerOn && ControlCircuitOn;
        }

        /// <summary>数组任一为 true 返回 true（null/空数组视为 false）。</summary>
        static bool Any(bool[] array)
        {
            if (array == null) return false;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i]) return true;
            }
            return false;
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
