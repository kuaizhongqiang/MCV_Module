# Contract: TaskInspectionController

Role: inspection (measurement) panel controller; drives two hints from two event sources — a hover-driven floating tip and a snap-driven operation record — and closes the chain by reporting the score and returning to the menu.

Fields:
mgr:InspectionManager  [SerializeField] inspection manager reference

Methods:
Awake()  base -> subscribe GlobalInteractionEventData, InspectionProbeSnapEventData, AllStepsCompletedEvent (resident controller, subscribe once)
OnDestroy()  unsubscribe all three -> base
OnViewBound()  fresh panel: View.CloseTips() + View.ClearOpRecord()
OnGlobalInteraction(e)  Enter/Exit only -> ResolveHoverName; Enter shows the tip, Exit closes it, other types leave the tip alone
ResolveHoverName(target)  InspectionElementPointObj -> GetPointName(); InspectionProbeObj -> ProbeName; else null
OnProbeSnapped(e)  snapped with a point -> View.SetOpRecord(BuildOpRecordLine(...)); un-snapping does nothing
BuildOpRecordLine(probeType, point)  format "\"{ChnNameMap.Get(probeType)}\"接入{point.GetPointName()}点"
OnAllStepsCompleted(e)  ReportMeasureScore -> ReturnToMenu
ReportMeasureScore()  report the Inspection scored unit with completed:true -> warn when clip / task data / unit is missing
ReturnToMenu()  publish SceneStateChangeEventData(SceneState.Menu)

Notes:
- Contact events (InspectionProbeEventData) are deliberately not used: contact flips throughout a drag, while the tip needs "which object is pointed at" and "which point is really snapped", hence hover and snap respectively.
- The panel only displays; when to open and what text to show live in this controller (the same MVC split as TaskStructureController).
- View is a Unity object: after destroy it must be tested with the fake-null (== null), not with ?. which misses it.
- The chain-end event is used instead of the Finish button so a Finish without usingId (no panel) still finishes; navigation and scoring belong to the C layer, not to step conditions.
- Only the latest record is kept (an identical text is skipped by the panel); un-snapping leaves the record untouched, so a rebound failure keeps the last snapped text.
- ReturnToMenu only publishes state; GlobalUIMgr owns the Canvas switch.
