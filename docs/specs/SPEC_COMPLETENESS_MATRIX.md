# 规格完整性矩阵

本文件用于判断“是否已经可以无歧义进入全量自动编程”。状态只允许：`COMPLETE`、`PARTIAL`、`BLOCKED`。

| 领域 | 状态 | 主文档 | 编码前仍需人工决定？ |
|---|---|---|---|
| 世界观/时间线 | COMPLETE | design/01_WORLD_LORE.md | 否 |
| 四大体系/15国 | COMPLETE | design/02_FACTIONS_COUNTRIES.md + CONTENT_REGISTRY.md | 否，平衡只按流程调整 |
| 稳定ID | COMPLETE | CONTENT_REGISTRY.md | 否 |
| 单位基线 | COMPLETE | UNIT_CATALOG.md | 否，允许平衡调参 |
| 建筑基线 | COMPLETE | BUILDING_CATALOG.md | 否 |
| 武器/装甲 | COMPLETE | WEAPONS_ARMOR_MATRIX.md | 否 |
| 科技树/分叉 | COMPLETE | TECH_TREE_CATALOG.md | 否 |
| 经济公式 | COMPLETE | ECONOMY_FORMULAS.md | 否 |
| 指挥/算力/电力 | COMPLETE | ECONOMY_FORMULAS.md | 否 |
| AI指挥状态机 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | 否 |
| 敌方AI规则 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | 否 |
| 机器人自主 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md | 否 |
| 电子战/网络战 | COMPLETE | AI_ROBOTICS_EW_STATE_MACHINES.md + WEAPONS_ARMOR_MATRIX.md | 否 |
| 空军 | COMPLETE | UNIT_CATALOG.md + AIR_NAVAL_ORBITAL_RULES.md | 否 |
| 海军 | COMPLETE | UNIT_CATALOG.md + AIR_NAVAL_ORBITAL_RULES.md | 否 |
| 轨道支援 | COMPLETE | AIR_NAVAL_ORBITAL_RULES.md | 否 |
| 超级武器 | COMPLETE | SUPERWEAPONS_STRATEGIC_ABILITIES.md | 否 |
| 16张地图 | COMPLETE | MAP_SPECS.md | 否，正式美术可迭代 |
| 24关任务 | COMPLETE | CAMPAIGN_MISSION_SPECS.md | 否，正式对白/过场可后补 |
| UI/输入 | COMPLETE | UI_ART_AUDIO_ACCESSIBILITY.md | 否 |
| 美术预算 | COMPLETE | UI_ART_AUDIO_ACCESSIBILITY.md | 否，具体概念图需生产 |
| 音频/本地化 | COMPLETE | UI_ART_AUDIO_ACCESSIBILITY.md + DIFFICULTY_TUTORIAL_LOCALIZATION.md | 否，具体台词需生产 |
| 难度/教学 | COMPLETE | DIFFICULTY_TUTORIAL_LOCALIZATION.md | 否 |
| 存档/录像 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md | 否 |
| 网络/重连 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md + development/03_NETWORK_DETERMINISM.md | 否 |
| 模组/编辑器 | COMPLETE | SAVE_NETWORK_MOD_EDITOR_SCHEMA.md | 否 |
| 自动测试 | COMPLETE | TEST_MATRIX.md | 否 |
| 性能预算 | COMPLETE | TEST_MATRIX.md + development/04_PERFORMANCE_NAVIGATION.md | 否 |
| 验收 | COMPLETE | ACCEPTANCE_CRITERIA.md | 否 |

## 允许AI自主处理的事项

- 依据既有公式生成具体数据文件与模式定义。
- 依据稳定ID生成代码骨架、占位资产引用、测试用例。
- 在不改变阵营哲学的前提下，依据自动对战结果提出平衡调参。
- 依据地图规格生成灰盒、导航区、资源点与测试脚本。
- 依据任务书生成任务状态机、触发器和占位对白键。

## 必须升级为设计变更的事项

以下不能由编程代理自行决定：新增/删除国家；改变四大体系定位；改变五级科技结构；把轨道改成完整太空RTS；取消AI权限红线；让机器人淘汰人类单位；改动核心资源种类；引入无反制超级武器；显著改变标准对局时长。

## 完整性的含义

`COMPLETE`表示实现者不需要再问“这个系统应该怎么运作”即可开始编码；不表示最终数值永不调整。平衡数值允许在既定模型、角色与边界内通过测试闭环迭代。