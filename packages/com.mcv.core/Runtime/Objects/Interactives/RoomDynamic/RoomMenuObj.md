# Contract: RoomMenuObj

Role: project HUD (exhibit label) inside the roaming room; hover highlight plus a click that requests entering the matching project content page.

Fields:
nomalColor:Color  [SerializeField] project-name color while not highlighted
highlightColor:Color  [SerializeField] project-name color while hovered; default (0,1,0,0.5)
projectName:string  [SerializeField] the matching ProjectClip id (displayName also accepted); default "ProjectName"
projectNameText:Text  [SerializeField] project name label
projectICO:Image  [SerializeField] project icon
m_HighlightObjs:Transform[]  runtime only; highlight decorations = every child except [0] (the background), activated together on hover
m_Clip:ProjectClip  resolved project; null = not resolved yet
m_ProjectNameTextComp:TextComponent  the label node's component, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the hover color must go through the component to reach the TMP that is actually rendered

Methods:
Awake()  base registration -> cache the label node's TextComponent -> cache decorations -> resolve the project -> reset to unhighlighted
CacheHighlightObjs()  cache children [1..n] as highlight decorations
ApplyProject()  resolve projectName and refresh the label through m_ProjectNameTextComp (falls back to TextComponent.SetTextOn); keep the prefab text when nothing resolves
SetIcon(Sprite)  external icon injection; called by RoomOneDynamicMgr after the package loads
ProjectName()  the inspector projectName
FindClip(string)  static; projectName -> ProjectClip, exact id first then displayName fallback; silently null while data is not ready
MoEnterEvent / MoExitEvent  hover highlight on / off
MoClickEvent()  re-resolve once, then publish RoomMenuEnterRequestEvent
SetHighlight(bool)  toggle the decorations and the label color (endpoints come from the inspector); the color goes through m_ProjectNameTextComp.ColorValue, falling back to TextComponent.SetColorOn only when the node has no component

Notes:
- projectName stores the id; the Chinese name is only taken from ProjectClip.displayName and is not duplicated in the inspector.
- The click does not wait for confirmation or navigate itself: the HUD dies with the room scene, so the decision belongs to the persistent MenuController.
- Decorations are collected as "everything except the background", so adding or removing children needs no code change.
- The initial state must be unhighlighted, independent of the child visibility authored in the prefab.
