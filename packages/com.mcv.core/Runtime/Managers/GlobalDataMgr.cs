using System.Collections;
using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Models.System;
using MCV_Module.Models.User;
using MCV_Module.Singleton;
using UnityEngine;

namespace MCV_Module.Managers
{
    public class GlobalDataMgr : SingletonGlobalMgr<GlobalDataMgr>
    {
        #region 参数
        [SerializeField, Header("系统数据")] SystemData systemData = new SystemData();
        [SerializeField, Header("目录数据")] MenuData menuData = new MenuData();
        [SerializeField, Header("内容数据")] ProjectData projectData = new ProjectData();
        [SerializeField, Header("用户数据")] UserData userData = new UserData();
        [SerializeField, Header("语言数据")] LanguageData languageData = new LanguageData();

        public SystemData SystemData { get => systemData; set => systemData = value; }
        public MenuData MenuData { get => menuData; set => menuData = value; }
        public ProjectData ProjectData { get => projectData; set => projectData = value; }
        public UserData UserData { get => userData; set => userData = value; }
        public LanguageData LanguageData { get => languageData; set => languageData = value; }
        #endregion

        #region 生命周期
        protected GlobalDataMgr() { }

        protected override IEnumerator DelayInit()
        {
            // 异步读取 JSON（WebGL 兼容，用 UnityWebRequest 替代 File.ReadAllText）
            bool loaded = false;

            yield return JsonReaderWriter.ReadAsync<SystemData>("SystemData", (data, ok) =>
            {
                if (ok) SystemData = data;
                loaded = true;
            });
            yield return new WaitUntil(() => loaded);

            // 语言数据必须加载：否则 WriteJson 会用默认空值覆盖 JSON，编辑器生成的 Clip 会被清掉
            bool langLoaded = false;
            yield return JsonReaderWriter.ReadAsync<LanguageData>("LanguageData", (data, ok) =>
            {
                if (ok) LanguageData = data;
                langLoaded = true;
            });
            yield return new WaitUntil(() => langLoaded);

            // 目录数据：加载后 AI 预热才能拿到【当前目录结构】描述
            bool menuLoaded = false;
            yield return JsonReaderWriter.ReadAsync<MenuData>("MenuData", (data, ok) =>
            {
                if (ok && data != null) MenuData = data;
                menuLoaded = true;
            });
            yield return new WaitUntil(() => menuLoaded);

            // 内容数据：加载后 AI 预热才能拿到【当前学习内容】描述
            bool projectLoaded = false;
            yield return JsonReaderWriter.ReadAsync<ProjectData>("ProjectData", (data, ok) =>
            {
                if (ok && data != null) ProjectData = data;
                projectLoaded = true;
            });
            yield return new WaitUntil(() => projectLoaded);

            // WriteJson();

            // P5 修复既有缺陷：原实现未置 isInit=true，导致 Setup 启动链等待 15s 超时
            isInit = true;
            yield break;
        }
        #endregion

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

        #region 登录
        /// <summary>
        /// 登录白名单验证。
        /// 【暂空】当前直接返回 true（通过），后续在此接入账号密码/白名单校验。
        /// </summary>
        public static bool VerifyLogin(string userName, string password, UserType type)
        {
            // TODO: 白名单验证逻辑（暂空，直接通过）
            return true;
        }

        /// <summary>
        /// 写入登录用户数据（覆盖当前 UserData 并记录登录时间）。
        /// </summary>
        public static void SetUserData(string userName, string password, UserType type)
        {
            UserData data = GlobalDataMgr.Instance.UserData;
            if (data == null)
            {
                data = new UserData();
                GlobalDataMgr.Instance.UserData = data;
            }
            data.userName = userName;
            data.password = password;
            data.userType = type;
            data.loginTime = System.DateTime.Now;
        }
        #endregion

        #region 菜单数据
        /// <summary>
        /// 获取目录数据。
        /// </summary>
        public static MenuData GetMenuData()
        {
            return GlobalDataMgr.Instance.MenuData;
        }

        /// <summary>
        /// 获取根菜单列表。
        /// </summary>
        public static List<MenuClip> GetRootMenus()
        {
            MenuData menuData = GetMenuData();
            return menuData != null ? menuData.GetRootClips() : new List<MenuClip>();
        }

        /// <summary>
        /// 获取指定菜单的子菜单列表。
        /// </summary>
        public static List<MenuClip> GetChildMenus(MenuClip parent)
        {
            MenuData menuData = GetMenuData();
            return menuData != null && parent != null
                ? menuData.GetChildClips(parent)
                : new List<MenuClip>();
        }
        #endregion

        #endregion

        #region 私有方法
#if UNITY_EDITOR
        // 测试/调试用：将当前数据写回 JSON（同步 IO 仅限 Editor）
        void WriteJson()
        {
            JsonReaderWriter.Write<SystemData>("SystemData", SystemData, null);
            JsonReaderWriter.Write<ProjectData>("ProjectData", ProjectData, null);
            JsonReaderWriter.Write<UserData>("UserData", UserData, null);
            JsonReaderWriter.Write<LanguageData>("LanguageData", LanguageData, null);
        }
#endif
        #endregion
    }
}
