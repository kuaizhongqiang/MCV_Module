using System;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Models.Project;
using MCV_Module.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /*
        菜单面板：垂直环形封面流式滚动菜单。
        - 数据来源：由 MenuController 通过 Init(clips, selectedIndex) 注入当前层级的兄弟列表，
          本面板只负责渲染与表现，不做层级/数据决策。
        - 滚动逻辑：由一般类 MenuScrollLogic（UI/Tools）负责状态、数值计算与滚动协程
          （惯性/吸附/速度采样）；MenuPanel 用 StartCoroutine 启动，并通过回调刷新布局。
        - 交互：btnParent 区域内鼠标左键拖动跟手；滚轮触发阻尼滚动。
        - 表现：maxShowBtnCount 必须为奇数且 >=3，正中间最大 alpha=1，两端渐小渐透明；
          环形循环（首尾相接），首尾各占一个展示位置。
    */
    public class MenuPanel : PanelBase
    {
        #region 序列化参数
        [Header("引用")]
        [SerializeField] Transform btnParent;
        [SerializeField] Transform detailParent;
        [SerializeField] string menuParentBtnPath = "UI/MenuParentBtn";
        [SerializeField] string MenuDetailBtnPath = "UI/MenuDetailBtn";

        [Header("滚动参数")]
        [SerializeField] Vector2 btnSceleLimit = new Vector2(0.3f, 1f); // x=最小缩放 y=最大缩放
        [SerializeField] float btnSpacing = 20f;                        // 按钮间距（随缩放参与位置计算）
        [SerializeField] int maxShowBtnCount = 7;                       // 一屏最多显示数量（奇数且>=3）
        [SerializeField] float dampingTime = 0.6f;                      // 惯性阻尼时长（越大滑得越久衰减越慢）
        [SerializeField] float dragSensitivity = 3f;                    // 鼠标拖拽灵敏度（越大跟手越快）
        [SerializeField] float wheelInitialSpeed = 4f;                  // 滚轮产生的初始惯性速度（每格）
        [SerializeField] float snapThreshold = 0.05f;                   // 惯性速度低于此值时开始吸附
        [SerializeField] float snapDuration = 0.3f;                     // 吸附动画时长
        const float minAlpha = 0.2f;                                    // 两端最小透明度

        [Header("子目录参数")]
        [SerializeField] float detailAnimDuration = 0.4f;                 // 显隐效果总时长
        #endregion

        #region 私有参数
        readonly MenuScrollLogic scrollLogic = new MenuScrollLogic();   // 滚动逻辑（状态 + 协程）
        readonly MenuDetailLogic detailLogic = new MenuDetailLogic();   // 子目录逻辑（状态 + 协程）
        Coroutine scrollRoutine;     // 当前惯性/吸附协程
        Coroutine inputRoutine;      // 输入轮询协程（拖动 + 滚轮）
        Coroutine detailAnimRoutine; // 子目录按钮显隐效果协程
        bool dragging;               // 是否正在拖动
        float dragStartY;            // 拖动起点鼠标 Y
        float dragStartFocus;        // 拖动起点焦点

        List<MenuClip> currentClips = new List<MenuClip>();   // 当前层级兄弟列表（数据）
        readonly List<MenuSlot> slots = new List<MenuSlot>(); // 固定槽位（环形窗口）
        float btnHeight = 100f;      // 实例化后按实际按钮高度覆盖
        float containerHeight = 0f;  // btnParent 容器高度，用于中心锚点偏移

        int SlotCount => MenuScrollLogic.ClampOdd(maxShowBtnCount, 3);
        int CenterSlot => SlotCount / 2;
        int HalfSlots => SlotCount / 2;
        #endregion

        #region 公开事件
        /// <summary>Controller 订阅：用户选中某菜单。</summary>
        public event Action<MenuClip> OnMenuSelected;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            if (btnParent == null)
            {
                Debug.LogError("[MenuPanel] 缺少 btnParent 引用", this);
                return;
            }
            var parentRect = btnParent as RectTransform;
            if (parentRect != null)
            {
                containerHeight = parentRect.rect.height;
            }

            // 注入滚动逻辑参数
            scrollLogic.dampingTime = dampingTime;
            scrollLogic.snapDuration = snapDuration;
            scrollLogic.snapThreshold = snapThreshold;
            scrollLogic.dragSensitivity = dragSensitivity;
            scrollLogic.wheelInitialSpeed = wheelInitialSpeed;

            // 滚动协程每帧推进后刷新布局；结束后清理协程引用
            scrollLogic.OnStep -= OnScrollStep;
            scrollLogic.OnStep += OnScrollStep;
            scrollLogic.OnComplete -= OnScrollComplete;
            scrollLogic.OnComplete += OnScrollComplete;

            // 注入子目录逻辑参数
            detailLogic.detailParent = detailParent;
            detailLogic.btnPrefabPath = MenuDetailBtnPath;
            detailLogic.animDuration = detailAnimDuration;
        }

        protected override void OnDestroy()
        {
            if (inputRoutine != null)
            {
                StopCoroutine(inputRoutine);
            }
            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
            }
            if (detailAnimRoutine != null)
            {
                StopCoroutine(detailAnimRoutine);
            }
            base.OnDestroy();
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 由 Controller 装配：设置当前层级列表并居中定位到 selectedIndex。
        /// 每次层级切换/面板重建后调用。
        /// </summary>
        public void Init(List<MenuClip> clips, int selectedIndex)
        {
            currentClips.Clear();
            if (clips != null)
            {
                currentClips.AddRange(clips);
            }

            if (slots.Count < SlotCount)
            {
                BuildSlots();
            }
            else if (slots.Count > SlotCount)
            {
                for (int i = slots.Count - 1; i >= SlotCount; i--)
                {
                    if (slots[i] != null && slots[i].button != null)
                    {
                        Destroy(slots[i].button.gameObject);
                    }
                }
                slots.RemoveRange(SlotCount, slots.Count - SlotCount);
            }

            scrollLogic.SetFocus(selectedIndex);   // 重新装配时直接定位，不保留旧惯性
            scrollLogic.step = btnHeight + btnSpacing;
            StopScroll();
            RefreshLayout();
            StartDetailRoutine(detailLogic.ShowRoutine(GetCenterClip()));

            if (inputRoutine == null)
            {
                inputRoutine = StartCoroutine(InputRoutine());
            }
        }
        #endregion

        #region 事件方法
        void OnScrollStep()
        {
            RefreshLayout();
            // 脱离静止（滚动推进中）：子目录若可见则播放消失动画
            if (detailLogic.IsVisible)
            {
                StartDetailRoutine(detailLogic.HideRoutine());
            }
        }

        void OnScrollComplete()
        {
            scrollRoutine = null;
            // 静止（吸附完成）：以当前中心选中的父菜单为准，刷新并显示子目录。
            StartDetailRoutine(detailLogic.ShowRoutine(GetCenterClip()));
        }
        #endregion

        #region 私有方法
        /// <summary>立即停止当前惯性/吸附协程（任何新输入接管时调用，保证输入随时响应）。</summary>
        void StopScroll()
        {
            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
                scrollRoutine = null;
            }
        }

        /// <summary>以指定初速启动惯性滚动协程（松手/滚轮调用）。可随时被打断并从当前状态续接。</summary>
        void StartInertia(float initialVelocity)
        {
            StopScroll();
            scrollLogic.StartInertia(initialVelocity);
            scrollRoutine = StartCoroutine(scrollLogic.ScrollInertia());
        }

        /// <summary>启动吸附协程：由 scrollLogic 把当前位置平滑对齐到最近整数格。</summary>
        void StartSnap()
        {
            StopScroll();
            scrollRoutine = StartCoroutine(scrollLogic.ScrollSnap());
        }

        IEnumerator InputRoutine()
        {
            while (true)
            {
                bool down = Input.GetMouseButtonDown(0);
                bool hold = Input.GetMouseButton(0);

                if (dragging)
                {
                    // 拖动中：直接跟手写 Focus，不跑惯性（拖动开始已停掉协程）
                    if (hold)
                    {
                        float dy = Input.mousePosition.y - dragStartY;
                        float newFocus = dragStartFocus - dy * scrollLogic.dragSensitivity / scrollLogic.step;
                        scrollLogic.DragUpdate(newFocus, Time.deltaTime);
                        RefreshLayout();
                    }
                    else // 松手：速度足够则惯性减速；否则直接吸附到最近整数格
                    {
                        dragging = false;
                        if (Mathf.Abs(scrollLogic.Velocity) > scrollLogic.snapThreshold)
                        {
                            StartInertia(scrollLogic.Velocity);
                        }
                        else
                        {
                            scrollLogic.ResetVelocity();
                            StartSnap();
                        }
                    }
                }
                else
                {
                    // 非拖动状态
                    if (down && MenuScrollLogic.IsPointerInside(btnParent, Input.mousePosition))
                    {
                        dragging = true;
                        StopScroll();   // 输入接管：立即停掉惯性/吸附，改为直接跟手
                        scrollLogic.ResetVelocity();
                        dragStartY = Input.mousePosition.y;
                        dragStartFocus = scrollLogic.Focus;
                    }

                    // 滚轮：速度脉冲进入惯性（方向反向）；始终可响应
                    float wheel = Input.mouseScrollDelta.y;
                    if (Mathf.Abs(wheel) > 0.01f)
                    {
                        float dir = -Mathf.Sign(wheel);
                        StartInertia(dir * scrollLogic.wheelInitialSpeed * Mathf.Abs(wheel));
                    }
                }

                yield return null;
            }
        }

        /// <summary>构建固定数量的环形槽位（绑定按钮实例、锚点、点击监听）。</summary>
        void BuildSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                GameObject prefab = Resources.Load<GameObject>(menuParentBtnPath);
                if (prefab == null)
                {
                    Debug.LogError($"[MenuPanel] 缺少必要资源：{menuParentBtnPath}");
                    return;
                }
                GameObject go = Instantiate(prefab, btnParent);
                go.name = $"MenuSlot_{i}";
                var slot = new MenuSlot();
                slot.button = go.GetComponent<Button>();
                slot.group = go.GetComponent<CanvasGroup>();
                slot.rect = go.GetComponent<RectTransform>();
                slot.dataIndex = -1;
                // 统一锚定到 btnParent 左下角、pivot 垂直居中：
                // 这样 anchoredPosition 指向按钮中心，且以容器底部为参考，
                // 中心按钮 y = containerHeight/2 即容器垂直中央，不出屏。
                if (slot.rect != null)
                {
                    slot.rect.anchorMin = new Vector2(0f, 0f);
                    slot.rect.anchorMax = new Vector2(0f, 0f);
                    slot.rect.pivot = new Vector2(0f, 0.5f);
                }
                if (slot.group == null)
                {
                    slot.group = go.AddComponent<CanvasGroup>();
                }
                if (slot.button == null)
                {
                    slot.button = go.AddComponent<Button>();
                }
                // 按钮点击：上报当前绑定的菜单
                MenuSlot captured = slot;
                slot.button.onClick.AddListener(() =>
                {
                    if (captured.dataIndex >= 0 && captured.dataIndex < currentClips.Count)
                    {
                        OnMenuSelected?.Invoke(currentClips[captured.dataIndex]);
                    }
                });
                slots.Add(slot);
            }

            // 读取实际按钮高度（首个有效槽）
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].rect != null && slots[i].rect.sizeDelta.y > 0f)
                {
                    btnHeight = slots[i].rect.sizeDelta.y;
                    break;
                }
            }
        }

        /// <summary>
        /// 按当前焦点计算每个槽位的数据绑定与视觉（位置/缩放/透明度）。
        /// 数据在焦点跨越整槽时切换，位置/缩放/透明度连续插值。
        /// </summary>
        void RefreshLayout()
        {
            if (slots.Count == 0 || currentClips.Count == 0)
            {
                return;
            }
            int count = currentClips.Count;
            float focusFloat = scrollLogic.Focus;
            int nearest = Mathf.RoundToInt(focusFloat);
            float frac = focusFloat - nearest;      // 滚动中间态的小数偏移
            float half = HalfSlots;
            float baseStep = btnHeight + btnSpacing;
            // 实时读取容器高度（布局可能延迟更新），作为中心基准
            if (btnParent != null && btnParent is RectTransform parentRect)
            {
                containerHeight = parentRect.rect.height;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                MenuSlot slot = slots[i];
                // 相对中心的浮点距离（含小数），用于连续位置
                float d = (i - CenterSlot) - frac;
                // 数据绑定：最近整数焦点 + 槽位偏移（环形取模）
                int dataIndex = MenuScrollLogic.Mod(nearest + (i - CenterSlot), count);
                slot.dataIndex = dataIndex;

                // 视觉归一化：中心 t=1，两端 t=0
                float t = 1f - Mathf.Clamp01(Mathf.Abs(d) / half);
                float scale = Mathf.Lerp(btnSceleLimit.x, btnSceleLimit.y, t);
                float alpha = Mathf.Lerp(minAlpha, 1f, t);

                if (slot.rect != null)
                {
                    // 中心槽锚定在容器高度一半处（(0, height/2)），上下分布
                    float centerOffset = containerHeight * 0.5f;
                    slot.rect.anchoredPosition = new Vector2(0f, centerOffset + d * baseStep);
                    slot.rect.localScale = new Vector3(scale, scale, 1f);
                }
                if (slot.group != null)
                {
                    slot.group.alpha = alpha;
                    // 只有中心槽可交互，避免误触两端
                    slot.group.interactable = t > 0.99f;
                    slot.group.blocksRaycasts = t > 0.99f;
                }

                // 同步文本（index 显示数据在列表中的真实索引，滚动不变）
                MenuClip clip = currentClips[dataIndex];
                SetSlotText(slot, clip, dataIndex);
            }
        }

        void SetSlotText(MenuSlot slot, MenuClip clip, int dataIndex)
        {
            if (slot.button == null || clip == null)
            {
                return;
            }
            Transform root = slot.button.transform;
            if (root.childCount >= 2)
            {
                var indexText = root.GetChild(1).GetComponent<Text>();
                var nameText = root.GetChild(2).GetComponent<Text>();
                if (indexText != null)
                {
                    indexText.text = dataIndex.ToString();
                }
                if (nameText != null)
                {
                    nameText.text = clip.displayName;
                }
            }
        }
        
        #region Detail部分
        /// <summary>取当前滚动中心选中的菜单（父目录）。</summary>
        MenuClip GetCenterClip()
        {
            if (slots.Count == 0 || currentClips.Count == 0)
            {
                return null;
            }
            int nearest = Mathf.RoundToInt(scrollLogic.Focus);
            int index = MenuScrollLogic.Mod(nearest, currentClips.Count);
            return currentClips[index];
        }

        /// <summary>启动子目录动画协程（先停掉上一个，保证输入随时响应）。</summary>
        void StartDetailRoutine(IEnumerator routine)
        {
            if (detailAnimRoutine != null)
            {
                StopCoroutine(detailAnimRoutine);
            }
            detailAnimRoutine = StartCoroutine(routine);
        }
        #endregion
        
        #endregion

        #region 内部类
        /// <summary>环形窗口的每个槽位：绑定一个按钮实例，滚动时仅切换数据绑定与视觉。</summary>
        class MenuSlot
        {
            public Button button;
            public CanvasGroup group;
            public RectTransform rect;
            public int dataIndex; // 当前绑定的数据索引
        }
        #endregion
    }
}
