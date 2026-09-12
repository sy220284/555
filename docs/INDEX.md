# 文档与实现总索引

本仓库已经从“纯设计文档”进入 `implementation_bootstrap`。编码时必须同时读取设计/规格和对应机器数据、代码契约；禁止继续把文档完成度当成实现完成度。

## 设计层

- `design/00_MASTER_GDD.md`：母设计、产品边界、规则深度预设、复杂度红线。
- `design/01_WORLD_LORE.md`：2037世界观、时间线、尤里/升格线索。
- `design/02_FACTIONS_COUNTRIES.md`：四大基础体系与15国特色。
- `design/03_GAMEPLAY_LOOP_MODES.md`：五种模式定位与玩家路径。
- `design/03A_FREEDOM_WARFARE.md`：高自由度战争、动态控制、无时间导演原则。
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
- `technical/SECURITY_ANTICHEAT.md`：安全边界摘要。
- `technical/OBSERVABILITY_CI.md`：日志、指标、持续集成和性能回归。

## 开发与落地层

- `development/00_ARCHITECTURE.md`：总体代码架构。
- `development/01_DATA_DRIVEN.md`：数据驱动与内容定义。
- `development/02_AI_AUTODEV.md`：AI全量自动开发模式。
- `development/03_NETWORK_DETERMINISM.md`：服务器权威网络、录像与重连。
- `development/04_PERFORMANCE_NAVIGATION.md`：性能、导航和大规模实体策略。
- `development/05_TEST_QA_BALANCE.md`：测试、AI农场和平衡闭环。
- `development/06_CONTENT_PIPELINE.md`：内容生产管线。
- `development/07_EXECUTION_MODEL.md`：并行任务、模块所有权与门禁。
- `development/08_IMPLEMENTATION_BOOTSTRAP.md`：当前真正已经入库的工程/数据/CI，以及尚未通过的门禁。
- `development/09_FULL_DESIGN_IMPLEMENTATION_AUDIT.md`：全设计深化、各系统实现状态、阻断项和风险驱动实现顺序。
- `development/10_FORMAL_IMPLEMENTATION_PROGRESS.md`：正式推进后真实通过的门禁、实跑数据与下一阻断项。

## 执行规格层

### 注册、单位、武器、科技与经济

- `specs/IDS_NAMING.md`：唯一稳定ID，包括 `MIN_` / `MAT_`。
- `specs/CONTENT_REGISTRY.md`：全局稳定ID入口与生命周期。
- `specs/UNIT_CATALOG.md`：单位数值基线。
- `specs/UNIT_LOADOUTS.md`：生产建筑、武器、能力、老练度、驻军和友军规则。
- `specs/WEAPONS_ARMOR_MATRIX.md`：伤害、装甲、命中和主动防护。
- `specs/ABILITY_STATUS_REGISTRY.md`：能力与状态。
- `specs/BUILDING_CATALOG.md`：建筑、防御和特色建筑。
- `specs/TECH_TREE_CATALOG.md`：T1—T5、分叉、征服材料前置、快速战争全科技。
- `specs/ECONOMY_FORMULAS.md`：资源、维修、指挥、算力、电力、补给公式。
- `specs/MINERAL_RESOURCE_SYSTEM.md`：矿种、采集、精炼、运输、枯竭和材料访问。
- `specs/CONQUEST_MINERAL_TECH_BINDING.md`：征服模式专属矿产—科技绑定与断供降级。
- `specs/SYSTEM_COMPLEXITY_BUDGET.md`：玩家操作、界面、CPU、网络、AI和资产六维预算。

### 游戏模式

- `specs/GAME_MODE_RULES.md`：五种模式权威胜负、动态控制、投降、友军与异常结算。
- `specs/MODE_PLAYABILITY_PROFILES.md`：五模式开局战备、核心决策、翻盘、地图和AI职责。
- `specs/MODE_RESOURCE_PROFILES.md`：五模式开局库存、地图经济和保障结构。
- `specs/QUICKWAR_FULL_READINESS.md`：快速战争满科技、完整建筑和65%—70%现役编制。
- `specs/BALANCE_BASELINES.md`：总体平衡基线。

### AI、机器人与多域战争

- `specs/AI_ROBOTICS_EW_STATE_MACHINES.md`
- `specs/AIR_NAVAL_ORBITAL_RULES.md`
- `specs/SUPERWEAPONS_STRATEGIC_ABILITIES.md`

### 地图、战役与玩家体验

- `specs/MAP_SPECS.md`：16图设计与矿产主题。
- `specs/MAP_LAYOUT_RUNTIME_SCHEMA.md`：地图侧车、ControlRegion、动态前线与灰盒契约。
- `specs/CAMPAIGN_MISSION_SPECS.md`
- `specs/CAMPAIGN_NARRATIVE_CAST.md`
- `specs/UI_ART_AUDIO_ACCESSIBILITY.md`
- `specs/AUDIO_VOICE_PIPELINE.md`
- `specs/DIFFICULTY_TUTORIAL_LOCALIZATION.md`

### 数据、工具与质量门禁

- `specs/SAVE_NETWORK_MOD_EDITOR_SCHEMA.md`
- `specs/TEST_MATRIX.md`
- `specs/IMPLEMENTATION_TEST_GATES.md`
- `specs/ACCEPTANCE_CRITERIA.md`
- `specs/SPEC_COMPLETENESS_MATRIX.md`

## 已实际入库的实现入口

### Unity/包

- `ProjectSettings/ProjectVersion.txt`：当前编辑器基线。
- `Packages/manifest.json`：正式依赖基线。
- `Packages/com.modernra.core`
- `Packages/com.modernra.rules`：纯C#权威规则内核，不允许引用Unity API。
- `Packages/com.modernra.simulation`
- `Packages/com.modernra.combat`
- `Packages/com.modernra.navigation`
- `Packages/com.modernra.ai`
- `Packages/com.modernra.intel-ew`
- `Packages/com.modernra.robotics`
- `Packages/com.modernra.network`
- `Packages/com.modernra.tests`

### 权威机器数据

- `/Data/Rulesets/rulesets.json`
- `/Data/Minerals/minerals.json`
- `/Data/Technology/common_technology.json`
- `/Data/Maps/MAP_GRAY_RANGE.map.json`
- `/Data/Schemas/*.schema.json`

### 无画面运行与自动门禁

- `Tools/content_validator.py`
- `Tools/project_structure_validator.py`
- `Tools/ModernRA.SimRunner`
- `.github/workflows/content-validation.yml`
- `.github/workflows/simulation-gates.yml`

当前无画面规则运行器已经验证1000单位、10000 tick × 10确定性、经济、共享路线、直射战斗、情报过滤、AI授权、机器人降级和本机权威网络规则；详细证据见 `development/10_FORMAL_IMPLEMENTATION_PROGRESS.md`。

这些通过仍不代表达到 `playable`；Unity真实编译、灰盒场景、ECS生产系统、目标硬件性能和G11完整歼灭仍是硬门禁。

## 编码前最小阅读集

任何编程代理至少读取：

1. `AGENT.md`
2. `technical/TECH_STACK.md`
3. `design/00_MASTER_GDD.md`
4. `development/00_ARCHITECTURE.md`
5. `development/08_IMPLEMENTATION_BOOTSTRAP.md`
6. `development/09_FULL_DESIGN_IMPLEMENTATION_AUDIT.md`
7. `development/10_FORMAL_IMPLEMENTATION_PROGRESS.md`
8. `specs/CONTENT_REGISTRY.md`
9. `specs/SYSTEM_COMPLEXITY_BUDGET.md`
10. 当前任务对应执行规格
11. `specs/TEST_MATRIX.md`、`specs/IMPLEMENTATION_TEST_GATES.md` 与 `specs/ACCEPTANCE_CRITERIA.md`

模式/经济/地图任务还必须读取 `03A_FREEDOM_WARFARE.md`、`GAME_MODE_RULES.md`、`MODE_PLAYABILITY_PROFILES.md` 和 `MODE_RESOURCE_PROFILES.md`。矿产任务再读 `MINERAL_RESOURCE_SYSTEM.md`；征服科技再读 `CONQUEST_MINERAL_TECH_BINDING.md`；快速战争再读 `QUICKWAR_FULL_READINESS.md`。

## 唯一优先级

`MASTER_GDD（产品原则） > TECH_STACK（技术选型） > 专项设计 > 执行规格 > 权威机器数据 > DEVELOPMENT代码边界 > 单项实现配置`。

若执行规格、数据与实现互相冲突，先停止功能扩展并统一三者；禁止长期保留双口径。
