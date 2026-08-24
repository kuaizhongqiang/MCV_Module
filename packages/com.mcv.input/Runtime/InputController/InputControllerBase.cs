using System.Collections;
using Cinemachine;
using MCV_Module.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.InputController
{
    public abstract class InputControllerBase : MonoBehaviour
    {
        [SerializeField] protected bool isActive = true;
        [Header("摄像机")]
        [Tooltip("摄像机向上可移动的角度范围（度）")]
        [SerializeField] protected float TopClamp = 89.0f;
        [Tooltip("摄像机向下可移动的角度范围（度）")]
        [SerializeField] protected float BottomClamp = -89.0f;
        [SerializeField] protected float mouseSensitive = 1.0f;
        [SerializeField] protected float defaultFov = 42f;
        [SerializeField] protected float zoomMin = 20f;        
        [SerializeField] protected float zoomMax = 120f;
        [SerializeField] protected float zoomSpeed = 5.0f;
        [SerializeField] protected Vector3 startPos = Vector3.zero;
        [SerializeField] protected Quaternion startRot = Quaternion.identity;

        protected Camera mainCamera;
        protected Coroutine zoomHandleCoroutine;

        public bool IsActive {get => isActive; set => isActive = value;}

        /// <summary>
        /// 子类在移动时设为 true，停止时设为 false
        /// </summary>
        protected bool IsMoving { get; set; }

        
        protected bool _hasDefaultFov;

        #region 继承方法
        public abstract void Transport(Transform target);
        #endregion

        #region 虚方法
        protected virtual void Awake()
        {
            StartCoroutine(DelayInit());
        }

        protected virtual void Update()
        {
            if (!isActive) return;
        } 

        protected virtual void OnDestroy()
        {
            if (GlobalInputMgr.Instance != null)
                GlobalInputMgr.UnregisterController(GetType().Name);
        }

        protected virtual IEnumerator DelayInit()
        {
            while (GlobalInputMgr.Instance == null)
            {
                yield return null;
            }

            GlobalInputMgr.RegisterController(GetType().Name, this);

            while (GlobalInputMgr.Instance == null)
            {
                yield return null;
            }

            mainCamera = GlobalCameraMgr.Camera;
        }
        #endregion

        #region 私有方法
        protected virtual void ZoomHandle()
        {
            // 移动时自动恢复默认 FOV
            if (IsMoving)
            {
                TryRestoreDefaultFov();
                return;
            }

            // 检测鼠标滚轮输入
            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) < 0.01f) return;

            // 输出为调整虚拟相机的lens.fov
            if (!TryGetVirtualCamera(out var vcam)) return;
            float targetFov = Mathf.Clamp(vcam.m_Lens.FieldOfView - scrollDelta * zoomSpeed, zoomMin, zoomMax);

            if (zoomHandleCoroutine != null)
                StopCoroutine(zoomHandleCoroutine);
            zoomHandleCoroutine = StartCoroutine(ZoomHandleDelay(vcam, targetFov));
        }

        private IEnumerator ZoomHandleDelay(CinemachineVirtualCamera vcam, float targetFov)
        {
            // 执行惯性处理
            float startFov = vcam.m_Lens.FieldOfView;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vcam.m_Lens.FieldOfView = Mathf.Lerp(startFov, targetFov, t);
                yield return null;
            }

            vcam.m_Lens.FieldOfView = targetFov;
            zoomHandleCoroutine = null;
        }

        private void TryRestoreDefaultFov()
        {
            if (!_hasDefaultFov) return;
            if (!TryGetVirtualCamera(out var vcam)) return;
            if (Mathf.Abs(vcam.m_Lens.FieldOfView - defaultFov) < 0.01f) return;

            if (zoomHandleCoroutine != null)
                StopCoroutine(zoomHandleCoroutine);
            zoomHandleCoroutine = StartCoroutine(ZoomRestoreCoroutine(vcam));
        }

        private IEnumerator ZoomRestoreCoroutine(CinemachineVirtualCamera vcam)
        {
            float startFov = vcam.m_Lens.FieldOfView;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vcam.m_Lens.FieldOfView = Mathf.Lerp(startFov, defaultFov, t);
                yield return null;
            }

            vcam.m_Lens.FieldOfView = defaultFov;
            zoomHandleCoroutine = null;
        }

        private bool TryGetVirtualCamera(out CinemachineVirtualCamera vcam)
        {
            vcam = null;
            if (mainCamera == null) return false;

            if (mainCamera.TryGetComponent<CinemachineBrain>(out var brain))
            {
                vcam = brain.ActiveVirtualCamera as CinemachineVirtualCamera;
            }

            // 首次获取虚拟相机时缓存默认 FOV
            if (vcam != null && !_hasDefaultFov)
            {
                defaultFov = vcam.m_Lens.FieldOfView;
                _hasDefaultFov = true;
            }

            return vcam != null;
        }

        #endregion
    }
}

