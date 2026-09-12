# AI、机器人、电子战状态机与评分公式

本文件是AI/机器人/EW执行规格。频率与 `development/00_ARCHITECTURE.md`、`technical/TECH_STACK.md`、`development/04_PERFORMANCE_NAVIGATION.md` 统一，禁止出现第二套值。

## 1. 总体分层

`StrategicAI -> ZoneAI -> BattleGroupAI -> LocalController`。

每层只处理自己的时间尺度，避免每个单位重复做全局决策。

- StrategicAI：1—2Hz。
- ZoneAI：2—5Hz。
- BattleGroupAI：5—10Hz。
- LocalController：10—15Hz。
- 局部避障：10—20Hz。
- 紧急反应：事件触发，可即时唤醒一次，但不得形成无限高频循环。

这些频率属于玩法规则，不按客户端性能动态变化。

## 2. 玩家AI战斗指挥权限

### L1 战术辅助
允许：队形、局部避障、自动反击、维修、补给、无人侦察、防空跟随。

### L2 战斗群
玩家指定区域/目标与姿态，AI选择路线、目标优先级、无人侦察、局部撤退。

### L3 战区
允许在授权区内部重新分配授权单位、轮换、有限反击、调用普通支援火力。

### L4 副指挥
仅战役/合作/战区模式；依据预算、风险、任务和禁令管理方向。

未经明确授权永远禁止：核武、国家终极能力、改变主科技分叉、突破战略资源保留额、拆核心基地、改变总主攻方向、全面撤退。

## 3. BattleGroup状态机

状态：`ASSEMBLE -> MOVE -> RECON -> ENGAGE -> CONSOLIDATE -> RESUPPLY -> WITHDRAW`。

转换：

- ASSEMBLE完成率>=80%或超时30s -> MOVE。
- MOVE进入敌情概率区 -> RECON。
- RECON发现Confirmed目标且战力比>=阈值 -> ENGAGE。
- ENGAGE目标清除/区域占领 -> CONSOLIDATE。
- 弹药/HP/补给低于阈值 -> RESUPPLY。
- 战力比<撤退阈值或任务取消 -> WITHDRAW。

姿态阈值：

- 保守：进攻战力比>=1.4，撤退<0.9。
- 均衡：>=1.15，撤退<0.75。
- 激进：>=0.95，撤退<0.60。

## 4. 目标效用评分

`Score = Threat*0.30 + MissionValue*0.25 + Vulnerability*0.15 + DistanceFactor*0.10 + CounterMatch*0.10 + IntelConfidence*0.10`。

所有分量归一化0—1。AI不得使用未侦察真实HP、隐藏科技等作弊信息；`IntelConfidence` 来自当前情报等级。

高价值目标标签：AI/Compute、Radar/Data、AirDefense、LongRangeFire、Logistics、Superweapon。

## 5. 战区分配评分

`ZonePriority = ObjectiveValue*0.35 + EnemyPressure*0.25 + ResourceValue*0.15 + StrategicConnectivity*0.15 + PlayerDirective*0.10`。

玩家显式“主攻/死守”命令可把PlayerDirective提升到0.5并重新归一化。

## 6. 自动生产

必须有玩家授权预算。维护目标格式：`role, desired_count, max_budget, strategic_resource_allowed`。

AI只在当前数量<目标、预算足够且指挥/算力有余量时补充。禁止因为“理想编制”透支玩家保留战略资源。

## 7. 敌方电脑战略AI

循环：`OBSERVE -> INFER -> PLAN -> ALLOCATE -> EXECUTE -> REVIEW`。

可见信息只来自己方情报系统。对玩家科技的推断必须带置信度，不允许读取真实隐藏数据。

战略候选：扩张、科技、正面进攻、侧翼、远程消耗、空袭、登陆、反后勤、反雷达、佯攻、撤退、巩固。

候选效用：`ExpectedGain - ExpectedLoss - OpportunityCost + DoctrineBias`。

## 8. 机器人自主等级

A1：远程控制。断联进入`SAFE_HOLD`，30s后尝试返航。

A2：本地导航。断联继续巡逻/返航/寻找掩体，不主动进入未知区域。

A3：战术自主。断联可完成既定区域防守/攻击Confirmed目标，不能选择新战略目标。

A4：集群自主。局部节点可分工、包围、火力分配；失去全局链路仍不允许改变玩家任务边界。

A4算力需求=同型A3×1.5。

## 9. 机器人局部状态机

`IDLE -> FOLLOW -> SEARCH -> ATTACK -> EVADE -> DAMAGED -> RETURN -> DISABLED`。

- HP<35%且非“死守” -> DAMAGED/RETURN。
- SystemStress>=80 -> 自主等级临时-1。
- NavigationQuality<0.4 -> 降速30%，优先停止复杂机动。
- CommsQuality<0.3 -> 执行断联规则。

## 10. 电子战数值

区域质量变量均为0—1：`SensorQuality, CommsQuality, NavigationQuality`。

干扰贡献采用递减叠加：`FinalQuality = Base * Π(1 - JamStrength_i * Exposure_i * (1-Resistance))`，最低夹到0.15，避免普通干扰让系统归零。

典型干扰：

- 便携EW：Strength0.15，半径350m。
- EW车辆：0.30，半径900m。
- 固定EW节点：0.35，半径1100m。
- 战略电磁能力：0.45，短时大范围。

Resistance：A1/普通链路0.1，抗干扰0.35，A3多链路0.55，A4/量子辅助0.70。光纤控制无线链路Resistance=1.0，但Sensor/物理攻击不免疫。

## 11. 网络战

CyberPressure每个目标0—100。攻击方通过已获得网络接触面逐步积累，防御方通过隔离、补丁、人工接管降低。

阈值：30产生轻微延迟；60产生错误情报/服务降级；90允许短时功能阻断。战略/英雄资产不可被永久夺取。

攻击暴露后，防御方获得溯源置信度并可提高后续防护；避免网络战无风险无限施法。

## 12. 假目标

假目标拥有`DeceptionStrength`。侦察检查：`DetectionPower + RandomDeterministicRoll > DeceptionStrength`则识破。高级多源传感器对假目标获得加成。

AI看到未识破假目标时必须按真实目标一样纳入评估，不能作弊识别。

## 13. AI中心损毁

L1本地控制仍存在；L2以上任务停止重规划，已下发任务继续。若存在边缘节点，则相应战区保持L2能力。AI中心恢复后分批重接管，避免所有单位同帧重规划造成性能峰值。

## 14. 算力调度

优先级默认：玩家直控编队 > 防空/反导 > 当前交战战斗群 > 侦察 > 后勤 > 待机机器人。玩家可修改，但不能把系统关键安全逻辑设为0。

过载时只延长决策周期和降低自主层级，不修改单位基础伤害/HP。

## 15. 难度AI

- 新兵：规划频率基准×0.6，较弱反制，资源1.0。
- 普通：基准，无作弊。
- 老兵：规划频率基准×1.15，更好侦察/撤退，无资源加成。
- 精英：基准×1.3，复杂佯攻与多线，无视野作弊，资源效率+3%上限。
- 指挥官：基准×1.4，最优战斗群与科技适配，资源效率+5%上限；仍必须真实侦察。

难度倍率作用于规划机会和质量预算，不得突破本文件各层硬最大频率形成机器反应作弊。

## 16. 验收

必须自动验证：授权边界、预算边界、禁用战略能力、无作弊视野、断联降级、算力过载、电子战递减叠加、假目标可被AI误判、玩家接管立即生效、同一权威输入重放关键决策一致、各AI层实际更新频率符合本文件范围。
