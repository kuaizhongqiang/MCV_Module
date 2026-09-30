# Contract: TaskLineConnectionPanel

Role: line-connection task panel (View); placeholder that only writes a title. UI assembly and content are still TODO.

Fields:
titleText:Text  title text
m_TitleTextComp:TextComponent  the title node's component, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the field becomes a fake null and SetTextOn silently no-ops

Methods:
Awake()  base first, cache the title node's TextComponent
Init(title)  write the title through m_TitleTextComp (falls back to TextComponent.SetTextOn); UI assembly is TODO
SetText(title)  forward to Init
GetPanelContent()  returns null (TODO)

Notes:
- GetPanelContent returns null: GlobalUIMgr.SafePanelContent tolerates it, but anything reading it directly must null-check.
- The panel is a placeholder template copied from TaskPurposePanel; the real line-connection UI is not assembled yet.
