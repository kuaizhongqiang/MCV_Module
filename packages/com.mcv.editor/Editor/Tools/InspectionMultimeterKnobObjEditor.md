# Contract: InspectionMultimeterKnobObjEditor (+ GearRow)

Role: angle-calibration panel for the multimeter knob (overriding the default inspector): fill the gear table from the panel scale and calibrate each gear by preview.

Fields:
GearRow.gearType:MultimeterGearType  the gear this row describes
GearRow.range:float  over-range limit; 0 disables the over-range judgement
GearRow.angle:float  the scale angle of this gear
m_ZeroAnchorIndex:int  the anchor gear that sits at 0 degrees (default OFF)
m_GearIndex:int / m_PreviewAngle:float  the gear being calibrated and its preview angle

Methods:
OnInspectorGUI()  draw the calibration panel
BuildRecommendedAngles()  fill the gear table for OFF / V= / Ohm / uA= / mA= / A= by shifting the whole table so the anchor gear lands on 0 degrees
ApplyPreview() / ResetPreview()  turn the visual layer to the preview angle without touching data
WriteCurrentAngle()  write the preview angle into the selected gear
FlipAngles()  mirror the whole table for a mirrored panel
MeasureScreenSign()  measure whether increasing angles turn counter-clockwise or clockwise on screen
EditorSetStartIndex(index)  set the initial gear

Notes:
- The panel scale is fixed and where the knob can stop is decided by the gear table angles, so filling starts by choosing which scale the knob currently points at (default OFF) and shifting the whole table by it, which removes the need to guess where local Z equals 0.
- Calibration flow: pick a gear, drag the preview angle until the knob points at the printed scale, then write the angle for that gear.
- Preview changes only the visual layer's localRotation and never touches the data or calls SetDirty; the runtime orientation is recomputed from the zeroEuler pose plus the gear angle, so a leftover preview pose is harmless.
- Fill, flip and write go through Undo and SetDirty on the real target data.
