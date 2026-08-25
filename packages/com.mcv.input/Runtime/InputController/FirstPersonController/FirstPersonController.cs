
using MCV_Module.InputController.Common.InputSystem;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCV_Module.InputController.FirstPersonController
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class FirstPersonController : InputControllerBase
    {
        [Header("玩家")]
        [Tooltip("角色的移动速度（米/秒）")]
        public float MoveSpeed = 4.0f;
        [Tooltip("角色的冲刺速度（米/秒）")]
        public float SprintSpeed = 6.0f;
        [Tooltip("角色的旋转速度")]
        public float RotationSpeed = 1.0f;
        [Tooltip("加速和减速")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("玩家可跳跃的高度")]
        public float JumpHeight = 1.2f;
        [Tooltip("角色使用自己的重力值。引擎默认值为 -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("再次跳跃前需要经过的时间。设为 0f 可立即再次跳跃")]
        public float JumpTimeout = 0.1f;
        [Tooltip("进入下落状态前需要经过的时间。有助于走下楼梯")]
        public float FallTimeout = 0.15f;

        [Header("玩家地面检测")]
        [Tooltip("角色是否着地。不属于 CharacterController 内置的着地检测")]
        public bool Grounded = true;
        [Tooltip("用于不平坦地面")]
        public float GroundedOffset = -0.14f;
        [Tooltip("着地检测的半径。应与 CharacterController 的半径匹配")]
        public float GroundedRadius = 0.5f;
        [Tooltip("角色用作地面的层级")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("Cinemachine 虚拟摄像机中设置的跟随目标，摄像机将跟随该目标")]
        public GameObject CinemachineCameraTarget;

        // Cinemachine 摄像机
        private float _cinemachineTargetPitch;

        // 玩家移动
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // 超时计时器（基于增量时间）
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;


        private PlayerInput _playerInput;
        private CharacterController _controller;
        private StarterAssetsInputs _input;

        private const float _threshold = 0.01f;

        //输入移动和转向
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
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();

            // 设置基类继承的摄像机角度限制
            TopClamp = 90.0f;
            BottomClamp = -90.0f;

            // 启动时重置超时计时器
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        protected override void Update()
        {
            base.Update();
            IsMoving = _input.move != Vector2.zero;
            ZoomHandle();
            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            if (!isActive)
            {
                return;
            }
            CameraRotation();
        }

        #region 移动

        private void GroundedCheck()
        {
            // 设置球体位置，带偏移量
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            // 如果有输入
            if (_input.look.sqrMagnitude >= _threshold)
            {
                // 不要将鼠标输入乘以 Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                // 限制俯仰角范围
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // 更新 Cinemachine 摄像机的目标俯仰角
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

                // 左右旋转玩家
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
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
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // 产生曲线效果而非线性效果，使速度变化更自然
                // 注意：Lerp 中的 T 会被钳制，因此无需手动限制速度
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // 将速度四舍五入到 3 位小数
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // 标准化输入方向
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 注意：Vector2 的 != 运算符使用近似比较，不会出现浮点误差，且比 magnitude 更高效
            // 如果有移动输入且玩家正在移动，则旋转玩家朝向
            if (_input.move != Vector2.zero)
            {
                // 移动
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            // 移动玩家
            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // 重置下落超时计时器
                _fallTimeoutDelta = FallTimeout;

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
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
        // 在FirstPersonController类中添加以下代码

        [Header("瞬移")]
        [Tooltip("瞬移时是否保留垂直速度")]
        public bool ResetVerticalVelocityOnTeleport = true;

        [Tooltip("瞬移时是否立即更新地面检测")]
        public bool ImmediateGroundedCheck = true;

        /// <summary>
        /// 瞬移方法
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="targetRotation">目标朝向</param>
        /// <param name="obstacleLayers">障碍物层级（传入时启用安全检测）</param>
        /// <param name="checkRadius">安全检测半径</param>
        /// <returns>是否成功瞬移</returns>
        private bool Teleport(Vector3 targetPosition, Quaternion targetRotation, LayerMask? obstacleLayers = null, float checkRadius = 0.5f)
        {
            // 障碍物安全检测
            if (obstacleLayers.HasValue && Physics.CheckSphere(targetPosition, checkRadius, obstacleLayers.Value))
            {
                Log.Warning("目标位置存在障碍物，瞬移取消");
                return false;
            }

            // 禁用CharacterController以避免位置更新冲突
            _controller.enabled = false;

            // 设置玩家位置和旋转
            transform.SetPositionAndRotation(targetPosition, targetRotation);

            // 同步摄像头旋转
            Vector3 eulerRotation = targetRotation.eulerAngles;
            _cinemachineTargetPitch = eulerRotation.x;
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0f, 0f);

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
