﻿﻿# Plastic SCM 详细工作流参考

本文档作为 `SKILL.md` 的补充，提供更深入的流程说明和 GUI 操作指南。

---

## 分支策略详解

### 主分支保护

`/main` 分支应始终处于可构建、可运行状态。任何合并到 `/main` 的操作必须：

1. 先在 `/develop` 上完成集成测试
2. 通过 Code Review
3. 合并后立即打 Label

### 功能分支生命周期

```
1. 从 /develop 创建     cm branch create /feature/xxx
2. 日常开发 + 频繁提交   cm checkin -c "feat: ..." --all
3. 定期从 /develop 更新  cm update; cm merge /develop --mergenative
4. 完成 → Code Review
5. 合并到 /develop       cm switch /develop; cm merge /feature/xxx
6. 删除功能分支          cm branch delete /feature/xxx
```

### 发布分支（可选）

正式版本发布时，可以从 `/develop` 拉出发布分支做最终验证：

```
cm branch create /release/v0.1 --from=/develop
```

完成后合并回 `/main` 和 `/develop`，打 Label，删除发布分支。

### 热修复

线上问题直接从 `/main` 拉修复分支：

```
cm branch create /fix/critical-bug --from=/main
```

修复后合并回 `/main` 和 `/develop`。

---

## Code Review 详细流程

### 发起 Review（GUI）

1. 确保功能分支所有变更已 checkin
2. 在 Plastic SCM GUI 中，切换到功能分支
3. 顶部菜单 → **Branches** → **Code Review**
4. 或右键分支名 → **Create Code Review**
5. 在弹出的 Web Dashboard 中：
   - 填写 Review 标题和描述
   - 指定 Reviewer
   - 关联外部 Issue（如有 Jira 集成）

### 发起 Review（Web Dashboard）

1. 浏览器打开 Unity Version Control Dashboard
2. 进入仓库 → **Code Reviews** 标签
3. 点击 **New Code Review**
4. 选择源分支和目标分支（如 `/feature/xxx` → `/develop`）
5. 填写描述，指定 Reviewer

### 审查操作（Reviewer）

- 在 Web Dashboard 查看文件 Diff
- 对具体代码行添加评论
- 可选状态：**Approve**（通过）、**Request Changes**（需修改）、**Comment**（仅评论）
- 所有 Reviewer 必须 Approve 才能合并（取决于仓库设置）

### 审查后合并

Review 通过后：

```
cm switch /develop
cm merge /feature/branch-name --mergenative
cm checkin -c "merge: 合并 /feature/xxx → /develop" --all
```

Plastic 会自动关联 changeset 到对应的 Code Review。

---

## Changeset 规范详解

### 信息格式

```
<type>: <简短描述>

<详细说明（可选）>

<关联信息（可选）>
```

### 类型前缀

| 前缀 | 用途 | 示例 |
|:--|:--|:--|
| `feat` | 新功能 | `feat: 新增 GlobalSceneMgr 场景加载` |
| `fix` | Bug 修复 | `fix: 修复 DialogPanel 层级遮挡` |
| `refactor` | 重构（不改变功能） | `refactor: 提取 InteractiveBase 公共方法` |
| `docs` | 文档变更 | `docs: 更新编码规范文档` |
| `chore` | 杂项（配置、依赖） | `chore: 升级 Addressables 到 1.22.3` |
| `style` | 格式调整（不影响逻辑） | `style: 统一缩进格式` |
| `test` | 测试 | `test: 添加 GlobalDataMgr 单元测试` |

### 每次提交的建议

- 一个 changeset 只做一件事（原子性）
- 确保提交前项目可编译通过
- 不要提交 `Temp/`、`Library/`、`Logs/` 等生成目录（已在 `.gitignore` 对应的 Plastic `ignore.conf` 中排除）
- `UserSettings/` 不提交

---

## Label 版本号规范

### 格式

```
v{主版本号}.{次版本号}[-{预发布标记}]
```

### 版本号规则

| 版本号 | 含义 |
|:--|:--|
| 主版本号 | 重大架构变更或不兼容修改 |
| 次版本号 | 功能新增或重要改进 |
| 预发布标记 | `alpha`（早期）、`beta`（功能基本完整）、`rc`（候选发布） |

### 示例

```
v0.1-alpha    # 框架可跑，完成 Setup 初始化
v0.2-alpha    # 漫游 + UI 展示可用
v0.3-beta     # 步骤系统可用，三种业务形态齐备
v1.0          # 首个完整实验全流程跑通
```

### 打 Label 命令

```
cm label create v0.1-alpha
cm label create v0.1-alpha --changeset=CS123     # 指定 changeset
```

应用 Label 到之前的变更：
```
cm label create v0.1-alpha --changeset=CS100
```

---

## 常见问题处理

### 1. 提交后发现漏了文件

```
# 修改遗漏文件后
cm checkin -c "fix: 补充遗漏文件" PATH/TO/MISSED_FILE
```

### 2. 回滚某个文件

```
cm undochange PATH/TO/FILE
```

### 3. 取消最近一次提交（本地）

```
cm undo
```

### 4. 查看文件修改历史（annotate/blame）

```
cm annotate PATH/TO/FILE
```

### 5. 对比差异

```
cm diff                           # 全部变更
cm diff PATH/TO/FILE              # 指定文件
cm diff CS_OLD..CS_NEW            # 两个 changeset 对比
```

### 6. Workspace 同步问题

```
cm update --mergenative            # 标准更新
cm update --forced                 # 强制覆盖本地（谨慎使用）
```

### 7. 忘记在哪个分支

```
cm status     # 显示当前分支和 pending changes
cm branch list --format=smart     # 查看所有分支
```

---

## Plastic SCM 与 Git 心智模型差异

| 特性 | Git | Plastic SCM |
|:--|:--|:--|
| 架构 | 分布式 | 集中式+分布式混合 |
| 提交 | commit 到本地，push 到远端 | checkin 直接到服务器 |
| 暂存区 | `git add` + `git commit` | 直接 checkin（无 staging 概念） |
| 历史 | 线性或 merge commit | 默认显示合并历史 |
| 分支 | 轻量指针 | 数据库记录，更重量但可视化更好 |
| 大文件 | Git LFS | 内置二进制文件支持 |
| 锁 | 无内置 | 内置文件锁（适合美术资源） |
| GUI | 第三方工具 | 原生 GUI 强大，可视化合并 |

**关键区别**：Plastic 的 `checkin` 兼有 Git 的 `add` + `commit` + `push` 三重作用。习惯 Git 的开发者需要适应"提交即推送"的模式。

---

## ignore.conf 说明

Plastic SCM 使用 `ignore.conf`（非 `.gitignore`）排除文件。项目根目录已有该文件，典型内容：

```
# ignore.conf 示例
Library/
Temp/
Logs/
UserSettings/
*.csproj
*.sln
obj/
Build/
```

修改忽略规则后无需特殊操作，Plastic 自动生效。
