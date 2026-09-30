# Contract: ElementCheckPointPairing

Role: inspection point pairing tool; keeps one terminal-pair table per component type and provides name normalisation, grouping, auto pairing, table lookup and cleanup of invalid entries.

Fields:
TerminalPair  struct: a pair of terminals (a / b labels plus kind, which only affects the default reading)
Coil / MainContacts / AuxNo / AuxNc / MotorWindings / ThermalRelay / TimerRelayContacts / ButtonContacts / SwitchContacts / FuseBody / BreakerPoles / TwoTerminal / EmptyPairs / CommonPairs  TerminalPair[] per device family
s_PairsByElement:Dictionary<ElementType,TerminalPair[]>  component type -> table
s_IndexByElement:Dictionary<ElementType,Dictionary<string,List<int>>>  component type -> (normalised label -> indices)
s_TempIndices:List<int>  reusable scratch list

Methods:
cctor()  BuildPairsByElement + BuildIndexByElement
BuildIndex(TerminalPair[]) / AddIndex(...) / Concat(...)  index a table / append an index / join tables
GetTerminalPairs(ElementType)  the table, falling back to the common table
NormalizePointName(string)  keep alphanumerics only, upper-cased
CollectTerminalPairIndices(...) / TryGetTerminalPairIndex(...) / ResolveTerminalPairIndex(...)  point -> pair indices (the last returns -1 when unknown)
GetTerminalPairLabel(...)  display label like "1L1-2T1"
CollectChildPoints(GameObject, List)  collect the inspection points, inactive ones included
CollectTerminalGroups(...)  group points by terminal pair, recording stray and unknown points in unmatched
IsInAnyGroup(...)  whether a point already sits in an established group
BuildTerminalPairs(...)  auto pair and fill default readings -> returns the number added
CreatePair(...) / ApplyDefaultReading(ref, ...)  create a pair and fill the static / actuated readings by kind
ResolveReading(...)  project the reading for the current actuation state
BuildAllPairs(...)  all-combination pairing (no readings filled)
ContainsPair(...) / TryFind(...) / IsSamePair(...)  table lookup and order-independent pair equality
RemoveInvalidPairs(...)  drop invalid entries -> returns the number removed

Notes:
- The pairing rules exist once, here: the Editor auto-pair / cleanup and the runtime lookup share them, so "generated in the editor but not found at runtime" cannot happen.
- A pair means exactly two non-null points in checkPoints, order-independent (red and black may be swapped).
- The table is chosen by component type: different devices use different terminal numbering, so mixing them in one table would always mispair. A new component type must add its own table or it silently falls back to the common table.
- Points sharing a terminal appear in every pair they take part in (e.g. JS7-A terminal 1 belongs to both 1-3 and 1-4), so the stray-point report must not flag them.
