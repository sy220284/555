# 武器、装甲与克制矩阵

## 1. 伤害类型

- `KINETIC` 动能：机炮、轻武器，对人员/轻装甲稳定。
- `PENETRATOR` 穿甲：坦克炮、反坦克导弹，对重装甲高效。
- `EXPLOSIVE` 爆炸：火炮、火箭、炸弹，对集群/建筑高效。
- `DIRECTED_ENERGY` 定向能：激光/高能束，对无人、传感器、轻装甲高效。
- `ELECTROMAGNETIC` 电磁：高功率微波、磁暴，对电子系统/低防护机器人高效。
- `SYSTEM_NEURAL` 系统/神经：网络/认知类，只对带可攻击系统标签的目标生效。

## 2. 装甲类别基础修正

数值为伤害乘数。

| 伤害\装甲 | INF | LIGHT | MEDIUM | HEAVY | AIR | NAVAL | STRUCT | ORBIT |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| KINETIC | 1.25 | 1.00 | 0.75 | 0.45 | 0.80 | 0.50 | 0.40 | 0.10 |
| PENETRATOR | 0.70 | 1.10 | 1.25 | 1.35 | 0.90 | 1.05 | 0.80 | 0.20 |
| EXPLOSIVE | 1.20 | 1.15 | 1.00 | 0.85 | 0.90 | 1.00 | 1.25 | 0.15 |
| DIRECTED_ENERGY | 1.00 | 1.20 | 1.00 | 0.75 | 1.30 | 0.85 | 0.75 | 0.40 |
| ELECTROMAGNETIC | 0.20 | 1.15 | 1.15 | 0.95 | 1.20 | 1.00 | 0.65 | 0.75 |
| SYSTEM_NEURAL | 0.80* | 0.90* | 0.90* | 0.75* | 0.90* | 0.80* | 0.70* | 0.85* |

`*` 仅对拥有人类神经、联网自动化或可渗透控制系统标签的目标生效，否则为0。

## 3. 代表武器稳定ID

| ID | 类型 | 单发伤害 | 射程m | 周期s | 最低情报 | 主要目标 |
|---|---|---:|---:|---:|---|---|
| WPN_RIFLE_STD | KINETIC | 18 | 220 | 0.18 | Suspected | INF/LIGHT |
| WPN_HMG_RWS | KINETIC | 32 | 420 | 0.12 | Suspected | INF/LIGHT/DRONE |
| WPN_ATGM_INF | PENETRATOR | 420 | 750 | 6.0 | Confirmed | MEDIUM/HEAVY |
| WPN_TANK_120 | PENETRATOR | 520 | 950 | 4.2 | Confirmed | HEAVY |
| WPN_AUTOCANNON_30 | KINETIC | 48 | 650 | 0.35 | Confirmed | INF/LIGHT/AIRLOW |
| WPN_HOWITZER_155 | EXPLOSIVE | 620 | 3200 | 8.0 | Suspected区域/Confirmed精确 | AREA/STRUCT |
| WPN_MLRS_STD | EXPLOSIVE | 260x6 | 4200 | 24.0 | Suspected | AREA |
| WPN_LOITER_AT | PENETRATOR | 360 | 2500任务半径 | 一次性 | Confirmed | VEHICLE |
| WPN_SHORAD_MSL | EXPLOSIVE | 220 | 1000 | 2.5 | Confirmed | AIR/DRONE |
| WPN_MRAD_MSL | EXPLOSIVE | 420 | 1800 | 4.5 | Confirmed | AIR |
| WPN_LRAD_MSL | EXPLOSIVE | 650 | 3000 | 7.0 | Locked | AIR/HIGHVALUE |
| WPN_ABM | EXPLOSIVE | 800 | 3500 | 10.0 | Locked | MISSILE |
| WPN_AAM_BVR | EXPLOSIVE | 520 | 2200等效 | 5.0 | Locked | AIR |
| WPN_ASM_HEAVY | PENETRATOR | 1300 | 4500 | 18.0 | Locked | NAVAL |
| WPN_CRUISE_LAND | EXPLOSIVE | 1150 | 6000 | 30.0 | Confirmed | STRUCT/HIGHVALUE |
| WPN_LASER_CUAS | DIRECTED_ENERGY | 55/tick | 700 | 0.25 | Confirmed | DRONE/LOITER |
| WPN_PRISM_BEAM | DIRECTED_ENERGY | 180/tick | 1100 | 0.5 | Confirmed | LIGHT/MEDIUM/STRUCT |
| WPN_HPM | ELECTROMAGNETIC | 180系统伤害 | 850 | 8.0 | Confirmed | ROBOT/DRONE/ELECTRONIC |
| WPN_TESLA_ARC | ELECTROMAGNETIC | 420 | 900 | 2.5 | Confirmed | VEHICLE/ROBOT |
| WPN_NEURAL_DISRUPT | SYSTEM_NEURAL | 100压力 | 650 | 5.0 | Confirmed | INF/AI |
| WPN_JP_RAILGUN_NAVAL | PENETRATOR | 980 | 2600 | 8.5 | Locked | NAVAL/MISSILE |
| WPN_YURI_BIOMECH_CLAW | KINETIC | 95 | 95 | 0.75 | Suspected | INF/LIGHT |
| WPN_YURI_GATLING_ROTARY | KINETIC | 26 | 720 | 0.10 | Confirmed | INF/LIGHT/AIRLOW |

`WPN_JP_RAILGUN_NAVAL`：高能耗远程直射，对水面目标和部分导弹有效，无爆炸范围伤害。

`WPN_YURI_BIOMECH_CLAW`：极短程生物机械近战/冲击武器，对重装甲低效，依赖接近目标。

`WPN_YURI_GATLING_ROTARY`：持续射击武器；连续锁定同一目标2秒后射速提高15%，5秒后提高至30%上限；停火2秒重置，不提高单发伤害。

## 4. 状态伤害

武器可额外产生已注册状态：

- `STATUS_SUPPRESSED`：人员命中/移动效率下降，最多30%。
- `STATUS_SENSOR_DEGRADED`：探测半径下降，最低保留40%。
- `STATUS_COMMS_DEGRADED`：目标共享延迟增加。
- `STATUS_MOBILITY_KILL`：车辆速度临时降低，不等同直接死亡。
- `STATUS_SYSTEM_STRESS`：机器人/AI累计压力；达到阈值后自主等级临时下降1级。
- `STATUS_NEURAL_STRESS`：人员/尤里目标累计压力，触发混乱但不永久夺取关键单位。

状态ID以 `ABILITY_STATUS_REGISTRY.md` 为准。

## 5. 命中模型

基础命中：`P = BaseAccuracy * SensorQuality * TargetTrack * MovementFactor * EWFactor * WeatherFactor`。

所有因子夹在0.35—1.15之间；最终命中率夹在5%—98%。

`TargetTrack`：Suspected=0.55，Confirmed=0.85，Locked=1.0。

移动射击：轻型稳定平台0.9，中型0.8，重型主炮0.65；停车后恢复1.0。

## 6. 主动防护

带APS单位拥有拦截充能。标准MBT：2次；高级MBT：3次；天启Ⅲ：4次。每25秒恢复1次，补给不足时恢复时间翻倍。

APS基础拦截率65%，高级80%，电子压制时最低40%；对顶攻/高速弹药不保证100%。

## 7. 防空弹药抽象

防空单位拥有 `ReadyShots`：SHORAD 8、MRAD 6、LRAD 4、ABM 3。补给充分时分别每4/6/10/16秒补1发；不足时速度减半；断供只保留现有备弹。

定向能防空不消耗弹药，但消耗电力/热容量。连续射击超过8秒进入过热，需6秒冷却。

## 8. 反无人/机器人克制

电子战不会直接造成HP伤害，只影响 Sensor/Comms/Navigation/SystemStress。高功率微波和磁暴才产生系统伤害。

A1受干扰最大，A2降低25%，A3降低50%，A4降低65%；光纤控制对无线链路干扰免疫，但仍受传感器、物理摧毁和高功率电磁攻击影响。

## 9. 建筑与战略武器

战略火力对基地核心建筑默认伤害乘数0.8；对数据、雷达、发射场等高价值节点为1.0。核武属于区域战略能力，伤害采用距离衰减，必须存在预警和拦截窗口。

## 10. 友军规则

标准排位：

- 直接锁定友军：禁止；
- 普通范围爆炸对友军25%；
- 超级武器对友军50%；
- 自伤遵循同样范围规则；
- 自定义房覆盖必须进入 `ruleset_id` 和录像内容哈希。

## 11. 平衡红线

1. 常规单位不得同时拥有顶级对地、对空、侦察、电子战和高机动。
2. 超远程武器必须至少受情报、冷却、弹药、暴露/机动中的三项约束。
3. 一次性武器平均成本交换目标不得长期超过2.0且无反制。
4. T5不能使T1—T3全部失去用途。
5. 所有 `WPN_` 必须正式注册，禁止匿名运行时武器。
