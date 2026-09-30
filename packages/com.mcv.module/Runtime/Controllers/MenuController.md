# Contract: MenuController

Role: menu entry controller; turns the menu panel's buttons into real actions (enter roaming / enter a device content page / quit / preview score).

Fields:
RoamingSceneName  const; roaming room scene name, must equal Resources/Config/SceneAAConfig sceneName ("11_Room1")
QuitDialogId  const DialogId.QuitApp; claims the quit confirmation
EnterDialogId  const DialogId.EnterProject; claims the enter-project confirmation
currentClips:List<MenuClip>  sibling list of the current level (unused leftover)
current:MenuClip  parent menu of the current level; null = root (unused leftover)
m_PendingSceneName:string  scene requested and awaiting ready; "" = none
m_PendingEnterClip:ProjectClip  project awaiting confirmation; null = none

Methods:
Awake()  base -> subscribe SceneLoadedEvent, DialogResultEvent, RoomMenuEnterRequestEvent (persistent; unsubscribe then subscribe)
OnViewBound()  unsubscribe then subscribe the four panel buttons -> SetCopyright
OnDestroy()  unbind the buttons and unsubscribe the three events -> base
OnRoamingClick()  already roaming -> hide the popup only; else guard duplicates and publish SceneSwitchRequestEvent -> set m_PendingSceneName
OnSceneLoaded(e)  only the pending scene switches the UI -> publish SceneStateChangeEventData(Roaming)
OnMenuBtnClick(clip)  route MenuClip.projectId to ProjectClip -> EnterProject; warn when missing
EnterProject(project)  SetCurrentClip -> publish TaskTypeChangeEventData(default task) -> unload the roaming scene when roaming -> publish SceneStateChangeEventData(UI)
ResolveDefaultTaskType(project)  first active task in Tasks; None when there is none
OnResultClick()  GetPanelOnActiveCanvas<ResultSummitPanel> -> PreviewScore -> panel.Show(ScoreRecordFormatter.Build(...))
OnQuitClick()  publish DialogRequestEvent(QuitDialogId, ...)
OnDialogResult(result)  claim by DialogId; confirmed quit -> QuitApplication; confirmed enter -> EnterProject(m_PendingEnterClip)
QuitApplication()  publish AppQuitEvent
OnRoomMenuEnterRequested(e)  remember the clip and publish DialogRequestEvent(EnterDialogId)

Notes:
- Entering is two steps (load the scene, then switch state): switching state clears every Canvas panel, so a one-step version shows an empty room first.
- Dialog results are claimed by DialogId and controllers persist across canvases, so every id must be globally unique (DialogId.QuitApp deliberately differs from FunctionController's DialogId.Exit).
- Do not resend the scene request while roaming: GlobalSceneMgr drops "already the current switch scene" and OnSceneLoaded never fires, leaving m_PendingSceneName stuck forever.
- EnterProject must publish the task-type change before the state change; reversing them makes the rebuilt content page miss the current task.
- The room-project HUD entry must be handled by this persistent controller: the room scene is unloaded when entering a content page and the HUD dies with it.
- currentClips / current are unused leftovers from the old hierarchical menu.
