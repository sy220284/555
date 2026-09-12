# 全建筑规格目录

## 1. 统一规则

字段：`CostIndustrial/Strategic`、`BuildTime`、`HP`、`Armor`、`Footprint`、`PowerDelta`、`ComputeDelta`、`CommandDelta`、`Tier`、`Prerequisites`。

建筑被摧毁后允许留下废墟；废墟默认阻挡大型车辆，工程单位可清理。核心节点损失只削弱对应体系，不允许单点全军停机。

## 2. 核心建筑

| ID | Tier | 工业/战略 | 时间s | HP | Footprint | Power | Compute | Command | 功能 |
|---|---:|---:|---:|---:|---|---:|---:|---:|---|
| BLD_COMMON_COMMAND | T1 | 5000/200 | 45 | 5000 | 8x8 | -20 | +20 | +80 | 建设核心、基础数据中继 |
| BLD_COMMON_PERSONNEL | T1 | 1800/0 | 22 | 1800 | 5x4 | -8 | 0 | 0 | 步兵生产 |
| BLD_COMMON_VEHICLE_FACTORY | T1 | 3200/100 | 35 | 3000 | 8x6 | -20 | 0 | 0 | 地面车辆生产 |
| BLD_COMMON_DRONE_CENTER | T1 | 2200/80 | 26 | 1900 | 6x5 | -12 | +10 | 0 | 无人机生产/维护 |
| BLD_COMMON_ROBOT_FACTORY | T3 | 3600/350 | 38 | 2800 | 7x6 | -25 | 0 | 0 | 机器人生产 |
| BLD_COMMON_HEAVY_ROBOT_FACTORY | T4 | 6500/900 | 60 | 4200 | 10x8 | -45 | 0 | 0 | 重型机器人/母平台 |
| BLD_COMMON_AIRBASE | T2 | 5200/350 | 50 | 3800 | 12x10 | -30 | 0 | 0 | 8个标准机位 |
| BLD_COMMON_NAVAL_BASE | T2 | 6000/400 | 55 | 4200 | 14x8 | -35 | 0 | 0 | 舰艇/潜艇生产维护 |
| BLD_COMMON_POWER | T1 | 1600/0 | 20 | 1500 | 4x4 | +120 | 0 | 0 | 基础电力 |
| BLD_COMMON_ADV_POWER | T3 | 4800/500 | 45 | 2600 | 6x6 | +360 | 0 | 0 | 高级能源 |
| BLD_COMMON_RADAR | T1 | 1800/50 | 22 | 1200 | 4x4 | -10 | 0 | 0 | 局部雷达/空情 |
| BLD_COMMON_DATACENTER | T2 | 2600/180 | 30 | 1800 | 5x5 | -18 | +40 | 0 | 情报融合/数据处理 |
| BLD_COMMON_AI_COMMAND | T3 | 4200/500 | 45 | 2400 | 6x6 | -35 | +100 | +30 | AI战斗指挥、算力 |
| BLD_COMMON_COMPUTE | T4 | 6500/900 | 60 | 2800 | 7x7 | -70 | +250 | 0 | 战略计算 |
| BLD_COMMON_SATCOM | T2 | 2800/220 | 30 | 1500 | 5x5 | -15 | +10 | 0 | 远程通信/轨道链路 |
| BLD_COMMON_LOGISTICS | T1 | 2200/0 | 28 | 2400 | 7x6 | -10 | 0 | 0 | 维修/补给/弹药 |
| BLD_COMMON_FORWARD_LOGISTICS | T1 | 1000/0 | 16 | 900 | 4x4 | -4 | 0 | 0 | 前线补给半径900m |
| BLD_COMMON_SPACEOPS | T4 | 7000/1100 | 70 | 3200 | 8x8 | -55 | +60 | 0 | 解锁轨道战略层 |
| BLD_COMMON_LAUNCHSITE | T4 | 8500/1400 | 80 | 3600 | 10x8 | -45 | 0 | 0 | 发射/补充卫星 |
| BLD_COMMON_ROBOT_REPAIR | T3 | 2200/120 | 26 | 1800 | 5x5 | -12 | 0 | 0 | 机器人维修/软件恢复 |

## 3. 防御建筑

| ID | Tier | 工业/战略 | HP | Power | 射程m | 主要克制 |
|---|---:|---:|---:|---:|---:|---|
| BLD_DEF_RWS | T1 | 700/0 | 650 | -4 | 350 | 步兵/轻机器人 |
| BLD_DEF_AT | T1 | 1200/20 | 850 | -5 | 650 | 中重装甲 |
| BLD_DEF_CUAS | T1 | 950/20 | 700 | -8 | 550 | 小无人机/巡飞弹 |
| BLD_DEF_SHORAD | T1 | 1400/50 | 800 | -10 | 900 | 低空/无人 |
| BLD_DEF_MRAD | T2 | 2400/120 | 950 | -14 | 1600 | 战斗机/直升机 |
| BLD_DEF_LRAD | T3 | 4200/350 | 1200 | -22 | 2600 | 高价值航空/远距拒止 |
| BLD_DEF_ABM | T4 | 5500/700 | 1300 | -35 | 3200 | 战术/战略导弹 |
| BLD_DEF_EW | T2 | 1800/120 | 750 | -18 | 1000 | 数据链/无人系统 |
| BLD_DEF_DECOY | T2 | 600/20 | 300 | -2 | - | 制造可侦破假目标 |
| BLD_DEF_BUNKER | T1 | 1300/0 | 2200 | 0 | - | 驻兵/掩体 |

## 4. 未来科技建筑

- `BLD_FUTURE_PRISM`：T5，6500/1200，HP1800，功耗80，射程1100；最多与3座邻近棱镜协同，每条链+20%伤害，上限60%。
- `BLD_FUTURE_TESLA`：T5，6200/1100，HP2200，功耗75，射程900；高电磁/系统伤害，对高防护重装效率下降。
- `BLD_FUTURE_IRONCURTAIN`：T5，15000/2800，HP3000，功耗120；区域主动防护，不提供绝对无敌。
- `BLD_YURI_NEURAL_TOWER`：T4，5200/900，HP1800，功耗55，Compute+40；施加认知/算法压力，可被隔离网络、人工接管和系统抗性反制。

## 5. 国家特色建筑

- `BLD_CN_SKYNET_DATACENTER`：T3，3300/300，HP2000，5x5，Power-22，Compute+80；联网传感器目标刷新+15%。
- `BLD_US_WARCLOUD_NODE`：T3，3600/380，HP1900，5x5，Power-25，Compute+90；授权战斗群反应时间-10%。
- `BLD_RU_WAR_INDUSTRY`：T2，4200/180，HP3600，9x7，Power-25；车辆/机器人同类连续生产每件+2%速度，上限20%，停产60秒清零。
- `BLD_KR_AUTO_ROBOT_FACTORY`：T3，3900/420，HP2700，7x6，Power-30；机器人连续生产速度上限+25%，运行功耗+20%。
- `BLD_IL_LAYERED_DEFENSE`：T3，4600/500，HP2400，7x7，Power-32；整合近/中程拦截调度，附近SHORAD/MRAD/激光平台目标分配效率+15%，自身不是万能高伤害塔。
- `BLD_TR_UAV_MOTHERSHIP`：T3，4100/420，HP2300，8x6，Power-28，Compute+40；作为地面无人航空母站，提供8个轻无人维护槽和局部狼群任务编组，不生产有人战机。
- `BLD_UA_DISTRIBUTED_DRONE_SHOP`：T2，1500/80，HP900，5x4，Power-10；低成本分散部署，无人机生产速度+20%。
- `BLD_AU_SEABED_SENSOR`：T3，2200/260，HP650，水下部署，Power-12；海下探测半径1400m，暴露后较脆弱。
- `BLD_PL_FIELDWORKS`：T2，1800/80，HP1800，6x5，Power-5；附近工事建造时间-35%。
- `BLD_IR_UNDERGROUND_MISSILE`：T3，4800/500，HP2600，8x6，Power-20；未发射时低可探测，发射后暴露；地下主体受常规爆炸减伤，但入口/通信可被压制。
- `BLD_YURI_NEURAL_CORE`：T4，7200/1200，HP3000，8x8，Power-80，Compute+300；失去后A4单位降A3，神经类能力冷却+30%。
- `BLD_YURI_SWARM_HIVE`：T3，3600/420，HP2200，7x6，Power-35，Compute+80；自动补充微型蜂群，受算力/能源/库存约束。

## 6. 电力规则

`PowerAvailable >= PowerDemand`：正常。

- 80%—99%：生产/维修速度-10%。
- 60%—79%：生产/维修-25%，远程雷达刷新-20%。
- <60%：按优先级切除低优先防御/计算任务。

关键防御不得因瞬间欠电永久失效，恢复电力后10秒内重启。

## 7. 建造与维修

工程协助：`1 + 0.35*sqrt(n)`，最高2.4倍。

建筑维修每秒恢复最大HP的1%，工业消耗为原价0.45%/秒；受伤后5秒才恢复满速维修。

## 8. 摧毁与降级

- Command摧毁：不能新增主基地级建筑，但已有单位继续作战。
- DataCenter摧毁：目标共享延迟+25%。
- AICommand摧毁：授权部队按自主等级降级。
- Compute摧毁：最大算力下降，可触发过载。
- SatCom摧毁：轨道/跨战区链路效率下降，本地链路保留。
- Logistics摧毁：前线补给恢复显著下降。
- LayeredDefense/UAVMothership等国家建筑被毁时只失去对应局部特色加成，不导致全军硬停机。

所有降级必须可观察、可恢复、可反制。

## 9. 验收

内容验证器必须确认所有 `BLD_` 注册ID均有规格、成本、前置、占地、功耗、生命和功能；特色建筑不存在“注册但无执行定义”。
