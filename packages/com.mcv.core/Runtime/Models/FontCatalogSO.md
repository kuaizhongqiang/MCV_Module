# Contract: FontCatalogSO

Role: 字体清单资产：fontId → 两种形态的资产路径 + 所属全局包名；**只存路径字符串、不持 Font / TMP_FontAsset 引用**。

Fields:
entries:List<FontCatalogEntry>  [SerializeField] 全部条目
Entries:IReadOnlyList<FontCatalogEntry>  只读视图

Methods:
Find(fontId)  线性查表；不存在返回 null

Notes:
- **为什么只存路径不存引用**：本资产住在 `Resources/Config/` 下（运行期 `Resources.Load` 取用）。一旦这里直连字体资产，Unity 会把字体一并打进 `resources.assets`，正是"字体进 AB、default 包不含字体"要避免的事。旧 Localization 分支的同类清单存的是引用，才不得不额外造一套字体脱钩/回挂机制。
- 条目字段：`id`（= `TextComponent.fontId`）、`legacyFontPath`（Assets/Fonts/xxx.ttf，Legacy 形态由 prefab 引用它，这里只作登记与校验）、`tmpFontAssetPath`（TMP 字体资产，TMP 形态没有 prefab 引用，运行期按此路径取）、`bundleName`（该字体所在的全局包）。
- 不进 `DataSO`/JSON 导出链路：它不是"运行期数据"，是资产登记表；加字体只改这份资产，零代码。
