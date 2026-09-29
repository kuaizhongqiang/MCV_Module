using System.Collections;
using System.Collections.Generic;
using MCV_Module.Objects.Tools;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 每帧只按状态快照哈希决定是否重建（原实现无条件重建是纯开销）；销毁必须 ReleaseLine 免记账残留
    /// <summary>检测线对象：按点列表 + 绘制参数生成网格线，Awake 建一次、Update 按需重建</summary>
    [RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public class InspectionLineObj : InteractiveBase
    {
        [SerializeField] List<Transform> points = new List<Transform>();
        [SerializeField] LineDrawData data = new LineDrawData();
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;

        /// <summary>上次绘制时的状态快照；与当前快照一致就跳过重建。</summary>
        int lastStateKey;
        bool hasStateKey;

        protected override void Awake()
        {
            base.Awake();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            CreateLine();
        }

        void Update()
        {
            // 按需重建：只有点 / 参数 / 自身变换变化时才重画（原实现每帧无条件重建，纯开销）
            if (!LineDataLegal()) return;
            if (hasStateKey && lastStateKey == BuildStateKey()) return;
            CreateLine();
        }

        protected override void OnDestroy()
        {
            // 释放本线独占的网格，避免记账表残留
            LineDraw.ReleaseLine(gameObject);
            base.OnDestroy();
        }

        public void CreateLine()
        {
            if (!LineDataLegal()) return;

            lastStateKey = BuildStateKey();
            hasStateKey = true;

            Vector3[] points = new Vector3[this.points.Count];
            for (int i = 0; i < this.points.Count; i++)
            {
                points[i] = this.points[i].position;
            }

            LineDraw.UpdateLine(gameObject, points, data);
        }

        // WHY: 自身变换必须计入哈希——网格顶点是本地空间、点取世界坐标，线对象一移动不重算就会整体漂移
        /// <summary>重建判定用的状态快照（无 GC 整数哈希）：点数量 + 各点世界坐标 + 自身变换 + 全部绘制参数</summary>
        int BuildStateKey()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + points.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    Transform point = points[i];
                    if (point == null) { hash = hash * 31 + i; continue; }

                    Vector3 world = point.position;
                    hash = hash * 31 + world.x.GetHashCode();
                    hash = hash * 31 + world.y.GetHashCode();
                    hash = hash * 31 + world.z.GetHashCode();
                }

                var t = transform;
                hash = hash * 31 + t.position.GetHashCode();
                hash = hash * 31 + t.rotation.GetHashCode();
                hash = hash * 31 + t.lossyScale.GetHashCode();

                hash = hash * 31 + data.width.GetHashCode();
                hash = hash * 31 + data.sectionSegments;
                hash = hash * 31 + data.RadialSegments;
                hash = hash * 31 + data.bazierOffsetDirection.GetHashCode();
                hash = hash * 31 + data.bazierOffsetDistance.GetHashCode();
                hash = hash * 31 + (data.material != null ? data.material.GetHashCode() : 0);
                return hash;
            }
        }

        bool LineDataLegal()
        {
            if (points.Count < 2) return false;
            if (data.width <= 0 || data.RadialSegments <= 3) return false;
            return true;
        }

    }
}