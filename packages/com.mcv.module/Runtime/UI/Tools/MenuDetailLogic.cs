using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MCV_Module.Managers;
using MCV_Module.Models.Project;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Tools
{
    /// <summary>
    /// 菜单子目录（Detail）逻辑（一般类，非 MonoBehaviour）。
    /// 负责子目录按钮的构建、显隐动画与"静止显示 / 滚动消失"的状态编排。
    /// 不持有 MonoBehaviour 协程，由 MenuPanel 用 StartCoroutine 启动本类提供的协程方法，
    /// 本类通过 IsVisible / IsAnimating 状态告知调用方是否需要继续动画。
    /// </summary>
    public class MenuDetailLogic
    {
        #region 依赖（由 MenuPanel 注入）
        public Transform detailParent;     // 子目录按钮的挂载父节点
        public string btnPrefabPath;       // 子目录按钮预制体 Resources 路径
        public float animDuration = 1f;    // 单个按钮显隐动画时长
        #endregion

        #region 状态
        readonly List<Button> detailBtns = new List<Button>();
        public MenuClip LastParent { get; private set; }
        public bool IsAnimating { get; private set; }
        /// <summary>当前是否处于"已显示、待滚动消失"状态。</summary>
        public bool IsVisible { get; private set; }
        #endregion

        #region 事件
        /// <summary>子目录按钮被点击时触发（上报选中的菜单），由 MenuPanel 订阅并转发给 Controller。</summary>
        public event Action<MenuClip> OnDetailSelected;
        #endregion

        /// <summary>
        /// 静止（吸附完成）时调用：以中心选中的父菜单为准刷新并显示子目录。
        /// 内容变化才重建按钮；统一播放显示动画。返回协程由 MenuPanel 启动。
        /// </summary>
        public IEnumerator ShowRoutine(MenuClip centerClip)
        {
            if (centerClip == null)
            {
                IsVisible = false;
                yield return AnimRoutine(false);
                yield break;
            }
            bool hasChildren = GlobalDataMgr.GetChildMenus(centerClip).Count > 0;
            bool contentChanged = centerClip != LastParent;
            LastParent = centerClip;
            if (contentChanged)
            {
                Rebuild(centerClip);
            }
            IsVisible = hasChildren;   // 有子目录则视为已显示（滚动时需消失）
            yield return AnimRoutine(hasChildren);
        }

        /// <summary>
        /// 滚动中（脱离静止）调用：播放子目录消失动画。返回协程由 MenuPanel 启动。
        /// </summary>
        public IEnumerator HideRoutine()
        {
            IsVisible = false;
            yield return AnimRoutine(false);
        }

        /// <summary>当前是否有子目录按钮。</summary>
        public bool HasButtons => detailBtns != null && detailBtns.Count > 0;

        #region 私有方法
        /// <summary>重建子目录按钮列表（数据为当前父菜单的子菜单）。</summary>
        void Rebuild(MenuClip parent)
        {
            ClearChildren(detailParent);
            detailBtns.Clear();
            if (parent == null)
            {
                return;
            }
            var clips = GlobalDataMgr.GetChildMenus(parent);
            for (int i = 0; i < clips.Count; i++)
            {
                Button btn = CreateBtn(clips[i], i);
                if (btn == null)
                {
                    continue;
                }
                detailBtns.Add(btn);
                CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0;
                }
                // 初始缩放设为最小，使显示动画从 0.3 平滑放大到 1
                btn.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            }
        }

        Button CreateBtn(MenuClip clip, int dataIndex)
        {
            if (clip == null || detailParent == null)
            {
                return null;
            }
            GameObject prefab = Resources.Load<GameObject>(btnPrefabPath);
            if (prefab == null)
            {
                return null;
            }
            GameObject go = UnityEngine.Object.Instantiate(prefab, detailParent);
            if (go.transform.childCount >= 3)
            {
                var indexText = go.transform.GetChild(1).GetComponent<Text>();
                if (indexText != null)
                {
                    indexText.text = (dataIndex + 1).ToString();
                }
                var labelText = go.transform.GetChild(2).GetComponent<Text>();
                if (labelText != null)
                {
                    labelText.text = clip.displayName;
                }
            }
            Button btn = go.GetComponent<Button>();
            BindClick(btn, clip, (c) => OnDetailSelected?.Invoke(c));
            return btn;
        }

        /// <summary>子目录按钮挂载点击监听：点击上报选中的菜单（MenuPanel 转发给 Controller 处理进入任务等）。</summary>
        static void BindClick(Button btn, MenuClip clip, Action<MenuClip> callback)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => callback?.Invoke(clip));
        }

        /// <summary>子目录按钮显隐动画协程。true 淡入放大，false 淡出缩小。</summary>
        IEnumerator AnimRoutine(bool isActive)
        {
            IsAnimating = true;
            if (detailBtns == null || detailBtns.Count == 0)
            {
                IsAnimating = false;
                yield break;
            }
            var canvasGroups = detailBtns.Select(x => x.GetComponent<CanvasGroup>()).ToArray();
            // 记录每个按钮当前实际状态作为插值起点，使消失/显示都能从当前位置开始过渡
            float[] startAlphas = new float[canvasGroups.Length];
            float[] startScales = new float[canvasGroups.Length];
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                startAlphas[i] = canvasGroups[i] != null ? canvasGroups[i].alpha : 0f;
                startScales[i] = detailBtns[i] != null ? detailBtns[i].transform.localScale.x : 0.3f;
            }
            // 每一个button间隔 0.2s 开始执行；true 从前向后，false 从后向前
            float stagger = 0.2f;
            float total = 0f;
            while (total < stagger * canvasGroups.Length + animDuration)
            {
                total += Time.deltaTime;
                for (int i = 0; i < canvasGroups.Length; i++)
                {
                    int index = isActive ? i : canvasGroups.Length - 1 - i;
                    float elapsed = total - stagger * index;
                    if (elapsed <= 0f)
                    {
                        continue;
                    }
                    ApplyStep(detailBtns[index], canvasGroups[index], isActive,
                        Mathf.Clamp01(elapsed / animDuration), startAlphas[index], startScales[index]);
                }
                yield return null;
            }
            // 收尾：全部定格到目标状态
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                ApplyStep(detailBtns[i], canvasGroups[i], isActive, 1f, startAlphas[i], startScales[i]);
            }
            IsAnimating = false;
        }

        void ApplyStep(Button btn, CanvasGroup cg, bool isActive, float t, float startAlpha, float startScale)
        {
            if (btn == null)
            {
                return;
            }
            float targetAlpha = isActive ? 1f : 0f;
            float targetScale = isActive ? 1f : 0.3f;
            float eased = t * t;
            if (cg != null)
            {
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                cg.interactable = isActive && t >= 1f;
                cg.blocksRaycasts = isActive && t >= 1f;
            }
            btn.transform.localScale = new Vector3(
                Mathf.Lerp(startScale, targetScale, eased),
                Mathf.Lerp(startScale, targetScale, eased),
                1f);
        }

        void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
        #endregion
    }
}
