using UnityEngine;
using MCV_Module.Event;
using MCV_Module.Singleton;
using System.Collections;
using Cinemachine;

namespace MCV_Module.Managers
{
    public class GlobalCameraMgr : SingletonGlobalMgr<GlobalCameraMgr>
    {
        #region 参数
        Camera _cam;
        CinemachineBrain brain;
        #endregion

        #region 生命周期
        protected override IEnumerator DelayInit()
        {
            // 相机统一由 GetCamera() 获取/创建，_cam 与 brain 同步维护，
            // 避免跨帧（yield）后访问到已销毁的相机引用。
            _cam = GetCamera();
            if (_cam != null)
            {
                brain = _cam.GetComponent<CinemachineBrain>();
            }
            yield return null;

            // 注册 EventBus 事件监听
            EventBus<CameraBgChangeEventData>.Subscribe(OnCameraBgChange);
            EventBus<CameraBlendChangeEventData>.Subscribe(OnCameraBlendChange);

            isInit = true;
        }

        protected override void OnDestroy()
        {
            EventBus<CameraBgChangeEventData>.Unsubscribe(OnCameraBgChange);
            EventBus<CameraBlendChangeEventData>.Unsubscribe(OnCameraBlendChange);
            base.OnDestroy();
        }
        #endregion

        #region 静态方法
        public static Camera Camera
        {
            get => GetCamera();
            set
            {
                if (value != null)
                {
                    Instance._cam = value;
                }
            }
        }

        #region 核心获取
        public static Camera GetCamera()
        {
            // 已持有的相机仍有效时直接复用（Unity 的"伪 null"判空能识别已销毁对象）
            if (Instance._cam != null)
            {
                // 确保 brain 与相机同步，避免单独持有过期的 brain 引用
                if (Instance.brain == null)
                    Instance.brain = Instance._cam.GetComponent<CinemachineBrain>();
                return Instance._cam;
            }

            Camera[] cams = Camera.allCameras;
            for (int i = 0; i < cams.Length; i++)
            {
                // 防御：跳过已销毁/待销毁的相机，避免重复 Destroy
                if (cams[i] == null) continue;
                // 只清理本管理器此前实例化过的相机（挂在 Instance 下的 MainCamera），
                // 不销毁场景中其他相机（AVPro、UI 相机等），避免误杀。
                if (cams[i].transform.parent == Instance.transform)
                    Destroy(cams[i].gameObject);
            }

            GameObject prefab = Resources.Load<GameObject>("MainCamera");
            if (prefab == null) return null;

            GameObject go = Instantiate(prefab, Instance.transform);
            go.name = "MainCamera";
            Instance._cam = go.GetComponent<Camera>();
            Instance.brain = go.GetComponent<CinemachineBrain>();
            return Instance._cam;
        }
        #endregion

        // ── 静态 API（预留，事件驱动） ─────────────────────────
        // public static void SetCameraBg(bool isSkybox)
        // {
        //     EventBus<CameraBgChangeEventData>.Publish(
        //         new CameraBgChangeEventData(isSkybox));
        // }

        // public static void SetCameraBlend(bool isCut, float blendTime = 1f)
        // {
        //     EventBus<CameraBlendChangeEventData>.Publish(
        //         new CameraBlendChangeEventData(isCut, blendTime));
        // }
        #endregion

        #region 私有方法
        #region EventBus 事件回调
        void OnCameraBgChange(CameraBgChangeEventData data)
        {
            BgChange(data.IsSkybox);
        }

        void OnCameraBlendChange(CameraBlendChangeEventData data)
        {
            BlendChange(data.IsCut, data.BlendTime);
        }
        #endregion

        #region 控制相机
        void BgChange(bool isSkybox)
        {
            Camera.clearFlags = isSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            Camera.backgroundColor = isSkybox ? Color.clear : Color.black;
        }

        void BlendChange(bool isCut, float blendTime = 1f)
        {
            if (isCut)
            {
                brain.m_DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Style.Cut, 0f);
            }
            else
            {
                brain.m_DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Style.EaseInOut, blendTime);
            }
        }
        #endregion
        #endregion
    }
}
