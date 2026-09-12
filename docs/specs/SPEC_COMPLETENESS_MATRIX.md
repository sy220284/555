# 规格完整性矩阵

本文件区分三件事：

1. **设计是否定义**：产品/玩法方向是否明确。
2. **是否进入实现链**：是否已经有机器可读数据、代码契约和自动门禁。
3. **生产内容是否完成**：地图、美术、配音、音效、过场、线上服务等是否真正完成。

设计状态：`COMPLETE / PARTIAL / BLOCKED`。实现生命周期：`design_defined -> data_defined -> code_contract_ready -> placeholder -> playable -> validated -> content_complete -> ship_ready`。

## 1. 编码规格完整性

| 领域 | 规格状态 | 主文档 | 实现链现状 |
|---|---|---|---|
| 世界观/时间线 | COMPLETE | design/01_WORLD_LORE.md | design_defined |
| 四大体系/15国 | COMPLETE | design/02_FACTIONS_COUNTRIES.md + CONTENT_REGISTRY.md | design_defined |
| 高自由度战争原则 | COMPLETE | design/03A_FREEDOM_WARFARE.md + GAME_MODE_RULES.md | ruleset首批data_defined |
| 规则深度预设 | COMPLETE | design/00_MASTER_GDD.md + `/Data/Rulesets/rulesets.json` | data_defined |
| 稳定ID命名 | COMPLETE | IDS_NAMING.md | code_contract_ready |
| 全局注册入口 | COMPLETE | CONTENT_REGISTRY.md | 部分data_defined |
| 单位数值 | COMPLETE | UNIT_CATALOG.md | design_defined，待批量数据化 |
| 单位武器/能力装配 | COMPLETE | UNIT_LOADOUTS.md | design_defined |
| 建筑 | COMPLETE | BUILDING_CATALOG.md | design_defined |
| 武器/装甲/命中 | COMPLETE | WEAPONS_ARMOR_MATRIX.md | Combat代码契约已建 |
| 普通能力/状态 | COMPLETE | ABILITY_STATUS_REGISTRY.md | design_defined |
| 科技树/分叉 | COMPLETE | TECH_TREE_CATALOG.md | 通用科技首批data_defined |
| 矿产/采掘/精炼 | COMPLETE | MINERAL_RESOURCE_SYSTEM.md | data_defined + Schema |
| 征服矿产—科技绑定 | COMPLETE | CONQUEST_MINERAL_TECH_BINDING.md + TECH_TREE_CATALOG.md | data_defined + CI门禁 |
| 五模式资源配置 | COMPLETE | MODE_RESOURCE_PROFILES.md | design_defined，规则集首批data_defined |
| 经济/维修/补给 | COMPLETE | ECONOMY_FORMULAS.md | design_defined |
| 指挥/算力/电力 | COMPLETE | ECONOMY_FORMULAS.md | Simulation/Robotics代码契约已建 |
| 五种游戏模式 | COMPLETE | GAME_MODE_RULES.md + MODE_PLAYABILITY_PROFILES.md | ruleset首批data_defined |
| 动态控制/前线 | COMPLETE | MAP_LAYOUT_RUNTIME_SCHEMA.md + GAME_MODE_RULES.md | design_defined |
| AI指挥状态机 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | AI权限代码契约已建 |
| 敌方AI规则 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | code_contract_ready |
| 机器人自主 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | Robotics代码契约已建 |
| 电子战/网络战 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md + WEAPONS_ARMOR_MATRIX.md | IntelEW代码契约已建 |
| 空军/海军/轨道 | COMPLETE | AIR_NAVAL_ORBITAL_RULES.md | design_defined |
| 超级武器 | COMPLETE | SUPERWEAPONS_STRATEGIC_ABILITIES.md | design_defined |
| 地图运行时数据结构 | COMPLETE | MAP_LAYOUT_RUNTIME_SCHEMA.md | design_defined，首张侧车未产出 |
| 16张地图设计意图 | COMPLETE | MAP_SPECS.md | layout_defined级之前 |
| 24关任务机制 | COMPLETE | CAMPAIGN_MISSION_SPECS.md + CAMPAIGN_NARRATIVE_CAST.md | design_defined |
| 人物/对白/过场规则 | COMPLETE | CAMPAIGN_NARRATIVE_CAST.md | design_defined |
| UI/输入 | COMPLETE | UI_ART_AUDIO_ACCESSIBILITY.md | design_defined |
| 美术预算 | COMPLETE | UI_ART_AUDIO_ACCESSIBILITY.md | design_defined |
| 音频架构/语音规则 | COMPLETE | AUDIO_VOICE_PIPELINE.md + UI_ART_AUDIO_ACCESSIBILITY.md | design_defined |
| 本地化 | COMPLETE | DIFFICULTY_TUTORIAL_LOCALIZATION.md + AUDIO_VOICE_PIPELINE.md | design_defined |
| 难度/教学 | COMPLETE | DIFFICULTY_TUTORIAL_LOCALIZATION.md | design_defined |
| 存档/录像 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md | design_defined |
| 网络/重连 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md + development/03_NETWORK_DETERMINISM.md | Network代码契约已建 |
| 模组/编辑器 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md | design_defined |
| 技术栈 | COMPLETE | technical/TECH_STACK.md | Unity工程骨架已建立 |
| 性能/硬件 | COMPLETE | development/04_PERFORMANCE_NAVIGATION.md + technical/HARDWARE_PERFORMANCE_TARGETS.md | 待真实Unity/目标机验证 |
| 后台/匹配/排位/赛季 | COMPLETE | technical/ONLINE_SERVICES_MATCHMAKING_LIVEOPS.md | design_defined |
| 安全/隐私/反作弊 | COMPLETE | technical/ONLINE_SECURITY_PRIVACY.md | design_defined |
| 自动测试/平衡 | COMPLETE | TEST_MATRIX.md | 首批EditMode测试+内容CI已建立 |
| 系统复杂度预算 | COMPLETE | SYSTEM_COMPLEXITY_BUDGET.md | 设计变更强制门禁 |
| 验收 | COMPLETE | ACCEPTANCE_CRITERIA.md | 已作为生命周期门禁 |

## 2. 实际实现/生产状态

| 领域 | 当前状态 | 已实际完成 | 下一完成条件 |
|---|---|---|---|
| Unity工程骨架/包结构 | PARTIAL | `ProjectVersion.txt`、`manifest.json`、核心嵌入包已入库 | 真实Unity 6000.3.24f1解析/编译/测试通过 |
| 30Hz模拟主干 | PARTIAL | `SimulationRate`、固定步进配置、Clock组件/系统 | 无画面10000 tick状态哈希Runner |
| 战斗代码边界 | PARTIAL | 装甲、武器、伤害事件契约 | 首个可复现武器/伤害系统 |
| 导航代码边界 | PARTIAL | 导航层、路径走廊、目标、避障状态 | MAP_GRAY_RANGE共享路径可跑 |
| AI代码边界 | PARTIAL | 权限、禁令、更新节拍、玩家覆盖代次 | 战斗群任务完整执行/接管测试 |
| 情报/EW代码边界 | PARTIAL | 六级情报、四级EW、签名状态 | 感知/EW传播和服务器可见性过滤 |
| 机器人代码边界 | PARTIAL | A1—A4、链路、算力分配契约 | 断联/过载降级状态机 |
| 网络代码边界 | PARTIAL | 命令/快照头和可见性契约 | 本机服务器/客户端回环同步 |
| 权威玩法数据 | PARTIAL | 五规则集、矿产、首批通用科技、Schema | 单位/建筑/武器/能力等全部数据化 |
| 内容验证器/CI | PARTIAL | Python验证器+GitHub Actions门禁 | Schema全面验证、Unity编译/测试CI |
| 16张地图侧车 `.map.json` | PARTIAL | 数据契约已定义 | 16/16存在；先完成MAP_GRAY_RANGE graybox_ready |
| 地图正式美术 | PARTIAL | 预算/规范已定义 | 正式资产通过性能与可读性 |
| 24关可通关灰盒 | PARTIAL | 任务规格已定义 | 24/24从开场到胜败可跑通；先闭环MIS_01 |
| 最终对白/配音/过场 | PARTIAL | 生产规则已定义 | 正式脚本、字幕、语音、过场完成 |
| 正式音效库 | PARTIAL | 音频架构/ID规则已定义 | FMOD工程、注册表、首批实际资产 |
| 正式音乐 | PARTIAL | 动态音乐规则已定义 | 动态层与阵营主题完成 |
| 在线后台 | PARTIAL | 服务拓扑规格已定义 | 账号/房间/匹配/结果/排位/遥测可运行 |
| 安全/反作弊生产实现 | PARTIAL | 威胁与规则已定义 | 安全门禁、加固、证据链和演练通过 |
| 最低目标机性能 | PARTIAL | 指标和算法路线已冻结 | 真实目标硬件实测通过 |
| 本地化内容 | PARTIAL | 结构/规则已定义 | 首发语言文本/字幕/语音实际齐备 |
| 法务/IP/第三方资产清单 | PARTIAL | 原则已定义 | 授权、来源、许可证和替换清单完成 |

当前项目准确状态：**`implementation_bootstrap`**。设计层无方向性 `BLOCKED`，但尚未达到 `playable`。

## 3. 允许AI自主处理

- 依据既有公式生成数据文件和Schema，并通过验证器。
- 依据稳定ID生成代码骨架、占位资产引用和自动测试。
- 在不改变阵营哲学的前提下依据自动对战提出平衡调参。
- 为16图生成 `.map.json` 初稿、ControlRegion、灰盒、导航区、资源点和公平/自由度测试，再由门禁决定是否合格。
- 依据任务书和叙事默认契约生成任务状态机、触发器、占位对白键。
- 依据音频规格生成候选音效/占位语音，但必须保留来源/许可元数据并进入人工质量门禁。

## 4. 必须升级为设计变更

以下不能由编程代理自行决定：新增/删除国家；改变四大体系定位；改变五级科技结构；把轨道改成完整太空RTS；取消AI权限红线；让机器人淘汰人类单位；改动核心资源池；引入无反制超级武器；在标准模式加入时间驱动资源/伤害/积分/科技机制；恢复固定前线节点链；更换冻结技术栈核心；提高最低硬件来掩盖性能问题；把征服矿产科技硬锁扩散到其他标准模式；绕过系统复杂度预算。

## 5. COMPLETE与实现完成的含义

“规格 COMPLETE”只表示规则已经足够编码，不代表代码、资产和线上服务完成。

核心设计只有进入至少 `data_defined + code_contract_ready + automated_gate` 才可以称为“已进入实现主线”。只有实现/数据/资产/测试/性能/网络/安全/文档/可玩性全部达到 `ACCEPTANCE_CRITERIA.md`，对象才可以进入 `ship_ready`。
