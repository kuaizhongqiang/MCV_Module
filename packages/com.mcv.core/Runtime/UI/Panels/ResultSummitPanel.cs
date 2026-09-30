using System;
using System.Collections.Generic;
using MCV_Module.UI.Tools;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    // WHY: 纯展示 + 交互, 不持有业务逻辑; 行定位靠 labelRoot 子物体名(不靠顺序), 改名或漏行会立刻打日志并错位。
    /// <summary>成绩预览面板（View）——纯展示 + 交互：左侧 11 行「标签 + 数值」+ 实验步骤记录 + 提交/关闭。</summary>
    [RequireController(typeof(MCV_Module.Controllers.ResultSummitController))]
    public class ResultSummitPanel : PanelBase
    {
        [Header("引用")]
        [Tooltip("11 行的父节点（预制体里的 LeftPart）")]
        [SerializeField] Transform labelRoot;
        [Tooltip("「实验步骤记录」多行文本（预制体里的 ContentText）")]
        [SerializeField] Text recordContentText;
        [SerializeField] Button submitBtn;
        [SerializeField] Button cancelBtn;

        // WHY: 提前缓存组件 —— TMP 形态下组件会卸载节点上的 Legacy Text，recordContentText 随后成"假 null"，
        // 静态入口 SetTextOn 会静默 no-op（记录文本一个字都不写），只有持有组件才写得进去。
        /// <summary>「实验步骤记录」文本节点上的组件（缓存见上）。</summary>
        TextComponent m_RecordContentTextComp;

        /// <summary>提交按钮点击。</summary>
        public event Action OnSubmitClicked;
        /// <summary>关闭按钮点击。</summary>
        public event Action OnCloseClicked;

        /// <summary>行名（= 预制体行物体名）→ 行对象。</summary>
        readonly Dictionary<string, ResultLabelStruct> rowDict = new Dictionary<string, ResultLabelStruct>();

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            // WHY: 在调用之前解析一次（此刻节点上的 Legacy Text 还在，GetComponent 稳定可用）—— 换过形态后字段成"假 null"。
            // 放在下面那段"引用缺失就 return"之前：别的引用漏配也不该连带跳过组件缓存。
            if (recordContentText != null) m_RecordContentTextComp = recordContentText.GetComponent<TextComponent>();

            // WHY: recordContentText 的判空要连组件一起看 —— TMP 形态下组件会卸载节点上的 Legacy Text，"字段为 null"是**正常状态**，
            // 只看字段会把面板整个判成"引用缺失"（连按钮绑定与建行都跳过），组件在就等于引用还在。
            if (labelRoot == null || (recordContentText == null && m_RecordContentTextComp == null) || submitBtn == null || cancelBtn == null)
            {
                Log.Error("[ResultSummitPanel] 缺少必要引用（LabelRoot / RecordContentText / SubmitBtn / CancelBtn），请在预制体上挂全；面板将不可用", this);
                return;
            }

            submitBtn.onClick.AddListener(HandleSubmitClick);
            cancelBtn.onClick.AddListener(HandleCloseClick);
            BuildRows();
        }

        protected override void OnDestroy()
        {
            if (submitBtn != null) submitBtn.onClick.RemoveListener(HandleSubmitClick);
            if (cancelBtn != null) cancelBtn.onClick.RemoveListener(HandleCloseClick);

            OnSubmitClicked = null;
            OnCloseClicked = null;
            rowDict.Clear();
            base.OnDestroy();
        }
        #endregion

        #region 对外接口（供 Controller / 打开方调用）
        /// <summary>按展示数据渲染并显示。</summary>
        public void Show(ResultSummitViewData viewData)
        {
            if (viewData == null)
            {
                Log.Error("[ResultSummitPanel] Show 收到空的展示数据，面板内容未刷新");
                return;
            }

            int filled = 0;
            foreach (KeyValuePair<string, ResultLabelStruct> pair in rowDict)
            {
                string label = viewData.GetLabel(pair.Key);
                if (label == null)
                {
                    // WHY: 预制体有这行、展示数据没有 → 通常是行被改名或数据侧漏配
                    Log.Warning($"[ResultSummitPanel] 预制体里的行「{pair.Key}」在展示数据中没有对应项，已跳过");
                    continue;
                }

                pair.Value.SetLabel(label);
                pair.Value.SetValue(viewData.GetValue(pair.Key));
                filled++;
            }

            if (filled < viewData.RowCount)
            {
                Log.Warning($"[ResultSummitPanel] 展示数据有 {viewData.RowCount} 行，预制体只对上 {filled} 行；请检查 labelRoot 下是否改名 / 漏行");
            }

            SetRecordText(viewData.RecordText);
            SetUIActive(true);
        }

        /// <summary>关闭并隐藏（不切场景、不销毁面板）。</summary>
        public void Hide()
        {
            SetUIActive(false);
        }

        /// <summary>单独改某行的数值（行名见 <see cref="ResultSummitViewData"/> 常量）。</summary>
        public void SetRow(string rowName, string value)
        {
            if (rowDict.TryGetValue(rowName, out ResultLabelStruct row))
            {
                row.SetValue(value);
                return;
            }
            Log.Warning($"[ResultSummitPanel] 找不到行「{rowName}」，SetRow 被忽略");
        }

        /// <summary>设置「实验步骤记录」文本。</summary>
        public void SetRecordText(string text)
        {
            if (m_RecordContentTextComp != null) m_RecordContentTextComp.SetText(text ?? string.Empty);
            else if (recordContentText != null) TextComponent.SetTextOn(recordContentText, text ?? string.Empty);
        }
        #endregion

        #region 私有
        /// <summary>扫描 labelRoot 的子物体建成「行名 → 行对象」表（行名取子物体名，不依赖顺序）。</summary>
        void BuildRows()
        {
            rowDict.Clear();
            for (int i = 0; i < labelRoot.childCount; i++)
            {
                Transform child = labelRoot.GetChild(i);
                if (child == null) continue;

                if (rowDict.ContainsKey(child.name))
                {
                    Log.Warning($"[ResultSummitPanel] labelRoot 下有重名的行「{child.name}」，后一个被忽略");
                    continue;
                }

                ResultLabelStruct row = ResultLabelStruct.Create(child);
                if (row == null)
                {
                    Log.Error($"[ResultSummitPanel] 行「{child.name}」结构不符合约定（应为 [0]Label(Text) + [1]Bg/ContentText(Text)），已跳过", child);
                    continue;
                }
                rowDict.Add(child.name, row);
            }

            if (rowDict.Count == 0)
                Log.Error("[ResultSummitPanel] labelRoot 下没有解析出任何行，请确认 11 个 LabelTextPrefab 实例挂在 labelRoot 下", this);
        }

        void HandleSubmitClick() { OnSubmitClicked?.Invoke(); }

        void HandleCloseClick() { OnCloseClicked?.Invoke(); }
        #endregion
    }

    /// <summary>一行「标签 + 数值」的取值句柄（行结构：[0] Label(Text) + [1] Bg → ContentText(Text)）。</summary>
    public class ResultLabelStruct
    {
        const int LabelChildIndex = 0;
        const int ValueHolderChildIndex = 1;

        /// <summary>当前行的标签文案。</summary>
        public string label;
        /// <summary>当前行的数值文案。</summary>
        public string value;

        readonly Text labelText;
        readonly Text valueText;

        // WHY: 行对象的组件要与 labelText / valueText **在同一处**赋值时缓存（那时 Legacy Text 还在）—— 换过形态后
        // labelText / valueText 成"假 null"，静态入口静默 no-op，该行的标签与数值就永远刷不出来。
        readonly TextComponent labelTextComp;
        readonly TextComponent valueTextComp;

        ResultLabelStruct(Text labelText, TextComponent labelTextComp, Text valueText, TextComponent valueTextComp)
        {
            this.labelText = labelText;
            this.labelTextComp = labelTextComp;
            this.valueText = valueText;
            this.valueTextComp = valueTextComp;
        }

        /// <summary>按行预制体的固定结构解析出两个 Text；结构不符返回 null（不抛异常）。</summary>
        public static ResultLabelStruct Create(Transform row)
        {
            if (row == null) return null;

            Transform labelChild = row.childCount > LabelChildIndex ? row.GetChild(LabelChildIndex) : null;
            Transform valueHolder = row.childCount > ValueHolderChildIndex ? row.GetChild(ValueHolderChildIndex) : null;
            if (labelChild == null || valueHolder == null || valueHolder.childCount == 0) return null;

            Text labelText = labelChild.GetComponent<Text>();
            Text valueText = valueHolder.GetChild(0).GetComponent<Text>();

            // WHY: 组件必须在这里（Text 还活着时）从**同一个节点**取出来 —— TMP 形态下节点上只剩 TextComponent，
            // 事后懒解析会碰上"假 null"；也正因如此，"结构不符"的判定要连组件一起看，否则 TMP 形态会把每一行都当坏行跳过。
            TextComponent labelTextComp = labelChild.GetComponent<TextComponent>();
            TextComponent valueTextComp = valueHolder.GetChild(0).GetComponent<TextComponent>();
            if ((labelText == null && labelTextComp == null) || (valueText == null && valueTextComp == null)) return null;

            return new ResultLabelStruct(labelText, labelTextComp, valueText, valueTextComp);
        }

        /// <summary>设置标签文案。</summary>
        public void SetLabel(string text)
        {
            label = text ?? string.Empty;
            if (labelTextComp != null) labelTextComp.SetText(label);
            else if (labelText != null) TextComponent.SetTextOn(labelText, label);
        }

        /// <summary>设置数值文案。</summary>
        public void SetValue(string text)
        {
            value = text ?? string.Empty;
            if (valueTextComp != null) valueTextComp.SetText(value);
            else if (valueText != null) TextComponent.SetTextOn(valueText, value);
        }
    }
}
