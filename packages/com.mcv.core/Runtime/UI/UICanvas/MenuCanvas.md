# Contract: MenuCanvas

Role: menu-page canvas; on rebuild it assembles the menu panel only -- the AI dialog panel is deliberately not mounted on this page.

Methods:
Awake()  base only
OnRebuild()  GetPanel<MenuPanel> then SetUIActive(true); no AI dialog panel

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- The menu page never shows a "back to main menu" button because it is the main menu; that button is configured on FunctionPanel, not here.
- The AI dialog panel is intentionally absent here (the user asked for it): only ContentCanvas and RoamingCanvas mount it, so do not re-add a GetPanel<AiDialogPanel> call on this canvas.
- The explicit SetUIActive(true) is required because MenuPanel's show/hide is Animator-driven and that Animator's `active` Bool defaults to false: without the call the panel would sit in the Hide state (and, with the current clips, at alpha 0) immediately after the rebuild.
