# Contract: ControllerPlaceholderTools

Role: editor tool that mounts one placeholder child per controller class under ControllerRoot in the content scene.

Methods:
MountControllers()  for every controller type in the MCV_Module.Controllers namespace, create a child object under ControllerRoot and attach the component; existing same-named children are skipped
FindContentScene() / LocateRoot()  locate the content scene and its ControllerRoot

Notes:
- Idempotent by design: an already mounted same-named child is skipped, so rerunning after adding a controller only fills in the new ones.
- Components are attached through the type API rather than by script guid, so no .meta is generated for the tool's own output.
- The content scene is overwritten in place; Unity asks to save any unsaved changes first.
