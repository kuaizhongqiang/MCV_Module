using System.Collections;
using MCV_Module.Utils;
using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    [RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshCollider))]
    /// <summary>已连接导线：两端子之间的管状网格，负责建/清网格与碰撞，并做端点对匹配。</summary>
    public class ElementLineObj : ElementObjBase, IEleLine
    {
        public override ElementType Type {get => ElementType.Line;}
        [Header("点列表"),SerializeField,Tooltip("如果少于2个点，则不绘制")] List<ElementPointObj> pointList = new();
        [Header("线绘制参数"),SerializeField,Tooltip("必要填写，否则不绘制")] LineDrawData lineDrawData;
        [Header("静态"),SerializeField,Tooltip("如果是静态则会在Editor/Start时绘制，否则认为是实例化线段")] bool isStatic = false;

        public bool IsStatic { get => isStatic; set => isStatic = value; }
        public LineDrawData LineDrawData { get => lineDrawData; set => lineDrawData = value; }
        public List<ElementPointObj> PointList { get => pointList; }

        MeshFilter meshFilter;
        MeshCollider meshCollider;

        #region 生命周期
        protected override void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            data.id = gameObject.name;
            base.Awake();
            var mgr = gameObject.GetComponentInParent<ElementManagerBase>();
            if (mgr != null) mgr.RegisterLine(this);
        }

        protected override void OnDestroy()
        {
            LineDraw.ReleaseLine(gameObject);
            base.OnDestroy();
            if (ElementManagerBase.Instance != null) ElementManagerBase.Instance.UnregisterLine(this);
        }

        protected override IEnumerator DelayInit()
        {
            var eleMgr = gameObject.GetComponentInParent<ElementManagerBase>();
            while(!eleMgr.IsInit)
            {
                yield return null;
            }
            if (pointList.Count < 2) yield break;
            var firtPoint = pointList[0];
            var lastPoint = pointList[pointList.Count - 1];
            while (!firtPoint.isInit || !lastPoint.isInit)
            {
                yield return null;
            }

            string newName = $"Line_{firtPoint.name}_{lastPoint.name}";
            gameObject.name = newName;
            data.id = newName;
            if (isStatic) CreateLine();
            isInit = true;

            eleMgr.RegisterLine(this);
        }
        #endregion

        #region 接口实现
        public void EditLinePoint(List<ElementPointObj> points)
        {
            pointList = points ?? new List<ElementPointObj>();
        }

        public void CreateLine()
        {
            if (pointList == null || pointList.Count < 2)
            {
                DestroyLine();
                return;
            }

            var points = new Vector3[pointList.Count];
            for (int i = 0; i < pointList.Count; i++)
            {
                if (pointList[i] == null)
                {
                    DestroyLine();
                    return;
                }
                // WHY: 网格顶点是本地的，必须把点的世界坐标换算进线的本地空间，否则线位置错乱
                points[i] = transform.InverseTransformPoint(pointList[i].transform.position);
            }

            if (lineDrawData.width <= 0 || lineDrawData.sectionSegments < 1)
            {
                Log.Warning($"{name}: lineDrawData 未配置完整，跳过绘制");
                DestroyLine();
                return;
            }

            LineDraw.UpdateLine(gameObject, points, lineDrawData);
            SyncCollider();


        }

        /// <summary>网格重建后同步 MeshCollider，保证碰撞与显示一致。</summary>
        void SyncCollider()
        {
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            }
        }

        public void DestroyLine()
        {
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
            // WHY: 先摘碰撞网格再释放；只销毁 LineDraw 创建的那张，预制体自带/共享网格不受影响
            if (meshCollider != null) meshCollider.sharedMesh = null;
            LineDraw.ReleaseLine(gameObject);
        }

        /// <summary>模板匹配：本线首尾端点与给定两端点是否构成同一条线（顺序无关），供 ConditionLineConnect 判定连线是否完成。</summary>
        public bool Matches(ElementPointObj a, ElementPointObj b)
        {
            if (pointList == null || pointList.Count < 2) return false;
            var first = pointList[0];
            var last = pointList[pointList.Count - 1];
            return (first == a && last == b) || (first == b && last == a);
        }

        protected override void MoEnterEvent()
        {
            Highlight(true);
        }

        protected override void MoExitEvent()
        {
            Highlight(false);
        }

        // WHY: 连线交互由控制器/任务模式（LineConnection）驱动，此处刻意留空。
        protected override void MoClickEvent()
        {
        }

        protected override void MoClickDoubleEvent()
        {
            DestroyLine();
        }
        #endregion
    }
}
