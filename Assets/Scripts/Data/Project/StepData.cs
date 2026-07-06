
using System;
using System.Collections.Generic;

namespace MCV_Module.Data.Project
{
    /// <summary>
    /// 步骤数据基类 —— 步骤系统三级结构的数据模型。
    /// TODO: M3 填入完整字段 —— 参考 Tuanjie StepDataModel
    /// </summary>
    [Serializable]
    public abstract class StepDataBase
    {
        public string stepId;
        public string displayName;

        // TODO: M3 实现 —— 步骤描述 / 提示文字
        // public string description;
    }

    /// <summary>
    /// 步骤配置 —— 每条步骤的配置信息。
    /// 包含步骤类型、前置条件、超时等。
    /// </summary>
    [Serializable]
    public class StepConfig
    {
        // TODO: M3 实现 —— 步骤配置字段
        public string stepId;
        public float timeoutSeconds = -1f; // -1 表示无超时

        // TODO: M3 实现 —— 前置步骤 ID 列表
        // public List<string> prerequisiteStepIds;

        // TODO: M3 实现 —— 步骤完成后的跳转策略
        // public StepCompleteAction onComplete;
    }

    /// <summary>
    /// 工序数据 —— 一个工序包含多个步骤。
    /// 三级结构：Processing → Step → Condition
    /// </summary>
    [Serializable]
    public class ProcessingData
    {
        // TODO: M3 实现 —— 工序基本字段
        public string processingId;
        public string displayName;

        // TODO: M3 实现 —— 本工序包含的步骤列表
        // public List<StepData> steps;
    }

    /// <summary>
    /// 步骤数据 —— 步骤系统的最小执行单元。
    /// </summary>
    [Serializable]
    public class StepData
    {
        // TODO: M3 实现 —— 步骤基本字段
        public string stepId;
        public string displayName;

        // TODO: M3 实现 —— 本步骤的条件列表
        // public List<ConditionData> conditions;

        // TODO: M3 实现 —— 步骤类型（自动执行 / 等待交互）
        // public StepExecuteType executeType;
    }

    /// <summary>
    /// 条件类型枚举 —— 8 种预置交互条件。
    /// </summary>
    public enum ConditionType
    {
        None,
        /// <summary>默认条件</summary>
        Default,
        /// <summary>点击交互</summary>
        Click,
        /// <summary>拖拽交互</summary>
        Drag,
        /// <summary>工具使用</summary>
        Tool,
        /// <summary>UI 交互</summary>
        UI,
        /// <summary>答题</summary>
        Question,
        /// <summary>连线配对</summary>
        LineConnect,
        /// <summary>完成 / 结束</summary>
        Finish,
    }
}
