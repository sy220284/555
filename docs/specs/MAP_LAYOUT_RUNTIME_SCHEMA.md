# 地图布局运行时数据规范

本文件补齐 `MAP_SPECS.md` 的执行层，使16张地图可以从“设计描述”进入可复现灰盒生成、自动测试和最终美术替换。地图必须支持 `design/03A_FREEDOM_WARFARE.md` 的高自由度战争规则。

## 1. 目标

`MAP_SPECS.md` 负责说明地图定位、尺寸、资源数量和玩法特征；本文件规定每张地图进入实现时必须具备的精确运行时数据。

最终地图不得依赖程序员凭文字猜坐标，也不得把固定推进顺序写死在场景脚本里。

## 2. 每张地图必须有侧车文件

路径：

`/Data/Maps/<MAP_ID>.map.json`

最低结构：

```json
{
  "schema_version": 3,
  "map_id": "MAP_GRAY_RANGE",
  "size_m": [8000, 8000],
  "playable_bounds": [],
  "spawn_sectors": [],
  "quickwar_deployment_zones": [],
  "resource_nodes": [],
  "strategic_sites": [],
  "control_regions": [],
  "home_core_regions": [],
  "theater_critical_regions": [],
  "roads": [],
  "bridges": [],
  "water_zones": [],
  "buildable_polygons": [],
  "terrain_regions": [],
  "ai_zones": [],
  "weather_profile": "none",
  "ruleset_compatibility": []
}
```

## 3. 出生区

每个 `spawn_sector`：

`spawn_id, team_slot, center, facing_deg, safe_radius_m, initial_build_polygon, rally_points`

规则：

- 1v1地图每个出生区中心到最近工业/战略资源路径差<8%；
- 团队图以同队总可用资源、主要交通和战略空间做公平统计；
- 安全建设半径默认>=600m；
- 出生朝向必须朝主要战区，避免初始镜头歧义。

## 4. 快速战争部署区

凡声明兼容 `RULESET_QUICKWAR_STANDARD` 的地图必须定义 `quickwar_deployment_zones`。

字段：

`zone_id, team_slot, polygon, base_anchor_points, formation_anchor_points, air_spawn_points, naval_spawn_points, protected_until_live, initial_sensor_mask, exit_corridors`

规则：

- 部署区必须同时容纳完整 `QuickWarBasePreset` 与65%—70%有效指挥容量的初始现役部队；
- 基地建筑锚点与主力编队锚点不得互相堵塞工厂出口、道路和航空跑道；
- 至少存在2条大型地面编队可并行离开部署区的主出口；
- 空军/海军生成点必须与地面单位碰撞分离；
- 在LIVE前部署区具有规则级保护，禁止对手获取完整高精度情报或造成不可逆伤害；
- LIVE开始后保护立即取消，区域恢复普通地图规则；
- 双方从部署区到第一主要战区的等价路径成本差必须进入公平测试；
- 出生区不能因为满编军队同时移动而形成单一不可恢复拥堵点。

快速战争具体建筑和部队内容由 `QUICKWAR_FULL_READINESS.md` 定义，地图只提供合法空间、锚点和交通条件。

## 5. 资源节点

字段：

`node_id, type, position, capacity, base_income_rate, max_harvest_groups, contest_weight`

资源类型：`I, S, D, E, R, A, P, L` 与 `MAP_SPECS.md` 一致。

所有资源必须有稳定 `node_id`，录像和任务脚本禁止直接引用坐标。

资源产量不能根据比赛进行时间自动变化；任何变化必须来自控制、破坏、科技或明确环境状态。

## 6. 控制区域 `ControlRegion`

地图必须把可争夺空间划分为区域图，而不是只放几个胜利圆圈。

每个区域：

`region_id, polygon, neighbors, strategic_weight, control_tags, local_command_sites, local_logistics_sites, contained_resource_nodes, contained_strategic_sites`

常见 `control_tags`：

- `HOME_CORE`
- `INDUSTRIAL`
- `LOGISTICS`
- `AIR_ACCESS`
- `NAVAL_ACCESS`
- `DATA`
- `ENERGY`
- `TRANSIT`
- `URBAN`
- `HIGH_GROUND`
- `THEATER_CRITICAL`

运行时状态由模式系统维护：`NEUTRAL / CONTESTED / CONTROLLED_UNSTABLE / CONTROLLED_STABLE`。

地图只提供拓扑、价值和设施，不在地图脚本中写“第几分钟变值钱”。

## 7. 动态前线

前线从 `control_regions[].neighbors` 和当前控制状态推导，不存固定 `FrontNode` 链。

规则：

- 任意相邻的己方稳定控制区与敌对/争夺区边界都可以形成前线；
- 两栖登陆/空降成功建立本地指挥和保障后，可以生成新的前线分支；
- 桥梁摧毁、道路封锁、补给中断可以改变区域连通状态；
- 包围区仍可以战斗，但其稳定控制/补给状态按实际连接重新计算；
- 地图至少提供多条有意义的战略路径，不能把标准前线规则做成单一本道。

## 8. 核心区域

`home_core_regions`：每队至少一个，用于前线/征服/歼灭模式判断核心战区。

字段：

`team_slot, region_id, required_command_tags, rebuild_allowed, alternate_core_regions`

`theater_critical_regions`：战区战争的重要区域集合，可包含港口、机场、工业区、数据中心、交通枢纽等。

战区胜负逻辑由 `GAME_MODE_RULES.md` 读取这些区域，地图本身不累计积分。

## 9. 道路与通道

道路字段：

`road_id, spline_points, width_m, terrain_cost_multiplier, vehicle_tags, destructible_segments`

最小通道宽度参考：

- 单装甲纵队：>=18m；
- 双向装甲主通道：>=35m；
- 大型团队主要推进带：>=60m；
- 城市主战街区主要路线：>=50m；
- 海军主航道大型舰艇有效航道：>=1500m。

快速战争出生区主出口应优先满足大型团队推进带标准，避免满编军队从狭口依次排队。

## 10. 桥梁

字段：

`bridge_id, endpoints, width_m, state, permanent_route, repairable, temporary_bridge_allowed, affected_region_edges`

状态：`INTACT, DAMAGED, CLOSED`。

排位地图不能存在“唯一桥一炸整张图永久断局”的结构，除非明确存在工程恢复、两栖或其他真实替代路径。

## 11. 地形区域

字段：

`terrain_id, polygon, terrain_type, movement_modifier, sensor_modifier, build_rule, height_band`

常见类型：`OPEN, URBAN, FOREST, RIDGE, VALLEY, MARSH, ICE, SHALLOW_WATER, DEEP_WATER`。

地形属性必须数据化，不由材质名称决定玩法。

## 12. 可建区

`buildable_polygons` 明确建筑可放置范围、坡度、最小离道路距离和禁建区。

前线/战区模式必须允许在合理区域建设前线保障、雷达、临时工事和部分指挥节点，使玩家可以自己创造新的战略轴线。

快速战争出生区可建区必须覆盖完整初始基地和后续至少30%的扩建余量。

## 13. AI区域

强制标签：

- `ZoneResource`
- `ZoneChoke`
- `ZoneUrban`
- `ZoneOpen`
- `ZoneNaval`
- `ZoneAirfield`
- `ZoneHighValue`

每个区域包含 `zone_id, polygon, tags, neighbors, strategic_weight`。

AI高层区域图可以与 `ControlRegion` 对齐或建立映射，但不得使用固定时间脚本推动AI攻击。

快速战争部署阶段AI可以预生成防御、机动和后勤任务，但在LIVE前不得越过部署边界执行攻击任务。

## 14. 战略设施

字段：

`site_id, site_type, position, capture_profile, owner, income_or_effect, destruction_rule, control_region_id`

占领作业和稳定控制规则使用 `GAME_MODE_RULES.md`；地图只定义设施本身。

## 15. 灰盒自动生成

当正式地形/美术尚未完成时，AI代理可根据侧车数据生成确定性灰盒：

1. 按 `playable_bounds` 建基础地形；
2. 生成道路/桥梁；
3. 生成控制区域和邻接关系；
4. 放出生区与快速战争部署区；
5. 放资源/战略设施；
6. 应用地形区域高度带；
7. 生成简化障碍/城区体块；
8. 烘焙导航和区域连通层；
9. 运行自动公平、自由路线和快速战争满编部署测试。

同一 `map.json + generator_version + seed` 必须生成同一灰盒拓扑。

## 16. 自动公平与自由度门禁

每次地图数据变化必须输出：

- 最近I/S路径距离与抵达成本；
- 可建设面积；
- 至少三条主要敌我陆路的成本差异（适用地图）；
- 控制区域图的割点/桥接边；
- 是否存在单点永久锁死整条战线；
- 主要桥梁/海峡替代路径；
- 空军/海军出入口；
- AI不可达/死区；
- 从出生区到敌核心至少两种战略路径（地形允许时）；
- 快速战争完整基地+满编现役军队是否能无重叠生成；
- 快速战争全军同时离开部署区是否出现永久拥堵；
- 至少1000场镜像AI换边胜率。

对称地图出生位胜率超过52/48且样本>=1000：阻止进入排位。

前线/战区认证图若只能形成一条不可替代推进轴，默认不通过自由度门禁。

快速战争认证图如果无法在不削减标准编制的情况下容纳双方完整战备状态，同样不通过认证。

## 17. 性能预算

- 区域图、道路图和静态传感遮挡数据构建期预烘焙；
- 动态破坏只更新受影响区域的导航、连接和遮挡；
- 16x16km地图采用区域流送；
- 服务器只加载玩法碰撞、区域、道路、资源和简化高度数据，不加载客户端高精资产；
- 快速战争完整战备状态必须批量生成/恢复，禁止逐单位逐建筑串行生成导致长时间卡顿。

## 18. 验收

地图进入 `graybox_ready` 必须：

- JSON Schema通过；
- 无悬空 `site_id/road_id/zone_id/region_id`；
- 所有出生点可到达至少一个工业资源和主要战区；
- 控制区域邻接图有效；
- 不存在无恢复手段的单一永久锁死路线；
- 模式要求的 `home_core_regions/theater_critical_regions` 完整；
- AI可导航并理解动态区域控制；
- 自动公平与自由度门禁通过。

声明兼容快速战争时还必须：

- 存在合法 `quickwar_deployment_zones`；
- 完整基地和65%—70%有效指挥容量的初始部队可无重叠生成；
- 地面/航空/海军出口不互相堵塞；
- 双方在LIVE前不会发生不可逆直接交火；
- 满编部队同时出动的导航与性能门禁通过。