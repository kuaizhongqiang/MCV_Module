using System.Collections;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.InputController.FocusRotationController
{
    public class FocusRotationControl : InputControllerBase
    {
        [SerializeField] Transform _target;
        [SerializeField] float inertiaTime = 0.5f;
        [SerializeField] float distance = 4f;

        private Camera[] _cameras;
        private Coroutine _smoothFollowCoroutine;
        private float _yaw;
        private float _pitch;
        private float _yawVelocity;
        private float _pitchVelocity;
        private bool _isTransitioning;
        private bool _freezeOrbit;

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            _cameras = GetComponentsInChildren<Camera>();
            if (_cameras.Length == 0) Log.Error("需要有相机组件");

            // 记录初始位姿，供 ResetPos 回到此状态
            startPos = transform.position;
            startRot = transform.rotation;
        }

        private void Start()
        {
            if (_target == null) return;
            transform.SetPositionAndRotation(startPos, startRot);
            InitializeOrbit(_target);
        }

        protected override void Update()
        {
            base.Update();
            if (_isTransitioning) return;
            if (_freezeOrbit) { _freezeOrbit = false; return; }

            ZoomHandle();
            HandleRot();
            HandlePos();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion

        #region 核心方法

        private void InitializeOrbit(Transform target)
        {
            distance = Vector3.Distance(startPos, target.position);
            Vector3 dir = startRot * Vector3.back;
            _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        void HandlePos()
        {
            if (_target == null) return;

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pos = _target.position + rot * (Vector3.back * distance);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(_target.position - pos));
        }

        void HandleRot()
        {
            if (_target == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * mouseSensitive * Time.deltaTime;
                _pitch -= delta.y * mouseSensitive * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, BottomClamp, TopClamp);

                // 记录惯性速度
                _yawVelocity = delta.x * mouseSensitive;
                _pitchVelocity = -delta.y * mouseSensitive;

                IsMoving = Mathf.Abs(delta.x) > 1f || Mathf.Abs(delta.y) > 1f;
            }
            else
            {
                // 惯性衰减
                float damping = 1f / Mathf.Max(inertiaTime, 0.01f);
                _yawVelocity = Mathf.Lerp(_yawVelocity, 0f, damping * Time.deltaTime);
                _pitchVelocity = Mathf.Lerp(_pitchVelocity, 0f, damping * Time.deltaTime);

                if (Mathf.Abs(_yawVelocity) > 0.01f || Mathf.Abs(_pitchVelocity) > 0.01f)
                {
                    _yaw += _yawVelocity * Time.deltaTime;
                    _pitch += _pitchVelocity * Time.deltaTime;
                    _pitch = Mathf.Clamp(_pitch, BottomClamp, TopClamp);
                    IsMoving = true;
                }
                else
                {
                    _yawVelocity = 0f;
                    _pitchVelocity = 0f;
                    IsMoving = false;
                }
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 重置相机到初始位置 + 恢复FOV + 重新瞄准目标
        /// </summary>
        public void ResetPos()
        {
            if (_smoothFollowCoroutine != null)
                StopCoroutine(_smoothFollowCoroutine);
            if (zoomHandleCoroutine != null)
                StopCoroutine(zoomHandleCoroutine);

            _smoothFollowCoroutine = null;
            _isTransitioning = false;

            // 直接定位到初始位姿 + 恢复 FOV
            transform.SetPositionAndRotation(startPos, startRot);
            SetAllCamerasFov(defaultFov);

            // 重新计算轨道角度，保留 startRot 一帧不被 HandlePos 覆盖
            if (_target != null)
                InitializeOrbit(_target);

            _freezeOrbit = true;
        }

        #endregion

        IEnumerator SmoothFollow()
        {
            _isTransitioning = true;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Transform target = _target;
            float keepDistance = distance;

            // 计算目标状态：从目标指向相机的方向
            Vector3 toTarget = target.position - startPos;
            float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Asin(Mathf.Clamp(toTarget.y / toTarget.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
            Quaternion targetOrbitRot = Quaternion.Euler(targetPitch, targetYaw, 0f);
            Vector3 finalPos = target.position + targetOrbitRot * (Vector3.back * keepDistance);
            Quaternion finalRot = Quaternion.LookRotation(target.position - finalPos);

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // smoothstep

                transform.position = Vector3.Lerp(startPos, finalPos, t);
                transform.rotation = Quaternion.Slerp(startRot, finalRot, t);
                yield return null;
            }

            transform.SetPositionAndRotation(finalPos, finalRot);

            // 切回轨道角度模式
            Vector3 newTargetDir = transform.position - target.position;
            distance = keepDistance;
            _yaw = Mathf.Atan2(newTargetDir.x, newTargetDir.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Asin(Mathf.Clamp(newTargetDir.y / distance, -1f, 1f)) * Mathf.Rad2Deg;

            _isTransitioning = false;
            _smoothFollowCoroutine = null;
        }

        #region 继承方法

        public override void Transport(Transform target)
        {
            if (target == null) return;
            _target = target;

            if (_smoothFollowCoroutine != null)
                StopCoroutine(_smoothFollowCoroutine);
            _smoothFollowCoroutine = StartCoroutine(SmoothFollow());
        }

        protected override void ZoomHandle()
        {
            if (_cameras == null || _cameras.Length == 0) return;

            // 移动时自动恢复默认 FOV
            if (IsMoving)
            {
                TryRestoreDefaultFov();
                return;
            }

            // 检测鼠标滚轮输入
            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) < 0.01f) return;

            // 从第一个 Camera 获取当前 FOV 作为基准
            float currentFov = _cameras[0].fieldOfView;
            float targetFov = Mathf.Clamp(currentFov - scrollDelta * zoomSpeed, zoomMin, zoomMax);

            if (zoomHandleCoroutine != null)
                StopCoroutine(zoomHandleCoroutine);
            zoomHandleCoroutine = StartCoroutine(ZoomHandleDelay(targetFov));
        }

        private IEnumerator ZoomHandleDelay(float targetFov)
        {
            // 执行惯性处理
            float startFov = _cameras[0].fieldOfView;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float fov = Mathf.Lerp(startFov, targetFov, t);
                SetAllCamerasFov(fov);
                yield return null;
            }

            SetAllCamerasFov(targetFov);
            zoomHandleCoroutine = null;
        }

        private void TryRestoreDefaultFov()
        {
            if (!_hasDefaultFov) return;
            if (Mathf.Abs(_cameras[0].fieldOfView - defaultFov) < 0.01f) return;

            if (zoomHandleCoroutine != null)
                StopCoroutine(zoomHandleCoroutine);
            zoomHandleCoroutine = StartCoroutine(ZoomRestoreCoroutine());
        }

        private IEnumerator ZoomRestoreCoroutine()
        {
            float startFov = _cameras[0].fieldOfView;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float fov = Mathf.Lerp(startFov, defaultFov, t);
                SetAllCamerasFov(fov);
                yield return null;
            }

            SetAllCamerasFov(defaultFov);
            zoomHandleCoroutine = null;
        }

        private void SetAllCamerasFov(float fov)
        {
            for (int i = 0; i < _cameras.Length; i++)
            {
                _cameras[i].fieldOfView = fov;
            }

            // 首次设置时缓存默认 FOV
            if (!_hasDefaultFov)
            {
                defaultFov = fov;
                _hasDefaultFov = true;
            }
        }

        #endregion
    }
}