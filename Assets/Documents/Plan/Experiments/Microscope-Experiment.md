# 示例实验：显微镜使用

> **选题**：显微镜标准操作流程
> **适用课程**：生物/医学实验基础
> **难度**：入门级
> **6 Task 设计**：完整覆盖 6 种 TaskType

---

## 实验流程

```
Purpose（实验目的）→ Equipment（认识仪器）→ Principle（成像原理）
→ Training（仿真操作）→ Test（小测验）
```

## Task 设计

### Task 1: Purpose（实验目的）

- **displayName**: 实验目的
- **SpeakingData**: 
  - showContent: "掌握显微镜的标准操作流程，了解显微镜各部件功能，能够独立完成显微镜的对光和观察操作。"
  - videoPath: "Videos/microscope_intro.mp4"

### Task 2: Equipment（实验仪器）

- **displayName**: 实验仪器
- **EquipmentData**:
  - equipmentName: "光学显微镜"
  - iconPath: "Icons/microscope"

### Task 3: Principle（实验原理）

- **displayName**: 实验原理
- **PrincipleData**:
  - showContent: "光学显微镜利用凸透镜放大原理，物镜和目镜组合实现高倍放大..."
  - showModelPath: "Models/microscope_cutaway"
  - videoPath: "Videos/optics_principle.mp4"

### Task 4: LineConnection（电路连接）

> 显微镜无电路连接，此 Task 可设为 null 跳过。

### Task 5: Training（仿真实验）

- **displayName**: 仿真实验
- **TipsData**:
  - tipText: "请按步骤操作显微镜：1. 安放标本 2. 调节粗准焦螺旋 3. 调节细准焦螺旋 4. 观察记录"
  - displayDuration: 5f

### Task 6: Test（小测验）

- **displayName**: 小测验
- **QuestionClip**:
  - questions:
    - questionText: "显微镜中起主要放大作用的是？"
      - options: ["目镜", "物镜", "反光镜", "粗准焦螺旋"]
      - correctAnswer: "物镜"
    - questionText: "调节焦距时应先使用？"
      - options: ["细准焦螺旋", "粗准焦螺旋", "光圈", "反光镜"]
      - correctAnswer: "粗准焦螺旋"

---

## ProjectClip JSON

详见 `StreamingAssets/Data/ProjectData.json` 中的 `clip_microscope` 配置。

## 场景配置

- 场景名: `3_Microscope`
- 需要 3D 模型: 显微镜主体（含物镜/目镜/载物台/反光镜）
- 交互物体: 粗准焦螺旋、细准焦螺旋、标本夹
- StepDirector: 配置 6 个步骤（对应 Training 内的操作序列）

## 验证

1. 运行 0_Setup → 主菜单显示"显微镜使用"实验
2. 依次通过 6 个 Task
3. 仿真实验步骤可交互操作
4. 小测验答题计分
5. 结果显示
