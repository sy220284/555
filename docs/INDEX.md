# 文档总索引

本仓库文档分为设计层、开发层、技术层、执行规格层。编码时优先读取与当前对象直接相关的执行规格；发生冲突时，上位设计决定玩法边界，执行规格决定具体实现。

## 设计层

- `design/00_MASTER_GDD.md`：母设计文档、产品边界、核心玩法。
- `design/01_WORLD_LORE.md`：2037 世界观、时间线、尤里“升格”。
- `design/02_FACTIONS_COUNTRIES.md`：四大基础体系与 15 国特色。
- `design/03_GAMEPLAY_LOOP_MODES.md`：对局循环、三档复杂度、多人模式。
- `design/04_TECH_ECONOMY_BALANCE.md`：五级科技、分叉、经济、指挥容量与平衡。
- `design/05_UNITS_BUILDINGS.md`：兵种、机器人、建筑和防御体系总览。
- `design/06_AI_ROBOTICS_EW.md`：AI 指挥、机器人、电子战、网络战、情报总览。
- `design/07_AIR_NAVAL_ORBITAL.md`：空军、海军、近空间与轨道战略层总览。
- `design/08_MAPS_CAMPAIGN.md`：9 大战区、16 张多人地图、24 关战役总览。
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
- `technical/SAVE_REPLAY_MODS_EDITOR.md`：存档、录像、模组、编辑器技术总览。
- `technical/SECURITY_ANTICHEAT.md`：安全、校验与反作弊边界。
- `technical/OBSERVABILITY_CI.md`：日志、性能遥测、持续集成门禁。

## 执行规格层

### 核心注册与数值
- `specs/IDS_NAMING.md`：基础命名规范。
- `specs/CONTENT_REGISTRY.md`：四阵营、15国、单位、建筑、地图、任务稳定 ID 与最低字段。
- `specs/UNIT_CATALOG.md`：通用及国家特色单位第一版精确参数。
- `specs/BUILDING_CATALOG.md`：建筑、防御、国家特色建筑精确参数。
- `specs/WEAPONS_ARMOR_MATRIX.md`：伤害、装甲、武器、命中、主动防护与反无人规则。
- `specs/TECH_TREE_CATALOG.md`：T1—T5 前置、成本、时间、分叉与国家科技。
- `specs/ECONOMY_FORMULAS.md`：资源、建造、维修、指挥、算力、电力、补给精确公式。
- `specs/BALANCE_BASELINES.md`：对局节奏与总体平衡基线。

### AI、机器人与多域战争
- `specs/AI_ROBOTICS_EW_STATE_MACHINES.md`：AI 四级权限、敌方AI、机器人自主、电子战/网络战状态机和公式。
- `specs/AIR_NAVAL_ORBITAL_RULES.md`：航空、海军、潜艇、无人海战与轨道战略层执行规则。
- `specs/SUPERWEAPONS_STRATEGIC_ABILITIES.md`：超级武器和15国/阵营战略能力的冷却、范围、效果和反制。

### 地图、战役与玩家体验
- `specs/MAP_SPECS.md`：16张多人地图逐图尺寸、资源、通路、设施、天气与验收。
- `specs/CAMPAIGN_MISSION_SPECS.md`：24关逐关目标、触发、AI、教学、存档和剧情揭示。
- `specs/UI_ART_AUDIO_ACCESSIBILITY.md`：UI、输入、美术预算、特效、音频、无障碍硬规格。
- `specs/DIFFICULTY_TUTORIAL_LOCALIZATION.md`：难度、新手教学、本地化与输入默认规则。

### 数据、工具与质量门禁
- `specs/SAVE_NETWORK_MOD_EDITOR_SCHEMA.md`：存档、录像、网络、重连、模组、编辑器数据结构。
- `specs/TEST_MATRIX.md`：自动测试、确定性、性能、地图、AI、平衡、模组完整测试矩阵。
- `specs/ACCEPTANCE_CRITERIA.md`：项目、系统、内容完成验收条件。
- `specs/SPEC_COMPLETENESS_MATRIX.md`：各领域是否已经达到可无歧义编码的完整性审计。

## 编码前最小阅读集

任何编程代理至少读取：
1. `AGENT.md`
2. `design/00_MASTER_GDD.md`
3. `development/00_ARCHITECTURE.md`
4. `specs/CONTENT_REGISTRY.md`
5. 当前任务对应的执行规格文件
6. `specs/TEST_MATRIX.md`
7. `specs/ACCEPTANCE_CRITERIA.md`

## 文档优先级

`MASTER_GDD`（产品原则） > 专项设计（系统定位） > 执行规格（具体参数/状态机） > 开发架构（代码组织） > 技术实现细节 > 单项配置。

若执行规格与母设计冲突，禁止私自编码，必须先修文档；若只是数值平衡调整，则在不改变角色和边界的前提下按测试闭环更新规格。