# Contract: ConditionMeasurePair

Role: completes when the red and black probes are each SNAPPED to the two configured points (step.Points). Red/black swapped counts as the same pair.

Fields:
redPoint:InspectionElementPointObj  point the red probe is snapped to; null = not connected
blackPoint:InspectionElementPointObj  point the black probe is snapped to
snapHandler:Action<InspectionProbeSnapEventData>  cached handler used for unsubscribe

Methods:
Type -> ConditionType.MeasurePair
ResetCondition()  override; base + unsubscribe + clear redPoint/blackPoint
Waiting()  ShowAnimationsAtFirstFrame -> TryResolvePoints -> subscribe snap event -> SeedFromScene() -> WaitUntilOrForceComplete(IsPairMatched) -> unsubscribe
OnProbeSnapped(e)  update the matching side; unsnapped records null
ApplyProbe(type, point)  store per probe type
SeedFromScene()  read the current snapped state of every InspectionProbeObj in the scene
UnsubscribeSnap()  idempotent
IsPairMatched(a, b)  (red==a && black==b) || (red==b && black==a)
TryResolvePoints(out a, out b)  requires exactly 2 entries, both InspectionElementPointObj, and a != b

Notes:
- Only SNAP counts, not touch: touch flips repeatedly while dragging; snap is the same criterion used by the multimeter reading and the operation log (InspectionProbeSnapEventData).
- SeedFromScene is required: when the previous step already placed the probes, no flip event will arrive, so waiting on events alone hangs forever.
- Config errors (not 2 points / not inspection points / same point) -> warn and skip.
- Does not cover constraints like "both probes on the same element vs different elements".
