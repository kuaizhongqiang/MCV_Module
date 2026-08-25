using System.Collections.Generic;
using MCV_Module.Controller;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 菜单面板控制器。
    ///
    /// 职责边界（MCV 单向数据流：Controller → View）：
    /// - 业务状态：当前所在层级（currentParent）、当前选中的菜单（selectedClip）、当前层级兄弟列表。
    /// - 数据来源：从 GlobalDataMgr 读取 MenuData，用工厂方法（GetRootClips/GetChildClips）取层级列表。
    /// - 层级切换决策：收到 View 上报的选中事件后，判断是否有子菜单——有则下钻，无则记为最终选中。
    ///
    /// 纯表现逻辑（滚动动画、按钮布局、输入采集）留在 MenuPanel，不在本类。
    /// Controller 常驻（跨 Canvas 重建不销毁），层级状态在此可跨面板存活。
    /// </summary>
    public class MenuController : ControllerBase<MenuPanel>
    {
        /// <summary>当前层级的兄弟列表（根层级时为根菜单）。</summary>
        readonly List<MenuClip> currentClips = new List<MenuClip>();

        /// <summary>当前层级的父菜单；null 表示当前处于根层级。</summary>
        MenuClip currentParent;

        /// <summary>当前焦点/选中的菜单（业务层选中，随滚动与下钻更新）。</summary>
        MenuClip selectedClip;

        protected override void OnViewBound()
        {
            // 先清后加，避免面板重建后重复订阅
            View.OnMenuSelected -= OnMenuSelected;
            View.OnMenuSelected += OnMenuSelected;

            // 测试期：GlobalDataMgr 无菜单数据时注入测试数据，保证可滚动/下钻验证。
            EnsureMenuData();

            // 首次进入：从根菜单开始；否则按已存的层级状态续接。
            if (currentClips.Count == 0)
            {
                EnterRoot();
            }
            else
            {
                View.Init(currentClips, GetSelectedIndex());
            }
        }

        /// <summary>确保 GlobalDataMgr 中有菜单数据；为空时注入修正后的测试数据。</summary>
        void EnsureMenuData()
        {
            MenuData menuData = GlobalDataMgr.GetMenuData();
            if (menuData != null && menuData.clips.Count > 0)
            {
                return;
            }
            GlobalDataMgr.Instance.MenuData = BuildTestData();
        }

        /// <summary>构造测试菜单数据（父 id 已修正，子项 parentId 与根菜单 id 严格匹配）。</summary>
        static MenuData BuildTestData()
        {
            MenuData menuData = new MenuData();

            // 根菜单
            menuData.clips.Add(new MenuClip("rootOne", "一级菜单1"));
            menuData.clips.Add(new MenuClip("rootTwo", "一级菜单2"));
            menuData.clips.Add(new MenuClip("rootThree", "一级菜单3"));

            // 一级菜单1 的子菜单（parentId = "rootOne"）
            menuData.clips.Add(new MenuClip("rootOneChildMenu1", "二级菜单1-1") { parentId = "rootOne" });
            menuData.clips.Add(new MenuClip("rootOneChildMenu2", "二级菜单1-2") { parentId = "rootOne" });
            menuData.clips.Add(new MenuClip("rootOneChildMenu3", "二级菜单1-3") { parentId = "rootOne" });

            // 一级菜单2 的子菜单（parentId = "rootTwo"）
            menuData.clips.Add(new MenuClip("rootTwoChildMenu4", "二级菜单2-1") { parentId = "rootTwo" });
            menuData.clips.Add(new MenuClip("rootTwoChildMenu5", "二级菜单2-2") { parentId = "rootTwo" });

            // 一级菜单3 的子菜单（parentId = "rootThree"）
            menuData.clips.Add(new MenuClip("rootThreeChildMenu6", "二级菜单3-1") { parentId = "rootThree" });
            menuData.clips.Add(new MenuClip("rootThreeChildMenu7", "二级菜单3-2") { parentId = "rootThree" });
            menuData.clips.Add(new MenuClip("rootThreeChildMenu8", "二级菜单3-3") { parentId = "rootThree" });
            menuData.clips.Add(new MenuClip("rootThreeChildMenu9", "二级菜单3-4") { parentId = "rootThree" });

            return menuData;
        }

        /// <summary>进入根层级并装配。</summary>
        void EnterRoot()
        {
            currentParent = null;
            currentClips.Clear();
            currentClips.AddRange(GlobalDataMgr.GetRootMenus());
            selectedClip = currentClips.Count > 0 ? currentClips[0] : null;
            if (View != null)
            {
                View.Init(currentClips, 0);
            }
        }

        /// <summary>进入指定菜单的子层级并装配。</summary>
        void EnterChildren(MenuClip parent)
        {
            currentParent = parent;
            currentClips.Clear();
            currentClips.AddRange(GlobalDataMgr.GetChildMenus(parent));
            selectedClip = currentClips.Count > 0 ? currentClips[0] : null;
            if (View != null)
            {
                View.Init(currentClips, 0);
            }
        }

        /// <summary>返回上一层级（父层级），已处于根层级时忽略。</summary>
        public void GoBack()
        {
            if (currentParent == null)
            {
                return;
            }
            MenuClip goToParent = currentParent;
            MenuClip goToParentParent = null;
            var menuData = GlobalDataMgr.GetMenuData();
            if (menuData != null)
            {
                goToParentParent = menuData.GetParentClip(goToParent);
            }
            // 回到父层级，焦点定位在刚下钻的那个父菜单上
            EnterLevel(goToParentParent);
            selectedClip = goToParent;
            if (View != null)
            {
                View.Init(currentClips, currentClips.IndexOf(goToParent));
            }
        }

        /// <summary>
        /// 按父菜单装配指定层级（不强制重设焦点）。
        /// </summary>
        void EnterLevel(MenuClip parent)
        {
            currentParent = parent;
            currentClips.Clear();
            if (parent == null)
            {
                currentClips.AddRange(GlobalDataMgr.GetRootMenus());
            }
            else
            {
                currentClips.AddRange(GlobalDataMgr.GetChildMenus(parent));
            }
        }

        /// <summary>
        /// View 上报：用户选中了某个菜单。
        /// 有子菜单则下钻；否则视为最终选中（当前仅记录，可在此扩展打开内容等）。
        /// </summary>
        void OnMenuSelected(MenuClip clip)
        {
            if (clip == null)
            {
                return;
            }
            selectedClip = clip;
            var menuData = GlobalDataMgr.GetMenuData();
            bool hasChildren = menuData != null && menuData.HasChildren(clip);
            if (hasChildren)
            {
                EnterChildren(clip);
            }
            else
            {
                // 最终选中（叶子菜单）→ 进入对应项目任务（默认进入 UI 状态）
                EnterTask(clip);
            }
        }

        /// <summary>
        /// 叶子菜单 → 进入任务：解析绑定的项目，写入当前项目，选第一个激活的任务，
        /// 先切状态（SceneState.UI）再发任务类型事件（GlobalUIMgr 据此重建任务面板，TaskListController 同步状态）。
        /// 事件驱动：发布方只管发，监听方 GlobalUIMgr / TaskListController 均为常驻对象。
        /// </summary>
        void EnterTask(MenuClip menuClip)
        {
            if (menuClip == null)
            {
                return;
            }

            // 解析项目：优先 clip 直接引用；否则按 projectId 从 ProjectData 查询
            ProjectClip project = menuClip.clip;
            if (project == null && !string.IsNullOrEmpty(menuClip.projectId))
            {
                project = GlobalDataMgr.GetProjectClip(menuClip.projectId);
            }
            if (project == null)
            {
                Debug.LogWarning($"[MenuController] 菜单「{menuClip.displayName}」未绑定项目（clip / projectId 均为空），无法进入任务");
                return;
            }

            // 写入当前项目（任务面板 / 任务列表从此读取）
            GlobalDataMgr.Instance.ProjectData.currentClip = project;

            // 过滤未激活任务，取第一个启用项作为默认进入的任务
            TaskType firstActive = TaskType.None;
            foreach (var task in project.Tasks)
            {
                if (task.TaskActive)
                {
                    firstActive = task.TaskType;
                    break;
                }
            }

            // 先切状态（当前默认全部进入 UI 状态），再发任务类型（GlobalUIMgr.OnTaskTypeChanged 重建任务面板）
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.UI));
            EventBus<TaskTypeChangeEventData>.Publish(new TaskTypeChangeEventData(project, firstActive));

            Debug.Log($"[MenuController] 进入项目「{project.displayName}」，默认任务 {firstActive}");
        }

        /// <summary>当前选中菜单在当前层级列表中的索引；取不到返回 0。</summary>
        int GetSelectedIndex()
        {
            if (selectedClip == null)
            {
                return 0;
            }
            int index = currentClips.IndexOf(selectedClip);
            return index < 0 ? 0 : index;
        }
    }
}
