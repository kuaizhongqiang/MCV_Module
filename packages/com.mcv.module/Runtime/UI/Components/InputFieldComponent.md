# Contract: InputFieldComponent

Role: wraps TMP_InputField; applies the serialized configuration in Awake and exposes value and clear helpers.

Fields:
inputField:TMP_InputField  [SerializeField] on this object or a child; auto-found when empty
defaultValue:string  [SerializeField] [TextArea] initial text
contentType:TMP_InputField.ContentType  [SerializeField] Standard
characterLimit:int  [SerializeField] 0 = unlimited
readOnly:bool  [SerializeField]

Methods:
Awake()  resolve inputField (GetComponentInChildren, inactive included) -> warn and return when missing -> apply contentType / characterLimit / readOnly / defaultValue
Value  get/set the text; null-safe (empty string when the field is missing)
HasValue  non-empty after Trim
Clear()  empty the field

Notes:
- Uses the TMP series on purpose to match TextComponent, so one UI never mixes Legacy and TMP input.
- The configuration is written in Awake, so these serialized values always win over the prefab's own settings.
