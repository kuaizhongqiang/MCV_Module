# MCV Framework（com.mcv.core + com.mcv.module + com.mcv.input）

Unity 2022.3 虚拟仿真实验教学框架，拆分自 `MCV_Module_0802` 宿主项目（包化改造，见宿主项目 `docs/mcv-package-refactor-design.md` v0.4）。

## 仓库结构（单仓库，UPM 多包）

```
packages/
├── com.mcv.core/    基础设施：Singleton/Event/Interfaces/Models/Utils、UI 基类、
│                     交互基类、Global*Mgr、IHighlightService（宿主注入高亮）
├── com.mcv.module/   业务域：Steps/Logic/Controllers/UI 具体实现、元件交互、
│                      ElementManagerBase、IVideoPlayer/VideoPlayerFactory
└── com.mcv.input/    输入/相机控制域：InputControllerBase、第一/第三人称控制器、
                      环绕聚焦相机、CameraBg、GlobalInputMgr
```

单向依赖：`com.mcv.module → com.mcv.core → (引擎 + 显式声明的 UPM 依赖)`、`com.mcv.input → com.mcv.core`。
命名空间保持 `MCV_Module.*` 不变；本地商业插件（HighlightPlus/AVProVideo）不进包，由宿主经接口适配器注入。

## 安装（Unity Package Manager）

版本锁定（推荐）：

```json
"com.mcv.core":   "https://github.com/kuaizhongqiang/MCV_Module.git?path=/packages/com.mcv.core#0.1.0",
"com.mcv.module": "https://github.com/kuaizhongqiang/MCV_Module.git?path=/packages/com.mcv.module#0.1.0",
"com.mcv.input":  "https://github.com/kuaizhongqiang/MCV_Module.git?path=/packages/com.mcv.input#0.1.0"
```

开发态（本地 clone 即时生效，改包代码无需走 GitHub）：

```json
"com.mcv.core":   "file:../../mcv-framework/packages/com.mcv.core",
"com.mcv.module": "file:../../mcv-framework/packages/com.mcv.module",
"com.mcv.input":  "file:../../mcv-framework/packages/com.mcv.input"
```

## 依赖

- core：`com.unity.nuget.newtonsoft-json@3.2.1`（显式声明）、ugui、inputsystem、cinemachine、addressables
- module：`com.mcv.core@0.1.0`、ugui、textmeshpro、inputsystem
- input：`com.mcv.core`、cinemachine、inputsystem

## 宿主契约

core 经 `Resources.Load` 读取宿主提供的内容（场景/面板 prefab、相机、音频、AA 配置、高亮 Profile），路径清单见宿主项目设计文档 3.3-D。
