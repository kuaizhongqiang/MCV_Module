# JSON 数据格式

## 概述

数据文件位于 `StreamingAssets/Data/`，通过 `JsonReaderWriter`（Newtonsoft.Json）读写。当前包含三份文件：

| 文件 | 对应类 | 说明 |
|------|--------|------|
| `SystemData.json` | `SystemData` | 项目信息、版权、渲染质量 |
| `ProjectData.json` | `ProjectData` | 实验片段列表 |
| `UserData.json` | `UserData` | 用户凭证与成绩 |

## 待完善

- 各 JSON 的完整字段定义与示例
- 读写时机与路径约定
