
using MCV_Module.Models;
using MCV_Module.Utils;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Components
{
    /// <summary>
    /// 输入框组件：包装 TMP_InputField，按序列化配置初始化并提供取值/赋值 API。
    /// 统一采用 TMP 体系（与 TextComponent 一致），避免同一 UI 内新旧文本混用。
    /// </summary>
    public class InputFieldComponent : ComponentBase
    {
        #region 参数
        [Header("引用"), Tooltip("同物体或子物体上的 TMP_InputField，为空时自动查找")]
        [SerializeField] TMP_InputField inputField;

        [Header("初始化"), Tooltip("初始文本")]
        [SerializeField, TextArea(1, 3)] string defaultValue = "";

        [Header("配置")]
        [SerializeField] TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;
        [SerializeField] int characterLimit = 0;
        [SerializeField] bool readOnly;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            if (inputField == null)
            {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }
            if (inputField == null)
            {
                Log.Warning($"[InputFieldComponent] 未找到 TMP_InputField：{name}", this);
                return;
            }

            inputField.contentType = contentType;
            inputField.characterLimit = characterLimit;
            inputField.readOnly = readOnly;
            inputField.text = defaultValue;
        }
        #endregion

        #region 公开方法
        /// <summary>当前输入内容，赋值也会同步到输入框。</summary>
        public string Value
        {
            get => inputField != null ? inputField.text : string.Empty;
            set { if (inputField != null) inputField.text = value; }
        }

        /// <summary>是否有非空输入（去掉首尾空白判断）。</summary>
        public bool HasValue => inputField != null && !string.IsNullOrEmpty(inputField.text.Trim());

        /// <summary>清空输入框。</summary>
        public void Clear()
        {
            if (inputField != null) inputField.text = string.Empty;
        }
        #endregion
    }
}
