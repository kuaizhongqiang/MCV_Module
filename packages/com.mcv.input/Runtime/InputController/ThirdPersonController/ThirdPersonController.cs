using MCV_Module.InputController.Common.InputSystem;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;




namespace MCV_Module.InputController.ThirdPersonController
{
    // WHY: 动画全部经 Animator 驱动，角色与胶囊体一律先做 animator 空值检查；去掉检查会在无 Animator 时报空。
    /// <summary>第三人称玩家控制器：处理移动/冲刺与平滑转向、跳跃与重力、地面检测、摄像机朝向、脚步/落地音效与瞬移。</summary>
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

        /// <summary>摄像机目标 yaw/pitch（由输入累加并钳制）。</summary>
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        /// <summary>玩家移动状态：当前速度、动画混合、目标朝向与垂直速度。</summary>
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        /// <summary>基于增量时间的超时计时器（跳跃冷却 / 下落判定）。</summary>
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        /// <summary>动画参数哈希 ID（由 AssignAnimationIDs 填充）。</summary>
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

        /// <summary>初始化：缓存组件与动画参数 ID，设定基类摄像机角度限制，重置跳跃 / 下落超时计时器。</summary>
        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();

            AssignAnimationIDs();

            TopClamp = 70.0f;
            BottomClamp = -30.0f;

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

        /// <summary>用带偏移的球体检测着地状态，并同步动画器 Grounded 参数。</summary>
        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        // WHY: 鼠标方案的视角输入本身帧率无关，乘 Time.deltaTime 会让转速随帧率变化。
        /// <summary>按视角输入累加并钳制 yaw/pitch，并驱动 Cinemachine 朝向目标。</summary>
        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        /// <summary>按输入/冲刺算出目标速度并平滑加减速、必要时平滑转向，驱动 CharacterController 位移并同步动画参数。</summary>
        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        // WHY: 未着地时必须清掉 _input.jump，否则落地瞬间残留输入会自动再跳。
        /// <summary>处理跳跃与重力：着地时重置计时并起跳，空中累积下落计时超时后置 FreeFall，并持续施加重力。</summary>
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;
            }

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

        /// <summary>在选中时按着地状态着色，绘制着地检测球体。</summary>
        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

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

        /// <summary>瞬移方法（内部实现，通过 Transport 调用）。</summary>
        private bool Teleport(Vector3 targetPosition, Quaternion targetRotation, LayerMask? obstacleLayers = null, float checkRadius = 0.5f)
        {
            if (obstacleLayers.HasValue && Physics.CheckSphere(targetPosition, checkRadius, obstacleLayers.Value))
            {
                Log.Warning("目标位置存在障碍物，瞬移取消");
                return false;
            }

            _controller.enabled = false;

            transform.SetPositionAndRotation(targetPosition, targetRotation);

            Vector3 eulerRotation = targetRotation.eulerAngles;
            _cinemachineTargetYaw = eulerRotation.y;
            _cinemachineTargetPitch = eulerRotation.x;
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);

            if (ResetVerticalVelocityOnTeleport)
            {
                _verticalVelocity = 0f;
            }

            if (ImmediateGroundedCheck)
            {
                GroundedCheck();
            }

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
