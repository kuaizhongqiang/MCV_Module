using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Models.ElementInspection;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 检测点必须是本物体子物体——Editor 扫描与「两支笔同元件」判定都依赖这层父子关系
    /// <summary>被检测元件：把本元件检测点两两配对 + 每对预期读数存成 InspectionData；数据由 Inspector 检测面板生成</summary>
    public class InspectionElementObj : InteractiveBase
    {
        [SerializeField] ElementType elementType = ElementType.None;
        [SerializeField] InspectionData inspectionData = new InspectionData();

        [SerializeField, Tooltip("控制本元件「动作态」的部件（试验按钮 / 操作手柄…）：任一处于按下态 = 元件已动作，" +
                                 "配了动作态读数的接点对随之翻转（动合导通 / 动断断开）。\n" +
                                 "留空 = 本元件没有可动作的部件，读数恒取静止组")]
        List<InspectionSwitchObj> actuationSwitches = new List<InspectionSwitchObj>();

        /// <summary>缓存的"已动作"状态（由各部件 <see cref="InspectionSwitchObj.PressedChanged"/> 驱动刷新）</summary>
        bool m_Actuated;

        /// <summary>元件类型（分类 / 显示用；也决定自动配对用哪张端子对表）。</summary>
        public ElementType ElementType => elementType;

        /// <summary>检测点配对数据（运行期只读，由万用表查询；写入只在 Editor 面板里发生）。</summary>
        public InspectionData Data => inspectionData;

        /// <summary>元件此刻是否"已动作"（<see cref="actuationSwitches"/> 里任一被按下）。</summary>
        public bool IsActuated => m_Actuated;

        /// <summary>元件此刻动作态：万用表按它决定这一对点取"静止组"还是"动作组"读数</summary>
        public ElementActuationState ActuationState =>
            m_Actuated ? ElementActuationState.Actuated : ElementActuationState.Normal;

        protected override void Awake()
        {
            base.Awake();

            for (int i = 0; i < actuationSwitches.Count; i++)
            {
                var sw = actuationSwitches[i];
                if (sw == null) continue;
                sw.PressedChanged += OnActuationSwitchChanged;
            }

            RefreshActuation();
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < actuationSwitches.Count; i++)
            {
                var sw = actuationSwitches[i];
                if (sw == null) continue;
                sw.PressedChanged -= OnActuationSwitchChanged;
            }

            base.OnDestroy();
        }

        /// <summary>部件状态翻转 → 重算本元件是否已动作（只在两个部件都被按下/松开时才有边界情况，直接全量重算最省心）。</summary>
        void OnActuationSwitchChanged(bool pressed) => RefreshActuation();

        /// <summary>重算"是否已动作"：任一动作部件处于按下态即为真。</summary>
        void RefreshActuation()
        {
            bool actuated = false;

            for (int i = 0; i < actuationSwitches.Count; i++)
            {
                var sw = actuationSwitches[i];
                if (sw == null || !sw.IsPressed) continue;

                actuated = true;
                break;
            }

            m_Actuated = actuated;
        }
    }
}
