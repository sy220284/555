# 全单位规格目录

本文件给出首发可执行基线。数值是第一版实现基准，后续只能通过平衡流程修改，不得由程序员任意改动。

## 1. 统一字段

- `HP`：结构/生命值。
- `Armor`：INF/LIGHT/MEDIUM/HEAVY/AIR/NAVAL/STRUCT/ORBIT。
- `Speed`：地面 m/s、空中/海上采用游戏化等效速度。
- `Sensor`：基础探测半径（米）。
- `Command`：指挥容量。
- `Compute`：算力占用。
- `Supply`：每分钟补给负担基数。
- `Tier`：T1—T5。

## 2. 通用地面单位

| ID | Tier | 工业/战略 | 时间s | HP | Armor | Speed | Sensor | Cmd | Compute | Supply | 核心定位 |
|---|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---|
| UNIT_COMMON_RIFLE | T1 | 300/0 | 8 | 100 | INF | 4.8 | 260 | 1 | 0 | 1 | 基础占领/反轻型 |
| UNIT_COMMON_AT | T1 | 700/20 | 12 | 90 | INF | 4.5 | 300 | 1 | 0 | 1 | 反装甲 |
| UNIT_COMMON_AA | T1 | 650/20 | 12 | 90 | INF | 4.5 | 450 | 1 | 0 | 1 | 近程防空 |
| UNIT_COMMON_RECON | T1 | 500/20 | 10 | 80 | INF | 5.2 | 550 | 1 | 0 | 1 | 侦察/标记 |
| UNIT_COMMON_ENGINEER | T1 | 450/0 | 10 | 80 | INF | 4.6 | 220 | 1 | 0 | 1 | 修复/占领/排雷 |
| UNIT_COMMON_EW_INF | T2 | 800/50 | 14 | 80 | INF | 4.5 | 300 | 1 | 1 | 1 | 局部干扰 |
| UNIT_COMMON_SPECOPS | T2 | 1100/100 | 18 | 120 | INF | 5.4 | 450 | 2 | 0 | 1 | 渗透/标记/破坏 |
| UNIT_COMMON_SCOUT_VEH | T1 | 1000/0 | 16 | 420 | LIGHT | 18 | 600 | 2 | 0 | 2 | 高速侦察 |
| UNIT_COMMON_IFV | T1 | 1450/0 | 20 | 850 | MEDIUM | 13 | 450 | 3 | 0 | 3 | 运兵/伴随火力 |
| UNIT_COMMON_MBT | T1 | 2300/80 | 28 | 1800 | HEAVY | 11 | 450 | 4 | 0 | 5 | 正面装甲主力 |
| UNIT_COMMON_SPG | T2 | 2800/100 | 30 | 900 | MEDIUM | 9 | 500 | 4 | 0 | 5 | 远程炮击 |
| UNIT_COMMON_MLRS | T2 | 3100/140 | 32 | 800 | MEDIUM | 9 | 500 | 4 | 0 | 6 | 区域火力/反集结 |
| UNIT_COMMON_SHORAD | T1 | 1850/60 | 24 | 700 | MEDIUM | 11 | 700 | 4 | 0 | 4 | 反无人/低空 |
| UNIT_COMMON_MRAD | T2 | 2600/120 | 28 | 700 | MEDIUM | 9 | 1000 | 4 | 0 | 5 | 中程防空 |
| UNIT_COMMON_EW_VEH | T2 | 2200/150 | 28 | 650 | MEDIUM | 10 | 700 | 4 | 1 | 4 | 电子压制 |
| UNIT_COMMON_REPAIR | T1 | 1200/0 | 20 | 550 | MEDIUM | 9 | 260 | 2 | 0 | 3 | 维修/回收 |
| UNIT_COMMON_COMMAND | T2 | 1800/120 | 26 | 650 | MEDIUM | 9 | 500 | 3 | 2 | 3 | 战斗群中继 |

## 3. 无人/机器人

| ID | Tier | 工业/战略 | 时间s | HP | Armor | Speed | Sensor | Cmd | Compute | 说明 |
|---|---:|---:|---:|---:|---|---:|---:|---:|---:|---|
| UNIT_COMMON_DRONE_RECON | T1 | 400/20 | 8 | 80 | AIR | 35 | 850 | 1 | 1 | 低成本侦察，易受干扰 |
| UNIT_COMMON_DRONE_ATTACK | T2 | 950/80 | 14 | 140 | AIR | 30 | 650 | 2 | 2 | 轻攻击/标记 |
| UNIT_COMMON_LOITER | T2 | 500/60 | 10 | 40 | AIR | 42 | 400 | 1 | 1 | 一次性精确攻击 |
| UNIT_COMMON_ROBOT_RECON | T2 | 450/30 | 10 | 140 | LIGHT | 7 | 650 | 1 | 1 | 城市/危险区侦察 |
| UNIT_COMMON_ROBOT_COMBAT | T3 | 700/80 | 14 | 300 | LIGHT | 6 | 400 | 1 | 2 | 伴随步兵/警戒 |
| UNIT_COMMON_ROBOT_SUPPORT | T3 | 650/60 | 14 | 280 | LIGHT | 6 | 300 | 1 | 1 | 工程/维修/补给模块 |
| UNIT_COMMON_UGV_HEAVY | T3 | 1900/220 | 26 | 1200 | HEAVY | 9 | 500 | 3 | 3 | 无人火力平台 |

自主等级：基础无人 T1=A1，T2=A2；战斗机器人默认 A2；T4 科技可升 A3；T5 集群科技可升 A4。A4 必须增加 50% 算力占用。

## 4. 航空

| ID | Tier | 工业/战略 | 时间s | HP | Speed | Sensor | Cmd | Compute | 核心角色 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| UNIT_COMMON_HELO_ATTACK | T2 | 3400/300 | 36 | 850 | 75 | 700 | 5 | 0 | 低空反装甲 |
| UNIT_COMMON_HELO_TRANSPORT | T2 | 2600/180 | 32 | 900 | 80 | 600 | 4 | 0 | 机动投送 |
| UNIT_COMMON_FIGHTER | T2 | 5200/500 | 42 | 1100 | 230 | 1200 | 6 | 0 | 制空/多用途 |
| UNIT_COMMON_STEALTH_STRIKE | T3 | 6200/750 | 48 | 950 | 220 | 1050 | 7 | 0 | 隐身打击 |
| UNIT_COMMON_UCAV | T3 | 4100/450 | 38 | 700 | 180 | 950 | 5 | 3 | 无人制空/打击 |
| UNIT_COMMON_AEW | T3 | 5800/650 | 50 | 1400 | 150 | 2200 | 7 | 0 | 空中预警/数据链 |
| UNIT_COMMON_EW_AIR | T3 | 5600/650 | 48 | 1200 | 165 | 1600 | 7 | 0 | 空中电子压制 |
| UNIT_COMMON_BOMBER | T4 | 9000/1300 | 70 | 1900 | 190 | 1300 | 10 | 0 | 战略远程打击 |
| UNIT_COMMON_TRANSPORT_AIR | T2 | 4200/300 | 45 | 1600 | 145 | 700 | 6 | 0 | 大型空运 |

航空单位采用出动/返场循环；默认弹药架次为 1—4 次攻击任务，完成后自动返航补充。

## 5. 海军

| ID | Tier | 工业/战略 | 时间s | HP | Speed | Sensor | Cmd | 核心角色 |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| UNIT_COMMON_PATROL | T1 | 1800/80 | 28 | 800 | 22 | 700 | 3 | 近海巡逻 |
| UNIT_COMMON_FRIGATE | T2 | 5200/450 | 55 | 2200 | 18 | 1300 | 8 | 反潜/护航 |
| UNIT_COMMON_DESTROYER | T3 | 9000/900 | 75 | 3800 | 17 | 1800 | 14 | 区域防空/导弹 |
| UNIT_COMMON_CRUISER | T4 | 14500/1600 | 100 | 5600 | 15 | 2000 | 18 | 大型导弹平台 |
| UNIT_COMMON_CARRIER | T4 | 18000/2200 | 120 | 7200 | 14 | 2200 | 20 | 移动航空基地 |
| UNIT_COMMON_SS_ATTACK | T3 | 8500/850 | 78 | 3000 | 14 | 1200 | 12 | 反舰/隐蔽控制 |
| UNIT_COMMON_SS_STRATEGIC | T4 | 13000/1800 | 110 | 4200 | 12 | 1100 | 18 | 战略远程打击 |
| UNIT_COMMON_USV | T2 | 1200/120 | 20 | 260 | 28 | 750 | 2 | 侦察/诱饵/消耗 |
| UNIT_COMMON_UUV | T3 | 1800/220 | 26 | 350 | 16 | 800 | 3 | 水下侦察/攻击 |
| UNIT_COMMON_AMPHIB | T2 | 5500/350 | 60 | 2600 | 14 | 850 | 9 | 两栖投送 |
| UNIT_COMMON_REPLENISH | T2 | 4200/200 | 50 | 2400 | 13 | 700 | 7 | 海上保障 |

## 6. 国家特色单位基线

国家特色单位继承最接近的通用模板，只修改以下内容；未列字段沿用模板。

- `UNIT_CN_DRAGON_MBT`：MBT；HP 1950，速度 11.5，传感 550；成本 2500/120；联网状态下首发命中与目标切换 +15%。
- `UNIT_CN_HIVE_UGV`：重型无人战车；成本 2200/300；可同时携带 2 架侦察与 4 架微型攻击无人机；Compute 5。
- `UNIT_CN_XUANWU_HEAVY_ROBOT`：T4；HP 2400；成本 4300/650；Cmd 6，Compute 7；重火力+近程反无人。
- `UNIT_CN_THUNDER_MLRS`：MLRS；最大射程 +20%，齐射后重新部署时间 +15%。
- `UNIT_CN_AEGIS_DESTROYER`：DESTROYER；Sensor 2100，区域防空容量 +25%。

- `UNIT_US_GHOST_FIGHTER`：FIGHTER；隐身等级 3，Sensor 1450；成本 6000/700；与 CCA 编组时发现/锁定时间 -20%。
- `UNIT_US_FALCON_CCA`：UCAV；Compute 4；与幽灵战机组队时射程/目标共享 +15%。
- `UNIT_US_ATLAS_ROBOT`：T4 重型机器人；HP 2000，Speed 10，Sensor 650；成本 4200/700；高自主 A4。
- `UNIT_US_AEW_ADV`：AEW；Sensor 2500；成本 6500/800。
- `UNIT_US_GLOBAL_FIRE`：远程火力平台；要求 Confirmed 以上目标；射程 +30%，单发成本高。

- `UNIT_RU_RHINO3_MBT`：MBT；HP 1900；成本 2100/70；建造时间 25s；精度略低。
- `UNIT_RU_APOC3_HEAVY`：T4；HP 3600；Armor HEAVY；速度 6.5；成本 5200/600；Cmd 8；双重反装甲+近防。
- `UNIT_RU_HOUND_ROBOT`：ROBOT_COMBAT；成本 550/50；HP 330；适合量产。
- `UNIT_RU_LANCET_SWARM`：LOITER；一次生产 3 架，成本 1250/140；高价值后方单位优先。
- `UNIT_RU_SHOCK_EW`：EW_VEH；干扰半径 +25%；自身 Sensor -10%。

- `UNIT_UK_TEMPEST`：高端战斗机；隐身2、Sensor 1500；远征机场维护时间 -15%。
- `UNIT_UK_DRAGONFIRE`：定向能防空平台；对 DRONE/LOITER 高效；恶劣天气伤害 -30%。
- `UNIT_FR_RAFALE_F`：任务模块切换时间为普通战机 50%。
- `UNIT_FR_STEALTH_UCAV`：UCAV，隐身3，适合压制防空。
- `UNIT_DE_LEOPARD_F`：MBT，维修成本 -25%，模块槽+1。
- `UNIT_DE_BOXER_MODULE`：可在保障点切换 IFV/AA/EW/COMMAND 四种配置，切换 20s。
- `UNIT_JP_RAILGUN_SHIP`：T4 驱逐级平台；高能耗、远程直射，对导弹/水面舰高效。
- `UNIT_KR_THUNDER_SPG`：SPG；射击后转移准备时间 -35%。
- `UNIT_IL_MERKAVA_F`：MBT；主动防护充能 +1，速度 -5%。
- `UNIT_TR_BAYRAKTAR`：DRONE_ATTACK；成本 -20%，HP -15%。
- `UNIT_TR_KIZILELMA`：UCAV；空战优先，适合批量编组。
- `UNIT_UA_FIBER_DRONE`：LOITER；免疫无线链路干扰，但移动/转向性能 -10%。
- `UNIT_UA_GROUND_ROBOT`：ROBOT_COMBAT；模块成本低，战损后适应科技积累 +1。
- `UNIT_AU_GHOSTSHARK`：UUV；Sensor +25%，续航/巡逻半径 +30%。
- `UNIT_PL_HEAVY_ARMOR_BG`：不是单车而是预编战斗群模板；创建时自动包含 MBT/IFV/AA 组合，价格无折扣。
- `UNIT_IR_LOW_COST_DRONE`：DRONE_ATTACK；成本 -35%，HP -30%，适合饱和。

## 7. 尤里单位

- `UNIT_YURI_NEURAL_INF`：T1，350/20，HP 95；对人员附带短时认知扰乱。
- `UNIT_YURI_BIOMECH`：T2，1200/120，HP 850；城市近战，抗电磁高、怕穿甲。
- `UNIT_YURI_HUNTER_ROBOT`：T3，1800/220，HP 1100，Compute 4；高机动猎杀。
- `UNIT_YURI_GATLING2`：T2，2000/120，HP 850；持续射击增速，对蜂群/轻空高效。
- `UNIT_YURI_GRAVITY_PLATFORM`：T4，3800/650，HP 1500；控制轻型目标位移，强冷却。
- `UNIT_YURI_NEURAL_CONTROL`：T4，4200/800；局部神经/算法干扰，不可永久夺取英雄/战略单位。
- `UNIT_YURI_PHANTOM_NODE`：T3，2400/350；生成可侦破假目标。
- `UNIT_YURI_GHOST_SWARM`：T3，1400/180；侦察/欺骗蜂群。
- `UNIT_YURI_AUTONOMOUS_FIGHTER`：T4，5600/850，Compute 6；完全无人空战。
- `UNIT_YURI_ABYSS_UUV`：T4，3200/450，Compute 4；深海渗透。

## 8. 单位通用规则

1. 人员单位是主要建筑占领与复杂政治目标执行者，机器人不能完全替代。
2. 重型平台不能进入全部城市狭窄通道。
3. 所有远程单位近距离存在最低射程或展开惩罚。
4. 防空弹药不是无限：普通对局抽象为 3 个备战等级，由保障系统自动恢复。
5. 机器人/无人单位在算力过载和网络受扰时按自主等级降级。
6. 所有国家独占单位必须至少有两个明确反制标签。