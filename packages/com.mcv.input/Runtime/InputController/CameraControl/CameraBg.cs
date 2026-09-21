using System.Collections;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.InputController.CameraControl
{
    public class CameraBg : MonoBehaviour
    {
        Camera cam;
        //float distance = 500f;
        Vector2 screenSize;
        float lastFov;
        float lastAspect;
        float lastLocalZ;
        MeshRenderer meshRenderer;
        Material material;
        //string bgName = "";
        Coroutine TexFadeCoroutine;
        const string MatTexture_1 = "_Texture_1";
        const string MatTexture_2 = "_Texture_2";
        const string ChangeTextureBool = "_TexOrColor";         // 这是一个float属性数据 0 = 颜色 1 = 纹理
        const string TexCutFade = "_CutTex";                    // 这是一个float属性数据0-1 平滑切换1 和2 两个纹理
        
        void Awake()
        {
            StartCoroutine(DelayInit());
            screenSize = new Vector2(Screen.width, Screen.height);
            InitMesh();

            // 场景状态变化：进入漫游（Roaming）时关掉背景遮挡面，否则它会挡住 3D 视角。
            // 订阅放在本组件（而非 GlobalCameraMgr）是为了保持依赖方向正确：
            // 包归属上 InputController/ 属 com.mcv.input、Managers/Global* 属 com.mcv.core，
            // 而 input 依赖 core；反过来引用会形成循环依赖。
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChange);
        }

        void OnDestroy()
        {
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChange);
        }

        /// <summary>进入漫游时隐藏自身（背景遮挡面），其他状态恢复显示。</summary>
        void OnSceneStateChange(SceneStateChangeEventData data)
        {
            if (data == null) return;

            // SetActive 传入相同值时是无操作，不必自己判重
            gameObject.SetActive(data.State != SceneState.Roaming);
        }

        IEnumerator DelayInit()
        {
            // 关键：先等一帧再访问 GlobalCameraMgr。
            // StartCoroutine 会把协程体同步执行到第一个 yield，而本组件可能正是
            // GlobalCameraMgr.GetCamera() 在 Instantiate(MainCamera) 时被创建的 ——
            // 若在这里同步取 Camera，就会「实例化 → Awake → 再实例化」无限递归（栈溢出）。
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