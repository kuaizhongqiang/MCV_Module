# Contract: InspectionElementObjEditor

Role: inspection panel for InspectionElementObj (overriding the default inspector): scan the child check points, auto-pair them into the correct and wrong groups, clean invalid entries and edit each pair.

Fields:
RightPath / WrongPath:const string  serialized paths of the correct and wrong pair lists
m_Points:readonly List<InspectionElementPointObj>  scanned child points, reused while drawing
m_Groups:readonly Dictionary<int,List<InspectionElementPointObj>>  points grouped by terminal-pair index
m_Unmatched:readonly List<InspectionElementPointObj>  points not matched to a terminal pair
Element:InspectionElementObj  the inspected target

Methods:
OnInspectorGUI()  condition type, scan, pairing buttons, pair lists and help box
DrawScan(element) / DrawTerminalSummary(element)  show the scanned points and the recognised terminal pairs
DrawPairingButtons(element)  per-terminal-pair buttons plus clear and all-combination actions
GenerateTerminalPairs(element, intoRight)  pair two terminals per group with default readings, preserving existing entries
GenerateAllCombinations(element)  all n(n-1)/2 combinations into the correct group, after confirmation
FinishGenerate(element, cleaned, added, group) / CleanInvalid(element)  finish the operation and drop pairs with missing references
DrawPairList(right, title) / DrawPairItem(listProp, itemProp, index) / DrawValueField(prop, label, unit, editable)  pair editing
BuildPairLabel(pointsProp) / JoinNames(points, max) / PingPair(pointsProp)  labels and point selection
ClearAll(element) / GetList(element, right)  clearing and list resolution

Notes:
- Auto-pairing fills the correct or wrong group with two-point combinations while keeping the existing entries and readings untouched; the same pair never lands in both groups.
- A pair registered in neither group counts as unregistered at runtime, which shows Err on the multimeter screen.
- Hitting the correct group sets IsPairCorrect true, and an open circuit still reads OL.
- R = 0 means conducting; open circuit is recorded with the explicit openCircuit flag rather than a large number.
- Enabling the actuated readings switches to the actuated row once the element is actuated, for example after the contactor test button.
- Data is read and written through SerializedProperty so undo, prefab overrides and multi-selection all behave, and serializedObject.Update() is called immediately after a button changes the data so the drawing below does not use stale values.
