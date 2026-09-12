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

## 设计已经进入实际实现链

仓库现在不再只有文档。已经实际加入：

- `ProjectSettings/ProjectVersion.txt`：Unity编辑器基线；
- `Packages/manifest.json`：核心正式包版本；
- `com.modernra.core`：稳定ID；
- `com.modernra.simulation`：权威热状态、30Hz模拟契约和固定步进配置；
- `com.modernra.combat`：装甲/武器/伤害事件契约；
- `com.modernra.navigation`：导航层、共享路径走廊和局部避障契约；
- `com.modernra.ai`：AI权限、禁令、更新节拍、玩家覆盖代次；
- `com.modernra.intel-ew`：六级情报、四级电子战与签名状态；
- `com.modernra.robotics`：A1—A4自主、链路和算力分配；
- `com.modernra.network`：命令/快照头和复制可见性边界；
- `com.modernra.tests`：首批编辑器契约测试；
- `/Data/Rulesets`：五种标准规则集首批权威数据；
- `/Data/Minerals`：具体矿产权威数据；
- `/Data/Technology`：通用科技与征服材料前置首批权威数据；
- `/Data/Maps/MAP_GRAY_RANGE.map.json`：首张地图机器侧车；
- `/Data/Schemas`：首批JSON Schema；
- `Tools/content_validator.py`：独立内容验证器；
- `.github/workflows/content-validation.yml`：数据自动门禁。

这些只说明项目从 `design_defined` 进入了 `implementation_bootstrap`，**不代表游戏已经可玩**。

## 当前准确状态

当前生命周期：**`implementation_bootstrap`**。

已完成的是：核心设计一致性、冻结技术路线、首批权威数据、核心代码边界和数据持续集成。

尚未通过的关键门禁：

- Unity 6000.3.24f1真实打开/编译/编辑器测试；
- 10000 tick确定性无画面模拟与状态哈希；
- `MAP_GRAY_RANGE`灰盒生成和资源采集；
- 正式移动/共享导航、武器伤害、战争迷雾、AI战斗群、机器人/EW和网络回环；
- 第一局完整歼灭灰盒；
- 正式美术、音效、配音、战役、线上服务和目标硬件实测。

因此不得把当前主线称为 `playable`、`content_complete` 或 `ship_ready`。

## 风险驱动实现顺序

工程编译/数据CI → 10000 tick模拟 → 灰原试验场资源循环 → 共享导航 → 武器伤害 → 情报/迷雾 → AI战斗群 → 机器人/EW → 本机服务器回环 → 完整歼灭灰盒 → 征服矿产科技 → 动态前线 → 快速战争满战备 → 战区大规模 → 正式表现/音频/内容生产。

每一步必须有自动测试、状态哈希和性能数据后再继续扩张。

## 关键入口

- [文档与实现总索引](docs/INDEX.md)
- [母设计](docs/design/00_MASTER_GDD.md)
- [全设计深化与实现审计](docs/development/09_FULL_DESIGN_IMPLEMENTATION_AUDIT.md)
- [实现主干状态](docs/development/08_IMPLEMENTATION_BOOTSTRAP.md)
- [最终技术栈](docs/technical/TECH_STACK.md)
- [系统复杂度预算](docs/specs/SYSTEM_COMPLEXITY_BUDGET.md)
- [完整性矩阵](docs/specs/SPEC_COMPLETENESS_MATRIX.md)
- [测试矩阵](docs/specs/TEST_MATRIX.md)
- [AI代理执行规则](AGENT.md)

## 最终开发原则

复杂度藏在系统里，玩家只面对清晰决策；AI负责执行，玩家保留战略权；玩家决定战争节奏；强力系统必须有成本、节点和反制；设计必须继续落到权威数据、代码契约、自动测试和性能验证，禁止重新回到“文档写完就算完成”。
