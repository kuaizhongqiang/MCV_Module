
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class LoginPanel : PanelBase
    {
        [SerializeField] GameObject _nameInput;
        [SerializeField] GameObject _passwordInput;
        [SerializeField] TextMeshProUGUI _tipsText;
        [SerializeField] Button _loginButton;

        TextMeshProUGUI _nameLabel;
        TMP_InputField _nameInputField;
        TextMeshProUGUI _passwordLabel;
        TMP_InputField _passwordInputField;

        public event Action OnLogin;

        protected override void Awake()
        {
            base.Awake();

            InputGroupHelper.InputGroup(_nameInput, out _nameLabel, out _nameInputField);
            InputGroupHelper.InputGroup(_passwordInput, out _passwordLabel, out _passwordInputField);
        }

        void SetTipsText(string text)
        {
            _tipsText.text = text;
        }

        public string GetName()
        {
            return _nameInputField.text;
        }
        public string GetPassword()
        {
            return _passwordInputField.text;
        }
    }
}
