# Contract: ContentFunctionPanel

Role: content-page shell (View); project name and copyright bar plus the Back entry. It only raises events and never decides. There is no AI entry any more (the AI panel opens itself through its own switch button).

Fields:
projectNameText:Text  project name, injected by the controller through Init
m_ProjectNameTextComp:TextComponent  the project-name node's component, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the field becomes a fake null and SetTextOn silently no-ops
companyText:GameObject  company object toggled by SetCopyright (prefab node CompanyImage)
copyrightText:GameObject  copyright object toggled by SetCopyright (prefab node CoptrightText)
backBtn:Button  back entry
OnBackBtnClick:event Action  raised on the back entry; the controller turns it into a DialogRequestEvent

Methods:
Awake()  base first, cache the project-name node's TextComponent, validate the four references, bind the Back entry
Init(projectName)  write the project name through m_ProjectNameTextComp (falls back to TextComponent.SetTextOn; source ProjectClip.displayName, read by the controller)
SetCopyright(ifCopyright, ifCompany)  toggle the company and copyright objects

Notes:
- The panel only raises events: the back target belongs to ContentFunctionController.
- The project name must be injected by the controller after binding; the panel never reads a data source.
- base.Awake has to run first, otherwise canvasGroup is null and the later SetUIActive throws.
- The AI entry was removed on request: the AI panel is created by ContentCanvas and opened/closed by its own AiSwitchBtn (AiDialogPanel.SetPanelActive), so the function bar no longer owns that responsibility. GlobalAiMgr.IsAiEnabled still gates whether the panel is created at all.
- The prefab's AiBtn node was deleted too (the field went with it), so nothing in the shell references the AI panel.
- The panel is rebuilt with its Canvas, so the click binding dies with the instance and no unsubscribe is needed.
- SetCopyright does not null-check its two objects, so a prefab missing them throws at runtime.
