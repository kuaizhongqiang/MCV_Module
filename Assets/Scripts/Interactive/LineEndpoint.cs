using UnityEngine;

namespace MCV_Module.Interactive
{
    /// <summary>
    /// 连线端点 —— 按命名编码配对机制工作。
    /// 命名约定: Line_GroupID_EndpointRole
    /// 例: Line_Circuit1_Start, Line_Circuit1_End
    /// </summary>
    public class LineEndpoint : InteractiveBase
    {
        [SerializeField] private string _groupId;
        [SerializeField] private string _endpointRole; // Start / End

        [SerializeField] private Color _connectedColor = Color.green;
        [SerializeField] private Color _highlightColor = Color.yellow;
        [SerializeField] private Renderer _renderer;

        private bool _isConnected;
        private Color _originalColor;
        public string GroupId => _groupId;
        public string EndpointRole => _endpointRole;
        public bool IsConnected => _isConnected;

        private void Start()
        {
            if (_renderer != null && _renderer.material != null)
                _originalColor = _renderer.material.color;
        }

        public bool CanConnectTo(LineEndpoint other)
        {
            if (_isConnected || other._isConnected) return false;
            if (_groupId != other._groupId) return false;
            if (_endpointRole == other._endpointRole) return false;
            return true;
        }

        public void Connect()
        {
            _isConnected = true;
            if (_renderer != null)
                _renderer.material.color = _connectedColor;
        }

        public void Disconnect()
        {
            _isConnected = false;
            if (_renderer != null)
                _renderer.material.color = _originalColor;
        }

        protected override void MoEnterEvent()
        {
            base.MoEnterEvent();
            if (!_isConnected && _renderer != null)
                _renderer.material.color = _highlightColor;
        }

        protected override void MoExitEvent()
        {
            base.MoExitEvent();
            if (!_isConnected && _renderer != null)
                _renderer.material.color = _originalColor;
        }

        protected override void MoClickEvent()
        {
            base.MoClickEvent();
            if (_isConnected)
            {
                Disconnect();
                return;
            }
            // TODO: 在 LineConnectionController 中处理配对逻辑
        }
    }
}
