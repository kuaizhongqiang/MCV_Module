using MCV_Module.Event;
using UnityEngine;

namespace MCV_Module.Interactive
{
    /// <summary>
    /// 连线端点 —— 按命名编码配对机制工作。
    /// 命名约定: Line_GroupID_EndpointRole (例: Line_Circuit1_Start)
    /// </summary>
    public class LineEndpoint : InteractiveBase
    {
        [SerializeField] private string _groupId;
        [SerializeField] private string _endpointRole;

        [SerializeField] private Color _connectedColor = Color.green;
        [SerializeField] private Color _highlightColor = Color.yellow;
        [SerializeField] private Color _selectedColor = Color.blue;
        [SerializeField] private Renderer _renderer;

        private bool _isConnected;
        private Color _originalColor;
        private static LineEndpoint s_SelectedEndpoint;

        public string GroupId => _groupId;
        public string EndpointRole => _endpointRole;
        public bool IsConnected => _isConnected;
        public static LineEndpoint SelectedEndpoint => s_SelectedEndpoint;

        private void Start()
        {
            if (_renderer != null && _renderer.material != null)
                _originalColor = _renderer.material.color;
        }

        public bool CanConnectTo(LineEndpoint other)
        {
            if (other == null) return false;
            if (_isConnected || other._isConnected) return false;
            if (_groupId != other._groupId) return false;
            if (_endpointRole == other._endpointRole) return false;
            return true;
        }

        public void Connect()
        {
            _isConnected = true;
            _renderer.material.color = _connectedColor;
        }

        public void Disconnect()
        {
            _isConnected = false;
            _renderer.material.color = _originalColor;
        }

        protected override void MoEnterEvent()
        {
            if (!_isConnected && _renderer != null)
                _renderer.material.color = _highlightColor;
        }

        protected override void MoExitEvent()
        {
            if (!_isConnected && _renderer != null)
            {
                // 如果是已选中端点，保持选中色，否则还原
                _renderer.material.color = s_SelectedEndpoint == this ? _selectedColor : _originalColor;
            }
        }

        protected override void MoClickEvent()
        {
            if (_isConnected)
            {
                Disconnect();
                EventBus<LineConnectedEvent>.Publish(new LineConnectedEvent(_groupId, false));
                s_SelectedEndpoint = null;
                return;
            }

            // 没有选中的端点 → 选中当前端点
            if (s_SelectedEndpoint == null)
            {
                s_SelectedEndpoint = this;
                _renderer.material.color = _selectedColor;
                return;
            }

            // 已有选中的端点 → 尝试配对
            if (CanConnectTo(s_SelectedEndpoint))
            {
                Connect();
                s_SelectedEndpoint.Connect();
                EventBus<LineConnectedEvent>.Publish(new LineConnectedEvent(_groupId, true));

                // 清除对端的高亮
                s_SelectedEndpoint._renderer.material.color = s_SelectedEndpoint._originalColor;
                s_SelectedEndpoint = null;
            }
            else
            {
                // 配对失败：取消之前的选中
                s_SelectedEndpoint._renderer.material.color = s_SelectedEndpoint._originalColor;
                s_SelectedEndpoint = null;
            }
        }
    }
}
