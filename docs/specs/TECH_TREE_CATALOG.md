# 五级科技树与分叉规格

## 1. 科技推进原则

科技不绑定比赛经过时间。T2—T5何时出现，只由建筑前置、研究成本、研究时间、分叉和玩家资源投入决定。

允许玩家高风险快科技，也允许长期停留T1/T2。任何系统不得读取 `elapsed_match_time` 判断某个科技是否可以研究。

自动平衡可以记录各科技在真实对局中的到达时点，但只能用于诊断成本/前置是否合理，不能把“目标第X分钟”写成解锁条件。

科技解锁采用建筑前置+研究成本+研究时间；研究完成后永久生效，除非模式规则关闭。

## 2. 通用主干

### T1 现代机械化
默认解锁：基础步兵、工程、侦察车、IFV、MBT、SHORAD、基础无人机、基础能源、车辆厂、人员中心、雷达、保障。

### T2 信息化
前置：`BLD_COMMON_DATACENTER`。

- `TECH_COMMON_DATALINK`：1200工业/100战略，45s；联网单位目标共享延迟-20%。
- `TECH_COMMON_PRECISION_FIRE`：1400/140，50s；Confirmed以上目标远程精度+10%。
- `TECH_COMMON_EW_BASIC`：1300/120，45s；解锁EW步兵/车辆/建筑。
- `TECH_COMMON_AIR_NET`：1500/160，50s；解锁AEW/EW航空前置。
- `TECH_COMMON_MRAD`：1200/100，40s；解锁中程防空。

### T3 无人/机器人
前置：`BLD_COMMON_AI_COMMAND` 或等效国家建筑。

- `TECH_COMMON_AUTONOMY_A2`：1600/220，55s；无人/机器人可用A2。
- `TECH_COMMON_ROBOT_COMBAT`：1800/260，60s；解锁战斗/支援机器人。
- `TECH_COMMON_UGV`：1900/300，60s；解锁重型无人地面平台。
- `TECH_COMMON_CCA`：2100/350，65s；解锁无人僚机/UCAV。
- `TECH_COMMON_CYBER`：1800/300，60s；解锁网络战基础能力。

### T4 智能战争
前置：`BLD_COMMON_COMPUTE`。

- `TECH_COMMON_AUTONOMY_A3`：2500/500，75s。
- `TECH_COMMON_DIRECTED_ENERGY`：2800/600，80s。
- `TECH_COMMON_HIGHSPEED_STRIKE`：3000/700，90s。
- `TECH_COMMON_LRAD_ABM`：2600/550，75s。
- `TECH_COMMON_STRATEGIC_AI`：3200/800，90s；解锁L3战区AI。
- `TECH_COMMON_SPACE_ACCESS`：3500/900，100s；解锁空天中心前置。

### T5 未来战争
前置：空天/高级能源/阵营专属未来实验条件之一。

- `TECH_COMMON_AUTONOMY_A4`：4200/1200，120s；A4算力成本+50%。
- `TECH_COMMON_QUANTUM_NAV`：3500/1000，105s；导航干扰影响减半。
- `TECH_COMMON_ORBITAL_CONTROL`：4500/1400，130s。
- `TECH_COMMON_FUTURE_POWER`：5000/1500，140s。

## 3. T4分叉规则

玩家在T4选择两个“主专精槽”。第三条及以后研究成本×1.75、研究耗时×1.5；第四条×2.5、研究耗时×2.0。该耗时是研究动作成本，不是比赛阶段锁。

这样不硬锁内容，但高阶全点需要承担真实经济和机会成本。

## 4. 中国专精

### 无人集群
- `TECH_CN_SWARM_1`：无人单位生产时间-10%。
- `TECH_CN_SWARM_2`：A3/A4跨域目标共享+15%。
- `TECH_CN_CROSSDOMAIN_SWARM`：空/地/海无人平台可由同一战斗群AI协调。

### 远程拒止
- `TECH_CN_DENIAL_1`：火箭/导弹射程+10%。
- `TECH_CN_DENIAL_2`：防空/反舰共享目标锁定时间-15%。
- `TECH_CN_SKYDOME`：解锁`STRAT_CN_SKYDOME`。

### 机器军团
- `TECH_CN_ROBOT_MASS`：机器人连续批量生产每5件降成本2%，最多10%。
- `TECH_CN_XUANWU`：解锁玄武重型机器人。

### 空天网络
- `TECH_CN_SKYNET`：卫星/预警/舰艇传感器融合+20%。
- `TECH_CN_ORBITAL_SERVICE`：轨道维修/检查效率+25%。

## 5. 美国专精

### 杀伤链/战争云
- `TECH_US_KILLCHAIN`：T3，2200/360，65s；前置`TECH_COMMON_DATALINK`；Confirmed以上目标在AEW/数据节点覆盖下目标共享延迟-15%，远程火力首次锁定时间-10%。
- `TECH_US_WARCLOUD`：T4，3000/700，85s；AI战斗群响应/任务重规划-15%。
- `TECH_US_A4_CCA`：T4，2800/650，80s；无人僚机可升A4。

### 隐身航空
- `TECH_US_STEALTH_NET`：隐身单位被远程传感器持续跟踪所需时间+20%。
- `TECH_US_GHOST`：解锁幽灵制空战机高级模块。

### 轨道体系
- `TECH_US_ORBITAL_AWARENESS`：卫星刷新+20%。
- `TECH_US_RAPID_LAUNCH`：补充卫星建造时间-25%。

### 定向能
- `TECH_US_DEW_1`：激光热容量+20%。
- `TECH_US_DEW_2`：解锁高级移动定向能平台。

## 6. 俄罗斯专精

### 工业动员
- `TECH_RU_MOBILIZATION`：T3，2000/300，70s；前置`BLD_RU_WAR_INDUSTRY`；车辆/机器人维修周转+10%，同类连续生产加速上限额外+5%，并作为`STRAT_RU_TOTAL_MOBILIZATION`前置之一。
- `TECH_RU_WAR_ECONOMY`：T4；车辆同类连续生产加速上限提高至25%。
- `TECH_RU_FIELD_REPAIR`：车辆战场维修效率+20%。

### 重装甲
- `TECH_RU_APOC3`：解锁天启Ⅲ。
- `TECH_RU_APS_HEAVY`：重型单位APS恢复+20%。

### 电子黑区
- `TECH_RU_EW_MASS`：EW半径+15%。
- `TECH_RU_BLACKSKY`：解锁大范围轨道/通信压制战略能力。

### 无人消耗
- `TECH_RU_LOITER_MASS`：巡飞武器批量成本-15%。
- `TECH_RU_HOUND_MASS`：猎犬机器人建造时间-20%。

## 7. 联合体系其他国家

- 英国：远征网络/激光防御二选一优先。
- 法国：独立数据链/隐身无人航空。
- 德国：模块化/自动保障。
- 日本：岛链传感/电磁炮。
- 韩国：自动军工/激光防御。
- 以色列：多层拦截/战场AI。
- 澳大利亚：海下无人/量子导航。
- 波兰：纵深防御/炮兵网络。

这些国家节点使用 `TECH_<国家>_<主题>` 命名，具体新增时必须先进入注册表再编码。

## 8. 其他国家

### 印度
复杂地形机动/三军火网两条主专精，强化高原机动和跨军种目标共享。

### 土耳其
无人航空规模/狼群自主两条主专精，强调低成本无人航空和任务分工。

### 乌克兰
- `TECH_UA_BATTLEFIELD_LEARNING`：T3，1900/260，60s；前置`BLD_UA_DISTRIBUTED_DRONE_SHOP`或`TECH_COMMON_CYBER`；敌方同类战术在已确认情报下重复出现后，允许获得最多10%的侦察/反制准备效率加成；不读取隐藏敌方数据，离开该对局不永久继承。
- 分布式制造：降低单一工厂被毁后的生产中断风险。

### 伊朗
地下化/饱和与诱饵两条主专精，强化设施生存与低成本无人/假目标交换。

## 9. 尤里科技

T2：
- `TECH_YURI_DECEPTION`：假目标。
- `TECH_YURI_NEURAL_BASIC`：神经压力。

T3：
- `TECH_YURI_MACHINE_HIVE`：蜂群/猎杀机器人。
- `TECH_YURI_MODEL_POISON`：AI污染。

T4分叉：神经控制、机器军团、认知云、轨道寄生。

T5：`TECH_YURI_ASCENSION_NETWORK`，解锁`STRAT_YURI_ASCENSION`。升格只在能力持续期间提升协同，不永久夺取对方核心资产。

## 10. 科技取消与切换

研究开始后可取消，返还70%未消耗资源；超过50%进度后取消只返还40%。主专精槽一旦锁定不可在标准排位中切换，战役可由脚本允许。

## 11. AI研究规则

AI不得跳过前置；必须支付相同资源与研究时间。AI只能依据自身合法情报选择分支，禁止读取未侦察玩家科技，也禁止根据比赛经过时间直接切换科技阶段。

## 12. 数据字段

每项科技必须包含：

`id, tier, branch, prerequisites, industrial_cost, strategic_cost, data_cost, research_time, mutually_exclusive_group, modifiers, unlocks, ai_weight, mode_rules, test_profile`

所有在 `CONTENT_REGISTRY.md` 中登记的 `TECH_` 必须能在本文件或未来正式拆分科技注册表中找到执行定义，否则构建失败。