// using MCV_Module.UI.Panels;
// using UnityEngine;
// using UnityEngine.InputSystem;

// namespace MCV_Module.Controller
// {
//     /// <summary>
//     /// 漫游 FPS 控制器 —— WASD 移动 + 鼠标视角。
//     /// 使用新 InputSystem，与 GlobalInteractiveMgr 统一。
//     /// </summary>
//     public class MovementController : ControllerBase<RoamingBottomPanel>
//     {
//         [SerializeField] private float _moveSpeed = 5f;
//         [SerializeField] private float _lookSpeed = 2f;
//         [SerializeField] private float _pitchLimit = 80f;

//         private CharacterController _controller;
//         private Camera _playerCamera;
//         private Keyboard _keyboard;
//         private Mouse _mouse;
//         private Vector2 _moveInput;
//         private float _pitch;
//         private float _yaw;

//         protected override void OnViewBound()
//         {
//             _controller = GetComponent<CharacterController>();
//             if (_controller == null)
//                 _controller = gameObject.AddComponent<CharacterController>();

//             _playerCamera = GetComponentInChildren<Camera>();
//             if (_playerCamera == null)
//             {
//                 var camObj = new GameObject("PlayerCamera");
//                 camObj.transform.SetParent(transform);
//                 camObj.transform.localPosition = new Vector3(0, 1.7f, 0);
//                 _playerCamera = camObj.AddComponent<Camera>();
//             }

//             _keyboard = Keyboard.current;
//             _mouse = Mouse.current;

//             Cursor.lockState = CursorLockMode.Locked;
//             Cursor.visible = false;
//         }

//         private void Update()
//         {
//             if (_controller == null || _playerCamera == null) return;
//             if (_keyboard == null || _mouse == null) return;

//             HandleLook();
//             HandleMove();

//             if (_keyboard.escapeKey.wasPressedThisFrame)
//             {
//                 Cursor.lockState = CursorLockMode.None;
//                 Cursor.visible = true;
//             }
//         }

//         private void HandleLook()
//         {
//             Vector2 delta = _mouse.delta.ReadValue() * _lookSpeed * Time.deltaTime;
//             _yaw += delta.x;
//             _pitch -= delta.y;
//             _pitch = Mathf.Clamp(_pitch, -_pitchLimit, _pitchLimit);

//             transform.localRotation = Quaternion.Euler(0, _yaw, 0);
//             _playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
//         }

//         private void HandleMove()
//         {
//             _moveInput = _keyboard.wasdKey.ReadValue();

//             Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
//             if (move.magnitude > 1f) move.Normalize();

//             if (!_controller.isGrounded)
//                 move.y = -9.81f;

//             _controller.Move(move * _moveSpeed * Time.deltaTime);
//         }

//         protected override void OnDestroy()
//         {
//             base.OnDestroy();
//             Cursor.lockState = CursorLockMode.None;
//             Cursor.visible = true;
//         }
//     }
// }
