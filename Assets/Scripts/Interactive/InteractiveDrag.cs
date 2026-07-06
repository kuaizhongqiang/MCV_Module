using MCV_Module.GlobalManager;
using UnityEngine;

namespace MCV_Module.Interactive
{
    /// <summary>
    /// 可拖拽交互元件 —— InteractiveBase 的 Drag 类型子类。
    /// 使用 GlobalCameraMgr.Camera 统一相机引用。
    /// </summary>
    public class InteractiveDrag : InteractiveBase
    {
        [SerializeField] private bool _useDragConstraint;
        [SerializeField] private Vector3 _dragOffset = Vector3.zero;

        private Camera _cam;
        private Vector3 _dragStartPosition;
        private bool _isDragging;

        protected override void MoDownEvent()
        {
            base.MoDownEvent();
            _cam = GlobalCameraMgr.Camera;
            _dragStartPosition = transform.position;
            _isDragging = true;
        }

        protected override void MoUpEvent()
        {
            base.MoUpEvent();
            _isDragging = false;
        }

        protected override void MoMoveEvent(Vector2 delta)
        {
            base.MoMoveEvent(delta);
            if (!_isDragging || _cam == null) return;

            Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 300f))
            {
                Vector3 targetPos = hit.point + _dragOffset;
                if (_useDragConstraint)
                    targetPos.y = _dragStartPosition.y;
                transform.position = targetPos;
            }
        }
    }
}
