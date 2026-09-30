# Contract: StepUIController (implements IStepUiPanel)

Role: step UI-text dispatcher; serves the step condition ConditionUI — shows a paged help entry and reports when the user confirms.

Fields:
currentData:StepUiData  current entry
currentPage:int  current page index

Methods:
OnViewBound()  subscribe the resolved View (idempotent)
OnDestroy()  unsubscribe the View -> base
ShowData(uiId)  FindInfo -> resolve or create the panel -> subscribe -> page 0 -> activate -> ShowCurrentPage; missing entry or panel -> warn and fire OnPanelClosed
ClosePanel()  clear data and page, ResolvePanel(false), unsubscribe, deactivate when active
ShowCurrentPage(panel)  clamp the page and write it to the panel
OnPrevClicked() / OnNextClicked()  move one page and rewrite it
OnConfirmClicked()  fire OnPanelClosed
Subscribe(panel) / Unsubscribe(panel)  unsubscribe then subscribe / unbind the three page events
FindInfo(uiId)  pick the StepUiData entry with this id out of the polymorphic container; null when missing
ResolvePanel(createIfMissing)  GetActiveCanvas -> GetPanel (create) or FindPanel (no create)

Notes:
- The container is polymorphic: only StepUiData (contentType = UI) is matched, other types are skipped.
- ResolvePanel(false) must be used on the hide path; creating there would flash a brand-new instance.
- One page hides the prev/next buttons; with multiple pages any page can confirm.
- Activate before ShowPage: the layout rebuild coroutine is skipped while inactive, so a reused panel keeps stale text sizes.
- Deactivation must be guarded against an already-inactive GameObject, otherwise StartCoroutine throws inside the step condition's Prepare coroutine and the whole step chain deadlocks.
- A missing entry or panel still fires OnPanelClosed, so the step chain never deadlocks.
