using System.Collections;
using MCV_Module.Managers;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.InputController.CameraControl
{
    /// <summary>相机背景板：按相机 FOV/宽高比缩放平面，并支持 AB 加载双纹理淡入切换</summary>
    public class CameraBg : MonoBehaviour
    {
        Camera cam;
        Vector2 screenSize;
        float lastFov;
        float lastAspect;
        float lastLocalZ;
        MeshRenderer meshRenderer;
        Material material;
        Coroutine TexFadeCoroutine;
        const string MatTexture_1 = "_Texture_1";
        const string MatTexture_2 = "_Texture_2";
        const string ChangeTextureBool = "_TexOrColor";         // 这是一个float属性数据 0 = 颜色 1 = 纹理
        const string TexCutFade = "_CutTex";                    // 这是一个float属性数据0-1 平滑切换1 和2 两个纹理

        // WHY: 包 id 必须与 Assets/Editor/BuildTools/CameraBgBundleTools.cs 的 Ids 一致，下标 0=_Texture_1、1=_Texture_2
        /// <summary>背景图的包配置 id 数组（顺序对应材质 _Texture_1 / _Texture_2）</summary>
        static readonly string[] BgPackageIds = { "camerabg_room", "camerabg_contactor" };
        
        void Awake()
        {
            StartCoroutine(DelayInit());
            screenSize = new Vector2(Screen.width, Screen.height);
            InitMesh();
        }

        IEnumerator DelayInit()
        {
            // WHY: 必须先等一帧再取 Camera；StartCoroutine 会同步跑到首个 yield，本组件可能正由 Instantiate 触发，同步取会「实例化→Awake→再实例化」栈溢出
            yield return null;

            while(GlobalCameraMgr.Camera == null)
            {
                yield return null;
            }
            cam = GlobalCameraMgr.Camera;
            // 初始化时记录初始值
            if (cam != null)
            {
                lastFov = cam.fieldOfView;
                lastAspect = cam.aspect;
                lastLocalZ = transform.localPosition.z;
                SetScale();
            }

            // 启动时加载两张背景图（同一个 AB 包；不阻塞相机初始化，加载完再写材质）
            LoadBgTextures();
        }

        /// <summary>启动时从 AB 包加载两张背景图，按 Sprite 加载后取 .texture 写入材质</summary>
        void LoadBgTextures()
        {
            if (material == null)
            {
                Log.Error("[CameraBg] 材质为空，背景图无法写入");
                return;
            }

            GlobalAssetsMgr.LoadSpritesByPackageIdsAsync(BgPackageIds, sprites =>
            {
                if (sprites == null || sprites.Count < BgPackageIds.Length || sprites[0] == null || sprites[1] == null)
                {
                    Log.Error($"[CameraBg] 背景图加载不完整（{sprites?.Count ?? 0}/{BgPackageIds.Length}），保持材质默认颜色");
                    return;
                }

                SetTwoTexture(sprites[0].texture, sprites[1].texture);
                material.SetFloat(ChangeTextureBool, 1f);   // 0 = 颜色、1 = 纹理：切到纹理模式
                material.SetFloat(TexCutFade, 0f);          // 起始显示 _Texture_1
                Log.Info($"[CameraBg] 背景图已加载：_Texture_1 = {sprites[0].name}，_Texture_2 = {sprites[1].name}");
            },
            error => Log.Error($"[CameraBg] 背景图加载失败：{error}"));
        }

        void Update()
        {
            if (cam == null)
            {
                return;
            }

            // 只在相机参数或位置变化时才更新
            float currentFov = cam.fieldOfView;
            float currentAspect = cam.aspect;
            float currentLocalZ = transform.localPosition.z;

            if (!Mathf.Approximately(currentFov, lastFov) || 
                !Mathf.Approximately(currentAspect, lastAspect) || 
                !Mathf.Approximately(currentLocalZ, lastLocalZ))
            {
                SetScale();
                lastFov = currentFov;
                lastAspect = currentAspect;
                lastLocalZ = currentLocalZ;
            }
        }

        void SetScale()
        {
            float fov = cam.fieldOfView * Mathf.Deg2Rad;
            float aspect = cam.aspect;
            float localZ = transform.localPosition.z;

            float viewHeight = 2f * localZ * Mathf.Tan(fov * 0.5f);
            float viewWidth = viewHeight * aspect;

            transform.localScale = new Vector3(viewWidth, viewHeight, 1f);
        }
    
        void InitMesh()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            material = meshRenderer.sharedMaterial;
        }

        public void SmoothFadeTexture(bool fadeIn)        
        {
            if (material.GetTexture(MatTexture_1) == null || material.GetTexture(MatTexture_2) == null) return;

            material.SetFloat(ChangeTextureBool,1f);
            
            if (TexFadeCoroutine != null)
            {
                StopCoroutine(TexFadeCoroutine);
            }

            TexFadeCoroutine = StartCoroutine(FadeTexture(fadeIn));
        }

        IEnumerator FadeTexture(bool fadeIn)
        {
            float time = 0f;
            float current = material.GetFloat(TexCutFade);
            float duration = 1f;
            float target = fadeIn ? 1f : 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                current = Mathf.Lerp(current, target, time / duration);
                material.SetFloat(TexCutFade, current);
                yield return null;
            }
            
            material.SetFloat(TexCutFade, target);

            TexFadeCoroutine = null;
        }
    
        public void SetTwoTexture(Texture2D tex1, Texture2D tex2)
        {
            if (tex1 == null || tex2 == null) return;
            
            material.SetTexture(MatTexture_1, tex1);
            material.SetTexture(MatTexture_2, tex2);
        }
    }
}