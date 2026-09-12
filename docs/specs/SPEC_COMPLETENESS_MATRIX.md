# 规格完整性矩阵

本文件区分两件事：

1. **规格是否足够编码**：实现者是否还需要自行发明规则。
2. **生产内容是否已经实际产出**：地图坐标、美术、配音、音效、过场、线上服务等是否已完成实现/资产。

状态只允许：`COMPLETE`、`PARTIAL`、`BLOCKED`。

## 1. 编码规格完整性

| 领域 | 规格状态 | 主文档 | 编码前仍需人工决定？ |
|---|---|---|---|
| 世界观/时间线 | COMPLETE | design/01_WORLD_LORE.md | 否 |
| 四大体系/15国 | COMPLETE | design/02_FACTIONS_COUNTRIES.md + CONTENT_REGISTRY.md | 否 |
| 高自由度战争原则 | COMPLETE | design/03A_FREEDOM_WARFARE.md + GAME_MODE_RULES.md | 否 |
| 稳定ID命名 | COMPLETE | IDS_NAMING.md | 否 |
| 全局注册入口 | COMPLETE | CONTENT_REGISTRY.md | 否 |
| 单位数值 | COMPLETE | UNIT_CATALOG.md | 否，允许平衡调参 |
| 单位武器/能力装配 | COMPLETE | UNIT_LOADOUTS.md | 否 |
| 建筑 | COMPLETE | BUILDING_CATALOG.md | 否 |
| 武器/装甲/命中 | COMPLETE | WEAPONS_ARMOR_MATRIX.md | 否 |
| 普通能力/状态 | COMPLETE | ABILITY_STATUS_REGISTRY.md | 否 |
| 科技树/分叉 | COMPLETE | TECH_TREE_CATALOG.md | 否 |
| 经济/维修/补给 | COMPLETE | ECONOMY_FORMULAS.md | 否 |
| 指挥/算力/电力 | COMPLETE | ECONOMY_FORMULAS.md | 否 |
| 五种游戏模式 | COMPLETE | GAME_MODE_RULES.md | 否 |
| 动态控制/前线 | COMPLETE | MAP_LAYOUT_RUNTIME_SCHEMA.md + GAME_MODE_RULES.md | 否 |
| AI指挥状态机 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | 否 |
| 敌方AI规则 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | 否 |
| 机器人自主 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | 否 |
| 电子战/网络战 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md + WEAPONS_ARMOR_MATRIX.md | 否 |
| 空军/海军/轨道 | COMPLETE | AIR_NAVAL_ORBITAL_RULES.md | 否 |
| 超级武器 | COMPLETE | SUPERWEAPONS_STRATEGIC_ABILITIES.md | 否 |
| 地图运行时数据结构 | COMPLETE | MAP_LAYOUT_RUNTIME_SCHEMA.md | 否 |
| 16张地图设计意图 | COMPLETE | MAP_SPECS.md | 否 |
| 24关任务机制 | COMPLETE | CAMPAIGN_MISSION_SPECS.md + CAMPAIGN_NARRATIVE_CAST.md | 否，正式内容可覆盖默认模板 |
| 人物/对白/过场规则 | COMPLETE | CAMPAIGN_NARRATIVE_CAST.md | 否 |
| UI/输入 | COMPLETE | UI_ART_AUDIO_ACCESSIBILITY.md | 否 |
| 美术预算 | COMPLETE | UI_ART_AUDIO_ACCESSIBILITY.md | 否 |
| 音频架构/语音规则 | COMPLETE | AUDIO_VOICE_PIPELINE.md + UI_ART_AUDIO_ACCESSIBILITY.md | 否 |
| 本地化 | COMPLETE | DIFFICULTY_TUTORIAL_LOCALIZATION.md + AUDIO_VOICE_PIPELINE.md | 否 |
| 难度/教学 | COMPLETE | DIFFICULTY_TUTORIAL_LOCALIZATION.md | 否 |
| 存档/录像 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md | 否 |
| 网络/重连 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md + development/03_NETWORK_DETERMINISM.md | 否 |
| 模组/编辑器 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md | 否 |
| 技术栈 | COMPLETE | technical/TECH_STACK.md | 否 |
| 性能/硬件 | COMPLETE | development/04_PERFORMANCE_NAVIGATION.md + technical/HARDWARE_PERFORMANCE_TARGETS.md | 否 |
| 后台/匹配/排位/赛季 | COMPLETE | technical/ONLINE_SERVICES_MATCHMAKING_LIVEOPS.md | 否 |
| 安全/隐私/反作弊 | COMPLETE | technical/ONLINE_SECURITY_PRIVACY.md | 否 |
| 自动测试/平衡 | COMPLETE | TEST_MATRIX.md | 否 |
| 验收 | COMPLETE | ACCEPTANCE_CRITERIA.md | 否 |

## 2. 实际生产/实现状态

这部分不能因为“文档有规则”就写成完成。

| 领域 | 生产状态 | 完成条件 |
|---|---|---|
| Unity工程骨架/包结构 | PARTIAL | 实际仓库工程可编译、启动、跑基础局 |
| 权威模拟核心 | PARTIAL | 30Hz模拟、回放哈希和测试通过 |
| 单位/建筑运行数据 | PARTIAL | 所有注册ID生成正式数据并通过引用检查 |
| 16张地图侧车 `.map.json` | PARTIAL | 16/16均存在、ControlRegion完整并达到graybox_ready |
| 地图正式美术 | PARTIAL | 排位/战役地图正式资产通过性能与可读性 |
| 24关可通关灰盒 | PARTIAL | 24/24从开场到胜败可跑通 |
| 最终对白/配音/过场 | PARTIAL | 正式脚本、字幕、语音、过场资产完成 |
| 正式音效库 | PARTIAL | 音频注册表、FMOD工程和资产来源齐备 |
| 正式音乐 | PARTIAL | 动态音乐层与阵营主题完成 |
| 在线后台 | PARTIAL | 账号/房间/匹配/结果/排位/遥测服务可运行 |
| 安全/反作弊生产实现 | PARTIAL | 安全门禁、服务加固、证据链和演练通过 |
| 自动测试基础设施 | PARTIAL | CI/夜间农场/性能回归实际运行 |
| 最低目标机性能 | PARTIAL | 目标硬件真实验收通过 |
| 本地化内容 | PARTIAL | 首发语言文本/字幕/语音实际齐备 |
| 法务/IP/第三方资产清单 | PARTIAL | 授权、来源、许可证和替换清单完成 |

当前没有 `BLOCKED` 的设计级问题；但生产状态不得冒充 `COMPLETE`。

## 3. 允许AI自主处理

- 依据既有公式生成数据文件和Schema。
- 依据稳定ID生成代码骨架、占位资产引用和测试。
- 在不改变阵营哲学的前提下依据自动对战提出平衡调参。
- 为16图生成 `.map.json` 初稿、ControlRegion、灰盒、导航区、资源点和公平/自由度测试，再由门禁决定是否合格。
- 依据任务书和叙事默认契约生成任务状态机、触发器、占位对白键。
- 依据音频规格生成候选音效/占位语音，但必须保留来源/许可元数据并进入人工质量门禁。

## 4. 必须升级为设计变更

以下不能由编程代理自行决定：新增/删除国家；改变四大体系定位；改变五级科技结构；把轨道改成完整太空RTS；取消AI权限红线；让机器人淘汰人类单位；改动核心资源种类；引入无反制超级武器；在标准模式加入时间驱动资源/伤害/积分/科技机制；恢复固定前线节点链；更换冻结技术栈核心；提高最低硬件来掩盖性能问题。

## 5. COMPLETE的含义

“规格 COMPLETE”只表示规则已经足够编码，不代表代码、资产和线上服务已经完成。

只有实现/数据/资产/测试/性能/网络/安全/文档/可玩性全部达到 `ACCEPTANCE_CRITERIA.md`，对象才可以进入 `ship_ready`。