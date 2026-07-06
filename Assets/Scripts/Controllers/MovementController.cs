using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controller
{
    /// <summary>
    /// 漫游 FPS 控制器 —— WASD 移动 + 鼠标视角。
    /// 严格遵循 MCV：Controller 挂载在场景物体上，数据流单向。
    /// </summary>
    public class MovementController : ControllerBase<RoamingBottomPanel>
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _lookSpeed = 2f;
        [SerializeField] private float _pitchLimit = 80f;

        private CharacterController _controller;
        private Camera _playerCamera;
        private Vector3 _moveInput;
        private float _pitch;
        private float _yaw;

        protected override void OnViewBound()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
                _controller = gameObject.AddComponent<CharacterController>();

            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                var camObj = new GameObject("PlayerCamera");
                camObj.transform.SetParent(transform);
                camObj.transform.localPosition = new Vector3(0, 1.7f, 0);
                _playerCamera = camObj.AddComponent<Camera>();
            }

            // 锁定鼠标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_controller == null || _playerCamera == null) return;

            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * _lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * _lookSpeed;

            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -_pitchLimit, _pitchLimit);

            transform.localRotation = Quaternion.Euler(0, _yaw, 0);
            _playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        private void HandleMove()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 move = transform.right * h + transform.forward * v;
            if (move.magnitude > 1f) move.Normalize();

            // 重力
            if (!_controller.isGrounded)
                move.y = -9.81f;

            _controller.Move(move * _moveSpeed * Time.deltaTime);

            // ESC 释放鼠标
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
