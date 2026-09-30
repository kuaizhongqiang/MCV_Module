# PreviewRig.cs

类型：组件契约（View 层）
位置：`Assets/Scripts/UI/Components/PreviewRig.cs`
命名空间：`MCV_Module.UI.Components`

## 一句话职责

给所在面板那个「显示模型预览」的 `RawImage` **运行期自建**材质与 color/alpha 两张 `RenderTexture`，并把它们挂到指定展示根的两台相机上。

## 为什么需要它

面板 prefab 住在 AB 包（`UI/ui`）里。若让 prefab 直引材质，包内会再生出一份**材质 + RT（+ shader）副本**，而 UI 引擎能把「每个渲染器的纹理」送进材质的通道**只有 `_MainTex`**：

- UGUI `Graphic.UpdateMaterial()` = `canvasRenderer.SetMaterial(materialForRendering, 0)` + `canvasRenderer.SetTexture(mainTexture)`；
- Unity 文档（`CanvasRenderer.SetMaterial`）：给了 texture 就用它当 `MainTex`。

本工程的 `VideoAlphaUIShader` 取色读的是 `_ColorMap` / `_AlphaMap` 两个**材质序列化属性**，引擎送不进去 ⇒ 包内那份副本指向哪张 RT，面板就只能显示那张。实测：包内 RT 与工程 RT **不是同一对象**，且每次加载包都会另生成一份。

⇒ 定案：**shader 放 default 包（`Assets/Resources/Shaders/`），材质与两张 RT 全部运行期自建并自持**，谁都不共用、也不落盘，包与源的分歧从根上消失。

## 契约

### 序列化字段

| 字段 | 含义 | 默认 |
|---|---|---|
| `rawImage` | 显示预览的 RawImage（缺省时 `Awake` 用 `GetComponent<RawImage>()` 兜底） | — |
| `showRootName` | 展示根对象名（1_Content 场景根级对象） | `ShowObjParent` |
| `colorCameraPath` | 展示根下到彩色相机的相对路径 | `CamParent/ColorCam` |
| `alphaCameraPath` | 彩色相机下到 alpha 相机的相对路径 | `AlphaCam` |
| `colorCameraOverride` / `alphaCameraOverride` | 直接指定相机（填了就不按名字解析，便于调试） | 空 |
| `shaderResourcePath` | `Resources` 下 shader 路径（不带扩展名） | `Shaders/VideoAlphaUIShader` |
| `templateMaterial` | 可选模板材质：复制其属性以继承 `_AlphaUseRedChannel` 等取值 | 空 |
| `fallbackSize` | 量不出 RawImage 尺寸时的兜底 | `1024x1024` |
| `maxSize` | RT 尺寸上限 | `2048` |

### 运行期只读出口

`PreviewMaterial` / `ColorTarget` / `AlphaTarget` / `ResolvedColorCamera` / `ResolvedAlphaCamera`（未建成或未解析到为 `null`）。

### 时序与行为

- **建**：`OnEnable` → `Build()`。放 `OnEnable` 而不是 `Awake`，因为未激活层级下面板布局没跑完、`RawImage.rect` 为 0，量出来的 RT 尺寸就会是 0。
- **尺寸**：RT = `RawImage.rect` 的宽高 × `canvas.scaleFactor`（即**面板显示多大的像素区就渲染多大**），夹到 `[2, maxSize]`。分辨率 / scaleFactor 变化时 `Update` 检测到尺寸不符即重建。
- **重建**：先建新 RT → 绑定材质 / RawImage / 两台相机 → **再**释放旧 RT。顺序不可颠倒，否则会出现材质指向已释放 RT 的窗口。
- **释放**：`OnDisable` 摘掉两台相机的 `targetTexture`（且只在仍是本 rig 挂的那张时才摘，避免把新面板刚挂上的摘掉）；`OnDestroy` 再销毁自建材质与两张 RT。
- **相机解析**：优先用 override 字段；否则按「展示根名 + 相对路径」找。**必须用 `Resources.FindObjectsOfTypeAll` 而不是 `GameObject.Find`** —— 展示根平时是关着的（用到才 `SetActive(true)`），`GameObject.Find` 只找激活对象。

## 为什么 color / alpha 必须是两张 RT（不能合并）

`ColorCam` **开了后处理时，它写出的 alpha 通道恒为 1**，而预览模型的透明边缘要靠 `AlphaCam` 单独出的 alpha 图（shader 用 `_AlphaUseRedChannel` 选 A 或 R 通道）。合并成一张就永远拿不到正确的透明边缘。

## 挂载点

| 面板 prefab | RawImage | 展示根 |
|---|---|---|
| `Assets/Prefabs/UI/Panels/TaskInfoPanel.prefab` | 根下 `RawImage`（1080×1080） | `ShowObjParent` |
| `Assets/Prefabs/UI/Panels/TaskStructurePanel.prefab` | 根下 `RawImage`（1920×1080 拉伸） | `ControlObjParent` |

两个面板在 Canvas 重建时互斥（`CanvasBase.Rebuild()` → `ClearPanels()` 销毁旧面板），故可安全共用展示根的那两台场景相机。

## 已知边界

- **改了 panel prefab 必须重建 `UI/ui` 包**，运行期 `UIPrefabUtil.Get` 才拿得到新 prefab。
- **编辑期看不到预览**：材质与 RT 都是运行期 `new` 的，编辑期 RawImage 上仍是包内那份材质引用（兜底）。这是刻意取舍。
- shader 进 default 包（体积几 KB），换取向来由代码驱动的材质 / RT 全部自持。

## 验证记录

2026-09-26 编辑态实测（`1_Content` 场景，手动调 `Build()`）：

- Info 面板：RawImage 1080×1080 ⇒ `colorRt`/`alphaRt` = **1080×1080**；`ShowObjParent/CamParent/ColorCam`、`AlphaCam` 均挂上自建 RT。
- Structure 面板：RawImage 1920×1080 ⇒ `colorRt`/`alphaRt` = **1920×1080**；`ControlObjParent` 那两台同样挂上。
- 闭环：往 `ShowObjParent/ObjParent` 放 `contactor_info_model` 并强制两台相机 `Render()` 后读像素 —— `colorRt` 模型像素 63243、**最亮 227**、中心 `RGBA(174,172,156,255)`；`alphaRt` 同步有内容（平均 A=13）。
