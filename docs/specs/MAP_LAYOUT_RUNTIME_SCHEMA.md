# 地图布局运行时数据规范

本文件补齐 `MAP_SPECS.md` 的执行层，使16张地图可以从“设计描述”进入可复现灰盒生成、自动测试和最终美术替换。

## 1. 目标

`MAP_SPECS.md` 负责说明地图定位、尺寸、资源数量和玩法特征；本文件规定每张地图进入实现时必须具备的精确运行时数据。

最终地图不得依赖程序员凭文字猜坐标。

## 2. 每张地图必须有侧车文件

路径：

`/Data/Maps/<MAP_ID>.map.json`

最低结构：

```json
{
  "schema_version": 1,
  "map_id": "MAP_GRAY_RANGE",
  "size_m": [8000, 8000],
  "playable_bounds": [],
  "spawn_sectors": [],
  "resource_nodes": [],
  "strategic_sites": [],
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
- 团队图以“同队总可用资源+前线抵达时间”做镜像/统计公平；
- 安全建设半径默认>=600m；
- 出生朝向必须朝主要战区，避免镜头初始方向歧义。

## 4. 资源节点

字段：

`node_id, type, position, capacity, base_income_rate, max_harvest_groups, contest_weight`

资源类型：`I, S, D, E, R, A, P, L` 与 `MAP_SPECS.md` 一致。

所有资源必须有稳定 `node_id`，录像和任务脚本禁止直接引用坐标。

## 5. 道路与通道

道路字段：

`road_id, spline_points, width_m, terrain_cost_multiplier, vehicle_tags, destructible_segments`

最小通道宽度参考：

- 单装甲纵队：>=18m；
- 双向装甲主通道：>=35m；
- 大型团队主要推进带：>=60m；
- 城市主战街区：主要路线>=50m；
- 海军主航道：大型舰艇有效航道>=1500m（海陆综合大图）。

## 6. 桥梁

字段：

`bridge_id, endpoints, width_m, state, permanent_route, repairable, temporary_bridge_allowed`

状态：`INTACT, DAMAGED, CLOSED`。

排位地图必须至少存在一条无法被永久摧毁而彻底断局的备用路线。

## 7. 地形区域

字段：

`region_id, polygon, terrain_type, movement_modifier, sensor_modifier, build_rule, height_band`

常见类型：`OPEN, URBAN, FOREST, RIDGE, VALLEY, MARSH, ICE, SHALLOW_WATER, DEEP_WATER`。

地形属性必须数据化，不由材质名称决定玩法。

## 8. 可建区

`buildable_polygons` 明确建筑可放置范围、坡度、最小离道路距离、禁建区。

禁止运行时通过“看起来像平地”推断可建。

## 9. AI区域

强制标签：

- `ZoneResource`
- `ZoneChoke`
- `ZoneUrban`
- `ZoneOpen`
- `ZoneNaval`
- `ZoneAirfield`
- `ZoneHighValue`

每个区域包含 `zone_id, polygon, tags, neighbors, strategic_weight`。

AI可以在运行时重新评估价值，但高层区域图必须来自稳定数据。

## 10. 战略设施

字段：

`site_id, site_type, position, capture_radius, owner, income_or_effect, destruction_rule`

占领时间和权重使用 `GAME_MODE_RULES.md`；地图只定义设施本身，不复制模式算法。

## 11. 灰盒自动生成

当正式地形/美术尚未完成时，AI代理可根据侧车数据生成确定性灰盒：

1. 按 `playable_bounds` 建基础地形；
2. 生成道路/桥梁；
3. 放出生区；
4. 放资源/战略设施；
5. 应用地形区域高度带；
6. 生成简化障碍/城区体块；
7. 烘焙导航层；
8. 运行自动公平测试。

同一 `map.json + generator_version + seed` 必须生成同一灰盒拓扑。

## 12. MAP_SPECS描述转执行数据规则

当前16图的文字描述是布局意图，不直接当运行时坐标。首次实现每张图时必须在同一提交中创建对应 `.map.json`。

在 `.map.json` 尚未提交前，该地图状态最多为 `layout_defined`；存在侧车并通过基础结构测试后为 `graybox_ready`；美术不影响规则数据稳定ID。

## 13. 自动公平门禁

每次地图数据变化必须输出：

- 最近I/S路径距离与抵达时间；
- 第一中立战略点抵达时间；
- 可建设面积；
- 三条最短敌我主要陆路差异；
- 主要桥梁/海峡替代路径；
- 空军/海军出入口；
- AI不可达/死区；
- 至少1000场镜像AI换边胜率。

对称地图出生位胜率超过52/48且样本>=1000：阻止进入排位。

## 14. 性能预算

- 地图区域图、道路图和静态传感遮挡数据构建期预烘焙；
- 动态破坏只更新局部导航和遮挡区；
- 16x16km地图采用区域流送；
- 服务器只加载玩法碰撞、区域、道路、资源和简化高度数据，不加载客户端高精资产。

## 15. 验收

地图进入 `graybox_ready` 必须：

- JSON Schema通过；
- 无悬空 `site_id/road_id/zone_id`；
- 所有出生点可到达至少一个工业资源和一个主要战区；
- 不存在唯一可永久切断路线；
- 主要模式胜负区数据完整；
- AI可导航；
- 自动公平门禁通过。
