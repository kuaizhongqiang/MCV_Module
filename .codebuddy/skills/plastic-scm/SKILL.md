---
name: plastic-scm
description: Plastic SCM (Unity Version Control) version control workflows. Covers branch management, changeset commits, labels, code reviews, sync/update, conflict resolution. Use when user needs to create branches, commit changes, create labels, initiate Code Reviews, merge branches, resolve conflicts, or asks any Plastic SCM operations.
---

# Plastic SCM (Unity Version Control)

## Overview

Plastic SCM is Unity's native VCS. This skill provides automation scripts and quick-reference commands. For deep-dive workflows, load `references/plastic-workflow.md`.

## Terminology (Git -> Plastic)

| Git | Plastic | Git | Plastic |
|:--|:--|:--|:--|
| commit | changeset | tag | label |
| push | checkin | PR | Code Review |
| pull | update | stash | shelve |
| issue | NOT built-in | milestone | NOT built-in (use label) |

## Scripts

Run from PowerShell in the workspace root. All scripts require `cm` CLI available in PATH.

### Check Status

```powershell
python .codebuddy/skills/plastic-scm/scripts/plastic_status.py
```

Shows current branch, pending changes count, and recent changesets at a glance.

### Create Branch

```powershell
python .codebuddy/skills/plastic-scm/scripts/plastic_branch.py feature scene-loader
python .codebuddy/skills/plastic-scm/scripts/plastic_branch.py fix ui-crash --from /main
```

Enforces naming: `feature/{name}`, `fix/{name}`. Automatically switches to new branch.

### Format Changeset

```powershell
python .codebuddy/skills/plastic-scm/scripts/plastic_changeset.py feat "add scene loading pipeline"
```

Validates and formats the commit message, opens interactive editor for multi-line message if needed.

## Quick Reference

### Branch
```bash
cm branch create /feature/{name}      # Create
cm switch /feature/{name}             # Switch
cm branch list --format=smart          # List
cm branch delete /feature/{name}      # Delete (after merge)
```

### Commit
```bash
cm status                              # Pending changes
cm checkin -c "feat: summary" --all   # Commit all
cm checkin -c "fix: summary" FILE     # Commit specific files
```

### Update & Sync
```bash
cm update --mergenative                # Pull latest
cm sync                                # Push (distributed mode)
```

### Label
```bash
cm label create v0.1-alpha             # Tag current
cm label list                          # List labels
```

### Shelve
```bash
cm shelve create "WIP description"     # Stash
cm shelve list                         # List stashes
cm shelve apply ID                     # Restore
```

### Code Review (GUI/Web only)
1. Complete feature branch, all changes checked in
2. Plastic GUI: right-click branch -> Create Code Review
3. Web Dashboard: assign reviewer, add description
4. After approval:
```bash
cm switch /develop
cm merge /feature/xxx --mergenative
cm checkin -c "merge: merge /feature/xxx into /develop"
```

### Conflict
```bash
cm status                              # Lists conflicts
cm resolve --merge ID --mergenative    # Resolve interactively
```

## Conventions

- **Branch**: `/main` (stable), `/develop` (integration), `/feature/{name}`, `/fix/{name}`
- **Changeset**: `type: summary` -- types: `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `test`
- **Label**: `v{major}.{minor}[-{tag}]` -- e.g. `v0.1-alpha`, `v1.0`
- **No built-in issue tracker** -- use document library or external tools

## Reference

Load for detailed information:
- `references/plastic-workflow.md` -- Branch lifecycle, Code Review deep dive, Git vs Plastic mental model, ignore.conf, troubleshooting
