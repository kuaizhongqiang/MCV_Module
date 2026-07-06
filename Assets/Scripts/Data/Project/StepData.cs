using System;
using System.Collections.Generic;

namespace MCV_Module.Data.Project
{
    /// <summary>
    /// 步骤系统数据 —— 三级结构：Processing → Step → Condition
    /// </summary>

    [Serializable]
    public class ProcessingData
    {
        public string processingId;
        public string displayName;
        public List<StepData> steps = new List<StepData>();
    }

    [Serializable]
    public class StepData
    {
        public string stepId;
        public string displayName;
        public string description;
        public float timeoutSeconds = -1f;
        public List<ConditionConfig> conditions = new List<ConditionConfig>();
        public StepExecuteType executeType = StepExecuteType.Auto;
    }

    [Serializable]
    public class ConditionConfig
    {
        public string conditionId;
        public ConditionType type = ConditionType.Default;
        public string targetId;      // 目标物体/UI ID
        public string extraParam;    // 额外参数（JSON 字符串）
    }

    public enum StepExecuteType
    {
        Auto,       // 自动执行
        Waiting,    // 等待交互
    }

    public enum ConditionType
    {
        None,
        Default,     // 默认无操作
        Click,       // 点击交互
        Drag,        // 拖拽交互
        Tool,        // 工具使用
        UI,          // UI 交互
        Question,    // 答题
        LineConnect, // 连线配对
        Finish,      // 完成/结束
    }
}
