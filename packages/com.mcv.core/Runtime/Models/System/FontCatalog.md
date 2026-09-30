# Contract: FontCatalog

Role: 字体清单的取用门面：从 `Resources/Config/FontCatalog` 取 FontCatalogSO，按 fontId 解析条目、包名与两种形态的资产路径。纯查表，不做加载。

Fields:
ResourcePath:string  const "Config/FontCatalog"（相对 Resources，不带扩展名）
s_Asset:FontCatalogSO  static 缓存
s_Resolved:bool  static「只解析一次」标志

Methods:
Asset  get；首次访问 Resources.Load；缺失时告警一次并返回 null
Invalidate()  清缓存（Editor 工具改完清单后调用）
Find(fontId)  按 id 取条目；清单缺失或 id 不存在返回 null
BundleNameOf(fontId)  该字体所属全局包名；取不到返回空串
LegacyFontPathOf(fontId)  该字体的 Legacy 字体资产路径；取不到返回空串
TmpFontAssetPathOf(fontId)  该字体的 TMP 字体资产路径；取不到返回空串

Notes:
- 纯查表、不做加载：字体资产由 GlobalAssetsMgr 从字体包取，本类只回答「叫什么、在哪、哪个包」，避免把加载时机耦合进数据层。
- 清单缺失时**降级而不是崩**：`Asset` 返回 null、调用方据此保持节点上已有的字体引用不动（B1 的 TextComponent 就完全不碰字体，看不到清单也不影响显示）。
- `Invalidate()` 存在的理由：Editor 工具改完清单资产后清缓存，免得同一个编辑器会话里一直用旧表。
