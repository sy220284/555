# 主动能力与状态稳定ID注册表

本文件登记 `UNIT_LOADOUTS.md`、AI、电子战和未来科技引用的非武器能力/状态。能力只描述规则入口，具体数值优先引用对应专项规格。

## 1. 通用主动能力

| ID | 用途 | 核心规则 |
|---|---|---|
| ABL_COMMON_GARRISON | 驻军 | 规则见 UNIT_LOADOUTS.md |
| ABL_COMMON_MARK_TARGET | 标记目标 | 提高目标情报/火控共享，不直接增伤 |
| ABL_COMMON_REPAIR | 维修 | 消耗工业资源，规则见 ECONOMY_FORMULAS.md |
| ABL_COMMON_CAPTURE | 占领 | 仅合法可占领对象 |
| ABL_COMMON_MINE_CLEAR | 排雷 | 工程单位局部排雷 |
| ABL_COMMON_LOCAL_JAM | 局部干扰 | 小半径EW影响 |
| ABL_COMMON_SABOTAGE | 破坏 | 对合法设施产生短时功能降级 |
| ABL_COMMON_RECON_PULSE | 侦察脉冲 | 短时提高指定区域侦察刷新 |
| ABL_COMMON_AREA_JAM | 区域干扰 | EW车辆/节点能力 |
| ABL_COMMON_RECOVER_WRECK | 残骸回收 | 按经济规则返还工业资源 |
| ABL_COMMON_BATTLEGROUP_RELAY | 战斗群中继 | 提高局部通信/AI任务稳定性 |
| ABL_COMMON_ONEWAY_ATTACK | 单程攻击 | 巡飞/自杀式任务 |
| ABL_COMMON_SUPPLY | 补给 | 提供局部SupplyCapacity |
| ABL_COMMON_AIR_TRANSPORT | 空运 | 装载、投送、返航 |
| ABL_COMMON_STEALTH_STRIKE | 隐身突击 | 任务级隐身打击姿态 |
| ABL_COMMON_AEW_SCAN | 空中预警扫描 | 增强空情与目标共享 |
| ABL_COMMON_AIR_JAM | 空中干扰 | 空基电子战 |
| ABL_COMMON_HEAVY_STRIKE | 重型打击 | 轰炸机任务能力 |
| ABL_COMMON_ASW | 反潜搜索 | 对水下目标执行搜索/攻击流程 |
| ABL_COMMON_CARRIER_AIRWING | 舰载机联队 | 管理舰载航空出动容量 |
| ABL_COMMON_SUBMERGE | 潜航 | 切换水下隐蔽状态 |
| ABL_COMMON_DECOY | 诱饵 | 生成可侦破假目标 |
| ABL_COMMON_AMPHIB_TRANSPORT | 两栖运输 | 海陆投送 |

## 2. 国家/特色能力

| ID | 对象 | 规则 |
|---|---|---|
| ABL_CN_NETWORKED_FIRST_SHOT | 龙骑主战平台 | 联网且目标Confirmed以上时首轮火控获得国家修正 |
| ABL_CN_DRONE_HIVE | 蜂巢无人车 | 放出/回收编组无人机，受算力与库存限制 |
| ABL_US_STEALTH_LINK | 幽灵战机 | 与预警/僚机数据链联动 |
| ABL_US_CCA_LINK | 猎鹰CCA | 与有人机共享目标/任务 |
| ABL_DE_MODULE_SWITCH | 德国模块车辆/机器人 | 仅在保障/维修点切换已解锁任务模块；切换期间不能攻击 |
| ABL_JP_MISSION_MODE_SWITCH | 日本岛屿机动平台 | 在反舰/区域防空支援模式之间切换，切换有固定时间和冷却 |
| ABL_TR_WOLFPACK_LINK | 土耳其无人航空 | 同一战斗群内自动分配侦察/诱饵/攻击角色，不改变玩家战略目标 |
| ABL_UA_FIBER_CONTROL | 光纤无人机 | 对无线控制链干扰免疫，仍受传感/物理攻击影响 |
| ABL_IR_MOBILE_LAUNCH | 伊朗机动导弹 | 发射后进入高暴露状态，必须重新部署后恢复低特征 |

## 3. 尤里能力

| ID | 对象 | 规则 |
|---|---|---|
| ABL_YURI_GRAVITY_PULSE | 重力平台 | 对轻型目标施加短时位移/机动扰乱；不能移动重型战略单位，强冷却 |
| ABL_YURI_NEURAL_FIELD | 神经控制平台 | 对人员/低自主机器累计神经/系统压力；不能永久夺取英雄/战略资产 |
| ABL_YURI_PHANTOM_DECOY | 幻影节点 | 生成带DeceptionStrength的可侦破假目标 |
| ABL_YURI_SWARM_DECEPTION | 幽灵蜂群 | 在侦察/欺骗姿态间切换，欺骗姿态降低直接火力能力 |
| ABL_YURI_LOCAL_HIVE_LINK | 尤里机器军团 | 共享局部目标/威胁信息，失去神经核心时按规则降级 |

## 4. 状态ID

- `STATUS_SUPPRESSED`
- `STATUS_SENSOR_DEGRADED`
- `STATUS_COMMS_DEGRADED`
- `STATUS_MOBILITY_KILL`
- `STATUS_SYSTEM_STRESS`
- `STATUS_NEURAL_STRESS`
- `STATUS_SUPPLY_LOW`
- `STATUS_SUPPLY_CUT`
- `STATUS_POWER_SHED`
- `STATUS_COMPUTE_OVERLOAD`
- `STATUS_JAM_LIGHT`
- `STATUS_JAM_HEAVY`
- `STATUS_EW_BLACKZONE`
- `STATUS_STEALTH`
- `STATUS_SUBMERGED`
- `STATUS_DECOY_UNVERIFIED`
- `STATUS_DECOY_REVEALED`
- `STATUS_HIGH_EMISSION`

## 5. 能力数据字段

每项能力最低包含：

`id, schema_version, target_rule, range, duration, cooldown, resource_cost, strategic_cost, power_cost, compute_cost, intel_requirement, prerequisites, state_effects, ai_usage, network_authority, audio_event, vfx_event, test_profile`

无消耗字段显式写0。

## 6. 权威规则

- 所有能力由服务器验证并执行权威结果；
- 表现层VFX/音效不能决定命中、占领、干扰或状态；
- AI只能使用其权限和预算允许的能力；
- 战略级能力使用 `STRAT_`，不能混入普通 `ABL_`；
- 模式切换/模块切换必须进入录像事件和状态哈希，禁止只存在于表现层。

## 7. 验收

CI拒绝：悬空能力ID、状态ID未注册、缺少网络权威定义、能力没有AI使用策略、能力无测试配置、客户端可直接提交结果、模式切换不进入录像/存档。
