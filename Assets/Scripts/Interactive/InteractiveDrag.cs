using UnityEngine;

namespace MCV_Module.Interactive
{
    /// <summary>
    /// 可拖拽交互元件 —— InteractiveBase 的 Drag 类型子类。
    /// 支持 OnDragEnter / OnDragStay / OnDragExit / OnDragUp。
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
            _cam = Camera.main;
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

            // 将鼠标屏幕坐标转换为世界坐标进行拖拽
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 300f))
            {
                Vector3 targetPos = hit.point + _dragOffset;
                if (_useDragConstraint)
                {
                    // 约束在原始 Y 轴高度
                    targetPos.y = _dragStartPosition.y;
                }
                transform.position = targetPos;
            }
        }
    }
}
