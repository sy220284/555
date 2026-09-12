# 文档总索引

本仓库文档分为设计层、技术上位层、开发层、执行规格层。编码时优先读取当前对象对应的执行规格；发生冲突时按本文优先级处理，禁止代理自行择一执行。

## 设计层

- `design/00_MASTER_GDD.md`：母设计文档、产品边界、核心玩法。
- `design/01_WORLD_LORE.md`：2037世界观、时间线、尤里/升格线索。
- `design/02_FACTIONS_COUNTRIES.md`：四大基础体系与15国特色。
- `design/03_GAMEPLAY_LOOP_MODES.md`：对局循环与五种模式定位。
- `design/04_TECH_ECONOMY_BALANCE.md`：五级科技、分叉、经济与平衡。
- `design/05_UNITS_BUILDINGS.md`：兵种、机器人、建筑与防御总览。
- `design/06_AI_ROBOTICS_EW.md`：AI、机器人、电子战、情报总览。
- `design/07_AIR_NAVAL_ORBITAL.md`：空海轨道总览。
- `design/08_MAPS_CAMPAIGN.md`：16张多人地图、24关战役总览。
- `design/09_ART_UI_AUDIO.md`：美术、界面、特效、音频总览。

## 技术上位层

- `technical/TECH_STACK.md`：冻结技术栈与实现边界。
- `technical/PERFORMANCE_ARCHITECTURE.md`：性能架构总规则。
- `technical/HARDWARE_PERFORMANCE_TARGETS.md`：消费者硬件与标准画质验收。
- `technical/BUILD_AND_LAYOUT.md`：工程目录、包边界与构建目标。
- `technical/CODING_STANDARDS.md`：C#/DOTS/Burst编码规范。
- `technical/ONLINE_SERVICES_MATCHMAKING_LIVEOPS.md`：在线服务、匹配、排位、赛季、灰度和回滚。
- `technical/ONLINE_SECURITY_PRIVACY.md`：账号、传输、反作弊、安全、隐私与供应链。
- `technical/SAVE_REPLAY_MODS_EDITOR.md`：存档、录像、模组、编辑器技术总览。
- `technical/SECURITY_ANTICHEAT.md`：安全边界摘要；详细规则见ONLINE_SECURITY_PRIVACY。
- `technical/OBSERVABILITY_CI.md`：日志、指标、持续集成和性能回归。
- `technical/README.md`：技术文档索引。

## 开发层

- `development/00_ARCHITECTURE.md`：总体代码架构。
- `development/01_DATA_DRIVEN.md`：数据驱动与内容定义。
- `development/02_AI_AUTODEV.md`：AI全量自动开发模式。
- `development/03_NETWORK_DETERMINISM.md`：服务器权威网络、可复现录像与重连。
- `development/04_PERFORMANCE_NAVIGATION.md`：当前性能、导航和大规模单位执行策略。
- `development/05_TEST_QA_BALANCE.md`：测试、AI对战农场和平衡闭环。
- `development/06_CONTENT_PIPELINE.md`：内容生产管线。
- `development/07_EXECUTION_MODEL.md`：并行任务图、模块所有权与门禁。

## 执行规格层

### 注册、单位、武器与规则

- `specs/IDS_NAMING.md`：唯一稳定ID命名规范。
- `specs/CONTENT_REGISTRY.md`：全局稳定ID入口。
- `specs/UNIT_CATALOG.md`：单位数值基线。
- `specs/UNIT_LOADOUTS.md`：单位生产建筑、武器、能力、老练度、驻军与友军规则。
- `specs/WEAPONS_ARMOR_MATRIX.md`：伤害、装甲、武器、命中、主动防护。
- `specs/ABILITY_STATUS_REGISTRY.md`：普通能力与状态ID。
- `specs/BUILDING_CATALOG.md`：建筑、防御与特色建筑。
- `specs/TECH_TREE_CATALOG.md`：T1—T5科技前置、成本、分叉。
- `specs/ECONOMY_FORMULAS.md`：资源、维修、指挥、算力、电力、补给公式。
- `specs/GAME_MODE_RULES.md`：五种模式胜负、占领、投降、友军和异常结算。
- `specs/BALANCE_BASELINES.md`：总体节奏与平衡基线。

### AI、机器人与多域战争

- `specs/AI_ROBOTICS_EW_STATE_MACHINES.md`：AI权限、频率、状态机、机器人自主、EW/网络战。
- `specs/AIR_NAVAL_ORBITAL_RULES.md`：航空、海军、潜艇、无人海战与轨道执行规则。
- `specs/SUPERWEAPONS_STRATEGIC_ABILITIES.md`：超级武器和战略能力。

### 地图、战役与玩家体验

- `specs/MAP_SPECS.md`：16张地图设计规格。
- `specs/MAP_LAYOUT_RUNTIME_SCHEMA.md`：地图精确运行时侧车数据与灰盒生成契约。
- `specs/CAMPAIGN_MISSION_SPECS.md`：24关逐关目标与机制。
- `specs/CAMPAIGN_NARRATIVE_CAST.md`：人物、叙事分幕、默认任务契约、对白与过场。
- `specs/UI_ART_AUDIO_ACCESSIBILITY.md`：UI、输入、美术预算、无障碍硬规格。
- `specs/AUDIO_VOICE_PIPELINE.md`：FMOD/WarAudioDirector、音效语音生成、混音、来源追溯。
- `specs/DIFFICULTY_TUTORIAL_LOCALIZATION.md`：难度、教学、本地化与输入默认规则。

### 数据、工具与质量门禁

- `specs/SAVE_NETWORK_MOD_EDITOR_SCHEMA.md`：服务器权威存档/网络/录像/重连/模组/编辑器数据规格。
- `specs/TEST_MATRIX.md`：自动测试、性能、AI、地图、网络和平衡测试矩阵。
- `specs/ACCEPTANCE_CRITERIA.md`：完成验收条件。
- `specs/SPEC_COMPLETENESS_MATRIX.md`：文档完整性和剩余生产资产状态。

## 编码前最小阅读集

任何编程代理至少读取：

1. `AGENT.md`
2. `technical/TECH_STACK.md`
3. `design/00_MASTER_GDD.md`
4. `development/00_ARCHITECTURE.md`
5. `specs/CONTENT_REGISTRY.md`
6. 当前任务对应执行规格
7. `specs/TEST_MATRIX.md`
8. `specs/ACCEPTANCE_CRITERIA.md`

## 唯一优先级

`MASTER_GDD（产品原则） > TECH_STACK（技术选型） > 专项设计（系统定位） > 执行规格（具体参数/状态机） > DEVELOPMENT架构（代码组织） > 单项实现配置`。

若执行规格与上位文档冲突，先修文档再编码。不同执行规格彼此冲突时，以更具体对象的专项规格为准，但必须同步修正其他文档，不能长期保留双口径。
