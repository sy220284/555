# 全单位规格目录

本文件给出首发可执行基线。数值是第一版实现基准，后续只能通过平衡流程修改，不得由程序员任意改动。

## 1. 统一字段

- `HP`：结构/生命值。
- `Armor`：INF/LIGHT/MEDIUM/HEAVY/AIR/NAVAL/STRUCT/ORBIT。
- `Speed`：地面m/s、空中/海上采用游戏化等效速度。
- `Sensor`：基础探测半径（米）。
- `Command`：指挥容量。
- `Compute`：算力占用。
- `Supply`：每分钟补给负担基数。
- `Tier`：T1—T5。

生产建筑、武器、能力和音频引用统一见 `UNIT_LOADOUTS.md`。

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
| UNIT_COMMON_MLRS | T2 | 3100/140 | 32 | 800 | MEDIUM | 9 | 500 | 4 | 0 | 6 | 区域火力 |
| UNIT_COMMON_SHORAD | T1 | 1850/60 | 24 | 700 | MEDIUM | 11 | 700 | 4 | 0 | 4 | 反无人/低空 |
| UNIT_COMMON_MRAD | T2 | 2600/120 | 28 | 700 | MEDIUM | 9 | 1000 | 4 | 0 | 5 | 中程防空 |
| UNIT_COMMON_EW_VEH | T2 | 2200/150 | 28 | 650 | MEDIUM | 10 | 700 | 4 | 1 | 4 | 电子压制 |
| UNIT_COMMON_REPAIR | T1 | 1200/0 | 20 | 550 | MEDIUM | 9 | 260 | 2 | 0 | 3 | 维修/回收 |
| UNIT_COMMON_COMMAND | T2 | 1800/120 | 26 | 650 | MEDIUM | 9 | 500 | 3 | 2 | 3 | 战斗群中继 |

## 3. 无人/机器人

| ID | Tier | 工业/战略 | 时间s | HP | Armor | Speed | Sensor | Cmd | Compute | 说明 |
|---|---:|---:|---:|---:|---|---:|---:|---:|---:|---|
| UNIT_COMMON_DRONE_RECON | T1 | 400/20 | 8 | 80 | AIR | 35 | 850 | 1 | 1 | 低成本侦察 |
| UNIT_COMMON_DRONE_ATTACK | T2 | 950/80 | 14 | 140 | AIR | 30 | 650 | 2 | 2 | 轻攻击/标记 |
| UNIT_COMMON_LOITER | T2 | 500/60 | 10 | 40 | AIR | 42 | 400 | 1 | 1 | 一次性精确攻击 |
| UNIT_COMMON_ROBOT_RECON | T2 | 450/30 | 10 | 140 | LIGHT | 7 | 650 | 1 | 1 | 城市/危险区侦察 |
| UNIT_COMMON_ROBOT_COMBAT | T3 | 700/80 | 14 | 300 | LIGHT | 6 | 400 | 1 | 2 | 伴随步兵/警戒 |
| UNIT_COMMON_ROBOT_SUPPORT | T3 | 650/60 | 14 | 280 | LIGHT | 6 | 300 | 1 | 1 | 工程/维修/补给 |
| UNIT_COMMON_UGV_HEAVY | T3 | 1900/220 | 26 | 1200 | HEAVY | 9 | 500 | 3 | 3 | 无人火力平台 |

自主等级：基础无人T1=A1，T2=A2；战斗机器人默认A2；T4科技可升A3；T5集群科技可升A4。A4算力占用×1.5。

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

航空采用出动/返场循环；默认弹药架次为1—4次攻击任务，完成后返航补充。

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

规则：特色单位必须声明最近通用模板；未列字段继承模板。若为“预编战斗群”，它是生产模板，不生成新的万能单实体。

### 中国
- `UNIT_CN_DRAGON_MBT`：模板MBT；T2；HP1950，Speed11.5，Sensor550，成本2500/120；联网且目标Confirmed以上时首轮火控+15%。
- `UNIT_CN_HIVE_UGV`：模板UGV_HEAVY；T3；2200/300；HP1250，Compute5；携带2侦察+4微型攻击无人机库存。
- `UNIT_CN_XUANWU_HEAVY_ROBOT`：模板UGV_HEAVY；T4；4300/650；HP2400，Cmd6，Compute7；重火力+反无人。
- `UNIT_CN_THUNDER_MLRS`：模板MLRS；T3；射程+20%，齐射后再部署时间+15%。
- `UNIT_CN_AEGIS_DESTROYER`：模板DESTROYER；T4；Sensor2100，区域防空容量+25%。

### 美国
- `UNIT_US_GHOST_FIGHTER`：模板FIGHTER；T3；6000/700；HP1050，Sensor1450，隐身3；与CCA编组锁定时间-20%。
- `UNIT_US_FALCON_CCA`：模板UCAV；T3；4400/520；Compute4；与有人机共享目标效率+15%。
- `UNIT_US_ATLAS_ROBOT`：模板UGV_HEAVY；T4；4200/700；HP2000，Speed10，Sensor650，A4。
- `UNIT_US_AEW_ADV`：模板AEW；T4；6500/800；Sensor2500。
- `UNIT_US_GLOBAL_FIRE`：模板SPG；T4；4800/850；HP800，射程等效+30%；只攻击Confirmed以上高价值目标。

### 俄罗斯
- `UNIT_RU_RHINO3_MBT`：模板MBT；T1；2100/70；HP1900，25s；精度-5%。
- `UNIT_RU_APOC3_HEAVY`：模板MBT；T4；5200/600；HP3600，Speed6.5，Cmd8；双主炮等效齐射+重近防。
- `UNIT_RU_HOUND_ROBOT`：模板ROBOT_COMBAT；T3；550/50；HP330，量产型。
- `UNIT_RU_LANCET_SWARM`：模板LOITER；T3；1250/140一次生产3架。
- `UNIT_RU_SHOCK_EW`：模板EW_VEH；T3；干扰半径+25%，自身Sensor-10%。

### 英国
- `UNIT_UK_SAS`：模板SPECOPS；T2；1250/120；HP130，Speed5.8，Sensor500；渗透/标记效率+15%。
- `UNIT_UK_TEMPEST`：模板FIGHTER；T4；6500/800；HP1050，Sensor1500，隐身2；远征机场整备-15%。
- `UNIT_UK_DRAGONFIRE`：模板SHORAD；T3；2600/300；HP650；定向能反无人，恶劣天气效率-30%。
- `UNIT_UK_ATTACK_SS`：模板SS_ATTACK；T3；8800/900；Sensor1300，隐蔽确认时间+15%。

### 法国
- `UNIT_FR_RAFALE_F`：模板FIGHTER；T3；5600/580；任务模块切换时间为普通战机50%。
- `UNIT_FR_STEALTH_UCAV`：模板UCAV；T3；4500/560；隐身3，HP650，适合压制防空。
- `UNIT_FR_LECLERC_F`：模板MBT；T2；2550/120；HP1750，Speed12.2；移动射击稳定系数+5%。

### 德国
- `UNIT_DE_LEOPARD_F`：模板MBT；T2；2500/110；HP1850；维修工业成本-25%，模块槽+1。
- `UNIT_DE_BOXER_MODULE`：模板IFV；T2；1700/80；可在保障点20秒切换IFV/AA/EW/COMMAND配置。
- `UNIT_DE_MODULE_ROBOT`：模板ROBOT_COMBAT；T3；850/100；HP340；可在维修点切换侦察/战斗/工程模块，30秒。
- `UNIT_DE_AUTO_SUPPORT`：模板REPAIR；T3；1550/100；HP600；维修/补给效率+20%，无主战武器。

### 日本
- `UNIT_JP_ISLAND_MOBILE`：模板MLRS；T3；3400/260；HP900，Sensor800；可切换反舰/区域防空支援模式，切换25秒。
- `UNIT_JP_AEGIS_SHIP`：模板DESTROYER；T3；9800/1000；Sensor2100，防空/反导弹药容量+20%。
- `UNIT_JP_RAILGUN_SHIP`：模板DESTROYER；T4；11800/1500；HP3600；高能耗远程直射，主武器 `WPN_JP_RAILGUN_NAVAL`。
- `UNIT_JP_ASW_UNMANNED`：模板USV；T3；1500/180；Sensor950；反潜搜索半径+25%。

### 韩国
- `UNIT_KR_BLACKPANTHER_F`：模板MBT；T2；2500/120；HP1780，Speed12，Sensor520；火控确认时间-10%。
- `UNIT_KR_THUNDER_SPG`：模板SPG；T2；3000/140；射击后转移准备时间-35%。
- `UNIT_KR_SKYLIGHT_LASER`：模板SHORAD；T3；2700/320；定向能反无人，不消耗导弹备弹但受热容量限制。

### 以色列
- `UNIT_IL_MERKAVA_F`：模板MBT；T2；2600/140；HP1950；APS充能+1，Speed-5%。
- `UNIT_IL_SMART_INF`：模板RIFLE；T2；550/40；HP105，Sensor330；处于数据链覆盖时目标识别+15%。
- `UNIT_IL_IRONBEAM`：模板SHORAD；T3；2800/340；激光反无人/巡飞弹，恶劣天气效率下降。

### 印度
- `UNIT_IN_HIGHLAND_BG`：预编战斗群模板；T2；由1 IFV+2 RIFLE+1 AT组成，单项价格总和不打折；复杂地形移动惩罚降低15%。
- `UNIT_IN_HIGHSPEED_STRIKE`：模板SPG；T4；4300/700；HP700，远程高速打击，单发成本高，必须Confirmed以上目标。
- `UNIT_IN_HEAVY_ROCKET`：模板MLRS；T3；3500/220；区域齐射弹量+20%，装填时间+15%。

### 土耳其
- `UNIT_TR_BAYRAKTAR`：模板DRONE_ATTACK；T2；成本-20%，HP-15%，适合规模化侦察打击。
- `UNIT_TR_KIZILELMA`：模板UCAV；T3；3900/420；空战优先，Sensor1000，Compute3。

### 乌克兰
- `UNIT_UA_FIBER_DRONE`：模板LOITER；T2；550/70；免疫无线控制链干扰，机动/转向-10%。
- `UNIT_UA_GROUND_ROBOT`：模板ROBOT_COMBAT；T3；650/70；HP310；模块成本低。
- `UNIT_UA_HEAVY_ATTACK_DRONE`：模板UCAV；T3；3600/380；HP620，Sensor850；低成本重打击，Compute3。

### 澳大利亚
- `UNIT_AU_GHOSTBAT`：模板UCAV；T3；4300/480；HP680，Sensor1050；与有人战机编组时共享目标效率+10%。
- `UNIT_AU_GHOSTSHARK`：模板UUV；T3；2100/260；Sensor+25%，巡逻半径+30%。
- `UNIT_AU_ATTACK_SS`：模板SS_ATTACK；T3；9000/900；Sensor1250；与海底传感器联网时目标确认+15%。

### 波兰
- `UNIT_PL_HEAVY_ARMOR_BG`：预编战斗群模板；T2；1 MBT+1 IFV+1 SHORAD，价格为单项总和，无折扣；生成后仍是独立单位。
- `UNIT_PL_MASS_ARTILLERY`：模板SPG；T2；2650/90；建造时间-15%，HP-10%，适合数量化炮兵。
- `UNIT_PL_LAYERED_AA`：模板MRAD；T3；2800/150；与SHORAD编组时目标分配效率+15%。

### 伊朗
- `UNIT_IR_MOBILE_MISSILE`：模板MLRS；T3；3300/250；远程单发/小齐射模式，发射后暴露提高，必须机动转移。
- `UNIT_IR_LOW_COST_DRONE`：模板DRONE_ATTACK；T2；成本-35%，HP-30%，用于饱和。
- `UNIT_IR_DECOY`：模板DRONE_RECON；T2；350/20；无武器，生成高DeceptionStrength假目标并消耗少量算力。

## 7. 尤里单位

- `UNIT_YURI_NEURAL_INF`：T1，350/20，HP95；对人员附带短时神经压力。
- `UNIT_YURI_BIOMECH`：T2，1200/120，HP850；城市近战，抗电磁高、怕穿甲。
- `UNIT_YURI_HUNTER_ROBOT`：T3，1800/220，HP1100，Compute4；高机动猎杀。
- `UNIT_YURI_GATLING2`：T2，2000/120，HP850；持续射击增速，对蜂群/轻空高效。
- `UNIT_YURI_GRAVITY_PLATFORM`：T4，3800/650，HP1500；控制轻型目标位移，强冷却。
- `UNIT_YURI_NEURAL_CONTROL`：T4，4200/800；局部神经/算法干扰，不可永久夺取英雄/战略单位。
- `UNIT_YURI_PHANTOM_NODE`：T3，2400/350；生成可侦破假目标。
- `UNIT_YURI_GHOST_SWARM`：T3，1400/180；侦察/欺骗蜂群。
- `UNIT_YURI_AUTONOMOUS_FIGHTER`：T4，5600/850，Compute6；完全无人空战。
- `UNIT_YURI_ABYSS_UUV`：T4，3200/450，Compute4；深海渗透。

尤里单位装配和能力必须同样进入 `UNIT_LOADOUTS.md`，不能因为是剧情阵营绕过注册体系。

## 8. 单位通用规则

1. 人员单位是主要建筑占领与复杂目标执行者，机器人不能完全替代。
2. 重型平台不能进入全部城市狭窄通道。
3. 所有远程单位近距离存在最低射程或展开惩罚。
4. 防空弹药采用 `ReadyShots` 与保障恢复规则，不是无限弹药。
5. 机器人/无人单位在算力过载和网络受扰时按自主等级降级。
6. 所有国家独占单位至少有两个明确反制标签。
7. “预编战斗群”只是一条生产/编组宏，不允许成为拥有多倍能力却只占一个碰撞实体的超级单位。
8. 特色单位数值继承链必须能被内容验证器解析；模板不存在即构建失败。
