
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    public class ElementPointObj : ElementObjBase, IElePoint
    {
        public override ElementType Type {get => ElementType.Point;}
        [SerializeField] ElementPointNameType pointType = ElementPointNameType.None;
        [Header("连线绘制参数"),SerializeField,Tooltip("拖线预览与实例化连线共用；未配置时使用默认值")] LineDrawData lineData;
        GameObject tmpLine = null;

        protected override void Awake()
        {
            base.Awake();
            HighlightPluginInit();
        }

        protected override IEnumerator DelayInit()
        {
            var element = transform.parent.GetComponentInParent<ElementObjBase>();
            if (element == null) yield break;
            while(element.isInit == false)
            {
                yield return null;
            }

            gameObject.name = GetName();
            data.id = gameObject.name;
            isInit = true;
        }

        #region 接口实现
        /// <summary>
        /// 创建一条从本点出发的临时拖线（起始点重合），返回该对象供后续 UpdateTmpLine 更新。
        /// </summary>
        public GameObject CreateTmpLine()
        {
            return CreateTmpLine(GetDrawData());
        }

        /// <summary>
        /// 以显式绘制参数创建临时拖线（供连线管理器传入统一的临时线参数）。
        /// </summary>
        public GameObject CreateTmpLine(LineDrawData data)
        {
            DestroyLine();
            tmpLine = LineDraw.CreateLine(name + "_tmp", new Vector3[] { transform.position, transform.position }, data);
            return tmpLine;
        }

        /// <summary>
        /// 更新临时拖线：本点 → 当前悬停的目标点（无目标或悬停自身时收缩回本点）。
        /// </summary>
        public void UpdateTmpLine(GameObject line)
        {
            if (line == null) return;
            var target = GetHoverPoint();
            Vector3 to = (target != null && target != this) ? target.transform.position : transform.position;
            UpdateTmpLine(line, to, GetDrawData());
        }

        /// <summary>
        /// 更新临时拖线到显式终点（供连线管理器传虚拟平面交点等动态端点）。
        /// </summary>
        public void UpdateTmpLine(GameObject line, Vector3 to)
        {
            UpdateTmpLine(line, to, GetDrawData());
        }

        /// <summary>
        /// 以显式绘制参数更新临时拖线到显式终点（供连线管理器传入统一的临时线参数）。
        /// </summary>
        public void UpdateTmpLine(GameObject line, Vector3 to, LineDrawData data)
        {
            if (line == null) return;
            LineDraw.UpdateLine(line, new Vector3[] { transform.position, to }, data);
        }

        /// <summary>
        /// 结束连线：若悬停在有效目标点上，则实例化正式 ElementLineObj 并绘制，随后销毁临时线。
        /// 无目标或悬停自身时视为取消。
        /// </summary>
        public void CreateLine()
        {
            CreateLine(GetHoverPoint());
        }

        /// <summary>
        /// 以显式目标点结束连线（供连线管理器传入已确认的目标点，避免二次解析悬停）。
        /// target 为 null 或自身时视为取消。
        /// </summary>
        public void CreateLine(ElementPointObj target)
        {
            if (target == null || target == this)
            {
                DestroyLine();
                return;
            }

            var go = new GameObject($"{name}_{target.name}");
            var mgr = GetComponentInParent<ElementManagerBase>();
            go.transform.SetParent(mgr != null ? mgr.transform : null);
            go.transform.position = transform.position;

            var line = go.AddComponent<ElementLineObj>();
            line.EditLinePoint(new List<ElementPointObj> { this, target });
            line.LineDrawData = GetDrawData();
            line.CreateLine();

            DestroyLine();
        }

        /// <summary>
        /// 销毁临时拖线。
        /// </summary>
        public void DestroyLine()
        {
            if (tmpLine == null) return;
            if (Application.isPlaying) Destroy(tmpLine);
            else DestroyImmediate(tmpLine);
            tmpLine = null;
        }
        #endregion

        /// <summary>
        /// 当前鼠标悬停的连线目标点（通过 GlobalInteractiveMgr.Current 查询）。
        /// </summary>
        ElementPointObj GetHoverPoint()
        {
            var current = GlobalInteractiveMgr.Instance != null ? GlobalInteractiveMgr.Instance.Current : null;
            return current as ElementPointObj;
        }

        /// <summary>
        /// 获取绘制参数；未配置时返回可用的默认值（细线）。
        /// </summary>
        public LineDrawData GetDrawData()
        {
            if (lineData.width <= 0 || lineData.sectionSegments < 1)
            {
                return new LineDrawData
                {
                    width = 0.003f,
                    sectionSegments = 20,
                    RadialSegments = 8,
                    material = null,
                };
            }
            return lineData;
        }

        protected override void MoEnterEvent()
        {
            Highlight(true);
        }

        protected override void MoExitEvent()
        {
            Highlight(false);
        }

        protected override void MoClickEvent()
        {
            // 连线交互由控制器/任务模式驱动（LineConnection），此处预留
        }

        protected override string GetName()
        {
            var element = transform.parent.GetComponentInParent<ElementObjBase>();
            string result = element.name + "_" + ElementNameMap.GetName(pointType);
            return result;
        }
    }
}
