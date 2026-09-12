# 现代红色警戒（工作名）

一款把经典基地建设即时战略推进到2037年现代/近未来战争的项目：基地、资源、生产、科技、阵营差异和超级武器仍是核心，同时扩展无人系统、机器人、AI战斗指挥、电子战、综合防空、远程火力、海空联合作战与有限轨道支援。

> 若未取得相关商业IP授权，所有受保护专有名称、角色、音乐、美术与品牌必须在发行前替换为原创内容。项目逻辑ID与显示层分离，支持整体替换。

## 核心体验

1. 经典即时战略：建设、采集、生产、科技、爆兵、决战。
2. 现代战争体系：侦察—识别—压制—打击—突破—占领—保障。
3. AI统帅：玩家决定战略，AI执行被授权的局部战术、战斗群和战区任务。
4. 未来升级：机械化 → 信息化 → 无人/机器人 → 智能化 → 空天/未来能力。
5. 高自由度战争：不使用时间脚本强推节奏；允许快攻、稳推、围困、封锁、龟缩反击、侧翼、登陆和长期大战。

## 冻结产品边界

- 4套基础战争体系；15国世界设定；首发竞技优先10国。
- 5级科技，T4以后分叉。
- 5种多人模式：歼灭、征服、前线、战区战争、快速战争。
- 3种规则深度预设：简化、标准现代、完整战争；它们不是额外游戏模式。
- 完整陆战/空战、核心海战、有限轨道战略层。
- AI指挥、机器人、无人系统、电子战、网络战、简化后勤、战争迷雾与情报。
- 16张首发多人图、24关主线；当前不继续扩大数量，优先把既有内容做深做实。
- 征服模式独享战略矿产—部分高阶科技绑定；其他标准模式矿产不硬锁核心科技。
- 快速战争以全科技、完整功能建筑、65%—70%有效指挥容量现役编制直接开战。
- 标准模式禁止固定局长、超时增益、时间型资源/伤害/积分倍率和按分钟科技解锁。

## 冻结技术栈

- Unity 6.3 LTS当前基线；
- C#；
- Entities/DOTS + Burst + Job System；
- URP + Entities Graphics/GPU批量表现；
- Unity Transport + 项目自研RTS权威复制层；
- Linux专用服务器；
- Addressables；
- FMOD Studio + `WarAudioDirector`；
- 项目自研战争模拟、分层AI、分层导航、空间索引、2.5D权威战斗物理、情报/EW、机器人和网络相关性算法。

详细见 `docs/technical/TECH_STACK.md`。

## 性能基线

最低完整体验直接锚定当前主流游戏电脑：现代6核12线程级处理器、16GB双通道、8GB级独显、NVMe SSD，1080P标准画质。

标准现代对局目标800—1500有效活动实体、常规60FPS；大型团队1500—3000；战区扩展3000—5000。性能必须优先靠算法、数据结构、批处理、结果复用和表现批量化解决，不能靠削AI、砍单位或残缺画质。

## 已进入实际实现链

仓库现在不再只有文档。已经实际加入：

- `ProjectSettings/ProjectVersion.txt`：Unity编辑器基线；
- `Packages/manifest.json`：核心正式包版本；
- `com.modernra.core`：稳定ID；
- `com.modernra.rules`：纯C#确定性权威规则内核；
- `com.modernra.simulation`：30Hz实体模拟边界并引用统一规则内核；
- `com.modernra.combat`：装甲/武器/伤害事件契约；
- `com.modernra.navigation`：导航层、共享路径走廊、空间索引和局部避障契约；
- `com.modernra.ai`：AI权限组件与统一授权规则适配；
- `com.modernra.intel-ew`：六级情报、四级电子战与统一情报过滤规则适配；
- `com.modernra.robotics`：A1—A4自主状态与统一降级规则适配；
- `com.modernra.network`：命令/快照边界、五级复制可见性；
- `com.modernra.tests`：编辑器契约测试、确定性规则测试、规则适配测试；
- `Tools/ModernRA.SimRunner`：不依赖渲染/UI/音频的无画面权威规则运行器；
- `Tools/ModernRA.AnnihilationRunner`：地图驱动的完整歼灭规则闭环运行器；
- `Tools/runtime_map_codegen.py`：权威地图侧车生成编译期运行数据；
- `Tools/graybox_generator.py`：确定性灰盒/区域图/公平性派生；
- `GrayRangeBootstrapSystem`：Unity ECS地图锚点实体化；
- `AnnihilationRuleBridgeSystem`：统一歼灭规则到Unity ECS的固定步镜像桥；
- 可持久化确定性录像/检查点重放基础链；
- `/Data/Rulesets`、`/Data/Minerals`、`/Data/Technology`：首批权威数据；
- `/Data/Maps/MAP_GRAY_RANGE.map.json`：首张地图机器侧车；
- `/Data/Schemas`：首批JSON Schema；
- `Tools/content_validator.py`：独立内容验证器；
- `Tools/project_structure_validator.py`：Unity版本、包/程序集、架构边界与30Hz前置门禁；
- `Tools/run_unity_g1.py`：真实Unity批处理编译/EditMode执行器；
- `.github/workflows/content-validation.yml`：数据/结构/灰盒自动门禁；
- `.github/workflows/simulation-gates.yml`：确定性模拟/玩法自动门禁；
- `.github/workflows/unity-g1.yml`：真实Unity G1自托管执行入口。

整体生命周期仍是 **`implementation_bootstrap`**，不代表游戏已经可玩。

## 已实际验证

当前持续集成已经真实跑过：

- 纯C#权威规则运行器 `0 warning / 0 error` 构建；
- 同一地图、种子和命令条件下 `10000 tick × 10`，最终状态哈希完全一致：`A72CA0F85058C84B`；
- `MAP_GRAY_RANGE` 无画面模型实际生成1000个活动单位；
- 三条道路数据被读取，双方只构建2条共享方向路径走廊，不为每单位重建全图路径；
- 双方工业资源结果完全对称：`14999.600 / 14999.600`；
- 主采集累计 `20000.000`，第二采集组累计 `9999.200`，80%递减及采矿单位损失均生效；
- 第一种直射战斗发生6424次射击、636个击毁；
- 1000实体空间查询候选扫描较朴素全量扫描减少约94.6%；
- 1000单位自由目标获取/同步伤害战斗300 tick重复结果一致，基线哈希 `477FF238CD8346B6`；
- 3000实体分频错峰参考工作量较全频执行下降约57%；
- Unknown/Anomaly敌军不进入客户端状态；Detected/Classified/Confirmed/Tracked按不同精度下发；
- AI所属玩家、授权区、权限等级、禁令和玩家覆盖代次均进入判权；
- A1—A4机器人在断联、算力不足、重干扰/黑区下按等级降级；
- 本机权威服务器/客户端规则回环通过内容哈希校验、命令序列防重放、客户端意图校验、情报过滤、增量快照和客户端插值；
- `MAP_GRAY_RANGE` 标准歼灭规则闭环自然结束于 tick `1893`，最终哈希 `1979A9B7B4183764`；
- 歼灭录像16个检查点可持久化、读回并确定性重放；
- G1静态前置已验证冻结的 Unity `6000.3.24f1`、10个嵌入包、10个程序集和Simulation表现层依赖红线。

## 当前准确状态

当前生命周期：**`implementation_bootstrap`**。

已经通过的是权威规则、数据、无画面玩法、确定性、部分性能算法和Unity接桥代码。当前最高优先级阻断是：

- Unity `6000.3.24f1` 尚未在合法激活的真实编辑器/自托管执行器中完整解析包、编译全部程序集并运行EditMode测试（G1）；
- `MAP_GRAY_RANGE` 尚未在真实Unity进程内完成灰盒可视化、运行实体核对和导航烘焙；
- 正式ECS批处理经济、移动、导航、伤害、情报、AI、机器人/EW和Unity Transport复制系统尚未替换当前规则镜像/合同路径；
- 目标硬件上的800—1500实体P50/P95/P99性能预算尚未实测；
- 规则层歼灭已闭环，但Unity内玩家相机、框选、命令输入、可操作完整对局与玩家命令流录像尚未完成；
- 正式美术、音效、配音、战役、线上服务和发布级内容尚未生产。

因此不得把当前主线称为 `playable`、`content_complete` 或 `ship_ready`。

## 风险驱动实现顺序

真实Unity编译/EditMode → Unity灰原试验场运行核对 → 最低灰盒表现/相机/选择/命令 → 正式ECS批处理接入统一规则内核 → 目标硬件性能 → Unity完整歼灭灰盒 → 征服矿产科技 → 动态前线 → 快速战争满战备 → 战区大规模 → 正式表现/音频/内容生产。

每一步必须有自动测试、状态哈希和性能数据后再继续扩张。

## 关键入口

- [文档与实现总索引](docs/INDEX.md)
- [母设计](docs/design/00_MASTER_GDD.md)
- [全设计深化与实现审计](docs/development/09_FULL_DESIGN_IMPLEMENTATION_AUDIT.md)
- [正式实现推进记录](docs/development/10_FORMAL_IMPLEMENTATION_PROGRESS.md)
- [实现主干状态](docs/development/08_IMPLEMENTATION_BOOTSTRAP.md)
- [最终技术栈](docs/technical/TECH_STACK.md)
- [系统复杂度预算](docs/specs/SYSTEM_COMPLEXITY_BUDGET.md)
- [完整性矩阵](docs/specs/SPEC_COMPLETENESS_MATRIX.md)
- [测试矩阵](docs/specs/TEST_MATRIX.md)
- [实现门禁](docs/specs/IMPLEMENTATION_TEST_GATES.md)
- [AI代理执行规则](AGENT.md)

## 最终开发原则

复杂度藏在系统里，玩家只面对清晰决策；AI负责执行，玩家保留战略权；玩家决定战争节奏；强力系统必须有成本、节点和反制；设计必须继续落到权威数据、代码契约、自动测试和性能验证，禁止重新回到“文档写完就算完成”。
