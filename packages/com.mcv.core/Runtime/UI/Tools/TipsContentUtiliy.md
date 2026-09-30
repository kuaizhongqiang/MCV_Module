# Contract: TipsContentUtiliy

Role: animates a single tip object (alpha fade plus X shift) and switches its content between text mode and image mode; the panel only lends its StartCoroutine.

Fields:
obj:RectTransform  the tip root being animated
parent:Transform  the tip's parent node
canvasGroup:CanvasGroup  alpha target
host:MonoBehaviour  the panel used only to start and stop coroutines
moveDuration:float  animation length in seconds, 0.5
text:Text  text child used by text mode (a fake null after the swap; kept only as the fallback for nodes without a component)
textComp:TextComponent  the same node's component; every read and write goes through it
textGo:GameObject  the text node itself; SetActive must use this because text.gameObject is a fake null after the swap
rawImage:RawImage  image child used by image mode
moveLimit:Vector2  x = open X position, y = closed X position
moveCoroutine:Coroutine  the running open/close routine

Methods:
TipsContentUtiliy(parent, obj, moveDuration, host)  cache rect/canvas group/text + the node's TextComponent and its GameObject + image and store the duration and host
SwitchContent(isOpen, onComplete)  no-op when host is null; stop the running routine -> start OpenContent/CloseContent
StopAnimation()  stop the routine and clear the handle
SetPosState(isOpen)  jump to the open or closed X and alpha without animating
GetCurrentContent()  current text from textComp.RawText (falls back to TextComponent.ReadRaw only when the node has no component); "" when neither exists
SetContent(content)  write through textComp.SetText (falls back to TextComponent.SetTextOn), then show the text node via textGo and hide the RawImage
SetImage(texture)  assign the texture, show the RawImage and SetNativeSize, hide the text node via textGo
LoadAndSetImage(imageKey, fallbackText)  empty key -> SetContent(fallback); else load via GlobalAddressableMgr -> SetImage, and SetContent(fallbackText) on failure
OpenContent(onComplete)  private; fade in while shifting y -> x, then invoke the callback
CloseContent(onComplete)  private; fade out only (no shift), then invoke the callback

Notes:
- Not a MonoBehaviour: the panel's host runs the coroutines, so a destroyed host silently kills a running animation and a null host makes SwitchContent return at once.
- Opening animates alpha and X, closing animates alpha only; SetPosState is the instant version used when there is nothing to animate.
- Everything is resolved in the constructor, which is the only moment the legacy Text is still alive: the swap starts in the component's own Awake and unloads that Text when Destroy takes effect at the end of the frame, so a later GetComponent on the field would throw.
- Text goes through the cached TextComponent (SetText / RawText) -- calling the static TextComponent.SetTextOn with the Text field would silently no-op once that field is a fake null. The CJK layout (NBSP indentation + leading-punctuation avoidance) lives in the component and only runs when the node's cjkTypography switch is on (both TipsPanel content nodes have it on), so GetCurrentContent reads RawText and returns the original string.
- SetActive always goes through the cached GameObject: `text.gameObject` would be a fake null after the swap, so image mode would silently leave the text visible.
- Image loading is asynchronous: the callback decides between image and fallback text, and a failed load without a fallback shows nothing.
- Image mode needs a RawImage on the object; there is no TipImagePath switch in the code.
