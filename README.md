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
```

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

---

## 三种业务模式

| 模式 | 说明 | 是否严格 MCV |
|------|------|:--:|
| **漫游** | WASD 移动 + 鼠标点击 3D 物体 + UI Panel | ✅ |
| **UI 展示** | 图文 / 视频 / TextureReader 模型预览 | ✅ |
| **步骤系统** | 预制体配置 + 轻量调度器，策划友好 | ❌ 适度偏离 |

---

## 快速开始

### 环境要求

- **Unity** 2022.3.53f1 LTS
- **URP** 14
- **.NET** Standard 2.1
- **Git** 2.30+

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

## 项目结构

```
MCV_Module/
├── Assets/
│   ├── Documents/Plan/     # 项目设计文档（wiki 索引见 Overview.md）
│   ├── Scripts/             # 业务代码（按命名空间分层）
│   └── StreamingAssets/Data/  # JSON 数据文件
├── Packages/               # Unity Package Manager 清单
├── ProjectSettings/        # Unity 项目设置
└── .github/                # Issue / PR 模板 & CI
```

---

## 核心概念

| 术语 | 含义 |
|------|------|
| **ProjectClip** | 实验片段，一个完整教学实验单元，含 6 个 Task |
| **TaskType** | Purpose → Equipment → Principle → LineConnection → Training → Test |
| **GlobalManager** | 全局管理器，`DontDestroyOnLoad`，跨场景持留 |
| **MCV** | 自研架构，单向 Controller → View 绑定 |
| **PackageType** | Default(Resources) / AA(Addressables) / AB(AssetBundle) |

---

## 当前状态

| 模块 | 状态 |
|------|------|
| 全局管理器（8个） | 框架搭建完成 |
| 数据模型 | System / Project / User 定义完成，JSON 读写就绪 |
| MCV 模式 | IController / ControllerBase 定义完成 |
| UI 系统 | Canvas / Panel 基类 + 7 个面板骨架完成 |
| 交互系统 | IObj / InteractiveBase 接口完成 |
| Addressable | PackageConfigSO / Repository 完成 |
| 业务内容 | ProjectClip 6 任务数据类完成，实验内容待填充 |

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
