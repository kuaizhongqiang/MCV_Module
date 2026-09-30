# Contract: Localized

Role: 业务数据的英文列取值：按当前语言在中 / 英两列之间挑一个，英文列为空时回退中文。

Methods:
Pick(zh, en)  两列挑一；当前语言非中文且 en 非空时返回 en，否则返回 zh
Pick(zh[], en[], index)  集合列（`pages` 这类）；下标越界或 en[index] 为空 → 回退 zh[index]；两边都取不到返回空串
Name(DataBase)  `displayName` 的便捷入口；null 输入返回 null（便于直接替换 `?.displayName` 调用点）

Notes:
- 业务数据**不走 key 表**：它们是固化数据，英文直接作为数据列存在（模型字段 + JSON 里的 `*En` 键），所以这里只做"两列挑一"，不做查找与回退链 —— key 表那条回退链在 `GlobalDataMgr.PickClipText`（两份实现必然漂移，别混）。
- `GlobalDataMgr.Instance` 为空（数据未就绪 / 退出态）时**按中文返回**：宁可显示中文，也不显示空。
- 集合列**不假设等长**：英文列短于中文列时按下标安全回退（设计上要求等长，运行期不因此崩）。
- 翻译的落点是 `StreamingAssets/Data/*.json` 里的 `*En` 键（工具：`MCV Editor/文字/补齐业务数据英文列`），填好即生效、无需改代码。
- **唯一例外：随模型预制体走的业务数据**（结构页零件名 `StructureTaskObj.structureNameEn`）——它属于某套结构模型的零件，翻不出 JSON，故英文列序列化在组件里；改完 **必须重建内容 AB 包**才在运行期生效。
- **不适用**：弹框身份（已是 `DialogId` 枚举，不再是可翻译的 `Title` 串；弹框正文仍硬编码中文）、枚举显示名（`ChnNameMap` / `EnumAll`）、成绩档案的名称快照 —— 这些翻不得或另有归属，见 `Docs/design_ai/Localization.md` §5。
