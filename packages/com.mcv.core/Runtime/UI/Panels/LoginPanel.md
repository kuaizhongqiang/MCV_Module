# Contract: LoginPanel

Role: login view; user name and password input plus the login button, rendering the login type and the tip line. Partially implemented (see Notes).

Fields:
titleText:TextComponent  title node, re-driven by login type in UpdateLoginTypeUI
userNameLabel / passwordLabel:TextComponent  field labels (display only; nothing hides them yet)
loginButtonLabel:TextComponent  login button label (display only, never written)
tipsTextLabel:TextComponent  tip node (declared but currently unwritten — see Notes)
loginButton:Button  login button
userNameInputField / passwordInputField:InputField  inputs (the password one is not hidden for guest login yet)
userTypeDropdown:Dropdown  user type dropdown, default Unknow
currentType:UserType  current login type, default Unknow
m_UserTypeOptions:readonly List<UserType>  dropdown index -> UserType map (Admin excluded)
TipsNormalColor / TipsErrorColor / TipsSuccessColor  static readonly Color; grey / red / green
OnLoginRequested:event Action<LoginPanel>  raised after the required-field check passes
UserName / Password / UserType  properties over the two inputs and currentType

Methods:
Awake()  base first, validate the nine serialized references (five text nodes, the button, the two inputs, the dropdown), bind the login button, fill the dropdown, bind its listener and apply the default (Unknow) UI; logs an error and returns when a reference is missing
OnDestroy()  base, then unbind the login button and the dropdown listener
SetLoginTypeUI(type)  set currentType, sync the dropdown index and refresh the UI
ShowTips(message) / ShowTipsError(message) / ShowTipsSuccess(message)  forward to ShowTips(message, color) with the normal / error / success colour
ShowTips(message, color)  ⚠️ empty stub — the tip text and colour are not written yet
OnLoginButtonClick()  required-field check (guest: user name only; student / teacher: name and password) -> ShowTips -> raise OnLoginRequested
OnUserTypeChanged(index)  map the dropdown index back to UserType -> clear both inputs -> refresh the UI
UpdateLoginTypeUI()  ⚠️ only re-drives titleText; hiding the password label / input for guest login is not implemented yet
GetLoginTitle(type)  type -> Lang.Get(ui.login.title.student / teacher / admin / guest)
InitUserTypeDropdown()  fill the dropdown in UserType enum order, skipping Admin, keeping m_UserTypeOptions in step
GetUserTypeDisplayName(type)  type -> the dropdown option label, read through Lang.Get (ui.login.type.*); it must stay in code because the Dropdown is a legacy-Text exception node and never takes a languageKey

Notes:
- Flow: click login -> OnLoginRequested -> LoginController white-list check (still empty, passes) -> LoginSuccessEvent; the panel never logs in by itself.
- The dropdown index must stay aligned with m_UserTypeOptions, which is why Admin is skipped (its entry is not on this panel).
- Assigning userTypeDropdown.value fires onValueChanged, so SetLoginTypeUI also runs OnUserTypeChanged and clears both inputs.
- Switching login type clears the inputs so credentials of the previous type are never reused.
- The panel is rebuilt with its Canvas, so currentType resets to Unknow on every rebuild and must be re-applied by the controller.
- All five text nodes are typed `TextComponent` (retyped from the legacy `Text` fields); the earlier per-node caches (m_TipsTextComp / m_TitleTextComp / m_UserNameLabelComp / …) were dropped with that retype, so the Awake guard now checks the fields directly.
- ⚠️ Incomplete (recorded in TODO.md, not changed here): ShowTips(message, color) has an empty body, UpdateLoginTypeUI does not hide the password label / input, and the guest branch of the label check is unfinished. The tip colour fields are therefore currently unused.
