# Contract: InspectionMultimeterObj

Role: digital multimeter (screen plus gear knob plus red and black test leads); recomputes the whole screen from the gear, the probe snaps and the component actuation state, distinguishes the correct from the wrong point pair and formats the value with its unit.

Fields:
valueText:Text  [SerializeField] numeric screen (Legacy Text)
m_ValueTextComp:TextComponent  数字屏节点上的组件，在 Awake 缓存；TMP 形态下组件会卸载节点上的 Legacy Text（valueText 随即成"假 null"，属正常状态），静态入口会静默 no-op、屏幕永远不刷新
knobObj:InspectionMultimeterKnobObj  [SerializeField] gear knob; empty leaves the gear fixed to fallbackGear
fallbackGear:MultimeterGearType  [SerializeField] gear used when no knob is assigned
redProbeObj / blackProbeObj:InspectionProbeObj  [SerializeField] the two test leads
powerOffText:string  [SerializeField] screen text on the Off gear (default empty)
openCircuitText:string  [SerializeField] screen text for "not connected" on resistance gears (default "OL")
overRangeText:string  [SerializeField] screen text when out of range (default "OL")
valueTextChangeDuration:float  [SerializeField] seconds to transition to the target reading; <= 0 jumps
randomValueRange:float  [SerializeField] random jitter of the displayed value (judging is unaffected)
redPointObj / blackPointObj:InspectionElementPointObj  the snapped point of each probe (null = not connected)
currentValue:float  value currently on screen (animation intermediate); m_ExactValue:float  exact value of this reading, without jitter
m_ChangeCoroutine:Coroutine  value transition coroutine
m_LastGearKey:int / m_CurrentElement:InspectionElementObj / m_LastActuated:bool  last-frame gear index, component of the last reading, last-frame actuation state
DisplayValue / ExactValue / HasReading / IsPairCorrect / GearType / GearRange / RedPoint / BlackPoint  read-only views

Methods:
Awake()  subscribe the snap event, warn on unset references, initialise the gear snapshot -> RefreshReading
Update()  RefreshReading when the gear or the component actuation state changed
OnDestroy()  unsubscribe the snap event -> StopChange
GearKey()  knob index snapshot (-1 without a knob)
OnProbeSnapped(e)  update the matching probe's point -> RefreshReading
RefreshReading()  recompute the whole screen from gear -> connection -> data, setting IsPairCorrect and m_CurrentElement
ShowOpen(gear) / ShowCheckPointValue(gear, checkpoint)  "no path" display (OL on resistance gears, 0 otherwise) / display the pair reading
TryGetGearValue(gear, checkpoint, out value)  pick the field matching the gear
IsResistanceGear(gear)  whether the gear measures resistance (including diode and continuity)
ShowValue(value)  store the exact value, add jitter, -> PlayChange
ShowText(text)  write the text and clear the numeric reading flag
PlayChange(target) / ChangeTextAnim(value) / StopChange()  start, run and stop the value transition
SetShowText(value) / FormatReading(value, gear, range)  format and write to the screen / out-of-range check then per-gear formatting
FormatResistance / FormatVoltage / FormatCurrent  MOhm, kOhm, Ohm / mV, V / uA, mA, A
Num(value, format)  culture-invariant formatting

Notes:
- Judge with ExactValue and IsPairCorrect, never with the screen value, which carries the randomValueRange jitter.
- IsPairCorrect is cleared and set only inside RefreshReading; ShowText must not clear it, because a correct pair with an open circuit shows OL on screen and still counts as correct.
- Only a snap (touching a point without dragging) counts as connected, matching the operation record of the inspection panel.
- Both probes must be under the same component before the right / wrong tables are queried; when neither table has the pair it is treated as "no path between these points".
- The cached m_CurrentElement is only polled for IsActuated, never rebuilt with GetComponentInParent every frame.
- A digital meter has no ohmmeter zero adjustment.
