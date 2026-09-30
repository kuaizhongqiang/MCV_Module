# Contract: Lang

Role: 运行时取文案的唯一入口（给不走文本组件的代码用）；空安全 + 回退链调用，不自带那份逻辑。

Methods:
Get(key)  按 key 取当前语言文案；顺序：当前语言 → 中文（clips[0]）→ key 本身；永不返回 null、不抛
Get(key, args)  取文案后 string.Format；占位符与参数不匹配时返回未格式化文本并告警（不抛）
Has(key)  key 是否已登记（校验/调试用）

Notes:
- **回退链的实现只有一份**，在 `GlobalDataMgr.PickClipText`；本类只做空安全与日志，别在这里再抄一遍（两处实现必然漂移）。
- `GlobalDataMgr.Instance` 为空（数据管理器未就绪 / 退出态）时**直接返回 key 本身**：不崩、也不显示空白。
- key 命名约定：`{域}.{语义}`，如 `ui.function.exit` / `ui.login.title.student` / `ui.dialog.confirm` / `ui.loading.progress`（带占位符的用 `Get(key, args)`）。
- **不负责**：业务数据（器件名 / 题干 / 步骤提示）——那些走各自数据源上的英文列，不走 key 表；也不负责弹框标题那种「身份串」（控制器按 `Title ==` 认领，翻译会让匹配失效，见 `Docs/design_ai/Localization.md` §17）。
