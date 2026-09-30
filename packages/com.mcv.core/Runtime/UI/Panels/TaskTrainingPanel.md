# Contract: TaskTrainingPanel

Role: simulation-training task panel (View); placeholder that only writes a title. UI assembly and content are still TODO.

Fields:
titleText:Text  title text (a fake null after the swap; kept only as the fallback for nodes without a component)
m_TitleTextComp:TextComponent  the same node's component, cached in Awake; in TMP form the node's legacy Text is unloaded, so the write has to go through the component

Methods:
Awake()  base first, cache the component; parsing has to happen before the swap (the swap starts in the component's own Awake while Destroy only takes effect at the end of the frame)
Init(title)  write the title through the cached component (falling back to TextComponent.SetTextOn only when the node has no component); UI assembly is TODO
SetText(title)  forward to Init
GetPanelContent()  returns an empty string (TODO)

Notes:
- Placeholder template copied from TaskPurposePanel; it returns "" where TaskLineConnectionPanel returns null.
