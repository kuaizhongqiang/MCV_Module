# Contract: RoamingFunctionController

Role: roaming-page function controller; turns the back button into summon or hide of the MenuPanel popup under the roaming canvas.

Fields:
m_RoamingMenu:MenuPanel  the summoned menu panel; a rebuilt canvas drops it (fake-null) and the next click recreates it

Methods:
OnViewBound()  unsubscribe then subscribe View.OnBackBtnClick -> SetCopyright
OnDestroy()  unbind View.OnBackBtnClick -> base
OnBackClick()  no active canvas -> warn; else get or create MenuPanel and toggle its visibility by reading MenuPanel.IsShowing (NOT gameObject.activeSelf)

Notes:
- The menu on MenuCanvas must not be reused: two live instances would fight over MenuController's View binding.
- Visibility must be read from MenuPanel.IsShowing: the popup is hidden via Animator, never deactivated, so gameObject.activeSelf is always true (using it made the toggle hide-only after the first summon).
- The popup is hidden, not destroyed, so re-summoning skips an instantiation.
- GetPanel<MenuPanel>() also binds MenuController through [RequireController] the first time.
