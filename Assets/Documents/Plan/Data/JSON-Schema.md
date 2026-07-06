# JSON 数据格式

## 概述

数据文件位于 `StreamingAssets/Data/`，通过 `JsonReaderWriter`（Newtonsoft.Json）读写。

## 文件清单

| 文件 | 对应类 | 读写策略 |
|------|--------|---------|
| `SystemData.json` | `SystemData` | 启动时读一次，不写回 |
| `ProjectData.json` | `ProjectData` | 启动时读取，运行时查询 |
| `UserData.json` | `UserData` | 低优，暂不议 |

## SystemData

```json
{
  "id": "system",
  "displayName": "系统配置",
  "projectName": "MCV_Module",
  "version": "0.1.0",
  "copyright": "...",
  "renderQuality": "High"
}
```

## ProjectData

```json
{
  "id": "project",
  "displayName": "项目数据",
  "projectClips": [
    {
      "id": "clip_01",
      "displayName": "实训项目名称",
      "tasks": {
        "purpose": { "id": "task_01", "displayName": "任务目的", "content": "..." },
        "equipment": { "id": "task_02", "displayName": "实验仪器", "items": [] },
        "principle": { "id": "task_03", "displayName": "实验原理", "content": "..." },
        "lineConnection": { "id": "task_04", "displayName": "电路连接", "connections": [] },
        "training": { "id": "task_05", "displayName": "仿真实验", "sceneId": "3_xxx" },
        "test": { "id": "task_06", "displayName": "小测验", "questions": [] }
      }
    }
  ],
  "currentClipId": "clip_01"
}
```

**约束**：
- `projectClips` 之间无依赖，可自由跳转
- 每个 Clip 的 6 个 Task 允许为 null，无最少限制
- Step 数据（tipsId/audioId）在对应 Task 内定义

## Step 数据（ProjectClip 内嵌）

```json
{
  "steps": [
    {
      "stepId": "step_01",
      "displayName": "步骤名称",
      "tipsId": "tip_001",
      "audioId": "audio_001",
      "conditionType": "Click"
    }
  ]
}
```

## 容错策略

| 场景 | 处理 |
|------|------|
| 文件不存在 | 使用默认值，可选写回默认文件 |
| 字段缺失 | 使用类型默认值 |
| JSON 格式错误 | 日志报错 + 使用默认值 |

## 跨平台路径

`StreamingAssets/Data/` 在移动端为只读路径，通过 `Application.streamingAssetsPath` 运行时获取。Android 需 `UnityWebRequest` 读取。

→ [数据层](../Architecture/Data-Layer.md) | [ProjectClip 规范](ProjectClip-Spec.md)
