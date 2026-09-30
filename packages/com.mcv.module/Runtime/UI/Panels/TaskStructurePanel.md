# Contract: TaskStructurePanel

Role: structure panel (View); shows a mouse-following tip box (TipsFloat). When to open and what text to show are decided by TaskStructureController.

Fields:
floatParent:RectTransform  tip box root that follows the mouse (usually TipsFloat)
tipsText:Text  text inside the tip box (a fake null after the swap; kept only as the fallback for nodes without a component)
m_TipsTextComp:TextComponent  the same node's component, cached in Awake; in TMP form the node's legacy Text is unloaded, so both the write and the "is it configured" guard have to go through the component
tipsOffset:Vector2  offset from the mouse in canvas units; default (0, 80)
isTipsShow:bool  whether the tip box is currently following the mouse

Methods:
Awake()  base first, cache the component (parsing has to happen before the swap: the swap starts in the component's own Awake while Destroy only takes effect at the end of the frame), then hide the box
ShowTips(text)  activate the box first (the TMP swap starts from that activation's OnEnable), then write through the cached component (falling back to TextComponent.SetTextOn only when the node has no component); the guard counts "field null but component present" as configured, because a null field is the normal state in TMP form; then RequestLayoutRebuild(floatParent) and place the box under the mouse immediately
CloseTips()  hide the tip box
FollowMouse()  convert the mouse screen position to the box's parent local space and move the box

Notes:
- Place with localPosition, never anchoredPosition: anchoredPosition is relative to the anchor reference point and breaks if the anchor changes.
- Do not write Mouse.current.position straight into the position: the Canvas is Scale With Screen Size plus 1920x1080, so pixels are not canvas units.
- floatParent must be the node that follows the mouse, not a child inside a layout group (a VerticalLayoutGroup would snap it back).
- The tip box starts hidden, so its node is inactive and the component's swap is deferred to OnEnable -- that is by design (the same deferral the component documents for panels instantiated inactive), not a missed conversion. That deferral is also why ShowTips writes the text only after SetActive(true) and why the layout rebuild cannot happen in the same frame.
- Why the rebuild is deferred: the box is a parent LayoutGroup whose child owns the ContentSizeFitter, and the parent measures the child's current sizeDelta -- rebuilding in the same frame as the activation (or as the write) measures an empty text and the box collapses to its padding until the next show; PanelBase.RequestLayoutRebuild waits a frame and rebuilds child-before-parent.
