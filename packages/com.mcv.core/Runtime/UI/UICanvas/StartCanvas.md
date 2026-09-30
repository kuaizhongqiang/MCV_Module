# Contract: StartCanvas

Role: start-page canvas; on rebuild it assembles the start panel and, on the first run of this release, the render-quality panel.

Methods:
Awake()  base only
OnRebuild()  GetPanel<StartPanel>; when GlobalDataMgr.IsRenderQualitySetted() is false, also GetPanel<RenderQualityPanel> and log

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- Applying the quality level is not done here: GlobalDataMgr.DelayInit already applied the JSON level at startup, and RenderQualityController writes SystemData (with qualitySetted = true) after the user picks.
- Showing the panel writes no data; qualitySetted false simply means nobody has chosen yet in this release.
