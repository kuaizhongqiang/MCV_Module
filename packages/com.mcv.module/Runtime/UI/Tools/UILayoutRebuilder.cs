using System.Collections;
using System.Collections.Generic;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Tools
{
    // WHY: 本类只存在一个理由 ——「文本 / 子物体状态变完之后怎么刷布局才是对的」。两条都是实测踩出来的，
    //       写错就表现为「第一次打开排版是错的，第二次（组件已 ready）才对」：
    //  ① 必须等一帧：TMP 形态下 TextComponent 换形态要跨帧（先禁用卸载 Legacy、等一帧才挂 TMP），未 ready 时写入只进
    //     pending 缓冲 —— 同帧直接 ForceRebuildLayoutImmediate 量到的是「空文本」的尺寸。
    //  ② 必须自下而上：面板 prefab 普遍是「子节点自带 ContentSizeFitter + 父级 LayoutGroup，且父级 childControlWidth = false
    //     ⇒ 父级量的是子节点当前的 sizeDelta」，而 LayoutRebuilder 单次重建里 PerformLayoutControl 是自上而下的 ——
    //     父级永远量到子级的旧尺寸，必须按深度由深到浅、子先父后逐个刷才收敛。
    // WHY: 面板一律走 PanelBase.RequestLayoutRebuild（它在本类之上再叠「防重入 + 失活跳过」）；本类同时提供同步入口，
    //       给不持有协程的普通类用（如 AiBubbleStruct —— UI/Tools 的约定就是"普通类不持有协程"）。
    /// <summary>UI 布局重建唯一实现：等一帧（等 TextComponent 装配 / 文本落地）→ ForceUpdateCanvases → 按深度由深到浅逐个强制重建。</summary>
    public static class UILayoutRebuilder
    {
        #region 同步
        /// <summary>同步重建一棵子树：收集 root 下带 LayoutGroup / ContentSizeFitter 的节点，按深度降序逐个刷（子先父后），最后兜底刷 root。</summary>
        public static void RebuildSubtree(Transform root)
        {
            if (root == null) return;

            // WHY: 未激活层级下 LayoutRebuilder 会把布局组件当"被禁用"直接跳过（StripDisabledBehavioursFromList），
            //       留一条 Verbose，免得又出现"刷了却没生效"这种无痕问题。
            if (!root.gameObject.activeInHierarchy)
                Log.Verbose($"[UILayoutRebuilder] 目标层级未激活，本次重建不会生效：{root.name}");

            List<Entry> entries = new List<Entry>();
            Collect(root, 0, entries);
            entries.Sort(CompareByDepthDesc);

            for (int i = 0; i < entries.Count; i++)
                LayoutRebuilder.ForceRebuildLayoutImmediate(entries[i].rect);

            // WHY: 兜底再刷一次 root 本身：root 带控制器时它已在 entries 里（重复刷无害），不带时也不会漏掉调用方指定的那一层。
            if (root is RectTransform rootRect) LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        /// <summary>同步沿父链由下往上逐级重建（给"只知道自己那个文本节点"的调用点）；stopAt 为向上刷到的那一层（含它）。</summary>
        public static void RebuildChain(Transform node, Transform stopAt = null)
        {
            if (node == null) return;

            if (!node.gameObject.activeInHierarchy)
                Log.Verbose($"[UILayoutRebuilder] 目标层级未激活，本次重建不会生效：{node.name}");

            Transform current = node;
            while (current != null)
            {
                if (current is RectTransform rect) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                if (stopAt != null && current == stopAt) return;
                current = current.parent;
            }
        }
        #endregion

        #region 协程（供面板宿主 StartCoroutine）
        /// <summary>协程：等一帧 → 刷一次画布 → 子树重建。</summary>
        public static IEnumerator RebuildSubtreeNextFrame(Transform root)
        {
            // WHY: 这一帧就是留给 TextComponent 的（挂 TMP、把 pending 文本落到控件上）
            yield return null;
            Canvas.ForceUpdateCanvases();
            RebuildSubtree(root);
        }

        /// <summary>协程：等一帧 → 刷一次画布 → 沿父链逐级重建。</summary>
        public static IEnumerator RebuildChainNextFrame(Transform node, Transform stopAt = null)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            RebuildChain(node, stopAt);
        }
        #endregion

        #region 收集
        /// <summary>待重建节点：节点本身 + 它在子树里的深度（深度用于排序，保证子先父后）。</summary>
        struct Entry
        {
            public RectTransform rect;
            public int depth;
        }

        // WHY: 缓存成静态委托 —— 每次重建都新建闭包会白白产生 GC，而重建是高频（翻页 / 悬停 / 流式文本）调用。
        static readonly System.Comparison<Entry> CompareByDepthDesc = (a, b) => b.depth.CompareTo(a.depth);

        /// <summary>递归收集子树里带布局控制器的节点：LayoutGroup（改子物体）与 ContentSizeFitter（改自身）。</summary>
        static void Collect(Transform node, int depth, List<Entry> entries)
        {
            if (node is RectTransform rect &&
                (node.GetComponent<LayoutGroup>() != null || node.GetComponent<ContentSizeFitter>() != null))
            {
                entries.Add(new Entry { rect = rect, depth = depth });
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Collect(node.GetChild(i), depth + 1, entries);
            }
        }
        #endregion
    }
}
