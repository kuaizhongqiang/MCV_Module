using System.Collections;
using MCV_Module.Models;
using MCV_Module.Steps;

namespace MCV_Module.Interfaces
{
    /// <summary>
    /// 步骤条件接口 —— 纯 C# 类，三阶段协程模型（对齐 Tuanjie v2.0）。
    /// 由 StepManager 协程 yield 驱动：Prepare → Waiting → Complete。
    /// NextStep/Skip 通过 ForceComplete() 协作式打断 Waiting；跳转前置 ResetCondition() 全员归位。
    /// </summary>
    public interface ICondition
    {
        /// <summary>条件类型（与枚举 ConditionType 一一对应）</summary>
        ConditionType Type { get; }

        /// <summary>条件状态机（Ready → Waiting → Complete）</summary>
        StepStutus Status { get; set; }

        /// <summary>一次性初始化（StepHandler.Awake 中 new 后调用；幂等）</summary>
        void ConditionInit(StepHandler step);

        /// <summary>阶段①准备：通用显隐 + 隐藏动画物体 + 子类钩子</summary>
        IEnumerator Prepare();

        /// <summary>阶段②等待：子类实现交互循环，条件满足或 ForceComplete 时返回</summary>
        IEnumerator Waiting();

        /// <summary>阶段③完成：OnCompleteHide + 播放动画 + 等播完 + hideOnComplete</summary>
        IEnumerator Complete();

        /// <summary>快速执行：跳过 Waiting，动画瞬间跳到最后一帧（跳转前缀用）</summary>
        IEnumerator FastForward();

        /// <summary>强制完成：置 Status=Complete 并令 Waiting 协程尽快退出（NextStep/Skip 调用）</summary>
        void ForceComplete();

        /// <summary>重置条件：清打断标志/残留订阅、Status 回 Ready（跳转前置 PrepareAllConditions 调用）</summary>
        void ResetCondition();
    }
}
