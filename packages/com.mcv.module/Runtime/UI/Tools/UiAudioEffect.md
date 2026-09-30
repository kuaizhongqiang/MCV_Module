# Contract: UiAudioEffectBase (+ UiAudioEffect)

Role: UI sound base class for any raycastable UI element; plays hover/exit/click effects from the UGUI event system and lets subclasses override the Mo* hooks.

Fields:
enterAudio:AudioEffectType  [SerializeField] hover sound, Hover
playEnter:bool  [SerializeField] enable the hover sound, true
exitAudio:AudioEffectType  [SerializeField] exit sound, None
playExit:bool  [SerializeField] enable the exit sound, false
clickAudio:AudioEffectType  [SerializeField] click sound, Click
playClick:bool  [SerializeField] enable the click sound, true
interactableOnly:bool  [SerializeField] play only while the Selectable is interactable, true

Methods:
OnPointerEnter / OnPointerExit / OnPointerClick(eventData)  CanPlay guard -> MoEnter / MoExit / MoClick
MoEnter() / MoExit() / MoClick()  virtual; play the matching effect when its flag is on
CanPlay()  private; active and enabled, plus a Selectable check when interactableOnly
PlayEffect(type)  private; skip AudioEffectType.None, then GlobalAudioMgr.PlayAudio(type)
UiAudioEffect  empty concrete subclass; attach it and the sounds work

Notes:
- Events come from the UGUI event system only; 3D interaction rays are handled by GlobalInteractiveMgr and never trigger these sounds.
- The component must be raycastable: a disabled GameObject, or a CanvasGroup with blocksRaycasts off, receives no pointer events at all.
- interactableOnly checks the Selectable on the same object; when there is none the sound plays unconditionally.
- AudioEffectType.None is filtered in PlayEffect, so "exit off by default" is expressed through playExit = false.
