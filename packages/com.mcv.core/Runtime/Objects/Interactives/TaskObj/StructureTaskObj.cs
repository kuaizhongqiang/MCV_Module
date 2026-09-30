using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 本组件必须与零件 Collider 同一 GameObject——GlobalInteractiveMgr 用 raycast.collider.GetComponent&lt;InteractiveBase&gt;() 判定，挂父/子物体收不到交互
    /// <summary>结构页可交互零件：自报零件名 + 浮动提示开关（HoverOrTips 由 TaskStructureController 读）；不碰 UI</summary>
    public class StructureTaskObj : InteractiveBase
    {
        // WHY: 零件名是随模型预制体走的业务数据（每套结构模型各不相同），故不进 LanguageData 文案表、也不进 JSON——
        //      按全工程「业务数据英文列」口径就地加一列，由 Localized.Pick 在悬停时按当前语言取列（英文空 → 回退中文）。
        [Tooltip("浮动提示框要显示的零件名（结构名，中文列）")]
        [SerializeField] string structureName = string.Empty;

        [Tooltip("零件名的英文列（英文为空时回退中文列）")]
        [SerializeField] string structureNameEn = string.Empty;

        [Tooltip("true = 悬停用浮动提示框显示 StructureName；false = 不做悬停表现（高亮已从框架移除）")]
        public bool HoverOrTips = true;

        /// <summary>零件名（结构名）—— 浮动提示框的文字来源；按当前语言取中 / 英列（英文为空回退中文）。</summary>
        public string StructureName => Localized.Pick(structureName, structureNameEn);

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
        }
        #endregion

        #region 事件重写
        protected override void MoClickEvent()
        {
            // WHY: 点击本身不做任何事——能不能点、推进哪一步由 InstControlledManager 订阅 GlobalInteractionEventData 判定
        }
        #endregion
    }
}
