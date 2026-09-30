# Contract: TaskInfoPanel

Role: task info (introduction) panel (View); fills the picture slots and shows one text block. Content is prepared by TaskInfoController.

Fields:
picsParent:Transform  picture slot parent; every child is a slot
textParent:Transform  text block parent; only one block is shown at a time

Methods:
Init(pics, textIndex)  fill the slots in order, hide unused slots, show only the block at textIndex (out of range falls back to 0), then RequestLayoutRebuild()
GetPanelContent()  returns an empty string

Notes:
- Every slot must be iterated: the old code only touched the first picCount slots, leaving empty slots visible when there were fewer images than slots.
- textIndex out of range falls back to 0 with a warning instead of failing.
- Both slot visibility and the text block switch change the layout, so the panel asks for one rebuild at the end (PanelBase.RequestLayoutRebuild: a frame later + depth-descending); calling it twice in a row would drop the first request, hence the single call with the panel as root (and the early-return branch requests it too).
- The image set is loaded by the controller from data.images (AB ids); the text blocks are hand-filled in the prefab in ProjectData.clips order.
