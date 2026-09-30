# Contract: UIBase

Role: base for every UI element that fades in and out through a CanvasGroup; owns the show/hide animation and the hidden callback.

Fields:
canvasGroup:CanvasGroup  [RequireComponent] cached in Awake
ActiveAnimCoroutine:Coroutine  the running show/hide animation
isAnimating:bool  true while the animation runs
m_OnHiddenCallback:Action  invoked after the hide animation, before the GameObject is deactivated
isActiveOnInstance:bool  [SerializeField] visible on instantiate, true
isInteractable:bool  [SerializeField] interactable flag, true
animTime:float  [SerializeField] fade duration in seconds, 0.3
AnimDuration:float  public read-only view of animTime, for callers estimating the wait

Methods:
Awake()  virtual; cache CanvasGroup, apply interactable/blocksRaycasts, SetActive(isActiveOnInstance)
OnDestroy()  virtual; empty hook
SetUIActive(isActive) / SetUIActive(isActive, onHidden)  stop the running animation, store or clear the hidden callback, alpha 0 + SetActive(true) when showing, then start Anim
SetUIActiveImmediately(isActive)  stop the animation, SetActive, alpha 0/1, sync interactable/blocksRaycasts
StopRunningAnim()  stop and reset the animation
Anim(isActive)  Lerp CanvasGroup.alpha over animTime; on hide invoke the callback, then SetActive(false)
ClearChildren(parent)  static; destroy every child from the last index down

Notes:
- The hidden callback runs before SetActive(false), so a business callback never fires on a deactivated object (StartCoroutine would throw there).
- StopRunningAnim must null-check: a coroutine stopped externally leaves ActiveAnimCoroutine null and StopCoroutine(null) throws "routine is null".
- interactable and blocksRaycasts are only written when isInteractable is true, so an element disabled in the inspector stays non-interactable.
