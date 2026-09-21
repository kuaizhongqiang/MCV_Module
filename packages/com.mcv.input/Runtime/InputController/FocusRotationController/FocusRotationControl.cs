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
        [Tooltip("双击鼠标左键后滑动回初始位姿的时长（秒）")]
        [SerializeField] float resetDuration = 0.5f;
        [Tooltip("判定鼠标左键双击的最大间隔（秒）")]
        [SerializeField] float doubleClickInterval = 0.3f;

        private Camera[] _cameras;
        private Coroutine _smoothFollowCoroutine;
        private Coroutine _resetBackCoroutine;
        private float _lastLeftClickTime = float.NegativeInfinity;
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

            // 本控制器直接驱动真实相机，默认 FOV 以相机初始值为准（基类的默认值只在走 Cinemachine 虚拟相机时才准）
            if (_cameras.Length > 0)
            {
                defaultFov = _cameras[0].fieldOfView;
                _hasDefaultFov = true;
            }

            // 记录初始位姿，供 ResetPos 回到此状态
            startPos = transform.position;
            startRot = transform.rotation;
        }

        private void Start()
        {
            if (_target == null) return;

            // 初始观察位姿由 InitializeOrbit 推导出的轨道角决定，下一帧 HandlePos 会还原出同一位置/朝向；
            // 这里再 SetPositionAndRotation(startPos, startRot) 是无效操作（同一帧就会被覆盖），故不写。
            InitializeOrbit(_target);
        }

        protected override void Update()
        {
            base.Update();

            // 双击左键：从任意状态（含平移/缩放过渡中）滑动回初始位姿，先于其他判断处理
            HandleResetInput();
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
            // Debug：核对 Awake 记录的初始位姿与被调时的瞬时位姿是否一致。
            // 如果偏差非零，说明位姿在 Awake 之后被动过（轨道角按 startPos 推导，第一帧就会瞬移）。
            float posDiff = Vector3.Distance(startPos, transform.position);
            float rotDiff = Quaternion.Angle(startRot, transform.rotation);
            string poseCompare = string.Format(
                "位姿核对 startPos={0} / 当前pos={1} → 偏差 {2:F5}m；startRot={3} / 当前rot={4} → 偏差 {5:F3}°",
                startPos.ToString("F4"), transform.position.ToString("F4"), posDiff,
                startRot.eulerAngles.ToString("F3"), transform.rotation.eulerAngles.ToString("F3"), rotDiff);

            float radius;
            if (!TryGetOrbit(startPos, target, out _yaw, out _pitch, out radius))
            {
                Log.Error("初始相机位置与聚焦目标重合，无法推导环绕角度 | " + poseCompare);
                return;
            }

            _pitch = Mathf.Clamp(_pitch, BottomClamp, TopClamp);
            distance = radius;

            string orbitInfo = string.Format(
                " | 目标={0} 半径={1:F4} yaw={2:F3}° pitch={3:F3}°",
                target.position.ToString("F4"), distance, _yaw, _pitch);

            if (posDiff > 1e-4f || rotDiff > 0.01f)
                Log.Warning(poseCompare + orbitInfo);
            else
                Log.Info(poseCompare + orbitInfo);
        }

        /// <summary>
        /// 由「相机世界位置」推导环绕角与半径，必须与 ApplyOrbit 的
        /// pos = target + Euler(_pitch,_yaw,0) * (back * distance) 同源推导：
        /// 该式展开后「目标→相机」单位向量 = (-cos(pitch)sin(yaw), sin(pitch), -cos(pitch)cos(yaw))，
        /// 所以 yaw = atan2(-dir.x, -dir.z)。若写成 atan2(dir.x, dir.z) 会差 180°，相机会翻到目标另一侧。
        /// 相机与目标重合时返回 false（角度无意义）。
        /// </summary>
        private static bool TryGetOrbit(Vector3 camPos, Transform target, out float yaw, out float pitch, out float radius)
        {
            yaw = 0f;
            pitch = 0f;

            Vector3 offset = camPos - target.position;
            radius = offset.magnitude;
            if (radius < 1e-4f) return false;

            Vector3 dir = offset / radius;
            yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            return true;
        }

        /// <summary>
        /// 按当前 _yaw/_pitch/distance 摆放相机，并始终让相机看向目标。
        /// 逐帧环绕（HandlePos）与平滑复位（SmoothResetBack）共用，保证两者位置与朝向的算法完全一致。
        /// </summary>
        private void ApplyOrbit(Transform target)
        {
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pos = target.position + rot * (Vector3.back * distance);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(target.position - pos));
        }

        void HandlePos()
        {
            if (_target == null) return;
            ApplyOrbit(_target);
        }

        /// <summary>
        /// 复位过渡的一帧：把位置沿轨道插值到「初始位姿所在的轨道」上，朝向交给 ApplyOrbit 统一对准目标，
        /// 不再对旋转单独插值（分别插值位置与旋转时，相机朝向会脱离目标，目标就绕出视野了）。
        /// 目标轨道角每帧由 startPos 与目标当前位置重新推导：目标在过渡中移动时，终点仍是 startPos，且全程对准目标。
        /// 无聚焦目标或位姿退化时返回 false，调用方退回「位姿直接插值」。
        /// </summary>
        private bool ApplyResetFrame(float t, float fromYaw, float fromPitch, float fromDistance)
        {
            if (_target == null) return false;

            float toYaw, toPitch, toDistance;
            if (!TryGetOrbit(startPos, _target, out toYaw, out toPitch, out toDistance)) return false;
            toPitch = Mathf.Clamp(toPitch, BottomClamp, TopClamp);

            _yaw = Mathf.LerpAngle(fromYaw, toYaw, t);
            _pitch = Mathf.Lerp(fromPitch, toPitch, t);
            distance = Mathf.Lerp(fromDistance, toDistance, t);

            ApplyOrbit(_target);
            return true;
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

        /// <summary>
        /// 双击鼠标左键 → 滑动回到初始位姿。用 unscaledTime 计时，暂停/慢动作下同样有效。
        /// </summary>
        void HandleResetInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;

            float now = Time.unscaledTime;
            if (now - _lastLeftClickTime <= doubleClickInterval)
            {
                // 消费掉本次双击，避免连击被识别成第二次双击（三击不会连续复位两次）
                _lastLeftClickTime = float.NegativeInfinity;
                ResetPosSmooth();
            }
            else
            {
                _lastLeftClickTime = now;
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
            if (_resetBackCoroutine != null)
                StopCoroutine(_resetBackCoroutine);

            _smoothFollowCoroutine = null;
            _resetBackCoroutine = null;
            _isTransitioning = false;

            SetAllCamerasFov(defaultFov);

            // 有聚焦目标：直接按「初始位姿 → 轨道角」重算并立刻对准目标，
            // 不要停留在 startRot（它不一定看向目标），否则目标会有一两帧跑出视野。
            float yaw, pitch, radius;
            if (_target != null && TryGetOrbit(startPos, _target, out yaw, out pitch, out radius))
            {
                _yaw = yaw;
                _pitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);
                distance = radius;
                ApplyOrbit(_target);
                _freezeOrbit = false;
            }
            else
            {
                // 无目标：保持 startRot 一帧不被 HandlePos 覆盖
                transform.SetPositionAndRotation(startPos, startRot);
                _freezeOrbit = true;
            }
        }

        /// <summary>
        /// 滑动（缓动）回到初始位姿 + 恢复初始 FOV + 重新瞄准目标。
        /// 与 ResetPos() 的区别：ResetPos 是瞬移，本方法带 resetDuration 秒过渡，可打断平移/缩放过渡。
        /// </summary>
        public void ResetPosSmooth()
        {
            if (_resetBackCoroutine != null)
                StopCoroutine(_resetBackCoroutine);

            _resetBackCoroutine = StartCoroutine(SmoothResetBack());
        }

        #endregion

        /// <summary>
        /// 从当前位姿/FOV 缓动回 startPos/defaultFov。
        /// 过渡中每帧都是「先移动位置、再面向目标」（ApplyOrbit），不再单独插值旋转。
        /// </summary>
        IEnumerator SmoothResetBack()
        {
            // 打断正在进行的平移与缩放过渡，避免两者同时写 transform/FOV
            if (_smoothFollowCoroutine != null)
            {
                StopCoroutine(_smoothFollowCoroutine);
                _smoothFollowCoroutine = null;
            }
            if (zoomHandleCoroutine != null)
            {
                StopCoroutine(zoomHandleCoroutine);
                zoomHandleCoroutine = null;
            }

            _isTransitioning = true;

            Vector3 fromPos = transform.position;
            Quaternion fromRot = transform.rotation;

            // 起点轨道角由「当前位姿」反推（与 ApplyOrbit 同源）。
            // 不能直接用 _yaw/_pitch：被本协程打断的 SmoothFollow 可能刚把它们改到一半，与当前位姿不一致。
            float fromYaw = _yaw;
            float fromPitch = _pitch;
            float fromDistance = Mathf.Max(distance, 1e-4f);
            if (_target != null)
            {
                float yaw, pitch, radius;
                if (TryGetOrbit(transform.position, _target, out yaw, out pitch, out radius))
                {
                    fromYaw = yaw;
                    fromPitch = Mathf.Clamp(pitch, BottomClamp, TopClamp);
                    fromDistance = radius;
                }
            }

            float[] fromFov = new float[_cameras.Length];
            for (int i = 0; i < _cameras.Length; i++)
                fromFov[i] = _cameras[i].fieldOfView;

            float duration = Mathf.Max(resetDuration, 0.01f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                // 每帧：移动位置 + 面向目标；无聚焦目标时才退回位姿分别插值
                if (!ApplyResetFrame(t, fromYaw, fromPitch, fromDistance))
                    transform.SetPositionAndRotation(
                        Vector3.Lerp(fromPos, startPos, t),
                        Quaternion.Slerp(fromRot, startRot, t));

                for (int i = 0; i < _cameras.Length; i++)
                    _cameras[i].fieldOfView = Mathf.Lerp(fromFov[i], defaultFov, t);

                yield return null;
            }

            SetAllCamerasFov(defaultFov);

            // 有聚焦目标时：ApplyResetFrame(1) 已把位置摆到 startPos、朝向对准目标，
            // 且 _yaw/_pitch/distance 与结果同源，下一帧 HandlePos 会复现同一结果，无需 _freezeOrbit 停一帧。
            if (!ApplyResetFrame(1f, fromYaw, fromPitch, fromDistance))
            {
                transform.SetPositionAndRotation(startPos, startRot);

                // 无目标：轨道角按初始位姿推导，并留一帧不被 HandlePos 覆盖
                if (_target != null)
                    InitializeOrbit(_target);
                _freezeOrbit = true;
            }

            _isTransitioning = false;
            _resetBackCoroutine = null;
        }

        IEnumerator SmoothFollow()
        {
            _isTransitioning = true;

            // 局部名不要用 startPos/startRot，避免遮蔽基类记录的初始位姿
            Vector3 fromPos = transform.position;
            Quaternion fromRot = transform.rotation;
            Transform target = _target;

            // 环绕半径取「相机与目标的实际距离」；Start 时 _target 为空、轨道没初始化过也不会跳到序列化默认值
            float keepDistance = Vector3.Distance(fromPos, target.position);
            if (keepDistance < 1e-4f)
            {
                Log.Error("相机与聚焦目标重合，无法计算环绕半径");
                _isTransitioning = false;
                _smoothFollowCoroutine = null;
                yield break;
            }
            distance = keepDistance;

            // 计算目标状态：保持相机在目标的同一侧、同一半径，只把「看向目标」的角度转正
            Vector3 toTarget = target.position - fromPos;
            float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(toTarget.y / keepDistance, -1f, 1f)) * Mathf.Rad2Deg, BottomClamp, TopClamp);
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

                transform.position = Vector3.Lerp(fromPos, finalPos, t);
                transform.rotation = Quaternion.Slerp(fromRot, finalRot, t);
                yield return null;
            }

            transform.SetPositionAndRotation(finalPos, finalRot);

            // 切回轨道角度模式：直接沿用本次过渡计算出的角度（与 finalPos/finalRot 同源）。
            // 不要再从 transform 反推 yaw，方向取反会差 180°，下一帧 HandlePos 就会翻到目标另一侧。
            _yaw = targetYaw;
            _pitch = targetPitch;

            _isTransitioning = false;
            _smoothFollowCoroutine = null;
        }

        #region 继承方法

        public override void Transport(Transform target)
        {
            if (target == null) return;
            _target = target;

            // 切换聚焦目标时打断任何进行中的过渡（含双击复位），避免两个协程抢着写 transform
            if (_resetBackCoroutine != null)
            {
                StopCoroutine(_resetBackCoroutine);
                _resetBackCoroutine = null;
            }
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