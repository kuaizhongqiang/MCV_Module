
using MCV_Module.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class LoginPanel : PanelBase
    {
        [SerializeField] TextComponent titleText;
        [SerializeField] TextComponent userNameLabel;
        [SerializeField] TextComponent passwordLabel;
        [SerializeField] TextComponent loginButtonLabel;
        [SerializeField] TextComponent tipsTextLabel;
        [SerializeField] Toggle guestTypeToggle;
        [SerializeField] Toggle studentTypeToggle;
        [SerializeField] Toggle teacherTypeToggle;
        [SerializeField] InputFieldComponent userNameInputField;
        [SerializeField] InputFieldComponent passwordInputField;
        TextComponent guestTypeToggleLabel;
        TextComponent studentTypeToggleLabel;
        TextComponent teacherTypeToggleLabel;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            if (titleText == null || userNameLabel == null || 
                passwordLabel == null || loginButtonLabel == null || 
                tipsTextLabel == null || guestTypeToggle == null || 
                studentTypeToggle == null || teacherTypeToggle == null || 
                userNameInputField == null || passwordInputField == null)
            {
                Debug.LogError($"[LoginPanel] 缺少必要组件", this);
                return;
            }
            guestTypeToggleLabel = guestTypeToggle.GetComponentInChildren<TextComponent>();
            studentTypeToggleLabel = studentTypeToggle.GetComponentInChildren<TextComponent>();
            teacherTypeToggleLabel = teacherTypeToggle.GetComponentInChildren<TextComponent>();
        }
        #endregion
    }
}
