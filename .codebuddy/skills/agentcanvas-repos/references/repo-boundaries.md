# AgentCanvas 双仓库关系

## 仓库架构

```
UnityAgentCanvasMCP                    MCV_Module
  (独立 GitHub 仓库)                    (Unity 项目)
       │                                    │
       ├── docs/      设计文档              │
       ├── cli/       Python 源码            │
       │                                    │
       │  ←──── git submodule ────→    AgentCanvas/
       │     (源码线，实时同步)             ├── docs/
       │                                    └── cli/
       │
       │  ←──── GitHub Releases ──→    Assets/StreamingAssets/AgentCanvas/
       │     (产物线，构建时拉取)           ├── mcp.exe
       │                                    └── .env.example
```

## 编辑边界

### 在 UnityAgentCanvasMCP 仓库编辑

| 内容 | 说明 |
|:--|:--|
| `docs/` 下所有 .md 文件 | 设计文档。编辑后 push，MCV 侧通过 submodule 拉取 |
| `cli/` 下 Python 源码 | `main.py`、`mcp_server.py`、`cli_core.py`、`embedding_client.py` 等 |
| `README.md` | 仓库根说明 |
| GitHub Releases | CI 构建的 mcp.exe/cli.exe 通过 Release 分发 |

### 在 MCV_Module 仓库编辑

| 内容 | 说明 |
|:--|:--|
| `Assets/Scripts/GlobalManager/GlobalCLIMgr.cs` | Unity C#，HTTP/WS 服务端 |
| `Assets/StreamingAssets/AgentCanvas/` | 运行时文件目录（从 Release 拉取，不纳入 git） |
| 其他 MCV_Module 业务代码 | UGUI、DataBase、场景等 |

### 严禁操作

- **禁止在 MCV_Module 的 AgentCanvas/ 子目录中直接编辑文件** — 所有修改必须在 UnityAgentCanvasMCP 源仓库中进行，然后 push + submodule update
- **禁止将 GlobalCLIMgr.cs 放入 AgentCanvas 仓库** — .cs 文件依赖 Unity API，必须跟随 Unity 项目编译
- **禁止将 StreamingAssets/AgentCanvas/ 加入 .gitignore** — 即使文件来自 Release，该目录需要在 Unity Build 时存在

## 日常操作命令

### 更新 AgentCanvas 仓库并同步到 MCV

```bash
# 1. 在 UnityAgentCanvasMCP 仓库中编辑并提交
cd F:\Project\UnityAgentCanvasMCP
# ... 编辑 docs/ 或 cli/ ...
git add -A
git commit -m "描述你的修改"
git push origin main

# 2. 在 MCV_Module 中拉取最新
cd F:\Project\MCV_Module\AgentCanvas
git pull origin main

# 3. 锁定子模块版本
cd F:\Project\MCV_Module
git add AgentCanvas
git commit -m "Update AgentCanvas submodule"
```

### 首次克隆 MCV_Module 后初始化

```bash
git clone <MCV_Module_URL>
cd MCV_Module
git submodule update --init --recursive
```

### 拉取 AgentCanvas 最新设计文档

```bash
cd F:\Project\MCV_Module
git submodule update --remote AgentCanvas
```

### 检查子模块状态

```bash
git submodule status
# 输出样例:
#  cecd74a AgentCanvas (heads/main)   ← 锁定在该 commit
```
