using System.Collections;
using UnityEngine;

namespace MCV_Module.Logic
{
    /// <summary>
    /// 能耗制动控制逻辑类 —— 停止时定子通直流电制动（KT 定时）。
    /// 纯逻辑、不依赖 MonoBehaviour；由 ElementManagerBase 派生管理器喂入元件状态并读取结果。
    /// 原理：
    ///   正常运行：SB1 自锁吸合运行接触器 KM1；
    ///   按下停止 SB → KM1 断开 → 制动接触器 KM2 吸合（定子通直流），KT 计时；
    ///   KT 计时到（或期间重新启动/失电）→ KM2 断开。
    /// 用法：管理器在 NeedsBraking 时 StartCoroutine(RunBraking(brakeTime))；
    ///       协程在首个 yield 前同步置 BrakingOn=true，故先启动协程再应用元件，即可在制动开始即吸合 KM2。
    /// </summary>
    public class MotorDynamicBrakingControlLogic
    {
        /// <summary>自锁保持状态</summary>
        bool runLocked = false;

        /// <summary>电源状态（协程中断条件用）</summary>
        bool powerOn = false;

        /// <summary>制动请求（停止边沿置位）</summary>
        bool brakeRequest = false;

        /// <summary>运行接触器 KM1 是否吸合</summary>
        public bool RunOn { get; private set; }

        /// <summary>制动接触器 KM2 是否吸合（能耗制动）</summary>
        public bool BrakingOn { get; private set; }

        /// <summary>是否正在制动计时</summary>
        public bool Timing { get; private set; }

        /// <summary>是否需要启动制动计时（停止边沿且电源可用）</summary>
        public bool NeedsBraking => powerOn && !RunOn && !BrakingOn && !Timing && brakeRequest;

        /// <summary>
        /// 计算电路状态（喂入元件状态后，读取 RunOn / BrakingOn / NeedsBraking）。
        /// </summary>
        /// <param name="powerOn">电源是否可用（断路器闭合）</param>
        /// <param name="sb1Pressed">启动按钮是否按下（SB1）</param>
        /// <param name="sbPressed">停止按钮是否按下（SB）</param>
        public void Compute(bool powerOn, bool sb1Pressed, bool sbPressed)
        {
            this.powerOn = powerOn;
            bool wasRunning = RunOn;

            RunOn = powerOn && !sbPressed && (sb1Pressed || runLocked);
            runLocked = RunOn;

            // 停止边沿：此前运行、现在停止、电源可用、停止按钮按下 → 请求制动
            brakeRequest = wasRunning && !RunOn && powerOn && sbPressed;
        }

        /// <summary>
        /// 制动计时协程：管理器在 NeedsBraking 时 StartCoroutine 启动。
        /// 计时中重新启动或失电 → 立即中断制动。
        /// </summary>
        public IEnumerator RunBraking(float brakeTime)
        {
            if (Timing) yield break;
            Timing = true;
            BrakingOn = true;
            brakeRequest = false;

            float remain = Mathf.Max(0f, brakeTime);
            while (remain > 0f && powerOn && !RunOn)
            {
                yield return null;
                remain -= Time.deltaTime;
            }

            BrakingOn = false;
            Timing = false;
        }

        /// <summary>复位自锁、计时与输出状态。</summary>
        public void Reset()
        {
            runLocked = false;
            powerOn = false;
            brakeRequest = false;
            RunOn = BrakingOn = false;
            Timing = false;
        }
    }
}
