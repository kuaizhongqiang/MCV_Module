# Contract: MenuDetailLogic

Role: child-menu (Detail) buttons of the menu cover flow: builds the buttons, runs the staggered show/hide animation and owns the "visible while idle, gone while scrolling" state.

Fields:
detailParent:Transform  injected; node the child buttons are instantiated under
btnPrefabName:string  injected; UI-package prefab name of the child button prefab (see UIPrefabUtil); empty = unconfigured, no button is built
animDuration:float  per-button show/hide animation length, 1
detailBtns:List<Button>  the live child buttons
LastParent:MenuClip  the parent the current buttons were built from
IsAnimating:bool  true while an animation coroutine runs
IsVisible:bool  true while the child menu is shown and still has to disappear on scroll
OnDetailSelected:Action<MenuClip>  fired on button click; MenuPanel forwards it to the controller

Methods:
ShowRoutine(centerClip)  null clip -> IsVisible = false + hide animation; else rebuild only when the parent changed -> IsVisible = hasChildren -> show/hide animation
HideRoutine()  IsVisible = false -> hide animation
HasButtons  true when the button list is not empty
Rebuild(parent)  private; destroy old children, create one button per GetChildMenus entry, alpha 0 and scale 0.3 as animation start
CreateBtn(clip, dataIndex)  private; load and instantiate the prefab, write the index into child 1 and displayName into child 2 when the prefab has at least 3 children, then bind the click
BindClick(btn, clip, callback)  static; RemoveAllListeners then AddListener -> callback(clip)
AnimRoutine(isActive)  private; 0.2 s stagger per button (forward showing, backward hiding), then snap every button to the target state
ApplyStep(btn, cg, isActive, t, startAlpha, startScale)  private; t-squared easing for alpha and uniform scale; interactable/blocksRaycasts only at t >= 1
ClearChildren(parent)  private; destroys every child from the last index down
SetNodeText(node, value)  static; 把文案写到刚建好的按钮子节点上，优先走节点上的 TextComponent —— TMP 形态下节点上的 Legacy Text 被卸载，局部 Text 引用成"假 null"、静态入口静默 no-op（按钮会没有序号与名字）

Notes:
- Not a MonoBehaviour: MenuPanel starts the returned coroutines; calling StartCoroutine here would throw.
- Buttons are rebuilt only when the centre parent changes, so scrolling over the same parent never destroys live buttons mid-animation.
- The prefab layout (child 1 = index text, child 2 = label) and the 0.3 start scale are hard-wired to the prefab; a layout change shows blank labels instead of failing.
- The animation interpolates from each button's current alpha and scale, so it can start from any intermediate state.
- interactable and blocksRaycasts stay false until the animation completes, so half-faded buttons cannot be clicked.
- ClearChildren uses Destroy (end of frame), so old buttons coexist with the new ones for one frame after a rebuild.
