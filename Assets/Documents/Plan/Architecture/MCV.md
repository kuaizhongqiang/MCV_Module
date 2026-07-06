# MCV 模式详解

## 概述

本项目采用自研 MCV（Model-Controller-View）架构——**漫游与 UI 展示严格遵循，步骤系统适度偏离**。Model 由 `GlobalDataMgr` 统一托管，Controller 单向绑定 View，View 为 Unity GameObject / UI Panel。详细职责边界与数据流待补充。

## 待完善

- Model/Controller/View 的职责定义与交互契约
- Controller 生命周期（创建、绑定、销毁）
- 步骤系统为什么以及如何偏离 MCV
