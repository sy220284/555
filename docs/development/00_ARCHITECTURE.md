# 总体开发架构

## 目标

构建一套以 **Unity 6 + DOTS/Entities + Burst/Job System** 为生产底座、以项目自研战争核心算法为核心竞争力的 RTS 架构，满足：

- 30Hz 权威模拟；
- 800—1500 活动实体的标准现代对局；
- 3000—5000+ 实体的战区扩展；
- 完整 AI、机器人、电子战、情报、海空轨道规则；
- 专用服务器、录像、重连、模组与编辑器；
- AI 代理可持续并行修改且主分支始终可运行。

技术选型以 `../technical/TECH_STACK.md` 为准。

## 代码/包结构

推荐采用 package-first 结构：

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
  /com.modernra.tools
  /com.modernra.tests

/Data
  /Units
  /Buildings
  /Weapons
  /Technology
  /Countries
  /Maps
  /Campaign
```

## 权威模拟边界

`ModernRA.Simulation` 是唯一战争真相源。

允许：

- Entities 组件/Chunk；
- Burst/Job System；
- Unity.Mathematics；
- Native 容器；
- 项目自研纯规则模块。

禁止直接引用：

- Camera；
- Material；
- AudioSource；
- UI；
- 场景 GameObject 表现对象；
- VFX/Animator 状态作为规则依据。

表现层只消费权威状态，不反向决定战斗结果。

## 实体设计

普通作战单位采用轻量 ECS 数据，而不是每单位复杂 GameObject/MonoBehaviour。

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

冷数据如显示名、描述、本地化、资产引用不进入高频热循环。

## 固定模拟频率

- 权威时间推进：30Hz；
- 局部避障：10—20Hz；
- 单位战术控制：10—15Hz；
- 战斗群 AI：5—10Hz；
- 战区 AI：2—5Hz；
- 战略 AI：1—2Hz。

关键事件允许即时唤醒；频率属于游戏规则，不随客户端硬件改变。

## 系统依赖方向

```text
Core/Math/Data
→ Simulation
→ Combat / Navigation / Intel-EW / Economy
→ Robotics / AI / Air / Naval / Orbital
→ Network
→ Presentation / UI / Audio / Tools
```

低层不得引用高层。

## 自研战争核心

项目自研并长期掌控：

- 分层 AI；
- 分层寻路与战斗群路径；
- 空间网格/空间哈希；
- 目标/威胁评估；
- 情报与战争迷雾；
- 雷达/电子战；
- 机器人算力/自主等级；
- 轻量 2.5D 战斗碰撞与弹道；
- 大规模网络相关性与压缩。

这些能力不得被不可替换的第三方插件绑死。

## 导航架构

```text
战略区域图
→ 道路/桥梁/山口图
→ 战斗群共享路径走廊
→ 局部流场
→ 空间哈希局部避障
```

禁止每单位独立全图 A*。

## AI 架构

```text
战略AI
→ 战区AI
→ 战斗群AI
→ 单位轻量控制器
```

采用效用评分、状态机、任务规划、黑板和事件驱动；运行时竞技战斗禁止依赖大语言模型。

## 网络架构

采用专用服务器权威模拟：

```text
客户端命令
→ Unity Transport
→ 权威服务器30Hz模拟
→ 情报/相关性过滤
→ 批量增量快照
→ 客户端插值/有限预测
```

未被侦察的真实敌军状态不得发送给客户端。

## 可复现性

服务器模拟必须做到同一构建、同一输入流可以重放到一致关键状态哈希。

关键资源/经济/伤害优先整数或量化数据；随机源显式编号；并行阶段固定归并。

不再要求客户端通过纯锁步持有完整隐藏世界。

## 分布式基地与系统节点

能源、算力、通信、感知、保障是独立节点。节点损伤造成能力渐降，而不是简单全局开关。

## 数据驱动

权威设计数据使用稳定 ID + 结构化文本 + Schema 校验，构建时烘焙为运行时紧凑数据/Blob。

禁止大量 `if (country == X)` 硬编码国家差异。

## 占位实现

任何未完成依赖必须提供同接口占位实现，保证主分支始终：

- 可编译；
- 可启动；
- 可加载测试地图；
- 可跑一局基础模拟；
- 可执行自动测试。

## 性能上位约束

性能问题优先从：算法复杂度、数据布局、批处理、空间索引、结果复用、任务并行和表现批量化解决。

禁止通过削弱 AI、减少同模式单位、缩水地图、改变规则或把标准画质降成残缺低画质来达标。