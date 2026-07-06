# MCV_Module

通用虚拟仿真实验教学框架，面向理工科（本科 / 专科 / 职业教育），**不绑定具体课程**。

> **技术栈**：Unity 2022.3.53f1 LTS · URP 14 · C# (.NET Standard 2.1)

---

## 架构概览

```
Model      → GlobalDataMgr（数据层，持有 System / Project / User 数据）
Controller → ControllerBase<T>（逻辑层，单向绑定 View）
View       → Unity GameObject / UI Panel（表现层）
```

**场景加载管线**：

```
0_Setup → 1_Controller + 2_UI + N（三维实验场景）
           └── 99_Loading（过渡，90% 等待策略）
```

### 全局管理器（9个）

| 管理器 | 职责 |
|--------|------|
| `GlobalAudioMgr` | 音频播放 |
| `GlobalControllerMgr` | Controller 注册查找 |
| `GlobalDataMgr` | 数据持有与 JSON 读写 |
| `GlobalInteractiveMgr` | 交互物体注册与 Raycast 分发 |
| `GlobalSceneMgr` | Additive 场景加载 / 卸载 |
| `GlobalUiMgr` | Canvas / Panel 管理 |
| `GlobalAddressableMgr` | 资源统一加载（Resources / Addressables / AssetBundle） |
| `GlobalCameraMgr` | 相机管理 |
| `GlobalStepSystemMgr` | 步骤系统执行器管理 |

---

## Milestone 进度

| Milestone | 内容 | 状态 |
| --------- | ---- | ---- |
| **M1** | 全量文件骨架（10 Data + 28 Panel + 28 Controller） | ✅ |
| **M2** | EventBus + 场景加载管线 + 核心 UI + 交互系统 | ✅ |
| **M3** | 步骤系统引擎（StepDirector + 8 Condition + P0S0） | ✅ |
| **M4** | 示例实验全流程（显微镜使用 + JSON 配置） | ✅ |
| **M5** | 用户系统 + 发布就绪 | ⏸️ |

---

## 三种业务模式

| 模式 | 说明 | 是否严格 MCV |
|------|------|:--:|
| **漫游** | WASD 移动 + 鼠标点击 3D 物体 + UI Panel | ✅ |
| **UI 展示** | 图文 / 视频 / 模型预览 | ✅ |
| **步骤系统** | 三级结构（Processing → Step → Condition）+ 8 种 Condition | ❌ 适度偏离 |

---

## 项目结构

```
MCV_Module/
├── Assets/
│   ├── Documents/Plan/          # 设计文档（Overview.md 为索引）
│   ├── Scripts/
│   │   ├── Architecture/        # ControllerBase, IController
│   │   ├── Controllers/         # 28 个业务 Controller
│   │   ├── Data/                # 数据模型（Project / System / User / Addressable / Json）
│   │   ├── Event/               # EventBus + GameEvents
│   │   ├── GlobalManager/       # 9 个全局管理器
│   │   ├── Interactive/         # InteractiveBase + LineEndpoint + InteractiveDrag
│   │   ├── StepSystem/          # StepDirector + ConditionFactory + 8 Condition
│   │   ├── Tests/               # AddressableVerification
│   │   └── UI/                  # UIBase / CanvasBase / PanelBase + 28 Panel
│   └── StreamingAssets/Data/    # JSON 数据文件
├── Packages/                    # UPM 清单
├── ProjectSettings/             # Unity 设置
└── .github/                     # Issue / PR 模板 & CI
```

## 核心概念

| 术语 | 含义 |
|------|------|
| **ProjectClip** | 实验片段，一个完整教学实验单元，含 6 个 Task |
| **TaskType** | Purpose → Equipment → Principle → LineConnection → Training → Test |
| **GlobalManager** | 全局管理器，`DontDestroyOnLoad`，跨场景持留 |
| **MCV** | 自研架构，单向 Controller → View 绑定 |
| **PackageType** | Default(Resources) / AA(Addressables) / AB(AssetBundle) |

## 快速开始

### 环境要求

- **Unity** 2022.3.53f1 LTS
- **URP** 14
- **.NET** Standard 2.1

### 克隆与打开

```bash
git clone https://github.com/kuaizhongqiang/MCV_Module.git
# 用 Unity Hub 打开项目根目录，版本选 2022.3.53f1
```

### 构建

```bash
Unity.exe -quit -batchmode \
  -projectPath "g:/project/MCV_Module" \
  -buildTarget StandaloneWindows64 \
  -buildWindows64Player "Build/Windows/MCV_Module.exe"
```

---

## 开发指南

- [新增实验](Assets/Documents/Plan/Guides/New-Experiment.md)
- [新增 UI 面板](Assets/Documents/Plan/Guides/New-Panel.md)
- [新增交互物体](Assets/Documents/Plan/Guides/New-Interactive.md)
- [编码规范](Assets/Documents/Plan/Guides/Coding-Conventions.md)

完整文档索引见 [`Assets/Documents/Plan/Overview.md`](Assets/Documents/Plan/Overview.md)

---

## License

MIT
