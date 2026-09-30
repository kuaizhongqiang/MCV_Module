# Contract: TaskPurposePanel

Role: task purpose panel (View); writes a title and a body. This is the reference implementation other task panels are copied from.

Fields:
titleText / contentText:Text  title and body text
m_TitleTextComp / m_ContentTextComp:TextComponent  the two nodes' components, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy -- it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so afterwards the Text fields are fake nulls and TextComponent.SetTextOn would silently no-op

Methods:
Awake()  base first, cache both components; parsing has to happen before the swap (the swap starts in the components' own Awake while Destroy only takes effect at the end of the frame)
Init(title, content) / SetText(title, content)  write both texts through the cached components (falling back to TextComponent.SetTextOn only when a node has no component); writes that arrive before the assembly finishes are buffered inside the component, so no waiting is needed
GetPanelContent()  snapshot text for the AI context, read through the cached components' RawText (the Text fields would be fake nulls in TMP form)

Notes:
- This is the template panel: Init and SetText are duplicated on purpose so both call sites stay readable.
- The texts are no longer null-checked against the Text fields: the components are resolved in Awake and that is what the writes go through; the fields themselves stop being usable after the swap.
