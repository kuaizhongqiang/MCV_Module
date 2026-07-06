# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity 2022.3.53f1 URP project — virtual simulation experiment teaching framework. The project is named "MCV_Module" and lives under namespace `MCV_Module.*`.

**Current Status**: Milestones M1-M4 completed (~85%骨架完成), M5 暂缓.

## Unity Version & Environment

- **Editor**: Unity 2022.3.53f1c1 (LTS)
- **Render Pipeline**: Universal Render Pipeline (URP) 14.0.11
- **Target Platform**: Standalone (default) with multi-platform support
- **IDE**: Visual Studio Code (`.vscode/extensions.json` recommends `visualstudiotoolsforunity.vstuc`)
- **Scripting Backend**: IL2CPP (with Mono as fallback)
- **API Compatibility**: .NET Standard 2.1 (level 6)

## Key Packages

| Package | Version |
|---|---|
| `com.unity.addressables` | 1.22.3 |
| `com.unity.render-pipelines.universal` | 14.0.11 |
| `com.unity.textmeshpro` | 3.0.7 |
| `com.unity.timeline` | 1.7.6 |
| `com.unity.visualscripting` | 1.9.4 |
| `com.unity.test-framework` | 1.1.33 |
| `com.unity.burst` | 1.8.18 |
| `com.unity.mathematics` | 1.2.6 |
| `com.unity.scriptablebuildpipeline` | via Addressables |

## Project Structure

```
Assets/
├── Documents/Plan/              # 设计文档（Overview.md 为索引）
│   ├── Architecture/            # MCV / 场景管线 / 数据层
│   ├── Business/                # 漫游 / UI 展示 / 步骤系统
│   ├── Data/                    # JSON Schema / ProjectClip 规范
│   ├── Decisions/               # 架构决策记录
│   ├── Guides/                  # 新增实验 / Panel / 交互 指南
│   ├── Modules/                 # Addressable / 交互 / UI / 用户系统
│   └── Reviews/                 # Milestone Review 文档
├── Scenes/
│   └── ProjectScene/Setup.unity
├── Scripts/
│   ├── Architecture/            # ControllerBase.cs, IController.cs
│   ├── Controllers/             # 28 个业务 Controller
│   ├── Data/                    # DataBase / Project / Json / Addressable / System / User
│   ├── Event/                   # EventBus.cs + GameEvents.cs
│   ├── GlobalManager/           # 9 个 GlobalManager（含 GlobalStepSystemMgr）
│   ├── Interactive/             # InteractiveBase + LineEndpoint + InteractiveDrag
│   ├── StepSystem/              # StepDirector + ConditionFactory + 8 Conditions
│   ├── Tests/                   # AddressableVerification.cs
│   └── UI/                      # UIBase / CanvasBase / PanelBase + 28 Panels
├── StreamingAssets/Data/        # ProjectData.json / SystemData.json / UserData.json
└── Settings/                    # URP quality profiles
```

## Milestone Progress

| Milestone | Status | PRs |
| --------- | ------ | ---- |
| M1: 骨架占位符 (66 .cs) | ✅ Closed | #29 #30 #31 #32 #37 |
| M2: EventBus + 管线跑通 | ✅ Closed | #42 #43 #44 #45 #46 |
| M3: 步骤系统引擎 | ✅ Closed | #52 #53 #54 |
| M4: 示例实验全流程 | ✅ Closed | #59 #60 |
| M5: 用户系统 + 发布 | ⏸️ 暂缓 | — |

## Key Architecture Decisions

### MCV Pattern

- **Model**: `GlobalDataMgr` (持有 SystemData / ProjectData / UserData)
- **Controller**: `ControllerBase<T>` → 28 个业务 Controller (`MCV_Module.Controller`)
- **View**: `PanelBase` → 28 个 Panel (`MCV_Module.UI.Panels`)
- 数据单向流：Controller 从 DataMgr 读数据 → 驱动 View 更新

### EventBus

- `EventBus<T>.Subscribe/Publish/Unsubscribe` — 泛型类型安全
- 强引用模式，必须在 `OnDestroy`/`OnDisable` 中 Unsubscribe
- View 不直接订阅 EventBus，由 Controller 代理

### InputSystem

- 统一使用新 InputSystem (`Keyboard.current` / `Mouse.current`)
- 禁止使用 `Input.GetAxis` / `Input.anyKeyDown` 等旧 API

### StepSystem
- 三级驱动: Processing → Step → Condition
- 三阶段生命周期: Prepare → Waiting → Complete
- 8 种 Condition: Default/Click/Drag/Tool/UI/Question/LineConnect/Finish
- P0S0 快进: `StepDirector.JumpToStep(index, stepIndex)`

## Coding Conventions

- `[SerializeField] private _field` + `public Property => _field`（禁止 public 字段）
- TODO 标注: `// TODO: Mx 实现 —— [描述]`（Mx 指向正确的 Milestone）
- 架构类放 `Architecture/`，业务类放各自目录
- 避免 `GameObject.Find` 在 Update 中 — 提前缓存
- 避免 `Camera.main` — 使用 `GlobalCameraMgr.Camera`

## Scene Pipeline

```
0_Setup → 1_Controller + 2_UI + N（三维实验场景）
           └── 99_Loading（过渡，90% 等待策略）
```

Setup 初始化 9 个 GlobalManager → Jump() → GlobalSceneMgr.LoadSceneSingle()

## Commands

### Open / Launch
```bash
# Open via Unity Hub, select 2022.3.53f1
```

### Build
```bash
/path/to/Unity/2022.3.53f1c1/Editor/Unity.exe -quit -batchmode \
  -projectPath "g:/project/MCV_Module" \
  -buildTarget StandaloneWindows64 \
  -buildWindows64Player "Build/Windows/MCV_Module.exe"
```

### Tests
- Unity Editor → Window → General → Test Runner
- CLI: `-runTests -testPlatform EditMode` (or `PlayMode`)

### VS Code Debugging
- F5 to attach to running Unity Editor (requires "Visual Studio Tools for Unity" extension)

### GitHub PR workflow
```bash
# Create branch, implement, commit, push
git checkout -b mX-feature-name
# ...make changes...
git commit -m "feat(MX): description"
git push origin mX-feature-name

# Create PR using template
gh pr create --base main --head mX-feature-name --title "MX: title" --body "description"

# Merge and link to milestone
gh pr merge X --merge
echo '{"milestone": N}' | gh api repos/owner/repo/issues/X --input -

# Close milestone
echo '{"state":"closed"}' | gh api repos/owner/repo/milestones/N --input -
```
