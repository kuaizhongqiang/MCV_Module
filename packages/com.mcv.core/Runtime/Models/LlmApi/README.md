# Models/LlmApi —— AI 能力的纯数据层：模型价目、接口配置与请求 / 响应结构（不含网络调用与会话历史）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`Managers/GlobalAiMgr`（数据契约消费方）、`Assets/StreamingAssets/AiServer/AiServer.exe`（提示词与会话历史）

> 路径：`Assets/Scripts/Models/LlmApi/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Models.LlmApi`

## 选文件
- `ApiBase.cs` — 模型与接口配置：11 个模型价目聚合
- `LlmMessage.cs` — LLM 请求 / 响应数据壳（多为空壳）

## 跨文件约定
- **价目表集中在这里**，不要在 UI 或管理器里硬编码模型价格。
- 本目录**不做网络请求**，也不读 `GlobalDataMgr`；保持可被单独序列化 / 测试。
- **若 EXE 侧协议变更**：先改本目录的数据结构，再改 `GlobalAiMgr` 的解析代码，并同步同级 `A.md`。
