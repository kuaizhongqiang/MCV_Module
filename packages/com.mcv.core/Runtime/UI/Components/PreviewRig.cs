using MCV_Module.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    // WHY: 本组件存在的唯一理由 —— 面板 prefab 住在 AB 包里，若让它直引材质，包内会再生出一份**材质 + RT（+shader）副本**，
    //       而 UI 引擎能把「每个渲染器的纹理」送进材质的通道只有 _MainTex（UGUI 的 Graphic.UpdateMaterial 调
    //       CanvasRenderer.SetMaterial + SetTexture(mainTexture)，官方文档：给了 texture 就用它当 MainTex）。
    //       本工程的 VideoAlphaUIShader 取色读的是 _ColorMap / _AlphaMap 两个**材质序列化属性**，引擎送不进去 ⇒
    //       包内那份副本指向哪张 RT，面板就只能显示那张（实测：包内 RT 与工程 RT 不是同一对象，且每次加载都会另生成一份）。
    //       故改为：shader 放 default 包（GlobalAssetsMgr.GetRuntimeShader），材质与两张 RT 全部运行期自建并自持，
    //       谁都不共用、也不落盘 —— 包与源的分歧从根上消失。
    // WHY: RT 按各自 RawImage 的**实际像素尺寸**建（width/height × canvas.scaleFactor），不是按相机定的一档尺寸 ——
    //       面板显示多大就渲染多大，不浪费显存也不缩采样。
    // WHY: color / alpha 必须是**两张** RT + 两台相机，不能合并成一张：ColorCam 开了后处理时它写的 alpha 通道恒为 1，
    //       而预览模型的透明边缘要靠 AlphaCam 单独出的 alpha 图（shader 用 _AlphaUseRedChannel 选 A 或 R 通道）。
    /// <summary>模型预览渲染装置：为所在面板的 RawImage 运行期自建材质与 color/alpha 两张 RenderTexture，并把它们挂到指定相机的 targetTexture 上。</summary>
    [DisallowMultipleComponent]
    public class PreviewRig : MonoBehaviour
    {
        #region 序列化字段
        [Tooltip("显示预览的 RawImage（本面板内那个全屏节点）")]
        [SerializeField] RawImage rawImage;

        // WHY: 相机用「展示根名 + 子路径」在运行期解析，而不是在 prefab 上序列化场景对象引用 ——
        //       面板 prefab 住在 AB 包里，把场景对象引用烤进包既脆（场景改名/重建即断）又跨不过包边界。
        [Header("相机来源（运行期按场景层级解析）")]
        [Tooltip("展示根对象名（1_Content 场景的根级对象，如 ShowObjParent / ControlObjParent）")]
        [SerializeField] string showRootName = "ShowObjParent";

        [Tooltip("展示根下到彩色相机的相对路径")]
        [SerializeField] string colorCameraPath = "CamParent/ColorCam";

        [Tooltip("彩色相机下到 alpha 相机的相对路径（留空则与彩色相机同名子节点下找 AlphaCam）")]
        [SerializeField] string alphaCameraPath = "AlphaCam";

        [Tooltip("可选：直接指定相机（填了就不再按名字解析，便于调试/换根）")]
        [SerializeField] Camera colorCameraOverride;
        [SerializeField] Camera alphaCameraOverride;

        [Header("材质来源")]
        [Tooltip("运行时自建材质的 shader 在 Resources 下的路径（不带扩展名）")]
        [SerializeField] string shaderResourcePath = "Shaders/VideoAlphaUIShader";

        [Tooltip("可选模板材质：存在时复制它的属性，从而继承 _AlphaUseRedChannel 等取值（Color 出图 / Alpha 出 alpha，见文件头说明）")]
        [SerializeField] Material templateMaterial;

        [Header("兜底尺寸")]
        [Tooltip("RawImage 的 rect 还量不出尺寸时（布局未跑完 / 面板未激活）用这个尺寸建 RT")]
        [SerializeField] Vector2Int fallbackSize = new Vector2Int(1024, 1024);

        [Tooltip("RT 尺寸上限（防超屏分辨率把显存吃爆）")]
        [SerializeField] int maxSize = 2048;
        #endregion

        #region 运行期状态
        Material previewMaterial;
        RenderTexture colorRt;
        RenderTexture alphaRt;
        Camera colorCam;
        Camera alphaCam;
        bool built;
        #endregion

        /// <summary>运行期自建出来的材质（未建成为 null）。</summary>
        public Material PreviewMaterial => previewMaterial;
        /// <summary>彩色 RT（未建成为 null）。</summary>
        public RenderTexture ColorTarget => colorRt;
        /// <summary>alpha RT（未建成为 null）。</summary>
        public RenderTexture AlphaTarget => alphaRt;
        /// <summary>解析到的彩色相机（未解析到为 null）。</summary>
        public Camera ResolvedColorCamera => colorCam;
        /// <summary>解析到的 alpha 相机（未解析到为 null）。</summary>
        public Camera ResolvedAlphaCamera => alphaCam;

        #region 生命周期
        void Awake()
        {
            ResolveRefs();
        }

        // WHY: 放 OnEnable 而不是 Awake —— 未激活层级下面板的布局没跑完（面板常以 inactive 实例化），
        //       此时 RawImage.rect 会是 0，量出来的 RT 尺寸也就是 0；OnEnable 时布局已就绪，才是量尺寸的正确时机。
        void OnEnable()
        {
            Build();
        }

        void OnDisable()
        {
            ReleaseCameras();
        }

        // WHY: RT 尺寸跟的是「RawImage 的实际像素尺寸」，而它随分辨率 / Canvas scaleFactor 变 ——
        //       分辨率一变就得重建，否则画面被拉伸（这点与工程既有的「按相机定档固定尺寸」不同，是刻意的）。
        void Update()
        {
            if (!built || rawImage == null) return;

            Vector2Int want = ResolveSize();
            if (colorRt == null || want.x != colorRt.width || want.y != colorRt.height) Rebuild();
        }

        void OnDestroy()
        {
            ReleaseCameras();
            if (previewMaterial != null)
            {
                Destroy(previewMaterial);
                previewMaterial = null;
            }
            Release(ref colorRt);
            Release(ref alphaRt);
            built = false;
        }
        #endregion

        #region 构建与释放
        /// <summary>取 shader、建材质与两张 RT，并接到 RawImage 与两台相机上（OnEnable 调用；缺件时逐条报错并安全跳过）。</summary>
        void Build()
        {
            if (built) return;

            ResolveRefs();

            if (rawImage == null)
            {
                Debug.LogError("[PreviewRig] 未配置 RawImage，预览无法建立", this);
                return;
            }

            Shader shader = GlobalAssetsMgr.GetRuntimeShader(shaderResourcePath);
            if (shader == null) return;   // GetRuntimeShader 已报 Error

            // WHY: 尺寸变化触发的重建不给材质换新对象 —— 只换两张 RT 的绑定，否则每次分辨率变动都会多泄一份材质实例
            if (previewMaterial == null)
            {
                previewMaterial = templateMaterial != null ? new Material(templateMaterial) : new Material(shader);
                if (templateMaterial == null) previewMaterial.shader = shader;
            }

            Vector2Int size = ResolveSize();
            RenderTexture oldColor = colorRt;
            RenderTexture oldAlpha = alphaRt;
            colorRt = CreateRt("PreviewColor", size);
            alphaRt = CreateRt("PreviewAlpha", size);

            previewMaterial.SetTexture("_ColorMap", colorRt);
            previewMaterial.SetTexture("_AlphaMap", alphaRt);

            rawImage.texture = colorRt;        // 引擎会把它当 _MainTex 送下去（本 shader 不采样，但 Inspector/调试语义正确）
            rawImage.material = previewMaterial;

            ResolveCameras();
            if (colorCam != null) colorCam.targetTexture = colorRt;
            else Debug.LogError($"[PreviewRig] 解析不到彩色相机（展示根「{showRootName}」/ 路径「{colorCameraPath}」），彩色 RT 没人写入", this);

            if (alphaCam != null) alphaCam.targetTexture = alphaRt;
            else Debug.LogError($"[PreviewRig] 解析不到 alpha 相机（路径「{alphaCameraPath}」），alpha RT 没人写入", this);

            // 新 RT 已全部绑定到位，此刻才释放旧的两张（顺序不可颠倒 —— 见本方法说明）
            Release(ref oldColor);
            Release(ref oldAlpha);

            built = true;
        }

        /// <summary>把两台相机的 targetTexture 摘掉（面板收起 / 销毁时调用，避免相机继续往已释放的 RT 里写）。</summary>
        void ReleaseCameras()
        {
            if (colorCam != null && colorCam.targetTexture == colorRt) colorCam.targetTexture = null;
            if (alphaCam != null && alphaCam.targetTexture == alphaRt) alphaCam.targetTexture = null;
        }

        /// <summary>按新尺寸重建两张 RT（尺寸变化时由 Update 触发；先建新、绑定完再释放旧，避免材质出现指向已释放 RT 的窗口）。</summary>
        void Rebuild()
        {
            Release(ref colorRt);
            Release(ref alphaRt);

            built = false;
            Build();
        }

        /// <summary>解析出图的两台相机：优先用 Inspector 指定的覆盖字段，否则按「展示根名 + 相对路径」在场景里找（含未激活对象）。</summary>
        void ResolveCameras()
        {
            colorCam = colorCameraOverride;
            alphaCam = alphaCameraOverride;

            if (colorCam == null && !string.IsNullOrEmpty(showRootName))
            {
                // WHY: 不能用 GameObject.Find —— 它只找**激活**对象，而展示根平时是关着的（用到才 SetActive(true)，
                //       且本组件的 OnEnable 可能在展示根被打开之前就跑）。用 FindObjectsOfTypeAll 兜住未激活的根。
                Transform root = FindRootByName(showRootName);
                if (root != null && !string.IsNullOrEmpty(colorCameraPath))
                {
                    Transform t = root.Find(colorCameraPath);
                    if (t != null) colorCam = t.GetComponent<Camera>();
                }
            }

            // alpha 相机是彩色相机的子物体（1_Content 里 AlphaCam 挂在 ColorCam 下）
            if (alphaCam == null && colorCam != null && !string.IsNullOrEmpty(alphaCameraPath))
            {
                Transform t = colorCam.transform.Find(alphaCameraPath);
                if (t != null) alphaCam = t.GetComponent<Camera>();
            }
        }

        /// <summary>ResolveRefs 之外的空引用兜底（字段可能没在 prefab 里配全）。</summary>
        void ResolveRefs()
        {
            if (rawImage == null) rawImage = GetComponent<RawImage>();
        }

        /// <summary>按名字找场景里的根 Transform（含未激活对象；同名取第一个）。</summary>
        static Transform FindRootByName(string name)
        {
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                // 只认场景对象：资产（prefab 内的同名节点）与隐藏对象都不算
                if (t == null || t.parent != null) continue;
                if (!t.gameObject.scene.IsValid()) continue;
                if (t.name == name) return t;
            }
            return null;
        }

        /// <summary>按 RawImage 的实际像素尺寸算 RT 尺寸；量不出或异常时退回 <see cref="fallbackSize"/> 并夹到 <see cref="maxSize"/>。</summary>
        Vector2Int ResolveSize()
        {
            Vector2Int size = fallbackSize;

            if (rawImage != null && rawImage.rectTransform != null)
            {
                Rect rect = rawImage.rectTransform.rect;
                float scale = 1f;
                Canvas canvas = rawImage.canvas;
                if (canvas != null) scale = canvas.scaleFactor;

                int w = Mathf.RoundToInt(Mathf.Abs(rect.width) * scale);
                int h = Mathf.RoundToInt(Mathf.Abs(rect.height) * scale);
                if (w > 0 && h > 0) size = new Vector2Int(w, h);
            }

            size.x = Mathf.Clamp(size.x, 2, maxSize);
            size.y = Mathf.Clamp(size.y, 2, maxSize);
            return size;
        }

        /// <summary>建一张不落盘的 RT（ARGB32 / 32 位深度 / 无 mip），抗锯齿交给管线后处理，RT 自身不开 MSAA。</summary>
        static RenderTexture CreateRt(string name, Vector2Int size)
        {
            var rt = new RenderTexture(size.x, size.y, 32, RenderTextureFormat.ARGB32);
            rt.name = name;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.antiAliasing = 1;
            rt.Create();
            return rt;
        }

        /// <summary>释放一张运行期 RT（Destroy 即可 —— 它们不是资产，没有引用计数问题）。</summary>
        static void Release(ref RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            Object.Destroy(rt);
            rt = null;
        }
        #endregion
    }
}
