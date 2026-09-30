using System.Collections;
using MCV_Module.Utils;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class TaskListPanel : PanelBase
    {
        [SerializeField] Transform taskToggleParent;
        [Header("选中高亮"), Tooltip("高亮图的取得方式见 FindMask()；所有步骤项共用同一预制体，故只在 Awake 查一次并缓存")]
        [SerializeField] Color selectedColor = Color.white;
        [SerializeField] Color normalColor = Color.clear;
        ProjectClip currentProjectClip;
        /// <summary>当前显示的任务类型：由 Controller 注入（Init / SetTaskType），面板只用于显示与去重</summary>
        TaskType m_CurrentTaskType = TaskType.None;
        /// <summary>步骤项列表：与 GetActiveTasks() 按下标一一对应</summary>
        readonly List<StepItem> m_Items = new List<StepItem>();
        readonly float hideYFloat = -130f;
        bool isActiveNow = true;
        bool m_TargetActive = true;   // WHY: 当前动画/静止所朝向的目标状态，用于防重复触发

        /// <summary>单个步骤项：同时兼容 Toggle（Resources/UI/TaskToggle）与 Button 两种美术实现，只按子物体顺序装配。</summary>
        class StepItem
        {
            public Toggle toggle;
            public Button button;
            /// <summary>选中高亮图；来源 = FindMask()（Awake 装配时查一次并缓存，运行期不再查）</summary>
            public Image mask;

            public GameObject Go
            {
                get
                {
                    if (toggle != null) return toggle.gameObject;
                    return button != null ? button.gameObject : null;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (taskToggleParent == null)
            {
                Log.Error($"[TaskListPanel] 缺少必要组件", this);
                return;
            }

            m_Items.Clear();
            for (int i = 0; i < taskToggleParent.childCount; i++)
            {
                Transform child = taskToggleParent.GetChild(i);
                Toggle toggle = child.GetComponentInChildren<Toggle>(true);   // 含自身
                Button button = child.GetComponentInChildren<Button>(true);
                if (toggle == null && button == null)
                {
                    Log.Error($"[TaskListPanel] 步骤项 {child.name} 上既没有 Toggle 也没有 Button，点击不会生效", this);
                    continue;
                }
                m_Items.Add(new StepItem
                {
                    toggle = toggle,
                    button = button,
                    // WHY: 每个步骤项查一次并缓存（所有项共用同一预制体，查法只写在 FindMask 里）
                    mask = toggle != null ? FindMask(toggle) : FindMask(button)
                });
            }

            if (m_Items.Count == 0)
            {
                Log.Error("[TaskListPanel] taskToggleParent 下没有可用的步骤项，步骤导航不会生效", this);
            }
            else if (m_Items[0].mask == null)
            {
                // 只是提示：FindMask 返回 null 时选中态会回退到 UGUI 选中态（见 SetItemSelected），不影响点击
                Log.Warning("[TaskListPanel] 没取到选中高亮图（FindMask 返回 null），选中态回退到 UGUI 选中态");
            }

            m_TargetActive = isActiveNow;
            ActiveState(isActiveNow);
        }

        // WHY: 所有步骤项共用同一预制体，查到的必须是同一层级那张图；找不到可返回 null（回退 UGUI 选中态，不报错）
        /// <summary>取 Toggle 步骤项的选中高亮图；Awake 每个项调一次并缓存，运行期不再调用。</summary>
        Image FindMask(Toggle toggle)
        {
            var image = toggle.transform.GetChild(0).GetChild(1).GetComponent<Image>();
            return image;
        }

        /// <summary>【你来写】同上，Button 美术（如 Assets/Prefabs/UI/TaskLisClipBtn）版本。两处查找规则可以共用。</summary>
        Image FindMask(Button button)
        {
            var image = button.transform.GetChild(0).GetChild(1).GetComponent<Image>();
            return image;
        }

        /// <summary>由控制器在每次面板绑定后调用：按任务列表装配步骤项、勾选当前任务并挂切换监听（只装配启用的任务）。</summary>
        public void Init(ProjectClip project, TaskType taskType)
        {
            if (project == null) return;
            currentProjectClip = project;
            m_CurrentTaskType = taskType;

            var activeTasks = GetActiveTasks();

            for (int i = 0; i < m_Items.Count; i++)
            {
                StepItem item = m_Items[i];
                bool used = i < activeTasks.Count;

                if (item.Go != null) item.Go.SetActive(used);
                if (!used)
                {
                    // 多余项不参与交互，也不保留旧绑定
                    UnbindItem(item);
                    continue;
                }

                TaskType type = activeTasks[i].TaskType;
                BindItem(item, type);
                // WHY: 初始显示不触发切换回调（显示与逻辑分离：切换只由用户点击驱动）
                SetItemSelected(i, type == taskType);
            }
        }

        // WHY: 装配（Init）与显示刷新（SetTaskType）必须用同一口径，否则步骤项与任务会错位
        /// <summary>取启用的任务列表（过滤 taskActive=false 的项）。</summary>
        List<TaskDataBase> GetActiveTasks()
        {
            var result = new List<TaskDataBase>();
            if (currentProjectClip == null) return result;
            var tasks = currentProjectClip.Tasks;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].TaskActive) result.Add(tasks[i]);
            }
            return result;
        }

        /// <summary>挂点击监听：Toggle 走 onValueChanged（只认打开），Button 走 onClick。先清后挂防重复。</summary>
        void BindItem(StepItem item, TaskType type)
        {
            if (item.toggle != null)
            {
                item.toggle.onValueChanged.RemoveAllListeners();
                item.toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn) SelectTask(type);
                });
            }
            else if (item.button != null)
            {
                item.button.onClick.RemoveAllListeners();
                item.button.onClick.AddListener(() => SelectTask(type));
            }
        }

        void UnbindItem(StepItem item)
        {
            if (item.toggle != null) item.toggle.onValueChanged.RemoveAllListeners();
            else if (item.button != null) item.button.onClick.RemoveAllListeners();
        }

        /// <summary>点击步骤项 → 切换任务类型；与面板自己记录的显示态相同则忽略（显示态由控制器注入）。</summary>
        void SelectTask(TaskType type)
        {
            if (currentProjectClip == null) return;
            if (type == m_CurrentTaskType) return;

            EventBus<TaskTypeChangeEventData>.Publish(new TaskTypeChangeEventData(currentProjectClip, type));
        }

        // WHY: 只改显示不改逻辑，Toggle 走 SetIsOnWithoutNotify 避免回环；选中表现全由 isOn 驱动，不用 Selectable 的 Selected（那指 EventSystem 选中）
        /// <summary>刷新单个步骤项的选中显示（高亮图 → toggle.graphic → EventSystem 选中态依次回退）。</summary>
        void SetItemSelected(int index, bool isOn)
        {
            if (index < 0 || index >= m_Items.Count) return;
            StepItem item = m_Items[index];

            if (item.toggle != null)
            {
                item.toggle.SetIsOnWithoutNotify(isOn);
            }

            if (item.mask != null)
            {
                item.mask.color = isOn ? selectedColor : normalColor;
                return;
            }

            if (isOn && item.toggle != null && item.toggle.graphic != null) return;   // 交给 UGUI 按 graphic 淡入淡出

            if (isOn && item.Go != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(item.Go);
            }
        }

        /// <summary>按任务类型刷新整个列表的显示状态（仅显示，不发布事件）。</summary>
        public void SetTaskType(TaskType taskType)
        {
            if (currentProjectClip == null) return;
            m_CurrentTaskType = taskType;
            var tasks = GetActiveTasks();
            for (int i = 0; i < m_Items.Count; i++)
            {
                SetItemSelected(i, i < tasks.Count && tasks[i].TaskType == taskType);
            }
        }

        #region 覆盖Active方法
        public override void SetUIActive(bool isActive)
        {
            // WHY: 已是目标状态（静止或正在动画前往），不重复触发，避免 switch alpha 出现 0-1-0 抖动
            if (isActive == m_TargetActive) return;

            m_TargetActive = isActive;
            if (ActiveAnimCoroutine != null)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }
            ActiveAnimCoroutine = StartCoroutine(OverrideAnimCoroutine(isActive));
        }

        public override void SetUIActiveImmediately(bool isActive)
        {
            m_TargetActive = isActive;
            if (ActiveAnimCoroutine != null)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }

            ActiveState(isActive);
        }

        void ActiveState(bool isActive)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive;
                canvasGroup.alpha = isActive ? 1 : 0;
            }

            float targetY = isActive ? 0 : hideYFloat;
            var layoutRect = taskToggleParent.GetComponent<RectTransform>();
            Vector2 targetPos = new Vector2(layoutRect.anchoredPosition.x, targetY);
            layoutRect.anchoredPosition = targetPos;
        }

        IEnumerator OverrideAnimCoroutine(bool isActive)
        {
            isAnimating = true;
            float time = 0f;
            float currentLayoutAlpha = canvasGroup != null ? canvasGroup.alpha : (isActive ? 0 : 1);
            float targetLayoutAlpha = isActive ? 1 : 0;
            float targetY = isActive ? 0 : hideYFloat; 
            Vector2 currentPos = taskToggleParent.GetComponent<RectTransform>().anchoredPosition;
            Vector2 targetPos = new Vector2(currentPos.x, targetY);
            while (time < animTime)
            {
                time += Time.deltaTime;
                float t = time / animTime;
                taskToggleParent.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(currentPos, targetPos, t);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(currentLayoutAlpha, targetLayoutAlpha, t);
                yield return null;
            }

            ActiveState(isActive);

            ActiveAnimCoroutine = null;
            isAnimating = false;
        }
        #endregion
    }
}
