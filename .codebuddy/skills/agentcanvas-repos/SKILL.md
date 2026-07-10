---
name: agentcanvas-repos
description: >
  This skill defines the commit boundaries, pull principles, and repository management
  rules between MCV_Module (Unity project) and UnityAgentCanvasMCP (standalone GitHub repo).
  It should be used when the user asks to edit AgentCanvas documents, update the submodule,
  manage cross-repo changes, or clarify which files belong in which repository.
---

# AgentCanvas Repository Management

## Purpose

MCV_Module and UnityAgentCanvasMCP are two separate git repositories connected via
git submodule. This skill enforces the commit boundary: what goes where, how to sync,
and what must never be cross-committed.

## When to Use This Skill

Use this skill when:
- The user mentions editing AgentCanvas docs, CLI source, or GlobalCLIMgr.cs
- The user asks about the relationship between the two repos
- Submodule sync or update is needed
- The user is unsure which repo to commit a change to

## Commit Boundaries

### UnityAgentCanvasMCP (source repo at `F:\Project\UnityAgentCanvasMCP`)

Contains everything that is NOT Unity-specific:
- `docs/` ！ All design documentation (Architecture, Commands, Protocol, etc.)
- `cli/` ！ Python source code (main.py, mcp_server.py, cli_core.py, etc.)

### MCV_Module (Unity project at `F:\Project\MCV_Module`)

Contains Unity-specific files and the submodule reference:
- `AgentCanvas/` ！ git submodule pointing to UnityAgentCanvasMCP
- `Assets/Scripts/GlobalManager/GlobalCLIMgr.cs` ！ Unity C# HTTP/WS server
- `Assets/StreamingAssets/AgentCanvas/` ！ runtime executables from Release (not in git)

## Hard Rules

1. **AgentCanvas docs must be edited in UnityAgentCanvasMCP, NEVER in the submodule copy**
2. **GlobalCLIMgr.cs stays in MCV_Module only** ！ it depends on Unity APIs
3. **After pushing to UnityAgentCanvasMCP, always lock the submodule version in MCV_Module**:
   ```
   cd AgentCanvas && git pull origin main && cd .. && git add AgentCanvas && git commit
   ```
4. **StreamingAssets/AgentCanvas/ files come from GitHub Releases, not from submodule**

## Workflow Reference

For detailed commands and the full repo relationship diagram, read `references/repo-boundaries.md`.
