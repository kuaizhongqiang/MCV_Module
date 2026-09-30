# Contract: StepUIPanel

Role: step UI explanation panel (View); paginates one step's uiDatas content. Display and key reporting only, no decisions.

Fields:
titleText / contentText:Text  page title and body (required); fake nulls after the TMP swap, kept only as the fallback for nodes without a component
m_TitleTextComp / m_ContentTextComp:TextComponent  the two text nodes' components, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the Text fields become fake nulls and TextComponent.SetTextOn silently no-ops
confirmBtn / nextBtn / prevBtn:Button  controls (required)

Methods:
Awake()  base first, cache both text nodes' TextComponents, then validate the five references (a Text field only counts as missing when its cached component is null too)
ShowPage(title, content, pageIndex, pageCount)  write title and body through the cached components (falls back to TextComponent.SetTextOn), switch the pager buttons (a single page hides both), then RequestLayoutRebuild()
SetPagerVisible(visible)  show or hide both pager buttons together
ConfirmClick() / NextClick() / PrevClick()  raise the matching event

Notes:
- Layout goes through PanelBase.RequestLayoutRebuild (a frame later, depth-descending): the title/body sit on ContentSizeFitters and the pager buttons toggle their own active state, so the parent box needs a rebuild after both.
- The old private rebuild coroutine is gone for a reason: it resolved its nodes from `contentText`, which is a fake null in TMP form, so `contentText != null` was false and the whole "rebuild from the body outward" loop never ran.
- A single page hides prev and next as one unit: the requirement is to remove paging entirely when there is only one page; hiding just one leaves a dead button.
- Text is null-guarded because a rebuild can run while the panel is dormant.
- Which page is current, whether paging is allowed and what confirm does are all decided by StepUIController.
