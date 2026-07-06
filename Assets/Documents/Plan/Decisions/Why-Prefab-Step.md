# ADR: 为什么步骤系统用预制体驱动

## 概述

记录步骤系统采用 GameObject 预制体配置 + 运行时实例化方案而非纯代码驱动的决策理由。

## 待完善

- 备选方案（纯代码、ScriptableObject、Timeline）
- 决策核心考量（编辑便利性 vs 类型安全）
- 与 MCV Controller 的边界约定
