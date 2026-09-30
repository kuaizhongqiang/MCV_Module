# Contract: LoginPanel

Role: login view; user name and password input plus the login button, rendering the login type and the tip line.

Fields:
titleText:Text  title, switched by login type
userNameLabel / passwordLabel:Text  field labels; the password label is hidden for guest login
loginButtonLabel:Text  login button label
tipsTextLabel:Text  tip line
loginButton:Button  login button
userNameInputField / passwordInputField:InputField  inputs; the password one is hidden for guest login
userTypeDropdown:Dropdown  user type dropdown (default Unknow)
currentType:UserType  current login type
m_UserTypeOptions:readonly List<UserType>  dropdown index -> UserType map (Admin excluded)
TipsNormalColor / TipsErrorColor / TipsSuccessColor  static readonly Color; grey / red / green
m_TipsTextComp:TextComponent  the tip line's component, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the tip color must go through the component to reach the TMP that is actually rendered
m_TitleTextComp:TextComponent  the title node's component, cached in Awake for the same reason; after the swap the Text field is a fake null and TextComponent.SetTextOn silently no-ops
m_UserNameLabelComp / m_PasswordLabelComp / m_LoginButtonLabelComp:TextComponent  the three display-only labels' components, cached in Awake; they are never written, but their nodes all carry a TextComponent and get unloaded in TMP form, so without the caches the Awake guard would falsely report a missing reference and `passwordLabel.gameObject.SetActive(...)` would be silently skipped (the guest's password label would stay visible)
UserName / Password / UserType  properties over the inputs and the current type

Methods:
Awake()  base first, cache the tip line's and the title's TextComponents (parsing has to happen before the swap: the swap starts in Awake while Destroy only takes effect at the end of the frame), validate all nine references, bind the login button, init the dropdown, apply the default (Unknow) UI
OnDestroy()  unbind the login button and the dropdown listener, call base
SetLoginTypeUI(type)  set currentType, sync the dropdown index, refresh the UI
ShowTips(message) / ShowTips(message, color) / ShowTipsError(message) / ShowTipsSuccess(message)  write the tip line through m_TipsTextComp.SetText (falls back to TextComponent.SetTextOn only when the node has no component); the color goes through m_TipsTextComp.ColorValue (falls back to TextComponent.SetColorOn likewise)
OnLoginButtonClick()  required-field check (guest: user name only; student / teacher: name and password) -> tip and raise OnLoginRequested
OnUserTypeChanged(index)  map the dropdown index back to UserType -> clear both inputs -> refresh the UI
UpdateLoginTypeUI()  refresh the title through m_TitleTextComp (falls back to TextComponent.SetTextOn) and show or hide the password label and input; the label's node is resolved through m_PasswordLabelComp (its GameObject survives the swap) rather than the Text field, which would be a fake null in TMP form
GetLoginTitle(type)  type -> "学生登录" / "教师登录" / "管理员登录" / "游客登录"
InitUserTypeDropdown()  fill the dropdown in UserType enum order, skipping Admin, keeping m_UserTypeOptions in step
GetUserTypeDisplayName(type)  type -> the dropdown option label, read through Lang.Get (ui.login.type.student / teacher / admin / unknown); it must stay in code because the Dropdown is a legacy-Text exception node and never takes a languageKey

Notes:
- Flow: click login -> OnLoginRequested -> LoginController white-list check (still empty, passes) -> LoginSuccessEvent; the panel never logs in by itself.
- The dropdown index must stay aligned with m_UserTypeOptions, which is why Admin is skipped (its entry is not on this panel).
- Assigning userTypeDropdown.value fires onValueChanged, so SetLoginTypeUI also runs OnUserTypeChanged and clears both inputs.
- For Unknow (guest) the password label and the input object are both hidden, otherwise the guest sees an empty password row.
- Switching login type clears the inputs so credentials of the previous type are never reused.
- The panel is rebuilt with its Canvas, so currentType resets to Unknow on every rebuild and must be re-applied by the controller.
- Why the tip cache is required (visual correctness, not crash safety): the field stays typed `Text` (retyping it would break dozens of prefab references) and the node's legacy Text is unloaded rather than retired, so in TMP form the field is a fake null and `tipsTextLabel.color` would throw MissingReferenceException; the rendered control is the TMP, and ColorValue goes through whatever the component currently owns.
