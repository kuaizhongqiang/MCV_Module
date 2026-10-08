# Contract: MenuPanel

Role: menu-page view; generated skeleton (`MCV Editor/创建/UI Panel`, 2026-10-08). It currently only collects its menu buttons — display and interaction are not wired yet.

Fields:
companyImage:GameObject  [SerializeField] company logo object; assigned on the prefab, not driven by this script
copyrightImage:GameObject  [SerializeField] copyright object; same
backBtn:Button  [SerializeField] back button; bound but no listener is added here
menuRoot:Transform  [SerializeField] parent whose direct children are the menu items
menuButtons:List<Button>  the Button found on each direct child of menuRoot, collected once in Awake (children without a Button are skipped)
currentClip:ProjectClip  the clip the menu would open; declared but never read or written yet
OnMenuClick:event Action<ProjectClip>  declared for a menu-item pick; nothing raises it yet
OnBackClick:event Action  declared for the back button; nothing raises it yet

Methods:
Awake()  base first; logs "MenuPanel 需要手动挂载组件" and returns when any of companyImage / copyrightImage / backBtn / menuRoot is unassigned; otherwise fills menuButtons from menuRoot's direct children
OnDestroy()  base only

Notes:
- Skeleton: the four serialized references must be assigned by hand on `Assets/Prefabs/UI/Panels/MenuPanel.prefab` (UI package entry `ui_MenuPanel`); run `MCV Build/UI prefab AB` after editing the prefab.
- menuButtons is a snapshot taken in Awake and never rebuilt, so adding or removing menu items at runtime needs an explicit re-collect.
- backBtn has no onClick listener and neither event is raised yet, so MenuController.OnViewBound has nothing to subscribe to.
- The panel must stay display-only: company / copyright visibility and the menu data come from the controller (compare StartPanel, which reads GlobalUIMgr.IfCompany).
- Created together with `MenuCanvas`'s rebuild, which calls `GetPanel<MenuPanel>()`; the canvas is the only creation point.
