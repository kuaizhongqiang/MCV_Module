# CODEBUDDY.md This file provides guidance to CodeBuddy when working with code in this repository.

## Project Overview

MCV_Module is a virtual simulation experiment teaching platform for STEM education. Unity 2022.3.53f1 LTS + URP 14. Key packages: Addressables 1.22.3, TextMeshPro 3.0.7, Burst 1.8.18, Mathematics 1.2.6. No asmdef files exist — all scripts compile into `Assembly-CSharp`. See `CLAUDE.md` for detailed Unity version and package list.

Three business modes, concept boundaries may overlap: **Roaming** (WASD + click 3D objects → panel), **UI Display** (text/image/video/TextureReader), **Step System** (prefab-based, deviates from MCV — see Architecture below).

**VCS**: Git (GitHub). Remote: `https://github.com/kuaizhongqiang/MCV_Module.git`. Use the GitHub MCP integration for repository operations (create PR, manage issues, etc.).

**CI/CD**: GitHub Actions (`.github/workflows/build.yml`). Auto-builds on push/PR to `main`.

**Docs**: Project planning documents live in `Assets/Documents/Plan/` — `Overview.md` is the wiki index. README.md is the public-facing entry point.

## Commands

### Build
```bash
Unity.exe -quit -batchmode -projectPath "g:/project/MCV_Module" -buildTarget StandaloneWindows64 -buildWindows64Player "Build/Windows/MCV_Module.exe"
```

### Tests (none yet)
Run via Unity Editor Test Runner or CLI: `-runTests -testPlatform EditMode|PlayMode`.

### VS Code Debugging
Press F5 to attach to running Unity Editor. Requires "Visual Studio Tools for Unity" extension.

## Architecture

### Startup Flow

`Setup.cs` (scene `0_Setup.unity`) coroutine-polls 8 GlobalManagers in order: **AudioMgr → ControllerMgr → DataMgr → InteractiveMgr → SceneMgr → UiMgr → AddressableMgr → CameraMgr**. Each must complete `DelayInit()` (sets `isInit = true`) before next begins. Then jumps to runtime scenes: `1_Controller + 2_UI + N` (N = experiment 3D scene, loaded additively).

**Note**: `IsGlobalMgrInit<T>()` is a stub returning `true` — real init checking not implemented.

### Singleton System

- `SingletonBase`: Abstract `MonoBehaviour`, provides `DelayInit()` coroutine + `isInit` flag
- `SingletonGlobalMgr<T>`: Generic singleton, thread-safe (`lock`), lazy, `DontDestroyOnLoad`, quit-protected. All GlobalManagers inherit this. Access via `GlobalXxxMgr.Instance`.

### MCV Architecture

Self-designed **Model-Controller-View** with unidirectional Controller→View binding. Model = `GlobalDataMgr` (no separate Model class).

- `IController`: Interface with `View view` (MonoBehaviour constraint), `int id`, `bool isActive`
- `ControllerBase<T>`: Generic base, binds to a specific MonoBehaviour view
- `GlobalControllerMgr`: Central registry, `GetController(int id)` lookup

**Exception**: The Step System does NOT strictly follow MCV. Steps are configured as GameObjects/prefabs for designer-friendliness, instantiated at runtime by a lightweight scheduler. MCV applies to Roaming and UI Display modes.

### Data Layer (`MCV_Module.Data.*`)

All data inherits `DataBase` (id, displayName). `GlobalDataMgr` holds three root objects:

| Object | Class | Purpose |
|:--|:--|:--|
| `SystemData` | `SystemData` | Project info, copyright, render quality |
| `ProjectData` | `ProjectData` | List of `ProjectClip` + current clip ref |
| `UserData` | `UserData` | Login, scores, progress |

**ProjectClip** is the core entity. Each contains 6 task types (`TaskType` enum): Purpose → Equipment → Principle → LineConnection → Training → Test. Tasks use self-referencing generic: `TaskData<T> : TaskDataBase where T : TaskData<T>`. The `Tasks` property synthesizes all into `List<TaskDataBase>`.

Persistence: `JsonReaderWriter` (Newtonsoft.Json) reads/writes `StreamingAssets/Data/*.json`. Currently only `SystemData.json` is loaded in `DelayInit()`.

### Addressable System (`MCV_Module.Data.Addressable`)

Three loading strategies: `PackageType.Default` (Resources), `.AA` (Addressables), `.AB` (AssetBundle). Configured via `PackageConfigSO` / `PackageDatabaseSO` (ScriptableObjects), runtime via `PackageDataRepository`.

### UI System

Three-level: `UIBase` (CanvasGroup fade anim) → `CanvasBase` (full-screen layer) / `PanelBase` (panel within canvas). `GlobalUiMgr` provides `GetCanvas<T>()` / `GetPanel<T>()` type lookup. 7 existing panels: Menu, Title, Task, Function, Dialog, Tips, Copyright.

### Interactive System

`IObj` interface (6 mouse events: Enter/Exit/Down/Up/Drag/Click), `InteractiveBase` MonoBehaviour implementation. `GlobalInteractiveMgr` does per-frame raycast detection, dispatches enter/exit events.

## Key Conventions

- **Namespaces** mirror folder hierarchy: `MCV_Module.Data.Project`, `MCV_Module.GlobalManager`, etc.
- **Data classes**: `[Serializable]`, `[SerializeField] private` fields + public `{ get; }` properties. Runtime-only fields use `[NonSerialized]`.
- **Managers**: inherited from `SingletonGlobalMgr<T>`, init logic in `DelayInit()` coroutine, access via `Instance`
- **No ECS/DOTS** — all MonoBehaviour-based
