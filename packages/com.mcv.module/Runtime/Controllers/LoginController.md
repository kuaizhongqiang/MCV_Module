# Contract: LoginController

Role: login controller; on the panel's login event it verifies the input, writes the user data, starts the score session and publishes LoginSuccessEvent.

Methods:
OnViewBound()  clear then add OnLoginRequested
OnLoginRequested(panel)  read name / password / type -> VerifyLogin; on failure warn + ShowTipsError; else SetUserData -> BeginScoreSession -> publish LoginSuccessEvent -> ShowTipsSuccess

Notes:
- The scene switch after a successful login is owned by GlobalUIMgr, which subscribes LoginSuccessEvent (see its OnLoginSuccess); this controller only verifies, writes data and publishes.
- BeginScoreSession must run after SetUserData: the profile file name comes from the user data.
- The white-list check (GlobalDataMgr.VerifyLogin) is currently empty and always passes, so any account logs in.
