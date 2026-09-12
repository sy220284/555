# 文档总索引

## 设计层

- `design/00_MASTER_GDD.md`：母设计文档、产品边界、核心玩法。
- `design/01_WORLD_LORE.md`：2037 世界观、时间线、尤里“升格”。
- `design/02_FACTIONS_COUNTRIES.md`：四大基础体系与 15 国特色。
- `design/03_GAMEPLAY_LOOP_MODES.md`：对局循环、三档复杂度、多人模式。
- `design/04_TECH_ECONOMY_BALANCE.md`：五级科技、分叉、经济、指挥容量与平衡。
- `design/05_UNITS_BUILDINGS.md`：兵种、机器人、建筑和防御体系。
- `design/06_AI_ROBOTICS_EW.md`：AI 指挥、机器人、电子战、网络战、情报。
- `design/07_AIR_NAVAL_ORBITAL.md`：空军、海军、近空间与轨道战略层。
- `design/08_MAPS_CAMPAIGN.md`：9 大战区、16 张多人地图、24 关战役。
- `design/09_ART_UI_AUDIO.md`：美术、界面、特效、音频与可读性。

## 开发层

- `development/00_ARCHITECTURE.md`：总体代码架构。
- `development/01_DATA_DRIVEN.md`：数据驱动、内容定义、配置规则。
- `development/02_AI_AUTODEV.md`：AI 全量自动编程推进模式。
- `development/03_NETWORK_DETERMINISM.md`：确定性模拟、网络、录像、重连。
- `development/04_PERFORMANCE_NAVIGATION.md`：性能预算、寻路和大规模单位。
- `development/05_TEST_QA_BALANCE.md`：测试、AI 对战农场和平衡闭环。
- `development/06_CONTENT_PIPELINE.md`：单位、建筑、地图、战役和美术生产管线。
- `development/07_EXECUTION_MODEL.md`：并行任务图、模块所有权、交付门禁。

## 技术层

- `technical/README.md`：技术文档索引。
- `technical/BUILD_AND_LAYOUT.md`：构建、目录、依赖约束。
- `technical/CODING_STANDARDS.md`：编码规范与确定性注意事项。
- `technical/SAVE_REPLAY_MODS_EDITOR.md`：存档、录像、模组、编辑器。
- `technical/SECURITY_ANTICHEAT.md`：安全、校验与反作弊边界。
- `technical/OBSERVABILITY_CI.md`：日志、性能遥测、持续集成门禁。

## 规格层

- `specs/IDS_NAMING.md`：稳定 ID、命名、版本规则。
- `specs/BALANCE_BASELINES.md`：经济、单位价值、时间与容量基线。
- `specs/ACCEPTANCE_CRITERIA.md`：项目、系统、内容完成验收条件。

## 变更规则

上位文档优先级：`MASTER_GDD` > 专项设计 > 开发架构 > 技术实现 > 单项配置。任何破坏产品原则的技术便利都必须退回重新设计。