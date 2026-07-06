# 答疑记录

> 每轮 10 问，逐条销项。记录格式：编号 + 问题 + 答案 + 决议。

---

## 第 1 轮（宏观架构）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | 项目定位？ | 从头手搓，独立产品 | 独立产品 |
| 2 | MCV 核心契约？ | 尽量边界明确 | 需后续细化 |
| 3 | MCV 命名去留？ | 都行 | 暂保持 MCV |
| 4 | 步骤系统偏离 MCV？ | 是妥协，降低编辑者门槛 | Step 以编辑便利性优先 |
| 5 | 为什么选 MCV？ | 单纯想试试 | 尝试验证型决策 |
| 6 | 为什么步骤用预制体？ | 零代码编辑是核心要求 | 预制体 + 零代码编辑 |
| 7 | 优先级里程碑？ | 没想好 | 后续定 |
| 8 | 场景加载方式？ | 异步 + 99_Loading + LoadingCanvas | 异步加载 |
| 9 | Manager 初始化失败？ | 重试+超时+退出 | 重试+超时+Quit |
| 10 | Manager 间依赖？ | 并列，互相调用 | 平级互调 |

---

## 第 2 轮（Step 系统深化 + 基础设施）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | 零代码编辑具体层面？ | Inspector/Animation/Hierarchy 面板预置功能+参数化配置 | 预置功能+参数化 |
| 2 | Step 偏离 MCV 具体点？ | 待 Step 设计启动后再议 | 暂缓 |
| 3 | Manager 调用环？ | 各管一摊，无环 | 无环 |
| 4 | MCV 试下来感觉？ | 挺好用 | 继续 MCV |
| 5 | 短期目标？ | 通用化 + 应用到现有项目 | 通用化落地 |
| 6 | Controller 生命周期？ | 手动放置场景 + 手动注册 Mgr | 手动放置+注册 |
| 7 | Addressable 三种策略？ | 场景→AA，模型/预制体/图片→AB，内置→Default | 三策略分工 |
| 8 | N 场景命名？ | 按加载顺序数字前缀 | 数字前缀 |
| 9 | LoadingCanvas 驱动？ | GlobalSceneMgr 统一驱动 | SceneMgr 驱动 |
| 10 | 初始化失败关闭方式？ | 直接 quit | Application.Quit() |

---

## 第 3 轮（数据层 + UI 框架 + 交互 + 用户）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | DataBase 继承链？ | 中间件可有可无，AI 定 | AI 定 |
| 2 | JSON 容错？ | 待具体分析 | 待细化 |
| 3 | 缓存/脏标记？ | 读为主，写低优 | 读为主 |
| 4 | 6 种 TaskData 扩展字段？ | 图文、漫游、步骤三类 | 三类 Data |
| 5 | ProjectClip 依赖？ | 无，自由跳转 | 无依赖 |
| 6 | Tasks 合成？ | 允许少于 6 个 | null 跳过 |
| 7 | Canvas 层级？ | 2 层 + LoadingCanvas 顶层 | 暂两层 |
| 8 | Panel 栈管理？ | 支持返回，每种 Panel 单例 | 栈管理+单例 |
| 9 | InteractiveBase 分类？ | 按功能目的分类，开放拓展 | 按目的分类 |
| 10 | 用户系统？ | 低优，暂不议 | 低优 |

---

## 第 4 轮（交互分类 + 模块集成 + 文档策略）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | InteractiveBase 分类方式？ | 按功能目的分类。核心需求：统一 Update 周期 | 功能目的分类 |
| 2 | TaskData 三类与 6 个 TaskType 对应？ | 目的/设备/原理→图文，连线→步骤，实训→漫游+步骤，测试→图文 | 三类自由映射 |
| 3 | SceneMgr 与 AddressableMgr？ | SceneMgr 指挥 + AddressableMgr 加载 | 指挥+加载 |
| 4 | AB 策略？ | AA=场景加载，AB=资源包加载 | 两种管理方式 |
| 5 | Panel 层级管理？ | CanvasBase 负责 | 层级归 CanvasBase |
| 6 | Manager API 粒度？ | 灵活，可以根据需要增加 | 不硬定 |
| 7 | ADR 是什么？ | 用户提问 | 已解释 |
| 8 | JSON 写入（进度/评分）？ | 有但后续加 | 低优 |
| 9 | 实验指南粒度？ | 流程图为主 | 流程图优先 |
| 10 | 文档交叉引用？ | Overview 单向，子文档双向 | 单向+双向 |

---

## 第 5 轮（术语 + 业务模块 + 落地策略）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | ADR 补全？ | AI 判断即可 | AI 定 |
| 2 | Canvas sortingOrder？ | 有但不定死 | 留占位 |
| 3 | IObj 核心事件？ | Enter/Exit/Click 核心，Down/Up/Drag/Move 属拖拽 | Enter/Exit/Click |
| 4 | AA/AB/Default——对？ | 对 | 确认 |
| 5 | InteractiveBase 限定几种？ | 开放拓展，不定死 | 开放 |
| 6 | 漫游方案？ | FPS(WASD) + CharacterController 碰撞 | FPS |
| 7 | 视频播放？ | AVPro 计划中，未导入 | AVPro 计划 |
| 8 | 术语中文？ | ProjectData=项目数据 / ProjectClip=实训项目 / Task=实训任务 / TaskData=任务数据 / TaskType=任务类型 / StepSystem=步骤系统 | 术语统一 |
| 9 | 步骤原则先写入？ | 需要其他内容支撑，暂不单独写 | 等待配套 |
| 10 | 文档同步规则？ | 需澄清 | 已澄清下一轮 |

---

## 第 6 轮（收尾 + 落地补文档）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | 文档同步更新规则？ | 先文档后代码 | 文档驱动开发 |
| 2 | Panel 层级先不定死？ | 对 | CanvasBase 负责，表以后补 |
| 3 | 术语中英混排？ | ProjectClip（实训项目） | 中文正文+英文标注 |
| 4 | AddressableMgr API？ | `LoadAsset<T>(key, onSuccess, onFailure)` | 回调式异步 |
| 5 | PackageConfigSO 字段？ | AI 定，去 version/dependencies | 简单配置 |
| 6 | InteractiveBase 注册？ | Awake 自动注册 | Awake 自动 |
| 7 | Panel 注册方式？ | Awake 自己注册 | Awake 自动 |
| 8 | mermaid 图？ | 最好增加 | 加 mermaid |
| 9 | 模糊地带清除？ | Step + Line 待讨论 | Step + Line 待议 |
| 10 | 补文档顺序？ | 先步骤系统和连线 | Step + Line 优先 |

---

## 第 7 轮（Step 落地 + EventBus + 架构融合）✅ 已销项

> 参考文档：Tuanjie StepSystem_Manual.md

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | EventBus 引入 + 重写？ | 需要，具体内容重写 | 引入 EventBus，重写 |
| 2 | 8 种 Condition 与三类 TaskData 的关系？ | Condition 属 Step 层。Task → StepSystem → Step → Condition | Task → StepSystem → Step → Condition |
| 3 | StepSystem 层级？ | 必须是 StepSystem → Processing → Step 三级 | 三级结构确认 |
| 4 | 预置 Condition + Animation 满足零代码？ | 对，配合 Animation 解决时间轴编辑 | Animation 辅助时间轴 |
| 5 | StepDirector 算 Manager？ | StepDirector = 特殊 Manager，Condition 更像 Controller。可设 GlobalStepSystemMgr | StepDirector=Manager，Condition≈Controller |
| 6 | tipsId / audioId 数据存储？ | 每个 Step 包含 tipsId 和 audioId，Waiting 时 UI 显示文字 + AudioMgr 播音频。数据存在 JSON 中 | Step 配 tips+audio JSON |
| 7 | LineConnect 子系统？ | LineDraw 不可重写（只能优化/增量），其余允许重写 | LineDraw 保留，其余可重写 |
| 8 | 动画方案？ | 只用 Legacy Animation | Legacy Animation |
| 9 | Step UI Controller 归属？ | 放 MCV Controller 层 | MCV Controller 层 |
| 10 | P0S0 快进——原则 + 性能？ | 原则必须。理由：编辑便利性。编辑第 99 步不需要看前 98 步的初始状态 | 强制 P0S0 快进 |

---

## 第 8 轮（Step 细节 + EventBus + 开发优先级）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | EventBus 独立 or Manager？ | 独立机制，灵活使用。比如一个 controller 控制多个 controller 状态 | EventBus 独立 |
| 2 | GlobalStepSystemMgr 第 9 个？ | 对。StepDirector 跟预制体通过 AB 包加载 | 第 9 个 GlobalManager |
| 3 | tipsId/audioId 数据位置？ | 需要单独 model 层、数据结构和加载方式，还没写 | 待建 Step Data Model |
| 4 | LineDraw 不变边界？ | 划线逻辑不变、曲线路径不变、多点绘制不变，其他可变 | 三不变 |
| 5 | Processing/Step 中文？ | Processing=进程、Step=步骤，具体 AI 定 | 进程/步骤 |
| 6 | P0S0 性能补偿？ | 跳转前用 99_Loading 遮挡，慢点无所谓 | Loading 遮挡补偿 |
| 7 | 开发优先级？ | Step 是核心但依赖其他组件，所以后开发 | Step 后做 |
| 8 | 8 种 Condition 先后？ | 同步实现，不分先后 | 8 种全量一次性 |
| 9 | StepConditionBase + 8 子类？ | 纯拖拽，至少 8 个继承类 | Base + 8 |
| 10 | P0S0 原则写入 ADR？ | 设计原则：编辑第 N 步时不考虑 N-1 步状态 | 写入 ADR |

---

## 第 9 轮（收尾 + 文档补全落地）✅ 已销项

| # | 问题 | 答案 | 决议 |
|:-:|------|------|:--:|
| 1 | Step Data Model 草案？ | 参考 `Tuanjie_Structure/Assets/Scripts/Data/` | 参考 Tuanjie Data 脚本 |
| 2 | 术语关系图？ | 步骤系统包含多个进程，每个进程含多个步骤，每个步骤由一个条件控制 Waiting→Complete | Processing→Step→Condition |
| 3 | LineDraw "可变"含义？ | 可以修改代码和逻辑 | LineDraw 允许改代码 |
| 4 | 文档补全顺序？ | 先全量文档，然后代码开发 | 文档先行 |
| 5 | 8 种 Condition 全写？ | 尽量全面 | 全量覆盖 |
| 6 | FPS 方案？ | 直接搬 Tuanjie `FirstPersonController.cs` + 预制体 | 复用 Tuanjie FPS |
| 7 | Manager 初始化顺序？ | 都行 | AI 定 |
| 8 | 架构图？ | 你来定 | AI 定 |
| 9 | 模糊地带清了吗？ | 想讲的基本讲完了 | 思维拉齐完成 |
| 10 | 下一步策略？ | 先推到目录+概述阶段再逐一细化，还是一次性搞定？ | **等待 AI 建议** |

---

## 📊 九轮汇总

| 维度 | 核心决议 |
|------|---------|
| 项目 | 独立产品、文档驱动开发（先文档后代码） |
| 架构 | MCV + EventBus、9 个 GlobalManager 平级、先基础后 Step |
| Step | 三级结构（StepSystem→Processing→Step→Condition）、8 种 Condition 全量、P0S0 快进 |
| 资源 | AA管场景 / AB管预制体图片 / Default管内置 |
| 数据 | 三类 TaskData（图文/漫游/步骤）、读为主写低优、Step 需单独 Model |
| UI | CanvasBase 层级 + Panel 栈管理单例、LoadingCanvas 顶层、Legacy Animation |
| 交互 | 按功能目的开放拓展、Awake 自动注册 |
| 复用 | FPS Controller、Step 8 Condition 结构参考 Tuanjie，代码重写 |

---

## 🎯 下一步建议

> 你的 #10 问得好——先推到"目录+概述"再逐一细化，还是一次性定稿？

**我建议分两步走，理由：**

1. 一次性写 22 份完整文档，每份都可能成为瓶颈，你一卡我就停
2. "目录+概述"阶段相当于先把框架从"待完善"推进到"有结论"，速度快、风险低
3. 等全部概述完成，再按优先级逐个深入——浅的文档（如 ADR）可能一轮就定稿，深的（如 Step）需要多轮

**两步计划：**

| 阶段 | 目标 | 工作量 |
|:--:|------|:--:|
| **P1** | 22 份文档全部补全"概述"段，消除所有"待完善"占位，加 mermaid 架构总图 | 轻量 |
| **P2** | 按优先级逐份深入定稿（ADR → 架构 → 业务 → 模块 → 指南 → Step） | 分批 |

你觉得这个节奏可以吗？可以的话我立刻开始 P1。
