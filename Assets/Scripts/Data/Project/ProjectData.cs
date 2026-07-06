
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.Data.Project
{
    [Serializable]
    public class ProjectData
    {
        public List<ProjectClip> clips = new List<ProjectClip>();
        [NonSerialized] public ProjectClip currentClip = null;
        [NonSerialized] public TaskType currentTaskType = TaskType.None;
    }
    [Serializable]
    public class ProjectClip : DataBase
    {
        [SerializeField] TaskPurposeData taskPurposeData;
        [SerializeField] TaskEquipmentData taskEquipmentData;
        [SerializeField] TaskPrincipleData taskPrincipleData;
        [SerializeField] TaskLineConnectionData taskLineConnectionData;
        [SerializeField] TaskTrainingData taskTrainingData;
        [SerializeField] TaskTestData taskTestData;
        public List<TaskDataBase> Tasks
        {
            get
            {
                return new List<TaskDataBase>()
                {
                    taskPurposeData,
                    taskEquipmentData,
                    taskPrincipleData,
                    taskLineConnectionData,
                    taskTrainingData,
                    taskTestData
                };
            }
            set
            {
                taskPurposeData = value.Find(x => x.TaskType == TaskType.Purpose) as TaskPurposeData;
                taskEquipmentData = value.Find(x => x.TaskType == TaskType.Equipment) as TaskEquipmentData;
                taskPrincipleData = value.Find(x => x.TaskType == TaskType.Principle) as TaskPrincipleData;
                taskLineConnectionData = value.Find(x => x.TaskType == TaskType.LineConnection) as TaskLineConnectionData;
                taskTrainingData = value.Find(x => x.TaskType == TaskType.Training) as TaskTrainingData;
                taskTestData = value.Find(x => x.TaskType == TaskType.Test) as TaskTestData;
            }
        }

        public TData GetTaskData<TData>(TaskType taskType) where TData : TaskData<TData>
        {
            TaskDataBase rawData = taskType switch
            {
                TaskType.Purpose => taskPurposeData,
                TaskType.Equipment => taskEquipmentData,
                TaskType.Principle => taskPrincipleData,
                TaskType.LineConnection => taskLineConnectionData,
                TaskType.Training => taskTrainingData,
                TaskType.Test => taskTestData,
                _ => null,
            };
            return rawData as TData;
        }
        public TaskDataBase GetTaskData(TaskType taskType)
        {
            return taskType switch
            {
                TaskType.Purpose => taskPurposeData,
                TaskType.Equipment => taskEquipmentData,
                TaskType.Principle => taskPrincipleData,
                TaskType.LineConnection => taskLineConnectionData,
                TaskType.Training => taskTrainingData,
                TaskType.Test => taskTestData,
                _ => null
            };
        }

        // TODO: M1a 构造 —— ProjectClip 构造函数，初始化 6 个 TaskData
        public ProjectClip(string id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
            taskPurposeData = new TaskPurposeData($"{id}_purpose");
            taskEquipmentData = new TaskEquipmentData($"{id}_equipment");
            taskPrincipleData = new TaskPrincipleData($"{id}_principle");
            taskLineConnectionData = new TaskLineConnectionData($"{id}_lineConnection");
            taskTrainingData = new TaskTrainingData($"{id}_training");
            taskTestData = new TaskTestData($"{id}_test");
        }

        // TODO: M1a 工厂 —— GetTask 工厂方法，按 TaskType 获取对应数据
        public TaskDataBase GetTask(TaskType taskType)
        {
            return GetTaskData(taskType);
        }
    }

    [Serializable]
    public abstract class TaskDataBase : DataBase
    {
        public abstract TaskType TaskType { get; }
    }

    [Serializable]
    public abstract class TaskData<T> : TaskDataBase where T : TaskData<T>
    {
        public override TaskType TaskType => TaskType.None;
    }
    [Serializable]
    public enum TaskType
    {
        [InspectorName("空类型")]
        None,
        [InspectorName("任务目的")]
        Purpose,
        [InspectorName("实验仪器")]
        Equipment,
        [InspectorName("实验原理")]
        Principle,
        [InspectorName("电路连接")]
        LineConnection,
        [InspectorName("仿真实验")]
        Training,
        [InspectorName("小测验")]
        Test,

    }
    // ────────────────────── 内容数据类 ──────────────────────

    /// <summary>
    /// 口语/表述数据 —— 用在 Purpose / Principle 等需要文本展示的任务。
    /// TODO: M1a 骨架字段 —— 参考 Tuanjie SpeakingData
    /// </summary>
    [Serializable]
    public class SpeakingData
    {
        // TODO: M2 填入 —— 口语/表述内容
        public string showContent;      // 图文内容（支持富文本）
        public string videoPath;        // 关联视频路径
    }

    /// <summary>
    /// 仪器数据 —— 实验仪器清单。
    /// TODO: M1a 骨架字段 —— 参考 Tuanjie EquipmentData
    /// </summary>
    [Serializable]
    public class EquipmentData
    {
        // TODO: M2 填入 —— 仪器条目列表
        // public List<EquipmentItem> items;

        // 占位：仪器名称 / 缩略图
        public string equipmentName;
        public string iconPath;
    }

    /// <summary>
    /// 原理数据 —— 实验原理内容（图文 / 模型 / 视频）。
    /// TODO: M1a 骨架字段 —— 参考 Tuanjie PrincipleData
    /// </summary>
    [Serializable]
    public class PrincipleData
    {
        // TODO: M2 填入 —— 原理说明内容
        public string showContent;      // 图文内容
        public string showModelPath;    // 3D 模型路径
        public string videoPath;        // 视频路径
    }

    /// <summary>
    /// 提示/指引数据 —— 用于 Training 等需要操作指引的任务。
    /// TODO: M1a 骨架字段 —— 参考 Tuanjie TipsData
    /// </summary>
    [Serializable]
    public class TipsData
    {
        // TODO: M2 填入 —— 提示内容
        public string tipText;          // 提示文字
        public float displayDuration = 3f;  // 显示时长（秒）
    }

    // ────────────────────── TaskData 子类 ──────────────────────

    [Serializable]
    public class TaskPurposeData : TaskData<TaskPurposeData>
    {
        public override TaskType TaskType => TaskType.Purpose;

        // TODO: M1a 补充字段 —— 实验目的数据
        public SpeakingData speakingData;

        public TaskPurposeData(string id)
        {
            this.id = id;
            displayName = "任务目的";
        }
    }
    [Serializable]
    public class TaskEquipmentData : TaskData<TaskEquipmentData>
    {
        public override TaskType TaskType => TaskType.Equipment;

        // TODO: M1a 补充字段 —— 仪器清单数据
        public EquipmentData equipmentData;

        public TaskEquipmentData(string id)
        {
            this.id = id;
            displayName = "实验仪器";
        }
    }
    [Serializable]
    public class TaskPrincipleData : TaskData<TaskPrincipleData>
    {
        public override TaskType TaskType => TaskType.Principle;

        // TODO: M1a 补充字段 —— 原理内容数据
        public PrincipleData principleData;
        public SpeakingData speakingData;

        public TaskPrincipleData(string id)
        {
            this.id = id;
            displayName = "实验原理";
        }
    }
    [Serializable]
    public class TaskLineConnectionData : TaskData<TaskLineConnectionData>
    {
        public override TaskType TaskType => TaskType.LineConnection;

        // TODO: M1a 补充字段 —— 连线数据（引用 LineConnectionData）
        public LineConnectionData lineConnectionData;

        public TaskLineConnectionData(string id)
        {
            this.id = id;
            displayName = "电路连接";
        }
    }
    [Serializable]
    public class TaskTrainingData : TaskData<TaskTrainingData>
    {
        public override TaskType TaskType => TaskType.Training;

        // TODO: M1a 补充字段 —— 实训指引数据
        public TipsData tipsData;

        public TaskTrainingData(string id)
        {
            this.id = id;
            displayName = "仿真实验";
        }
    }
    [Serializable]
    public class TaskTestData : TaskData<TaskTestData>
    {
        public override TaskType TaskType => TaskType.Test;

        // TODO: M1a 补充字段 —— 测验题目数据
        public QuestionClip questionClip;

        public TaskTestData(string id)
        {
            this.id = id;
            displayName = "小测验";
        }
    }

}