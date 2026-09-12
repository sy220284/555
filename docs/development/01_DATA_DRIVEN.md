# 数据驱动与内容定义

## 1. 核心原则

单位、编队、武器、建筑、国家、科技、普通能力、战略能力、状态、规则集、地图规则、AI参数、音频引用必须配置化。程序实现通用能力，不为单一国家写长期分支。

权威设计源采用稳定ID + JSON + JSON Schema；构建时验证并烘焙为运行时紧凑数据。

## 2. 核心对象

至少包括：

- `FactionDefinition`
- `CountryDefinition`
- `UnitDefinition`
- `FormationDefinition`
- `BuildingDefinition`
- `WeaponDefinition`
- `TechDefinition`
- `AbilityDefinition`
- `StrategicAbilityDefinition`
- `StatusDefinition`
- `RulesetDefinition`
- `MapDefinition`
- `AIProfileDefinition`
- `AudioProfileDefinition`

稳定ID前缀只允许 `IDS_NAMING.md` 当前规则。

## 3. 单位字段

最低字段：

`id, display_name_key, faction, country, tech_tier, production_building, prerequisites, cost, build_time, command_cost, compute_cost, power_dependency, health, armor, speed, sensors, comms, autonomy, primary_weapon, secondary_weapons, abilities, veterancy_profile, supply_profile, ai_role, counter_tags, asset_refs, audio_profile, test_profile`

单位武器/能力装配以 `UNIT_LOADOUTS.md` 为执行真相源。

## 4. 编队模板

`FORM_` 不是单位实体。

最低字段：

`id, members, queue_policy, total_cost_policy, prerequisites, country, ai_usage, test_profile`

运行时展开为多个真实 `UNIT_`；禁止给Formation创建独立HP、碰撞、武器或网络实体。

## 5. 武器字段

`id, damage, damage_type, range, min_range, cycle, projectile_model, projectile_speed, accuracy, penetration, target_tags, intel_requirement, ammo_profile, state_effects, friendly_fire_policy, vfx_event, audio_event, test_profile`

所有 `WPN_` 必须在正式武器注册表存在，禁止运行时匿名武器。

## 6. 能力与状态

普通能力使用 `ABL_`，战略能力使用 `STRAT_`，状态使用 `STATUS_`。

能力必须定义：目标规则、范围、持续、冷却、资源/电力/算力成本、前置、状态效果、AI使用规则、网络权威、音频/VFX、测试配置。

状态必须定义：叠加规则、持续、刷新/覆盖规则、最大/最小效果、清除条件和存档/录像字段。

## 7. 规则集

`RulesetDefinition` 至少包含：

`id, mode_id, victory_rule, score_rule, surrender_rule, friendly_fire_rule, reconnect_rule, ai_permission, tech_modifier, economy_modifier, map_requirements, version`

地图脚本不得复制另一套胜负算法。

## 8. 地图数据

地图运行时数据来自 `/Data/Maps/<MAP_ID>.map.json`，字段和灰盒生成契约以 `MAP_LAYOUT_RUNTIME_SCHEMA.md` 为准。

地形材质、模型名称和场景层级不得成为资源、通行、占领、战略设施等规则的唯一真相源。

## 9. 音频数据

单位/武器/能力只保存稳定音频Profile/Event ID，不保存裸FMOD事件字符串作为唯一引用。

权威音频注册：

- `/Data/Audio/audio_registry.json`
- `/Data/Audio/voice_registry.json`
- `/Data/Audio/music_registry.json`
- `/Data/Audio/source_provenance.json`

生成式/第三方音频必须带来源和许可元数据。

## 10. 修正器

国家特色、科技、老练度、AI/网络状态、天气和模式通过可组合修正器处理。

修正顺序必须稳定、可测试、可解释。禁止在不同代码路径使用不同隐含计算顺序。

推荐顺序：

```text
Base
→ Country/Doctrine
→ Tech
→ Veterancy
→ Mode/Ruleset
→ Dynamic State(EW/Supply/Power/etc.)
→ Clamp
```

## 11. 版本与迁移

所有数据包带 `schema_version`；字段废弃必须提供迁移策略。

存档/录像记录：数据版本、内容哈希、规则集版本、协议版本。

稳定ID删除必须保留墓碑/迁移映射，禁止旧存档静默指向其他对象。

## 12. 数据验证

持续集成至少检查：

- ID唯一；
- 非法旧前缀；
- 引用存在；
- 所有可生产单位存在生产建筑、武器、能力、音频Profile；
- `FORM_` 成员全部存在且不递归形成环；
- 成本非负；
- 科技无循环；
- 生产链可达；
- 单位存在反制标签；
- 资源/容量字段完整；
- 能力网络权威定义完整；
- 规则集引用合法；
- 地图侧车Schema通过；
- 音频事件和来源元数据存在；
- 模式关闭某系统后仍有合法胜利路径。

## 13. 模组

官方数据和模组共用定义格式；排位只加载官方签名/哈希集合，自定义房可加载模组但所有参与者内容哈希必须一致。

模组新增ID使用自己的命名空间映射，不得无声明覆盖官方稳定ID。
