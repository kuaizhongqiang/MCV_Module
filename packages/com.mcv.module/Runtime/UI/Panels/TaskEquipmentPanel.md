# Contract: TaskEquipmentPanel

Role: equipment task panel (View); shows one piece of equipment and plays its audio. UI assembly is still TODO.

Fields:
titleText / contentText:Text  selected equipment title and body
m_TitleTextComp / m_ContentTextComp:TextComponent  the two text nodes' components, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the Text fields become fake nulls and TextComponent.SetTextOn silently no-ops
equipmentParent:Transform  parent for the equipment UI (not built yet)
currentEquipment:EquipmentStruct  currently selected equipment
equipmentList:List<EquipmentStruct>  all equipment of the task

Methods:
Awake()  base first, cache both text nodes' TextComponents
Init(equipmentStructs)  copy the list in; UI assembly is TODO
SetEquipment(equipmentStructs)  forward to Init
SelectEquipment(prefabKey)  find by prefabKey, write title and body through the cached components (falls back to TextComponent.SetTextOn), publish AudioPlayEventData
GetPanelContent()  summary text of the list and the current equipment

Notes:
- SelectEquipment does not null-check the Find result, so an unknown prefabKey dereferences null.
- currentEquipment is not valid before SelectEquipment has been called once.
- The audio is requested through EventBus (AudioPlayEventData), so GlobalAudioMgr must be initialised.
