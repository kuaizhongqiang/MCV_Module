# Contract: RenderQualityController

Role: render-quality controller; wires RenderQualityPanel to SystemData.renderQuality and Unity QualitySettings.

Methods:
OnViewBound()  unsubscribe then subscribe View.OnQualitySelected -> View.InitHardwareInfo()
OnDestroy()  unbind View.OnQualitySelected -> base
OnQualitySelected(level)  GlobalDataMgr.SetRenderQuality -> log -> View.SetUIActive(false)

Notes:
- qualitySetted is written only here, after the user picks a level: writing it on panel create or show marks it set on the first start page and the settings panel never shows again.
- It is read in StartCanvas.OnRebuild (which decides whether the panel shows); that runs after the managers' Init, so it is not the field default.
- Applying the level at startup is GlobalDataMgr.DelayInit's job, not this controller's.
