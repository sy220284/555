# 总体开发架构

## 1. 目标

构建以 **Unity 6 + DOTS/Entities + Burst/Job System** 为生产底座、以项目自研战争核心算法为核心竞争力的RTS架构，满足：

- 30Hz专用服务器权威模拟；
- 800—1500有效活动实体的标准现代对局；
- 3000—5000+实体的战区扩展；
- 完整AI、机器人、电子战、情报、海空轨道规则；
- FMOD大规模战场音频；
- 专用服务器、录像、重连、模组与编辑器；
- 在线匹配/排位/运营服务；
- AI代理可持续并行修改且主分支始终可运行。

技术选型以 `../technical/TECH_STACK.md` 为唯一技术上位依据。

## 2. 代码/包结构

```text
/Assets
  /GameContent
  /Presentation
  /UI
  /Audio
  /Maps

/Packages
  /com.modernra.core
  /com.modernra.simulation
  /com.modernra.combat
  /com.modernra.ai
  /com.modernra.navigation
  /com.modernra.intel-ew
  /com.modernra.robotics
  /com.modernra.air
  /com.modernra.naval
  /com.modernra.orbital
  /com.modernra.network
  /com.modernra.presentation
  /com.modernra.audio
  /com.modernra.online
  /com.modernra.tools
  /com.modernra.tests

/Data
  /Units
  /Buildings
  /Weapons
  /Abilities
  /Technology
  /Countries
  /Rulesets
  /Maps
  /Campaign
  /Audio
```

详细目录以 `../technical/BUILD_AND_LAYOUT.md` 为准。

## 3. 权威模拟边界

`ModernRA.Simulation` 是唯一战争真相源。

允许依赖：

- Entities组件/Chunk；
- Burst/Job System；
- Unity.Mathematics；
- Native容器；
- 项目自研纯规则模块。

禁止直接引用：

- Camera；
- Material；
- Audio/FM0D事件；
- UI；
- 场景GameObject表现对象；
- VFX/Animator状态；
- 在线账号/匹配业务对象。

表现、音频和在线层只能消费权威结果或发送合法意图，不能反向决定战斗结果。

## 4. 实体设计

普通作战单位采用轻量ECS数据，不使用每单位复杂GameObject/MonoBehaviour作为权威状态。

热数据示例：

```text
Position
Velocity
Heading
Health
Order
Target
WeaponCooldown
SensorFlags
SupplyState
```

显示名、描述、本地化、资产引用、音频事件等冷数据不进入高频热循环。

`FORM_` 编队模板不是实体，运行时只负责把多个真实 `UNIT_` 放入生产队列并建立战斗群。

## 5. 固定模拟频率

- 权威时间推进：30Hz；
- 局部避障：10—20Hz；
- 单位战术控制：10—15Hz；
- 战斗群AI：5—10Hz；
- 战区AI：2—5Hz；
- 战略AI：1—2Hz。

关键事件可即时唤醒一次；频率属于游戏规则，不随客户端硬件改变。

## 6. 系统依赖方向

```text
Core / Data
→ Simulation
→ Combat / Navigation / Intel-EW / Economy
→ Robotics / AI / Air / Naval / Orbital
→ Network
→ Presentation / Audio / UI / Tools
```

`ModernRA.Online` 位于对局生命周期外围，通过会话/匹配接口连接客户端和专用服务器，不进入战争模拟依赖链。

低层不得引用高层。

## 7. 自研战争核心

项目自研并长期掌控：

- 分层AI；
- 分层寻路与战斗群路径；
- 空间网格/空间哈希；
- 目标/威胁评估；
- 情报与战争迷雾；
- 雷达/电子战；
- 机器人算力/自主等级；
- 轻量2.5D战斗碰撞与弹道；
- 大规模网络相关性与压缩。

这些能力不得被不可替换的第三方插件绑死。

## 8. 导航架构

```text
战略区域图
→ 道路/桥梁/山口图
→ 战斗群共享路径走廊
→ 局部流场
→ 空间哈希局部避障
```

禁止每单位独立全图A*。

## 9. AI架构

```text
战略AI
→ 战区AI
→ 战斗群AI
→ 单位轻量控制器
```

采用效用评分、状态机、任务规划、黑板和事件驱动；运行时竞技战斗禁止依赖大语言模型。

## 10. 网络架构

```text
客户端命令
→ Unity Transport
→ 权威服务器30Hz模拟
→ 情报/空间/重要度过滤
→ 批量增量快照
→ 客户端插值/有限预测
```

未被侦察的真实敌军状态不得发送给无权限客户端。客户端不提交伤害、资源或真实位置作为权威结果。

## 11. 可复现性

同一服务器构建、同一初始快照、同一命令/事件流应重放到一致关键状态哈希。

关键资源/经济/伤害优先整数或量化数据；随机源显式编号；并行阶段固定归并。

不要求客户端通过纯锁步持有完整隐藏世界。

## 12. 音频架构

```text
Simulation/Presentation Event
→ AudioEventBus
→ WarAudioDirector
→ FMOD Adapter
```

权威模拟只发送语义事件，不直接调用FMOD。大规模声源聚类、优先级和虚拟化在音频层处理。

## 13. 在线服务边界

账号、组队、匹配、排位、赛季、结果与遥测属于业务服务层。战斗结果只接受专用服务器签名来源。

在线服务故障不能改变正在运行比赛的权威规则；需要时停止新匹配而不是让客户端接管服务器权威。

## 14. 分布式基地与系统节点

能源、算力、通信、感知、保障是独立节点。节点损伤造成能力渐降，不是简单全局开关。

## 15. 数据驱动

权威设计数据使用稳定ID + JSON/Schema，构建时烘焙为运行时紧凑数据/Blob。

禁止大量 `if (country == X)` 硬编码国家差异。

地图使用 `<MAP_ID>.map.json`；规则集、编队、音频注册表均进入统一内容验证链。

## 16. 占位实现

任何未完成依赖必须提供同接口占位，保证主分支始终可编译、可启动、可加载测试地图、可跑基础模拟、可执行自动测试。

占位状态不得被标记为 `content_complete` 或 `ship_ready`。

## 17. 性能上位约束

性能问题优先从算法复杂度、数据布局、批处理、空间索引、结果复用、任务并行和表现/音频批量化解决。

禁止通过削弱AI、减少同模式单位、缩水地图、改变规则、降低权威30Hz、把标准画质降成残缺低画质或提高最低配置来达标。
