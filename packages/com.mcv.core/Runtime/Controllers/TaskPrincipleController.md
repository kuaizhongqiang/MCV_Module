# Contract: TaskPrincipleController

Role: experimental-principle controller; reads the TaskPrinciple data of the current ProjectClip and hands its single video name to the panel to play (one-way Controller -> View).

Methods:
OnViewBound()  ResolveVideoName(); skip when empty, else View.LoadVideo(videoName)
ResolveVideoName()  ProjectClip -> TaskPrincipleData -> principleStructs[0].videoName; "" when clip or data is missing

Notes:
- The panel only plays and displays: it never reads data and does not know PrincipleStruct, so all resolution stays in the controller.
- Each task has exactly one principle video, hence index [0]; a missing clip or data logs a warning and yields "".
