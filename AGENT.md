# AGENT.md — AI 自动开发总规则

本文件是所有编程代理、评审代理、测试代理、内容代理和资产代理的强制执行规则。

## 1. 文档优先级

1. `docs/design/00_MASTER_GDD.md`：产品原则。
2. `docs/technical/TECH_STACK.md`：冻结技术栈。
3. 专项设计文档：系统定位。
4. `docs/specs/*`：具体参数、状态机、ID、规则与验收。
5. `docs/development/*`：代码组织、开发流程。
6. 机器可读权威数据 `/Data` 与实际实现；若与上位文档冲突，先修文档/数据再继续实现。

禁止代理在冲突中自行挑选更方便的一套。

## 2. 编码前强制阅读

任何任务至少读取：

- `AGENT.md`
- `docs/technical/TECH_STACK.md`
- `docs/design/00_MASTER_GDD.md`
- `docs/development/00_ARCHITECTURE.md`
- `docs/development/08_IMPLEMENTATION_BOOTSTRAP.md`
- `docs/development/09_FULL_DESIGN_IMPLEMENTATION_AUDIT.md`
- `docs/specs/CONTENT_REGISTRY.md`
- `docs/specs/SYSTEM_COMPLEXITY_BUDGET.md`
- 当前任务对应专项规格
- `docs/specs/TEST_MATRIX.md`
- `docs/specs/ACCEPTANCE_CRITERIA.md`

涉及模式、经济、地图、AI战略或胜负还必须读取：

- `docs/design/03A_FREEDOM_WARFARE.md`
- `docs/specs/GAME_MODE_RULES.md`
- `docs/specs/MODE_PLAYABILITY_PROFILES.md`
- `docs/specs/MODE_RESOURCE_PROFILES.md`
- `docs/specs/ECONOMY_FORMULAS.md`

涉及矿产/征服科技还必须读取：

- `docs/specs/MINERAL_RESOURCE_SYSTEM.md`
- `docs/specs/CONQUEST_MINERAL_TECH_BINDING.md`
- `docs/specs/TECH_TREE_CATALOG.md`

涉及性能/导航/大规模实体还读：

- `docs/development/04_PERFORMANCE_NAVIGATION.md`
- `docs/technical/PERFORMANCE_ARCHITECTURE.md`
- `docs/technical/HARDWARE_PERFORMANCE_TARGETS.md`

涉及网络/存档/录像还读：

- `docs/development/03_NETWORK_DETERMINISM.md`
- `docs/specs/SAVE_NETWORK_MOD_EDITOR_SCHEMA.md`

涉及音频/语音还读 `docs/specs/AUDIO_VOICE_PIPELINE.md`；涉及地图还读 `MAP_SPECS.md` 与 `MAP_LAYOUT_RUNTIME_SCHEMA.md`；涉及战役还读 `CAMPAIGN_MISSION_SPECS.md` 与 `CAMPAIGN_NARRATIVE_CAST.md`；涉及在线服务/安全还读对应 `ONLINE_*` 技术文档。

对象若没有稳定ID、参数、状态机/公式、引用链、机器数据或验收用例，禁止凭经验编码，先补齐对应层。

## 3. 冻结技术栈

生产主线：Unity 6.3 LTS、C#、Entities/DOTS、Burst、Job System、URP、Entities Graphics + GPU批量表现、Unity Transport + 自研RTS权威复制层、Linux Dedicated Server、Addressables、FMOD Studio + `WarAudioDirector`，以及项目自研AI、寻路、空间索引、2.5D战斗物理、情报/EW、机器人和网络相关性算法。

禁止代理自行切换引擎、引入独立C++权威核心、把HDRP变成默认、把Netcode for Entities变成不可替换核心、替换FMOD、升级到预览/实验依赖，或未经评审用第三方插件替换关键战争算法。

当前编辑器版本锁在 `ProjectSettings/ProjectVersion.txt`；包版本锁在 `Packages/manifest.json`。实际Unity验证未通过前不得宣称工程可编译。

## 4. 开发模式与生命周期

采用全量并行、接口优先、持续集成、自动闭环，但所有对象必须按生命周期推进：

`design_defined -> data_defined -> code_contract_ready -> placeholder -> playable -> validated -> content_complete -> ship_ready`

- `design_defined`：只有规则文档。
- `data_defined`：存在Schema和机器可读权威数据。
- `code_contract_ready`：类型/模块边界已经建立。
- `placeholder`：占位运行路径存在。
- `playable`：核心循环可内部完成。
- `validated`：正确性、可复现、性能和网络门禁通过。
- `content_complete`：正式资产/文本/音频等齐备。
- `ship_ready`：全部验收通过。

禁止跨级汇报；尤其禁止把文档、接口空壳或灰盒冒充“完成”。

## 5. 模块所有权

核心包：`com.modernra.core`、`simulation`、`combat`、`ai`、`navigation`、`intel-ew`、`robotics`、`air`、`naval`、`orbital`、`network`、`presentation`、`audio`、`online`、`tools`、`tests`。

跨模块修改优先通过接口和数据契约；禁止为方便直接重写其他模块。自研的是战争算法，不重造Unity已经成熟解决的编辑器、资源、渲染、任务调度和传输基础设施。

## 6. 稳定ID与权威数据

- 只允许 `IDS_NAMING.md` 当前前缀，包含 `MIN_` 和 `MAT_`。
- 禁止新增 `FACTION_`、`COUNTRY_`、`BLDG_`、`ABILITY_`、`MISSION_`。
- 权威设计源：JSON + JSON Schema + 稳定ID；构建时烘焙为Blob/紧凑数据。
- ScriptableObject只能作为编辑器/资源桥接。
- 国家差异数据化，禁止大量国家硬编码分支。
- 文档、`/Data`、代码契约三者冲突时必须修正后再实现。
- `Tools/content_validator.py` 和后续Unity内容验证器属于主线硬门禁。

## 7. C# / DOTS / Burst

普通作战单位严禁每单位复杂MonoBehaviour `Update()`、复杂GameObject权威状态、热路径LINQ/字符串/反射/GC、无界容器、全局锁和每帧大量结构变更。

高频计算必须优先Burst可编译、批处理、向量化、Job并行和数据局部性。表现层不得反向决定模拟结果。

## 8. 权威30Hz与性能规则

权威模拟固定30Hz。`FixedStepSimulationSystemGroup` 必须显式设置为 `1/30s`，不能依赖Unity默认60Hz。

最低完整体验：现代6核12线程、16GB双通道、8GB级独显、NVMe、1080P标准画质、800—1500有效活动实体、常规60FPS目标。

优化顺序：删除无效工作 → 事件/脏区 → 降复杂度 → 空间索引 → 结果共享 → ECS布局 → Burst/向量化 → Job并行 → 减少同步/结构变更 → 表现批量化。

严禁用降低AI、减少同模式单位、降低机器人自主、修改伤害/情报/EW、缩地图、降低权威频率、残缺画质或提高最低配置来掩盖性能问题。

## 9. AI与机器人

运行时竞技AI使用效用评分、状态机、任务规划和事件驱动，不使用大语言模型实时控制战斗。

频率：战略1—2Hz、战区2—5Hz、战斗群5—10Hz、单位10—15Hz、局部避障10—20Hz。

AI权限必须通过权威数据/组件判定，而不能只靠UI。未经玩家授权不得使用终极能力、战略储备、改变主科技、拆核心基地、全面撤退或改变总主攻。玩家直接命令必须覆盖AI旧计划。

机器人受工业/战略资源、电力、指挥容量、算力和链路状态约束；A1—A4失联按规则降级。

## 10. 导航

固定架构：战略区域图 → 道路/桥梁/山口图 → 战斗群共享路径走廊 → 局部流场/路径 → 空间哈希避障。

禁止每单位独立全地图A*。地图局部改变只使相关缓存版本失效。

## 11. 权威战斗物理

自研轻量2.5D几何/弹道为权威；PhysX只处理非权威残骸、碎片和视觉碰撞。逻辑弹道与视觉弹道解耦。

## 12. 网络、安全、录像

正式联网为专用服务器权威模拟。Unity Transport只作为传输层，自研相关性/快照/压缩为复制层。

未侦察真实敌军、潜艇、隐身和假目标真相不得发送给无权限客户端；客户端不得提交伤害、资源、位置和胜负作为权威结果。录像/存档/重连必须带内容哈希与版本，并同样遵守隐藏信息边界。

## 13. 游戏模式、规则深度与自由度

五种游戏模式与三种规则深度预设是两个轴。规则深度值固定：`SIMPLIFIED / MODERN_STANDARD / FULL_WAR`，禁止再用“经典模式/现代模式/战区模式”称呼它们。

标准规则禁止：按分钟改资源/积分/伤害/科技，固定比赛时长、强制缩圈、战略压力阶段、固定 `FrontNode` 链，以及为了缩短平均局长直接加时间惩罚。

合法秒数只用于建造、研究、冷却、占领、整备、重连、投票和过场等动作过程。

征服标准规则允许矿产—科技绑定，但只能约束部分T3/T4/T5高阶路线；不得锁T1/T2基础生存能力，也不得扩散到其他标准模式。

快速战争保持全科技、完整功能建筑、65%—70%有效指挥容量现役编制；战略武器按正常初始充能规则。

## 14. 系统复杂度预算

所有重大新增/扩展必须按 `SYSTEM_COMPLEXITY_BUDGET.md` 回答：玩家操作、HUD、CPU、网络、AI决策、资产生产六项预算。

任何一项无法估算，禁止直接进入实现。新增一个高频玩家操作，原则上必须同步自动化或删除等量低价值操作。模式专属复杂系统不得默认扩散。

## 15. 地图

每张实际地图必须有 `/Data/Maps/<MAP_ID>.map.json`。没有侧车最多只能标 `layout_defined`。

前线/战区必须支持动态 `ControlRegion` 和多战略轴线；快速战争必须容纳完整基地和65%—70%编制。首张实现固定从 `MAP_GRAY_RANGE` 开始，生成/验证工具成熟后再批量扩展其余15图。

## 16. 战役

24关不再继续扩量。关键脚本必须状态条件+兜底；跳过过场不能改变任务结果。先闭环 `MIS_01` 的任务、存档、AI、占位音频和过场跳过，再批量复制生产路径。

## 17. 音频/语音

统一 `AudioEventBus -> WarAudioDirector -> FMOD`。生成式音频/合成语音必须记录来源、模型/服务、许可和人工审批；禁止未经授权克隆现实人物或演员声线。

## 18. 在线服务与安全

在线结果只接受权威服务器签名。排位、匹配、赛季、灰度、回滚、安全、隐私和反作弊遵循对应技术文档。在线服务故障不得改变已运行比赛的权威规则。

## 19. 完成定义

功能达到对应生命周期状态必须同时具备：代码/数据、单元或集成测试、`TEST_MATRIX.md`相关门禁、可复现/状态哈希（适用）、最低目标机性能无回退、网络/录像（适用）、文档同步、AI正确使用或明确不可用、反制与错误降级、资产来源/许可（适用）。

## 20. 自动修复

失败 → 最小复现 → 根因定位 → 最小修改 → 回归 → 可复现/性能复测。修复必须补回归用例；除非证明结构不可维护，否则禁止先重写整个模块。

## 21. 主分支门禁

最终目标是主分支始终满足Unity批处理编译、客户端启动、Dedicated Server构建、测试地图加载、无画面模拟、数据无悬空ID、自动测试、基础录像/重连和性能回归。

当前仓库仍处 `implementation_bootstrap`，真实Unity编译和基础对局尚未通过，因此在这些门禁建立前禁止宣称“主分支已经可玩”。

## 22. 当前风险驱动实现顺序

1. Unity真实编译 + 数据CI；
2. 10000 tick确定性SimRunner；
3. `MAP_GRAY_RANGE`灰盒与资源采集；
4. 共享导航；
5. 武器/伤害；
6. 情报/战争迷雾；
7. AI战斗群；
8. 机器人/EW；
9. 本机服务器/客户端回环；
10. 第一局完整歼灭灰盒；
11. 征服矿产科技；
12. 前线动态战线；
13. 快速战争满战备；
14. 战区大规模扩展；
15. 正式表现、音频和内容批量生产。

最终原则：**好玩优先于写实，清晰优先于复杂；玩家决定战争节奏，系统提供战争规律；每个核心设计必须继续走到数据、代码和测试，不允许重新停留在文档。**
