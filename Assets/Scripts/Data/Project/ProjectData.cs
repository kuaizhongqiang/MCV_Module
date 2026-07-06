
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
    [Serializable]
    public class TaskDefrojectData : TaskData<TaskDefrojectData>
    {
        public override TaskType TaskType => TaskType.None;

        public TaskDefrojectData(string id)
        {
            this.id = id;
            displayName = "任务目的";
        }
    }
    [Serializable]
    public class TaskPurposeData : TaskData<TaskPurposeData>
    {
        public override TaskType TaskType => TaskType.Purpose;

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

        public TaskTestData(string id)
        {
            this.id = id;
            displayName = "小测验";
        }
    }

}