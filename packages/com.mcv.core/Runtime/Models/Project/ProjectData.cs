
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
        /// <summary>
        /// 获取根菜单
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 获取子菜单 
        /// </summary>
        /// <param name="parentClip"> 父菜单数据 </param>
        /// <returns></returns>
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
        /// <summary>
        /// 获取子菜单
        /// </summary>
        /// <param name="parentId"> 父菜单ID </param>
        /// <returns></returns>
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
        /// <summary>
        /// 获取父菜单
        /// </summary>
        /// <param name="childClip"> 子菜单数据 </param>
        /// <returns></returns>
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
        /// <summary>
        /// 获取父菜单
        /// </summary>
        /// <param name="childId"> 子菜单ID </param>
        /// <returns>父菜单；找不到（根菜单或无此ID）返回 null </returns>
        public MenuClip GetParentClip(string childId)
        {
            var child = GetClip(childId);
            if (child == null)
            {
                return null;
            }
            return GetParentClip(child);
        }
        /// <summary>
        /// 获取菜单
        /// </summary>
        /// <param name="clipId"> 菜单ID </param>
        /// <returns></returns>
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
        
        /// <summary>
        /// 获取菜单在其所属层级（同 parentId）中的索引，而不是整体数组的索引。
        /// 找不到时返回 -1。
        /// </summary>
        /// <param name="clipId"> 菜单ID </param>
        /// <returns></returns>
        public int GetClipIndex(string clipId)
        {
            var target = GetClip(clipId);
            if (target == null)
            {
                return -1;
            }
            return GetClipIndex(target);
        }

        /// <summary>
        /// 获取菜单在其所属层级（同 parentId）中的索引，而不是整体数组的索引。
        /// 找不到时返回 -1。
        /// </summary>
        /// <param name="clip"> 菜单数据 </param>
        /// <returns></returns>
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

        /// <summary>
        /// 判断菜单是否含有子菜单。
        /// </summary>
        /// <param name="clip"> 菜单数据 </param>
        /// <returns>有子菜单返回 true；无子菜单或参数为 null 返回 false </returns>
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
    
        /// <summary>
        /// 返回菜单数据的 JSON 描述, 体现 id / parentId 的层级结构。
        ///
        /// id / parentId 意义:
        ///   - id:       每个菜单项的唯一标识。
        ///   - parentId: 指向父菜单的 id; parentId 为 null 表示根菜单, 否则表示该菜单是
        ///                parentId 对应菜单的子菜单。
        /// 这里把扁平的 clips 列表还原为树形结构输出, 便于展示菜单层级关系。
        /// </summary>
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
            [JsonProperty("parentId")] public string parentId;
            [JsonProperty("children")] public List<MenuNodeDto> children = new List<MenuNodeDto>();
        }
    }
    [Serializable]
    public class MenuClip : DataBase
    {
        public string parentId;          // 便于创建结构性数据
        public string projectId;         // 绑定的项目 id（从 ProjectData.clips 查询，数据源单一）；为空时回退 clip 直接引用
        public ProjectClip clip;         // 绑定项目数据（旧字段，可空）

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
        [JsonIgnore]
        public List<TaskDataBase> Tasks
        {
            get
            {
                var list = new List<TaskDataBase>();
                if (taskPurposeData != null) list.Add(taskPurposeData);
                if (taskEquipmentData != null) list.Add(taskEquipmentData);
                if (taskPrincipleData != null) list.Add(taskPrincipleData);
                if (taskLineConnectionData != null) list.Add(taskLineConnectionData);
                if (taskTrainingData != null) list.Add(taskTrainingData);
                if (taskTestData != null) list.Add(taskTestData);
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
                default:
                    return "空任务类型，暂无任务描述";

            }
        }
    }

    [Serializable]
    public abstract class TaskData<T> : TaskDataBase where T : TaskData<T>
    {        
        protected bool taskActive = true;
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
    public struct EquipmentStruct
    {
        public string prefabKey;
        public string title;
        public string contentText;
        public string audioName;
    }

    [Serializable]
    public struct PrincipleStruct
    {
        public string title;
        public string contentText;
        public string videoName;
    }
}