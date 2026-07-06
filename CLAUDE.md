# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity 2022.3.53f1 URP project — early-stage with a data-layer architecture. The project is named "MCV_Module" and lives under namespace `MCV_Module.*`.

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
├── Scenes/
│   └── ProjectScene/Setup.unity       # Initial setup scene
├── Scripts/
│   └── Data/
│       ├── DataBase.cs                 # Base class (id, displayName)
│       ├── Addressable/
│       │   └── AddressableData.cs      # Addressable data types + PackageType enum
│       └── System/
│           └── SystemData.cs           # ProjectInfo, CopyRight, RenderQuality
└── Settings/                           # URP quality profiles
```

## Architecture

### Data Layer (`MCV_Module.Data`)

All data classes inherit from `DataBase` (namespace `MCV_Module.Data`), which provides `id` and `displayName` fields.

Three branches:
- **Addressable** (`MCV_Module.Data.Addressable`): `AddressableData` (empty) and `AddressableDataBase<T>` (generic wrapper with `PackageType` enum: `Default`, `AA`, `AB`). `DefaultPackageData` is the concrete implementation wrapping inner `Data`.
- **System** (`MCV_Module.Data.System`): `SystemData` aggregates `ProjectInfo`, `CopyRight`, and `RenderQuality` — each a `Serializable` class with default values for the project.

No assembly definitions (`*.asmdef`) exist yet — all scripts compile into Assembly-CSharp.

## Commands

### Open / Launch

```bash
# Open the project in Unity
# (double-click MCV_Module.sln or open folder in Unity Hub)
```

### Build

Build via Unity Editor menu (File → Build Settings) or CLI:

```bash
# Windows standalone build via CLI
/path/to/Unity/2022.3.53f1c1/Editor/Unity.exe -quit -batchmode \
  -projectPath "g:/project/MCV_Module" \
  -buildTarget StandaloneWindows64 \
  -buildWindows64Player "Build/Windows/MCV_Module.exe"
```

### Tests

No tests exist yet. When added, run via:
- Unity Editor → Window → General → Test Runner
- CLI: `-runTests -testPlatform EditMode` (or `PlayMode`)

### VS Code Debugging

- Press F5 to attach to a running Unity Editor instance (configured in `.vscode/launch.json`)
- Requires the "Visual Studio Tools for Unity" extension

### Code Style

- Namespaces follow folder hierarchy (`MCV_Module.Data.Addressable`)
- Data model classes are `[Serializable]` with explicit default constructors
- Fields use `camelCase` naming; properties use `PascalCase`
- `Serializable` types use public fields, wrapped by read-only `{ get; }` properties in generic types
