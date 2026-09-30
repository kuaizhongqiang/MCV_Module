# Contract: LoginCanvas

Role: login-page canvas; on rebuild it assembles the login panel.

Methods:
Awake()  base only
OnRebuild()  GetPanel<LoginPanel> and log

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- Everything else about the login flow belongs to LoginPanel / LoginController; this canvas only builds the panel.
