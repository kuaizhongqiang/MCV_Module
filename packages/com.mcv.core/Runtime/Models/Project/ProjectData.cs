
using System;
using System.Collections.Generic;
using UnityEngine;

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
    }
    [Serializable]
    public class MenuClip : DataBase
    {
        public string parentId;          // 便于创建结构性数据
        public ProjectClip clip;         // 绑定项目数据

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
    }

    [Serializable]
    public abstract class TaskDataBase : DataBase
    {
        public abstract TaskType TaskType { get; }
    }

    [Serializable]
    public abstract class TaskData<T> : TaskDataBase where T : TaskData<T>
    {        
        protected bool taskActive = true;
        public override TaskType TaskType => TaskType.None;
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
        public bool TaskActive => taskActive;
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
        public bool TaskActive => taskActive;
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
        public bool TaskActive => taskActive;
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
        public bool TaskActive => taskActive;
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
        public bool TaskActive => taskActive;
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
        public bool TaskActive => taskActive;
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