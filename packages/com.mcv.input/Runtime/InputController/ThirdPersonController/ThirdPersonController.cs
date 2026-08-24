using MCV_Module.InputController.Common.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

/* 注：动画通过控制器播放，角色和胶囊体均使用 animator 空值检查 */


namespace MCV_Module.InputController.ThirdPersonController
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class ThirdPersonController : InputControllerBase
    {
        [Header("玩家")]
        [Tooltip("角色的移动速度（米/秒）")]
        public float MoveSpeed = 2.0f;

        [Tooltip("角色的冲刺速度（米/秒）")]
        public float SprintSpeed = 5.335f;

        [Tooltip("角色转向移动方向的平滑时间")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("加速和减速")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("玩家可跳跃的高度")]
        public float JumpHeight = 1.2f;

        [Tooltip("角色使用自己的重力值。引擎默认值为 -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("再次跳跃前需要经过的时间。设为 0f 可立即再次跳跃")]
        public float JumpTimeout = 0.50f;

        [Tooltip("进入下落状态前需要经过的时间。有助于走下楼梯")]
        public float FallTimeout = 0.15f;

        [Header("玩家地面检测")]
        [Tooltip("角色是否着地。不属于 CharacterController 内置的着地检测")]
        public bool Grounded = true;

        [Tooltip("用于不平坦地面")]
        public float GroundedOffset = -0.14f;

        [Tooltip("着地检测的半径。应与 CharacterController 的半径匹配")]
        public float GroundedRadius = 0.28f;

        [Tooltip("角色用作地面的层级")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("Cinemachine 虚拟摄像机中设置的跟随目标，摄像机将跟随该目标")]
        public GameObject CinemachineCameraTarget;

        // TopClamp 和 BottomClamp 由基类 ControllerBase 提供，此处不再重复声明

        [Tooltip("用于覆盖摄像机的附加角度。锁定位置时微调摄像机角度")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("锁定摄像机在所有轴向上的位置")]
        public bool LockCameraPosition = false;

        // Cinemachine 摄像机
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // 玩家移动
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // 超时计时器（基于增量时间）
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // 动画参数哈希 ID
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private PlayerInput _playerInput;
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
                return _playerInput.currentControlScheme == "KeyboardMouse";
            }
        }


        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();

            AssignAnimationIDs();

            // 设置基类继承的摄像机角度限制
            TopClamp = 70.0f;
            BottomClamp = -30.0f;

            // 启动时重置超时计时器
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        protected override void Update()
        {
            base.Update();

            _hasAnimator = TryGetComponent(out _animator);

            IsMoving = _input.move != Vector2.zero;
            ZoomHandle();
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            if (!isActive) return;
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // 设置球体位置，带偏移量
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // 更新动画参数（使用动画器的角色）
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // 有输入且未锁定摄像机位置
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // 不要将鼠标输入乘以 Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // 将旋转值限制在 360 度范围内
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine 将跟随此目标
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // 根据移动速度、冲刺速度和是否按下冲刺键设定目标速度
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // 一个简化的加速和减速逻辑，便于移除、替换或迭代

            // 注意：Vector2 的 == 运算符使用近似比较，不会出现浮点误差，且比 magnitude 更高效
            // 如果没有输入，则将目标速度设为 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // 获取玩家当前水平速度的引用
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 加速或减速到目标速度
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // 产生曲线效果而非线性效果，使速度变化更自然
                // 注意：Lerp 中的 T 会被钳制，因此无需手动限制速度
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // 将速度四舍五入到 3 位小数
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // 标准化输入方向
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 注意：Vector2 的 != 运算符使用近似比较，不会出现浮点误差，且比 magnitude 更高效
            // 如果有移动输入且玩家正在移动，则旋转玩家朝向
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // 旋转角色朝向摄像机视角下的输入方向
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // 移动玩家
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // 更新动画参数（使用动画器的角色）
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // 重置下落超时计时器
                _fallTimeoutDelta = FallTimeout;

                // 更新动画参数（使用动画器的角色）
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // 着地时阻止速度无限下降
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // 跳跃
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // H * -2 * G 的平方根 = 达到目标高度所需的速度
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 更新动画参数（使用动画器的角色）
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // 跳跃超时
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // 重置跳跃超时计时器
                _jumpTimeoutDelta = JumpTimeout;

                // 下落超时
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // 更新动画参数（使用动画器的角色）
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // 如果未着地，禁止跳跃
                _input.jump = false;
            }

            // 在未达到终端速度时持续应用重力（乘以两次增量时间以实现线性加速）
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // 选中时，在着地检测碰撞体的位置绘制与半径匹配的 Gizmo 球体
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        #region 瞬移 (Teleport)

        [Header("瞬移")]
        [Tooltip("瞬移时是否保留垂直速度")]
        public bool ResetVerticalVelocityOnTeleport = true;

        [Tooltip("瞬移时是否立即更新地面检测")]
        public bool ImmediateGroundedCheck = true;

        /// <summary>
        /// 瞬移方法（内部实现，通过 Transport 调用）
        /// </summary>
        private bool Teleport(Vector3 targetPosition, Quaternion targetRotation, LayerMask? obstacleLayers = null, float checkRadius = 0.5f)
        {
            // 障碍物安全检测
            if (obstacleLayers.HasValue && Physics.CheckSphere(targetPosition, checkRadius, obstacleLayers.Value))
            {
                Debug.LogWarning("目标位置存在障碍物，瞬移取消");
                return false;
            }

            // 禁用CharacterController以避免位置更新冲突
            _controller.enabled = false;

            // 设置玩家位置和旋转
            transform.SetPositionAndRotation(targetPosition, targetRotation);

            // 同步摄像头旋转
            Vector3 eulerRotation = targetRotation.eulerAngles;
            _cinemachineTargetYaw = eulerRotation.y;
            _cinemachineTargetPitch = eulerRotation.x;
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);

            // 重置物理相关参数
            if (ResetVerticalVelocityOnTeleport)
            {
                _verticalVelocity = 0f;
            }

            // 强制更新地面检测
            if (ImmediateGroundedCheck)
            {
                GroundedCheck();
            }

            // 重新启用CharacterController
            _controller.enabled = true;
            return true;
        }

        #region 实现 ControllerBase 抽象方法

        public override void Transport(Transform target)
        {
            if (target != null)
            {
                Teleport(target.position, target.rotation);
            }
        }

        #endregion
        #endregion
    }
}
