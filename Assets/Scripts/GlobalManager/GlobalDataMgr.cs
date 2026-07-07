
using System.Collections;
using MCV_Module.Data.Json;
using MCV_Module.Data.Project;
using MCV_Module.Data.System;
using MCV_Module.Data.User;
using UnityEngine;

namespace MCV_Module.GlobalManager
{
    public class GlobalDataMgr : SingletonGlobalMgr<GlobalDataMgr>
    {
        protected GlobalDataMgr() { }

        [SerializeField,Header("系统数据")] SystemData systemData = new SystemData();
        [SerializeField,Header("内容数据")] ProjectData projectData = new ProjectData();
        [SerializeField,Header("用户数据")] UserData userData = new UserData();
        public SystemData SystemData { get => systemData; set => systemData = value; }
        public ProjectData ProjectData { get => projectData; set => projectData = value; }
        public UserData UserData { get => userData; set => userData = value; }

        protected override IEnumerator DelayInit()
        {
            WriteJson();
            SystemData = JsonReaderWriter.Read<SystemData>("SystemData", null);

            yield break;
        }

        #region 静态方法
        public static ProjectClip GetProjectClip()
        {
            return GlobalDataMgr.Instance.ProjectData.currentClip;
        }

        public static ProjectClip GetProjectClip(string clipId)
        {
            return GlobalDataMgr.Instance.ProjectData.clips.Find(clip => clip.id == clipId);
        }
        
        public static TaskDataBase GetTaskData(TaskType type)
        {
            ProjectClip clip = GetProjectClip();
            if (clip != null)
            {
                return clip.GetTaskData(type);
            }
            return null;
        }

        public static TaskDataBase GetTaskData(string clipId, TaskType type)
        {
            ProjectClip clip = GetProjectClip(clipId);
            if (clip != null)
            {
                return clip.GetTaskData(type);
            }
            return null;
        }
        #endregion

        #region 测试方法
        void WriteJson()
        {
            JsonReaderWriter.Write<SystemData>("SystemData", SystemData, null);
            JsonReaderWriter.Write<ProjectData>("ProjectData", ProjectData, null);
            JsonReaderWriter.Write<UserData>("UserData", UserData, null);
        }
        
        #endregion
    }
}