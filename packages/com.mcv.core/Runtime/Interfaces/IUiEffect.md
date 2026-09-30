# Contract: IUiEffect

Role: contract for UI hover and click effect components; lets a UI element react to pointer enter, exit and click.

Methods:
MoEnter()  pointer entered the UI
MoExit()  pointer left the UI
MoClick()  the UI was clicked

Notes:
- Implementer: UiAudioEffectBase (abstract, extends UIBase) under UI/Tools; it drives these from the UGUI IPointer* handlers and subclasses may override.
- UI effects ride the UGUI event system and are independent of GlobalInteractiveMgr's 3D raycasts; a deactivated or non-interactable component receives no events at all.
