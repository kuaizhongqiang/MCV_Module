# Contract: StartController

Role: start controller; orchestrates StartPanel's enter-login action.

Methods:
OnViewBound()  unsubscribe then subscribe View.OnStartRequested
OnStartRequested(panel)  publish SceneStateChangeEventData(Login)

Notes:
- Start -> Login is one-way: the panel only raises the event and holds no logic; the persistent GlobalUIMgr listens and switches the Canvas.
