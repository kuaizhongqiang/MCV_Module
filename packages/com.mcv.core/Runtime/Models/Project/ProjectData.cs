
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace MCV_Module.Models.Project
{
    [Serializable]
    public class MenuData
    {
        public List<MenuClip> clips = new List<MenuClip>();

        #region 工厂方法
        /// <summary>获取根菜单。</summary>
        public List<MenuClip> GetRootClips()
        {
            var list = new List<MenuClip>();
            foreach (var clip in clips)
            {
                if (clip.parentId == null)
                {
                    list.Add(clip);
                }
            }
            return list;
        }
        /// <summary>获取子菜单（按父菜单数据）。</summary>
        public List<MenuClip> GetChildClips(MenuClip parentClip)
        {
            var list = new List<MenuClip>();
            foreach (var clip in clips)
            {
                if (clip.parentId == parentClip.id)
                {
                    list.Add(clip);
                }
            }
            return list;
        }
        /// <summary>获取子菜单（按父菜单 ID）。</summary>
        public List<MenuClip> GetChildClips(string parentId)
        {
            var list = new List<MenuClip>();
            foreach (var clip in clips)
            {
                if (clip.parentId == parentId)
                {
                    list.Add(clip);
                }
            }
            return list;

        }
        /// <summary>获取父菜单（按子菜单数据）；找不到返回 null。</summary>
        public MenuClip GetParentClip(MenuClip childClip)
        {
            foreach (var clip in clips)
            {
                if (clip.id == childClip.parentId)
                {
                    return clip;
                }
            }
            return null;

        }
        /// <summary>获取父菜单（按子菜单 ID）；找不到（根菜单或无此 ID）返回 null。</summary>
        public MenuClip GetParentClip(string childId)
        {
            var child = GetClip(childId);
            if (child == null)
            {
                return null;
            }
            return GetParentClip(child);
        }
        /// <summary>获取菜单（按 ID）；找不到返回 null。</summary>
        public MenuClip GetClip(string clipId)
        {
            foreach (var clip in clips)
            {
                if (clip.id == clipId)
                {
                    return clip;
                }
            }
            return null;
        }
        
        /// <summary>获取菜单在其所属层级（同 parentId）中的索引，而不是整体数组的索引；找不到返回 -1。</summary>
        public int GetClipIndex(string clipId)
        {
            var target = GetClip(clipId);
            if (target == null)
            {
                return -1;
            }
            return GetClipIndex(target);
        }

        /// <summary>获取菜单在其所属层级（同 parentId）中的索引，而不是整体数组的索引；找不到返回 -1。</summary>
        public int GetClipIndex(MenuClip clip)
        {
            if (clip == null)
            {
                return -1;
            }
            int index = 0;
            foreach (var sibling in clips)
            {
                if (sibling.parentId == clip.parentId)
                {
                    if (sibling == clip)
                    {
                        return index;
                    }
                    index++;
                }
            }
            return -1;
        }

        /// <summary>判断菜单是否含有子菜单；有子菜单返回 true，无子菜单或参数为 null 返回 false。</summary>
        public bool HasChildren(MenuClip clip)
        {
            if (clip == null)
            {
                return false;
            }
            foreach (var item in clips)
            {
                if (item.parentId == clip.id)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    
        /// <summary>返回菜单数据的 JSON 描述（把扁平的 clips 还原为 id/parentId 树形结构）。</summary>
        public string MenuDataDescription()
        {
            var tree = new List<MenuNodeDto>();
            foreach (var root in GetRootClips())
            {
                if (root != null)
                    tree.Add(BuildNode(root));
            }
            return JsonConvert.SerializeObject(tree, Formatting.Indented);
        }

        /// <summary>递归构建某个菜单节点及其所有子节点(树形)。</summary>
        MenuNodeDto BuildNode(MenuClip clip)
        {
            var node = new MenuNodeDto
            {
                id = clip.id,
                displayName = clip.displayName,
                parentId = clip.parentId,
            };
            foreach (var child in GetChildClips(clip.id))
            {
                if (child != null)
                    node.children.Add(BuildNode(child));
            }
            return node;
        }

        /// <summary>菜单树节点 DTO（用于 JSON 序列化, 保留 parentId 以体现父引用）。</summary>
        [Serializable]
        class MenuNodeDto
        {
            [JsonProperty("id")] public string id;
            [JsonProperty("displayName")] public string displayName;
            [JsonProperty("displayNameEn")] public string displayNameEn;   // 英文列（空 = 回退中文）
            [JsonProperty("parentId")] public string parentId;
            [JsonProperty("children")] public List<MenuNodeDto> children = new List<MenuNodeDto>();
        }
    }
    [Serializable]
    public class MenuClip : DataBase
    {
        /// <summary>便于创建结构性数据。</summary>
        public string parentId;
        /// <summary>绑定的项目 id（从 ProjectData.clips 查询，数据源单一）；为空时回退 clip 直接引用。</summary>
        public string projectId;
        /// <summary>绑定项目数据（旧字段，可空）。</summary>
        public ProjectClip clip;

        public MenuClip() { }
        public MenuClip(string id, string displayName)
        {
            this.id = id;
            this.displayName = displayName;
        }
    }
    
    [Serializable]
    public class ProjectData
    {
        public List<ProjectClip> clips = new List<ProjectClip>();
        [NonSerialized] public ProjectClip currentClip = null;
        [NonSerialized] public TaskType currentTaskType = TaskType.None;
        [NonSerialized] public ProjectState projectState = ProjectState.Start;

        public string ProjectDescription()
        {
            string result = "";
            foreach (var clip in clips)
            {
                result += clip.ProjectClipDescription();
            }

            return result;
        }
    }
    [Serializable]
    public class ProjectClip : DataBase
    {
        [SerializeField, JsonProperty("taskPurposeData")] TaskPurposeData taskPurposeData;
        [SerializeField, JsonProperty("taskEquipmentData")] TaskEquipmentData taskEquipmentData;
        [SerializeField, JsonProperty("taskPrincipleData")] TaskPrincipleData taskPrincipleData;
        [SerializeField, JsonProperty("taskLineConnectionData")] TaskLineConnectionData taskLineConnectionData;
        [SerializeField, JsonProperty("taskTrainingData")] TaskTrainingData taskTrainingData;
        [SerializeField, JsonProperty("taskTestData")] TaskTestData taskTestData;
        [SerializeField, JsonProperty("taskExamData")] TaskExamData taskExamData;
        [SerializeField, JsonProperty("taskInfoData")] TaskInfoData taskInfoData;
        [SerializeField, JsonProperty("taskStructureData")] TaskStructureData taskStructureData;
        [SerializeField, JsonProperty("taskInspectionData")] TaskInspectionData taskInspectionData;
        [JsonIgnore]
        public List<TaskDataBase> Tasks
        {
            get
            {
                // WHY: 顺序 = 步骤在 TaskListPanel 中的装配顺序：四步在前，旧实验线在后。
                var list = new List<TaskDataBase>();
                if (taskInfoData != null) list.Add(taskInfoData);
                if (taskStructureData != null) list.Add(taskStructureData);
                if (taskPrincipleData != null) list.Add(taskPrincipleData);
                if (taskInspectionData != null) list.Add(taskInspectionData);
                // WHY: 旧实验线暂时保留，不下线。
                if (taskPurposeData != null) list.Add(taskPurposeData);
                if (taskEquipmentData != null) list.Add(taskEquipmentData);
                if (taskLineConnectionData != null) list.Add(taskLineConnectionData);
                if (taskTrainingData != null) list.Add(taskTrainingData);
                if (taskTestData != null) list.Add(taskTestData);
                if (taskExamData != null) list.Add(taskExamData);
                return list;
            }
            set
            {
                taskPurposeData = value.Find(x => x.TaskType == TaskType.Purpose) as TaskPurposeData;
                taskEquipmentData = value.Find(x => x.TaskType == TaskType.Equipment) as TaskEquipmentData;
                taskPrincipleData = value.Find(x => x.TaskType == TaskType.Principle) as TaskPrincipleData;
                taskLineConnectionData = value.Find(x => x.TaskType == TaskType.LineConnection) as TaskLineConnectionData;
                taskTrainingData = value.Find(x => x.TaskType == TaskType.Training) as TaskTrainingData;
                taskTestData = value.Find(x => x.TaskType == TaskType.Test) as TaskTestData;
                taskExamData = value.Find(x => x.TaskType == TaskType.Exam) as TaskExamData;
                taskInfoData = value.Find(x => x.TaskType == TaskType.Info) as TaskInfoData;
                taskStructureData = value.Find(x => x.TaskType == TaskType.Structure) as TaskStructureData;
                taskInspectionData = value.Find(x => x.TaskType == TaskType.Inspection) as TaskInspectionData;
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
                TaskType.Exam => taskExamData,
                TaskType.Info => taskInfoData,
                TaskType.Structure => taskStructureData,
                TaskType.Inspection => taskInspectionData,
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
                TaskType.Exam => taskExamData,
                TaskType.Info => taskInfoData,
                TaskType.Structure => taskStructureData,
                TaskType.Inspection => taskInspectionData,
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
            taskExamData = new TaskExamData($"{id}_exam");
            taskInfoData = new TaskInfoData($"{id}_info");
            taskStructureData = new TaskStructureData($"{id}_structure");
            taskInspectionData = new TaskInspectionData($"{id}_inspection");
        }

        // TODO: M1a 工厂 —— GetTask 工厂方法，按 TaskType 获取对应数据
        public TaskDataBase GetTask(TaskType taskType)
        {
            return GetTaskData(taskType);
        }
    
        public T GetTask<T>(TaskType taskType) where T : TaskData<T>
        {
            return GetTaskData<T>(taskType);
        }
    
        public string ProjectClipDescription()
        {
            string result = "";
            int count = 0;
            foreach (var item in Tasks)
            {
                if (item.TaskActive)
                {
                    result += $"{item.TaskDataDescription()}\n";
                    count++;
                }
            }

            result += $"{displayName}模块共{count}个任务";
            for (int i = 0; i < Tasks.Count; i++)
            {
                if (Tasks[i].TaskActive)
                {
                    result += $"{i + 1}. {Tasks[i].TaskDataDescription()}\n";
                }
            }

            return result;
        }
    }

    [Serializable]
    public abstract class TaskDataBase : DataBase
    {
        public abstract TaskType TaskType { get; }

        /// <summary>该任务是否启用(在项目任务列表中激活显示)。由具体任务数据实现。</summary>
        public abstract bool TaskActive { get; }

        public string TaskDataDescription()
        {
            string result = "";
            result += $"{displayName}：{TaskDesc(TaskType)}";

            return result;
        }

        static string TaskDesc(TaskType taskType)
        {
            switch (taskType)
            {
                case TaskType.Purpose:
                    return "任务目的用于展示每个实训任务的学习意义，并展示一个核心实验器材的三维模型动画";
                case TaskType.Equipment:
                    return "实验仪器用于展示每个实训任务所使用的实验器材，通过一个列表多个按钮点击切换更新主要画面中的模型，可以通过鼠标控制展示模型的姿态与尺寸";
                case TaskType.Principle:
                    return "实验原理用于展示每个实训任务所使用的实验原理，实验原理是通过多个视频展示实训的原理，可以通过列表切换";
                case TaskType.LineConnection:
                    return "电路连接用于开放性接线交互，分步骤引导学生依次拖拽导线连接电路元件：先连接电源与主干，再按序接入各元件并完成回路，每步由系统即时校验接线是否正确并给出反馈，帮助学生按规范步骤掌握连接方法与排查接线错误";
                case TaskType.Training:
                    return "仿真实验提供一个可交互的虚拟实验环境，按引导步骤带领学生逐步操作：先准备与检查器材，再分步执行实验、观察现象并记录数据，每步完成后再进入下一步，在不接触真实设备的情况下安全、有序地完成实训操作";
                case TaskType.Test:
                    return "小测验通过一组选择题检验学生对本次实训知识点的掌握程度，即时反馈作答正确与否，帮助学生巩固与自测学习效果";
                case TaskType.Exam:
                    return "考核从题库中随机抽取若干道单选题，考查学生对低压电器基础知识的掌握情况，作答即时反馈对错，答对后自动进入下一题";
                case TaskType.Info:
                    return "简介用于展示每个实训任务所使用的简介，简介通过一个列表多个按钮点击切换更新主要画面中的模型，可以通过鼠标控制展示模型的姿态与尺寸";
                    // TODO: 返回内容要重写
                case TaskType.Structure:
                    return "结构用于展示每个实训任务所使用的结构，结构通过一个列表多个按钮点击切换更新主要画面中的模型，可以通过鼠标控制展示模型的姿态与尺寸";
                    // TODO: 返回内容要重写
                case TaskType.Inspection:
                    return "检测用于展示每个实训任务所使用的检测，检测通过一个列表多个按钮点击切换更新主要画面中的模型，可以通过鼠标控制展示模型的姿态与尺寸";
                    // TODO: 返回内容要重写
                default:
                    return "空任务类型，暂无任务描述";

            }
        }
    }

    [Serializable]
    public abstract class TaskData<T> : TaskDataBase where T : TaskData<T>
    {        
        // WHY: 必须为 public 才会被 Newtonsoft 序列化（protected 字段不进 JSON）。
        /// <summary>该任务是否启用；可写入 JSON 配置，false 时 TaskListPanel 不装配该项（数据仍保留）。</summary>
        public bool taskActive = true;
        public override TaskType TaskType => TaskType.None;
        public override bool TaskActive => taskActive;
    }
    // ────────────────────── 内容数据类 ──────────────────────

    

    // ────────────────────── TaskData 子类 ──────────────────────

    [Serializable]
    public class TaskDefaultData : TaskData<TaskDefaultData>
    {        
        public override TaskType TaskType => TaskType.None;
        public TaskDefaultData(string id)
        {
            this.id = id;
            displayName = "默认无操作";
        }
    }

    [Serializable]
    public class TaskPurposeData : TaskData<TaskPurposeData>
    {
        public override TaskType TaskType => TaskType.Purpose;
        public string contentText;
        public string contentTextEn;                       // 英文列（空 = 回退中文）
        public string prefabKey;
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
        public List<EquipmentStruct> equipmentStructs = new List<EquipmentStruct>();
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
        public List<PrincipleStruct> principleStructs = new List<PrincipleStruct>();     

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
        public string prefabKey;
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
        public string prefabKey;
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
        public List<QuestionClip> questionClips = new List<QuestionClip>();
        public TaskTestData(string id)
        {
            this.id = id;
            displayName = "小测验";
        }
    }
    [Serializable]
    public class TaskInfoData : TaskData<TaskInfoData>
    {
        public override TaskType TaskType => TaskType.Info;
        public string contentText;
        public string contentTextEn;                       // 英文列（空 = 回退中文）

        /// <summary>简介模型（器件 3D 展示物体）id（= ProjectData.json 的 taskInfoData.prefabKey，形如 contactor_info_model）；为空则只装配图集与文案，不做物体渲染。</summary>
        public string prefabKey;

        // WHY: 保留此字段是为让 JSON 里已写好的键能被正常反序列化，不再被静默忽略。
        /// <summary>简介主图 key（预留，命名 {器件}_image）；当前无消费方，图集一律走 images。</summary>
        public string imageKey;

        /// <summary>简介图集（有序）id 列表（形如 contactor_info_01），运行时按 id 批量加载成 Sprite。</summary>
        public List<string> images = new List<string>();

        public TaskInfoData(string id)
        {
            this.id = id;
            displayName = "简介";
        }
    }
    [Serializable]
    public class TaskStructureData : TaskData<TaskStructureData>
    {
        public override TaskType TaskType => TaskType.Structure;
        public string prefabKey;
        public TaskStructureData(string id)
        {
            this.id = id;
            displayName = "结构";
        }
    }
    [Serializable]
    public class TaskInspectionData : TaskData<TaskInspectionData>
    {
        public override TaskType TaskType => TaskType.Inspection;

        /// <summary>检测预制体（包配置 id，形如 <c>contactor_inspection_model</c>）。</summary>
        public string prefabKey;

        public TaskInspectionData(string id)
        {
            this.id = id;
            displayName = "检测";
        }
    }

    [Serializable]
    public class TaskExamData : TaskData<TaskExamData>
    {
        public override TaskType TaskType => TaskType.Exam;
        public List<QuestionClip> questionClips = new List<QuestionClip>();
        public TaskExamData(string id)
        {
            this.id = id;
            displayName = "考核";
        }
    }
    [Serializable]
    public struct EquipmentStruct
    {
        public string prefabKey;
        public string title;
        public string titleEn;                             // 英文列（空 = 回退中文）
        public string contentText;
        public string contentTextEn;                       // 英文列（空 = 回退中文）
        public string audioName;
    }

    [Serializable]
    public struct PrincipleStruct
    {
        public string title;
        public string titleEn;                             // 英文列（空 = 回退中文）
        public string contentText;
        public string contentTextEn;                       // 英文列（空 = 回退中文）
        public string videoName;
    }
}