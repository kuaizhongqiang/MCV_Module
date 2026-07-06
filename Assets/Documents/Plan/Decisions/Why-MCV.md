# ADR: 为什么选择 MCV

> 状态：已采纳 | 日期：2026-07-06

## 决策

采用自研 **MCV（Model-Controller-View）** 架构作为项目主体架构模式。

## 背景

项目需要一套清晰的代码组织方式，将数据（Model）、逻辑（Controller）、表现（View）分离。在 Unity MonoBehaviour 环境下，C 和 V 天然容易耦合。

## 备选方案

| 方案 | 优点 | 缺点 |
|------|------|------|
| **经典 MVC** | 社区成熟、文档多 | View 持有 Controller 引用，单向数据流弱 |
| **MVP** | Presenter 解耦 View 与 Model | 接口层重，小团队维护成本高 |
| **MVVM** | 数据绑定自动化 | 依赖响应式框架（UniRx），引入额外依赖 |
| **Pure ECS** | 极致性能 | 不符合教学仿真场景，学习曲线陡峭 |
| **不用架构** | 零学习成本 | 随着规模增长必然失控 |

## 选择 MCV 的理由

1. **验证驱动**：团队想尝试一套自己的架构，MCV 是刻意轻量的实验
2. **解决了核心痛点**：C 和 V 分离后，逻辑和表现不再纠缠，代码可维护性明显提升
3. **适合 MonoBehaviour**：View = GameObject/Panel，Controller = 逻辑类，Model = GlobalDataMgr，映射自然
4. **小团队友好**：规则少、不引入第三方依赖、上手快

## 适用边界

- **漫游系统**：严格遵循 MCV
- **UI 展示**：严格遵循 MCV
- **步骤系统**：适度偏离（见 [Why-Prefab-Step.md](Why-Prefab-Step.md)），以编辑便利性优先

## 后果

- "MCV" 命名非行业标准，新人需额外解释
- 数据流规则尚未完全硬化，需在后续版本中沉淀为规范
