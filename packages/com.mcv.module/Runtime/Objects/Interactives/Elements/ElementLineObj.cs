using System.Collections;
using System.Collections.Generic;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.Elements
{
    [RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshCollider))]
    public class ElementLineObj : ElementObjBase, IEleLine
    {
        public override ElementType Type {get => ElementType.Line;}
        [Header("点列表"),SerializeField,Tooltip("如果少于2个点，则不绘制")] List<ElementPointObj> pointList = new();
        [Header("线绘制参数"),SerializeField,Tooltip("必要填写，否则不绘制")] LineDrawData lineDrawData;
        [Header("静态"),SerializeField,Tooltip("如果是静态则会在Editor/Start时绘制，否则认为是实例化线段")] bool isStatic = false;

        public bool IsStatic { get => isStatic; set => isStatic = value; }
        public LineDrawData LineDrawData { get => lineDrawData; set => lineDrawData = value; }
        public List<ElementPointObj> PointList { get => pointList; }

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        #region 生命周期
        protected override void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            data.id = gameObject.name;
            base.Awake();
            var mgr = gameObject.GetComponentInParent<ElementManagerBase>();
            if (mgr != null) mgr.RegisterLine(this);
        }

        protected override void OnDestroy()
        {
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
                // 网格顶点是本地空间，把点的世界坐标换算到线的本地空间
                points[i] = transform.InverseTransformPoint(pointList[i].transform.position);
            }

            if (lineDrawData.width <= 0 || lineDrawData.sectionSegments < 1)
            {
                Debug.LogWarning($"{name}: lineDrawData 未配置完整，跳过绘制");
                DestroyLine();
                return;
            }

            LineDraw.UpdateLine(gameObject, points, lineDrawData);
            SyncCollider();


        }

        /// <summary>
        /// 网格重建后同步 MeshCollider，保证碰撞与显示一致（创建/更新/清除都在同一时机处理）。
        /// </summary>
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
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshFilter.sharedMesh.Clear();
            }
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
            }
        }

        /// <summary>
        /// 模板匹配：本线端点（首尾）与给定两端点是否构成同一条线（顺序无关）。
        /// 供 ConditionLineConnect 判定"某模板连线是否已连上"。
        /// </summary>
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

        protected override void MoClickEvent()
        {
            // 连线交互由控制器/任务模式驱动（LineConnection），此处预留
        }

        protected override void MoClickDoubleEvent()
        {
            DestroyLine();
        }
        #endregion
    }
}
