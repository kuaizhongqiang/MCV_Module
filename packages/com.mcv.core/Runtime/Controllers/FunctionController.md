# Contract: FunctionController

Role: bottom function-bar controller; subscribes seven button events plus DialogResultEvent and SceneStateChangeEventData, and drives exit and back confirmation.

Fields:
ExitDialogId:DialogId  const DialogId.Exit; claims the exit confirmation
BackDialogId:DialogId  const DialogId.Back; claims the back confirmation (intentionally different from ContentFunctionController's DialogId.BackToMenu)
m_CurrentState:SceneState  cached navigation state from SceneStateChangeEventData (default Setup)

Methods:
Awake()  clear then add the resident SceneStateChangeEventData subscription -> base
OnSceneStateChanged(e)  cache e.State
OnViewBound()  clear then add the seven button handlers -> clear then add the resident DialogResultEvent subscription
OnDestroy()  unsubscribe the seven handlers, DialogResultEvent and SceneStateChangeEventData -> base
OnExitClick()  publish DialogRequestEvent(ExitDialogId, "确定要退出应用吗？当前进度将不会保存。", confirm + cancel)
OnBackClick()  resolve the target from m_CurrentState -> publish DialogRequestEvent(BackDialogId, "确定从{source}返回{target}吗？")
ResolveBackTarget(current, out targetState, out targetName)  UI/Roaming -> Menu "菜单界面"; Menu -> Login "登录界面"; else false
DescribeCurrentSource(current)  UI/Roaming -> "《项目名》·任务类型" (with fallbacks); Menu -> "菜单界面"; else SceneStateToChinese
TaskTypeToChinese(type)  TaskType -> Chinese label (mirror of EnumAll's InspectorName); None -> ""
SceneStateToChinese(state)  SceneState -> Chinese label (UI -> "任务界面"); else "当前界面"
OnSettingClick() / OnResourcePanelClick() / OnSummitClick() / OnRecordClick() / OnMuteClick()  TODO no-op stubs
OnDialogResult(result)  ignore unless Confirmed; ExitDialogId -> ExitApplication; BackDialogId -> GoBackByState
ExitApplication()  Editor: stop play; else publish AppQuitEvent
GoBackByState()  resolve the target -> when leaving Roaming unload the switched scene -> publish SceneStateChangeEventData(targetState)

Notes:
- Back follows running-flow Task -> Menu -> Login; only UI/Roaming and Menu have a parent, everything else is ignored.
- Exit goes through AppQuitEvent rather than Application.Quit so GlobalSceneMgr can clean up resources first.
- Leaving Roaming must UnloadSwitchedScene, otherwise re-entering stacks a second room instance.
- Both id consts must stay unique: controllers are persistent, so a shared DialogId would be claimed twice.
- The TaskType and SceneState to Chinese tables must stay in sync with EnumAll.cs InspectorName when enum values are added.
- Settings / resource / submit / record / mute are still TODO no-ops.
