using System.Collections.Generic;
using UnityEngine;
using MCV_Module.Utils;

namespace MCV_Module.Objects.Tools
{
    /// <summary>
    /// 管状网格绘制工具 — 给定控制点序列与配置参数，生成带 MeshFilter/MeshRenderer 的管道 GameObject。
    /// 独立工具类，任何模块均可调用，无需挂载到场景对象。
    /// </summary>
    public static class LineDraw
    {
        // ── 工作区缓冲（主线程单线程复用；嵌套调用时回退为新分配，保证正确性）──
        // 拖线/连线重建网格是高频路径，复用数组可显著降低 GC 分配。
        static Vector3[] s_ScratchVertices;
        static Vector3[] s_ScratchNormals;
        static Vector2[] s_ScratchUv;
        static int[] s_ScratchTriangles;
        static float[] s_ScratchArcLengths;
        static int s_ScratchDepth;

        // ── 网格所有权（一个对象一张网格）──
        // MeshFilter 的网格引用会被 Instantiate 复制（副本与模板指向同一张 Mesh），预制体自带的
        // 网格也可能被多个对象共用；一旦复用这种共享网格，多条线就会互相覆盖顶点，最终只剩最后
        // 重建的那条可见。故这里按对象记账，只复用「本对象自己独占」的网格，其余一律换成新的。
        static readonly Dictionary<GameObject, Mesh> s_OwnedMeshes = new Dictionary<GameObject, Mesh>();
        static readonly List<KeyValuePair<GameObject, Mesh>> s_DeadEntries = new List<KeyValuePair<GameObject, Mesh>>();

        /// <summary>
        /// 取工作区缓冲：可复用且尺寸精确匹配时复用，否则重新分配。
        /// 尺寸必须精确匹配 —— 网格是整段数组赋值（<c>mesh.vertices = array</c>），
        /// 长度偏大会带进多余顶点与残留三角形索引。
        /// </summary>
        /// <param name="canReuse">本次是否允许复用共享缓冲（嵌套重建时为 false）</param>
        static T[] GetScratch<T>(bool canReuse, ref T[] scratch, int size)
        {
            if (!canReuse || scratch == null || scratch.Length != size)
                scratch = new T[size];
            return scratch;
        }

        /// <summary>
        /// 创建并返回一根管状网格对象。
        /// </summary>
        /// <param name="name">生成对象的名称</param>
        /// <param name="points">路径控制点序列（至少 2 个点：起点, 途经点..., 终点）</param>
        /// <param name="data">管线绘制参数</param>
        public static GameObject CreateLine(string name, Vector3[] points, LineDrawData data)
        {
            if (!ValidatePoints(points)) return null;

            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            return RebuildMesh(go, points, data);
        }

        /// <summary>
        /// 更新已有管线的控制点和/或参数，重建 Mesh 后替换到原 GameObject 上。
        /// </summary>
        /// <param name="lineObj">由 CreateLine 创建的管线对象</param>
        /// <param name="points">新的控制点序列</param>
        /// <param name="data">新的绘制参数</param>
        /// <returns>传入的 lineObj（方便链式调用）</returns>
        public static GameObject UpdateLine(GameObject lineObj, Vector3[] points, LineDrawData data)
        {
            if (lineObj == null)
            {
                Log.Error("LineDraw.UpdateLine: lineObj 不能为 null");
                return null;
            }
            if (!ValidatePoints(points)) return lineObj;

            // 确保组件存在
            if (lineObj.GetComponent<MeshFilter>() == null)
                lineObj.AddComponent<MeshFilter>();
            if (lineObj.GetComponent<MeshRenderer>() == null)
                lineObj.AddComponent<MeshRenderer>();

            return RebuildMesh(lineObj, points, data);
        }

        static bool ValidatePoints(Vector3[] points)
        {
            if (points == null || points.Length < 2)
            {
                Log.Error("LineDraw: 至少需要 2 个控制点");
                return false;
            }
            return true;
        }

        // 共享核心：用 points → 路径 → 网格，填充到 GameObject 中
        static GameObject RebuildMesh(GameObject go, Vector3[] controlPoints, LineDrawData data)
        {
            Vector3[] path = GeneratePath(controlPoints, data);
            ApplyTubeMesh(go, path, data);
            return go;
        }

        // ──────────────────────────────────────────────
        //  路径生成 — Catmull-Rom 样条 + sin² 位移场
        //  • 经过所有控制点，G1 连续
        //  • 偏移为零时退化为直线（Catmull-Rom 镜像边界特性）
        //  • 偏移非零时以 sin² 混合在段中点达到最大，端点平滑过渡
        // ──────────────────────────────────────────────

        static Vector3[] GeneratePath(Vector3[] controlPoints, LineDrawData data)
        {
            int segmentCount = controlPoints.Length - 1;
            int samplesPerSegment = Mathf.Max(1, data.sectionSegments / segmentCount);
            int totalSamples = samplesPerSegment * segmentCount + 1;

            // 预分配数组（替代 List + ToArray 的双重分配）
            var path = new Vector3[totalSamples];

            // 扩展控制点（Catmull-Rom 需要前后各多一个点做边界）
            // 首尾用镜像点确保边界段的切线自然
            var extended = new Vector3[controlPoints.Length + 2];
            extended[0] = controlPoints[0] + (controlPoints[0] - controlPoints[1]);
            for (int i = 0; i < controlPoints.Length; i++)
                extended[i + 1] = controlPoints[i];
            extended[^1] = controlPoints[^1] + (controlPoints[^1] - controlPoints[^2]);

            Vector3 offset = data.bazierOffsetDirection == Vector3.zero
                ? Vector3.zero
                : data.bazierOffsetDirection.normalized * data.bazierOffsetDistance;
            bool useDisplacement = offset != Vector3.zero;

            int idx = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 p0 = extended[i];     // Pᵢ₋₁
                Vector3 p1 = extended[i + 1]; // Pᵢ
                Vector3 p2 = extended[i + 2]; // Pᵢ₊₁
                Vector3 p3 = extended[i + 3]; // Pᵢ₊₂

                for (int s = 0; s < samplesPerSegment; s++)
                {
                    float t = (float)s / samplesPerSegment;
                    Vector3 point = CatmullRom(p0, p1, p2, p3, t);

                    if (useDisplacement)
                    {
                        // sin² 混合：端点处位移=0 且导数为0 → G1 连续
                        float blend = Mathf.Sin(Mathf.PI * t);
                        point += offset * blend * blend;
                    }

                    path[idx++] = point;
                }
            }

            path[idx] = controlPoints[^1]; // 最后一个端点（idx == totalSamples - 1）
            return path;
        }

        /// <summary>Catmull-Rom 样条插值（张力 0.5），返回 p1→p2 之间 t 处的点。</summary>
        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            float s = 0.5f;
            return s * (2f * p1
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        // ──────────────────────────────────────────────
        //  管状网格构建
        // ──────────────────────────────────────────────

        static void ApplyTubeMesh(GameObject go, Vector3[] path, LineDrawData data)
        {
            // 只有最外层重建才能复用共享缓冲：重建过程中又触发重建（嵌套）时，内层复用会把
            // 外层还没赋值进网格的数据冲掉。注意深度必须在 Core 之前判定，否则最外层也会被判成嵌套。
            bool canReuseScratch = s_ScratchDepth == 0;
            s_ScratchDepth++;
            try
            {
                ApplyTubeMeshCore(go, path, data, canReuseScratch);
            }
            finally
            {
                s_ScratchDepth--;
            }
        }

        static void ApplyTubeMeshCore(GameObject go, Vector3[] path, LineDrawData data, bool canReuseScratch)
        {
            int pathLen = path.Length;
            int radial = Mathf.Max(3, data.RadialSegments);
            float radius = data.width * 0.5f;

            int vertexCount = pathLen * radial;
            int triCount = (pathLen - 1) * radial * 6;

            // 复用工作区缓冲（嵌套调用时 GetScratch 自动回退为新分配）
            Vector3[] vertices = GetScratch(canReuseScratch, ref s_ScratchVertices, vertexCount);
            Vector3[] normals = GetScratch(canReuseScratch, ref s_ScratchNormals, vertexCount);
            Vector2[] uv = GetScratch(canReuseScratch, ref s_ScratchUv, vertexCount);
            int[] triangles = GetScratch(canReuseScratch, ref s_ScratchTriangles, triCount);

            // 累积弧长（用于 UV.v 沿路径映射；复用工作区缓冲并填充）
            float[] arcLengths = GetScratch(canReuseScratch, ref s_ScratchArcLengths, pathLen);
            arcLengths[0] = 0f;
            for (int i = 1; i < pathLen; i++)
                arcLengths[i] = arcLengths[i - 1] + Vector3.Distance(path[i], path[i - 1]);
            float totalLength = arcLengths[pathLen - 1];

            for (int i = 0; i < pathLen; i++)
            {
                Vector3 tangent = ComputeTangent(path, i);
                Quaternion rotation = Quaternion.LookRotation(tangent, Vector3.up);
                float v = totalLength > 0f ? arcLengths[i] / totalLength : (float)i / (pathLen - 1);

                for (int j = 0; j < radial; j++)
                {
                    float angle = 2f * Mathf.PI * j / radial;
                    Vector3 localPos = new(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        0f
                    );

                    int idx = i * radial + j;
                    vertices[idx] = path[i] + rotation * localPos;

                    // 法线 = 径向方向
                    Vector3 localNormal = new(localPos.x, localPos.y, 0f);
                    normals[idx] = localNormal.sqrMagnitude > 0f
                        ? (rotation * localNormal).normalized
                        : rotation * Vector3.right;

                    uv[idx] = new Vector2((float)j / radial, v);
                }
            }

            // 三角形索引
            int t = 0;
            for (int i = 0; i < pathLen - 1; i++)
            {
                for (int j = 0; j < radial; j++)
                {
                    int bl = i * radial + j;
                    int br = i * radial + (j + 1) % radial;
                    int tl = (i + 1) * radial + j;
                    int tr = (i + 1) * radial + (j + 1) % radial;

                    triangles[t++] = bl;
                    triangles[t++] = br;
                    triangles[t++] = tl;

                    triangles[t++] = br;
                    triangles[t++] = tr;
                    triangles[t++] = tl;
                }
            }

            // 应用 Mesh（本对象独占的那张才可复用，详见 s_OwnedMeshes 注释）
            var mf = go.GetComponent<MeshFilter>();
            var mr = go.GetComponent<MeshRenderer>();

            Mesh mesh = AcquireMesh(go, mf);

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            // 必须写 sharedMaterial：写 material 会在运行期实例化一份材质并写进对象，
            // 导致同材质的连线各自持有一份副本（且可能把实例材质残留进场景）。
            mr.sharedMaterial = data.material;
        }

        // ──────────────────────────────────────────────
        //  网格所有权
        // ──────────────────────────────────────────────

        /// <summary>
        /// 取得本对象用于绘线的独占网格：是它自己的就清空复用，否则（空网格、预制体自带网格、
        /// 由 Instantiate 继承来的模板网格）换一张新的，避免多条线写进同一张网格互相覆盖。
        /// </summary>
        static Mesh AcquireMesh(GameObject go, MeshFilter mf)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh != null && s_OwnedMeshes.TryGetValue(go, out var owned) && owned == mesh)
            {
                mesh.Clear();
                return mesh;
            }

            mesh = new Mesh { name = go.name + "_LineMesh" };
            s_OwnedMeshes[go] = mesh;
            mf.sharedMesh = mesh;
            PruneDestroyed();
            return mesh;
        }

        /// <summary>
        /// 释放本工具为该对象独占创建的网格（清除线、对象销毁时调用）。
        /// 预制体自带或他处共享的网格不会被销毁。
        /// </summary>
        public static void ReleaseLine(GameObject lineObj)
        {
            if (lineObj == null) return;
            if (!s_OwnedMeshes.TryGetValue(lineObj, out var owned)) return;
            s_OwnedMeshes.Remove(lineObj);
            if (owned == null) return;

            var mf = lineObj.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh == owned) mf.sharedMesh = null;
            DestroyMesh(owned);
        }

        /// <summary>清理已销毁对象残留的记账，防止表格随时间增长（对象销毁时未显式释放的兜底）。</summary>
        static void PruneDestroyed()
        {
            if (s_OwnedMeshes.Count == 0) return;

            foreach (var kv in s_OwnedMeshes)
            {
                if (kv.Key == null) s_DeadEntries.Add(kv);
            }
            if (s_DeadEntries.Count == 0) return;

            for (int i = 0; i < s_DeadEntries.Count; i++)
            {
                s_OwnedMeshes.Remove(s_DeadEntries[i].Key);
                DestroyMesh(s_DeadEntries[i].Value);
            }
            s_DeadEntries.Clear();
        }

        /// <summary>销毁运行时创建的网格资源（编辑期用 DestroyImmediate，运行期用 Destroy）。</summary>
        static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
            else UnityEngine.Object.DestroyImmediate(mesh);
        }

        // ──────────────────────────────────────────────
        //  工具方法
        // ──────────────────────────────────────────────

        static Vector3 ComputeTangent(Vector3[] path, int index)
        {
            if (path.Length == 1) return Vector3.forward;

            if (index == 0)
                return (path[1] - path[0]).normalized;
            if (index == path.Length - 1)
                return (path[index] - path[index - 1]).normalized;

            return (path[index + 1] - path[index - 1]).normalized;
        }
    }

    // ──────────────────────────────────────────────
    //  数据模型
    // ──────────────────────────────────────────────

    [System.Serializable]
    public struct LineDrawData
    {
        [Tooltip("管道直径")]
        public float width;

        [Tooltip("沿路径采样段数（总段数，自动均分到每对控制点之间）")]
        public int sectionSegments;

        [Tooltip("截面圆周分段数（≥3，越大越圆润）")]
        public int RadialSegments;

        public Material material;

        [Tooltip("贝塞尔偏移方向（非零时启用弯曲；归一化后乘以偏移距离）")]
        public Vector3 bazierOffsetDirection;

        [Tooltip("贝塞尔偏移距离")]
        public float bazierOffsetDistance;
    }
}
