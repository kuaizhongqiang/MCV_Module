using UnityEngine;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Singleton;
using MCV_Module.Utils;
using System.Collections;
using Cinemachine;
using MCV_Module.InputController.CameraControl;

namespace MCV_Module.Managers
{
    public class GlobalCameraMgr : SingletonGlobalMgr<GlobalCameraMgr>
    {
        #region 参数
        Camera _cam;
        CinemachineBrain brain;
        CameraBg cameraBg;
        /// <summary>相机实例化中的重入标记（防 Instantiate → Awake → 再 Instantiate 的无限递归）。</summary>
        static bool s_IsCreating;
        #endregion

        #region 生命周期
        protected override IEnumerator DelayInit()
        {
            // WHY: 相机统一由 GetCamera() 取用并同步 brain/cameraBg，避免 yield 跨帧后拿到已销毁的引用
            _cam = GetCamera();
            if (_cam != null)
            {
                brain = _cam.GetComponent<CinemachineBrain>();
                // WHY: 必须 includeInactive——检查任务会把 bg 节点关掉，用默认重载会查不到、把缓存刷成 null
                cameraBg = Instance._cam.GetComponentInChildren<CameraBg>(true);
            }
            yield return null;

            // 注册 EventBus 事件监听
            EventBus<CameraBgChangeEventData>.Subscribe(OnCameraBgChange);
            EventBus<CameraBlendChangeEventData>.Subscribe(OnCameraBlendChange);
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChange);
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskTypeChange);

            isInit = true;
        }

        protected override void OnDestroy()
        {
            EventBus<CameraBgChangeEventData>.Unsubscribe(OnCameraBgChange);
            EventBus<CameraBlendChangeEventData>.Unsubscribe(OnCameraBlendChange);
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChange);
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskTypeChange);
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
            // 复用仍有效的相机（Unity 的伪 null 判空能识别已销毁对象）
            if (Instance._cam != null)
            {
                // WHY: brain 与相机同步，避免单独持有过期的 brain 引用
                if (Instance.brain == null)
                {
                    Instance.brain = Instance._cam.GetComponent<CinemachineBrain>();
                    // WHY: 必须 includeInactive——检查任务期间 bg 处于未激活，默认重载会查不到并把缓存刷成 null，
                    //      导致退出检查后 SetCameraBgActive(true) 因引用为 null 而失效（背景永久不显示）
                    Instance.cameraBg = Instance._cam.GetComponentInChildren<CameraBg>(true);
                }
                return Instance._cam;
            }

            // WHY: 正在实例化时返回 null，让调用方（如 CameraBg.DelayInit）等下一帧，打断递归链
            if (s_IsCreating) return null;

            Camera[] cams = Camera.allCameras;
            for (int i = 0; i < cams.Length; i++)
            {
                // 防御：跳过已销毁/待销毁的相机，避免重复 Destroy
                if (cams[i] == null) continue;
                // WHY: 只清理本管理器实例化过的相机（挂在 Instance 下），不误杀 AVPro / UI 相机
                if (cams[i].transform.parent == Instance.transform)
                    Destroy(cams[i].gameObject);
            }

            GameObject prefab = Resources.Load<GameObject>("MainCamera");
            if (prefab == null) return null;

            s_IsCreating = true;
            try
            {
                GameObject go = Instantiate(prefab, Instance.transform);
                go.name = "MainCamera";
                Instance._cam = go.GetComponent<Camera>();
                Instance.brain = go.GetComponent<CinemachineBrain>();
                Instance.cameraBg = Instance._cam.GetComponentInChildren<CameraBg>(true);
                return Instance._cam;
            }
            finally
            {
                // 无论实例化过程中是否抛异常/提前返回，都要清标记
                s_IsCreating = false;
            }
        }
        #endregion

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

        /// <summary>场景状态变化：进入漫游（Roaming）关闭相机背景遮挡面，其他状态保持/恢复显示。</summary>
        void OnSceneStateChange(SceneStateChangeEventData data)
        {
            if (data == null) return;

            bool isRoaming = data.State == SceneState.Roaming;
            SetCameraBgActive(!isRoaming);
            Log.Info(isRoaming
                ? "[GlobalCameraMgr] 进入漫游状态，关闭相机背景遮挡"
                : "[GlobalCameraMgr] 退出漫游状态，启用相机背景遮挡");
        }

        void OnTaskTypeChange(TaskTypeChangeEventData data)
        {
            if (data == null) return;

            bool isInspection = data.TaskType == TaskType.Inspection;
            SetCameraBgActive(!isInspection);
            Log.Info(isInspection
                ? "[GlobalCameraMgr] 进入检查任务，关闭相机背景遮挡"
                : "[GlobalCameraMgr] 退出检查任务，启用相机背景遮挡");
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

        #region 控制相机遮挡
        void SetCameraBgActive(bool active)
        {
            // 引用可能因相机重建而失效，先补齐再执行，避免「空引用只重取不生效」
            if (cameraBg == null)
                cameraBg = Camera != null ? Camera.GetComponentInChildren<CameraBg>(true) : null;

            if (cameraBg != null)
                cameraBg.gameObject.SetActive(active);
        }
        #endregion
        #endregion
    }
}
