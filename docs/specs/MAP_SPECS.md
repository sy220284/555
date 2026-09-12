# 16张首发多人地图执行规格

## 1. 通用坐标与验收

地图坐标原点为西南角，单位米。竞技地图所有出生位资源距离误差目标<8%，首个战略点抵达时间误差<10%。出生主基地周边至少有600m安全建设圈。

资源类型：I=工业，S=战略，D=数据中心，E=能源，R=雷达，A=机场，P=港口，L=物流。

`I/S` 是经济结算类别，具体矿区必须按 `MINERAL_RESOURCE_SYSTEM.md` 进一步标记 `mineral_id`。老地图的 `I` 默认迁移为 `MIN_BASE_METALS`，`S` 默认迁移为 `MIN_MIXED_STRATEGIC`。

排位地图的具体战略矿种必须镜像或做等价值/获取成本对称，不允许单个出生位天然控制全部稀土、电池金属、合金和核燃料优势。

## 2. 1V1地图

### MAP_GRAY_RANGE 灰原试验场
8x8km，2人，对称陆战。出生：(1200,1200)/(6800,6800)。每家近点I×2、S×1；中线I×2、D×1、R×2。三路线：西谷、中央公路、东高地。无海军；航空开放T2。天气关闭。目标：作为数值基准图。

矿产：I统一 `MIN_BASE_METALS`；S统一 `MIN_MIXED_STRATEGIC`，不提供专属材料标签，保证该图适合作为纯数值基准。

### MAP_RIFT_NODE 裂谷节点
8x8km，2人。出生西北/东南。中央裂谷不可通行，仅两座桥+一条南部绕行。每家I×2、S×1；桥区D×1、E×1。桥可炸毁，工程可架临时桥。适合机动/火炮博弈。

矿产：近家I=`MIN_BASE_METALS`；双方近家S使用等价值 `MIN_HIGH_PERFORMANCE_ALLOYS`，突出装甲/导弹工业主题，但不得把桥区做成唯一战略材料来源。

### MAP_NORTHERN_PLAIN 北方平原
10x8km，2人。宽正面，弱高地。每家I×2、S×1；中区I×3、R×1、L×1。四条浅通道，禁止单一咽喉。小概率雪但仅降低光学侦察10%。

矿产：I=`MIN_BASE_METALS`；双方S=`MIN_BATTERY_METALS`；中区额外I仍为基础金属。适合无人/机器人持续作战，但电池材料只提供小幅产业效率，不硬锁相关科技。

### MAP_DATA_CONTEST 数据争夺
8x8km，2人。资源中等、D×3均位于中轴。近家I×2/S×1；中心I×1。控制数据节点可明显加速AI/高级科技，但不直接增加工业收入。

矿产：I=`MIN_BASE_METALS`；双方S=`MIN_RARE_EARTHS`。稀土标签与数据中心形成电子/传感器主题，但所有位置必须严格镜像。

### MAP_DESERT_TWINS 沙海双城
10x10km，2人。两座可进入城区位于地图中部两侧，中央沙漠开放。I×2/S×1近家；城区各I×1/D×1；中央E×1。沙尘事件每8—12分钟可能发生一次，持续60—90秒，光学传感-20%，雷达不受影响。

矿产：近家I=`MIN_BASE_METALS`；双方近家S=`MIN_MIXED_STRATEGIC`。两座城区外围各放一个等价值可争夺 `MIN_RARE_EARTHS` 设施型矿区，用于形成左右选择，不能只有中央唯一路线。

### MAP_POLAR_FRONT 极昼前线
8x10km，2人。冰原+工业站。近家I×2/S×1；中央E×2/R×1。部分薄冰只允许步兵/轻车，通过重车会触发破裂区域并永久变为水域；关键主路线均有稳定陆桥避免随机死局。

矿产：I=`MIN_BASE_METALS`；S=`MIN_BATTERY_METALS`；中央工业站可提供一个对称的 `MIN_HIGH_PERFORMANCE_ALLOYS` 战略加工节点，但不能硬锁重型装备。

## 3. 2V2/3V3

### MAP_STEEL_PLAIN 钢铁平原
12x12km，4—6人。团队出生于两侧弧形区域。每玩家I×2/S×1；中央工业带I×4/D×2。三大推进带，铁路/公路提高轮式单位战略移动10%。

矿产：大部分I=`MIN_BASE_METALS`；各队S等价值分配 `MIN_HIGH_PERFORMANCE_ALLOYS`；中央工业带以基础金属和工业设施为主，强化“钢铁生产中心”定位。

### MAP_DNIEPER_LINE 第聂伯防线
12x10km，4人。大河纵贯南北，3座永久桥、2座可毁桥位；工程允许浮桥。两岸各有I×4/S×2；河心岛D×1/R×1。不能通过摧毁全部桥永久封死，永久桥不可完全摧毁，只能短时封锁。

矿产：两岸I=`MIN_BASE_METALS`；每队的两个S分别配置 `MIN_BATTERY_METALS` 与 `MIN_MIXED_STRATEGIC`，两边完全镜像。河心岛不放唯一战略矿，避免桥梁控制直接垄断全部高端资源。

### MAP_STRAIT_BLOCKADE 海峡封锁
12x12km，4—6人，海陆混合。两岸基地+中央海峡。每玩家I×2/S×1；P×2、A×2中立。海军航道宽度>=1.5km，确保大型舰队转向。陆上仍有可决胜路线，避免强迫海军。

矿产：陆上I=`MIN_BASE_METALS`；S=`MIN_MIXED_STRATEGIC`。港口可连接海上小型 `MIN_BATTERY_METALS`/多金属资源设施，但两队获取成本对称，且不能使海军成为唯一资源路线。

### MAP_RHINE_BELT 莱茵钢铁带
12x12km，4—6人。城市/工业密集，桥梁多。每队后方I×4/S×2；中央工业I×4/D×2/E×2。建筑可驻军，主要通路宽度至少80m。

矿产：后方I=`MIN_BASE_METALS`；每队S由 `MIN_HIGH_PERFORMANCE_ALLOYS` 与 `MIN_RARE_EARTHS` 各一类构成；中央工业仍以基础金属和工业设施收入为主。

## 4. 海陆空综合

### MAP_FIRST_ISLAND_CHAIN 第一岛链
16x16km，4—8人。陆地约35%、海面65%。每队拥有主岛与1个前进小岛。P×4/A×4/R×3。航母/岸基航空均可发挥。中央战略岛含D×1/E×1，但不是唯一胜利路径。

矿产：主岛基础矿=`MIN_BASE_METALS`；前进岛/中立岛可出现 `MIN_RARE_EARTHS` 与 `MIN_BATTERY_METALS`，按队伍镜像分布。中央战略岛不承载唯一一种关键矿产。

### MAP_DEEPBLUE_FORTRESS 深蓝壁垒
16x16km，4—8人。海面75%。每队沿海基地+深水港。海下地形包含深沟与浅滩，潜艇/无人潜航器有多条隐蔽航线。禁止单一狭口封锁全部海域。

矿产：沿海I=`MIN_BASE_METALS`；陆上S=`MIN_MIXED_STRATEGIC`；部分海底多金属资源节点可标记 `MIN_BATTERY_METALS`，需要海上工程/采掘设施和港口物流，不作为所有阵营核心科技硬条件。

### MAP_INDIAN_OCEAN_SHIELD 印度洋之盾
16x14km，6—8人。三个岛群+两块大陆边缘。补给舰和前进机场价值高。每队至少一条陆上/岛屿扩张与一条海上扩张选择。

矿产：大陆I=`MIN_BASE_METALS`；岛群战略矿在 `MIN_RARE_EARTHS / MIN_HIGH_PERFORMANCE_ALLOYS / MIN_BATTERY_METALS` 中按阵营等价值镜像配置，鼓励不同岛群具有不同产业价值。

## 5. 大型娱乐

### MAP_CONTINENTAL_HEARTLAND 大陆腹地
16x16km，8—12人。多山、多道路、多区域资源。I×24、S×12、D×5、E×6。用于战区AI、多战线、机器人规模与性能测试。非排位。

矿产：I以 `MIN_BASE_METALS` 为主；S可混合 `MIN_RARE_EARTHS / MIN_BATTERY_METALS / MIN_HIGH_PERFORMANCE_ALLOYS / MIN_NUCLEAR_FUEL / MIN_MIXED_STRATEGIC`。由于非排位，可体现区域资源专长和大型战区产业链，但仍不得让单一区域成为所有战略科技的唯一入口。

### MAP_EASTERN_SHIELD 东部之盾
16x14km，8人。防守方地形略优但进攻方初始工业资源+8%，用于攻防娱乐模式。普通遭遇战版本必须使用镜像资源规则。

矿产：娱乐攻防版本可采用非对称矿产布局；普通遭遇战版本的I/S矿种和获取成本必须镜像。基础I=`MIN_BASE_METALS`，S优先采用 `MIN_RARE_EARTHS` 与 `MIN_MIXED_STRATEGIC`。

## 6. 空天实验

### MAP_ORBITAL_DAWN 轨道黎明
10x10km，2—4人。地面包含发射场×2、卫星站×2、D×2。T4后开放简化轨道支援。轨道资产自动运行，玩家只选择部署/干扰/保护。用于验证轨道系统，不进入首发1V1核心排位池。

矿产：I=`MIN_BASE_METALS`；S以 `MIN_RARE_EARTHS` 和 `MIN_HIGH_PERFORMANCE_ALLOYS` 为主，可在非排位实验规则中加入对称 `MIN_NUCLEAR_FUEL` 节点测试高密度能源/轨道产业链。

## 7. 出生与资源自动检查

每次地图提交必须自动输出：
- 最近I/S资源的路径距离与抵达时间；
- 每种 `MIN_*` 的可达总等价值、运输成本和产业标签获取成本；
- 第一个中立战略点抵达时间；
- 可建设面积；
- 三条最短敌我陆路路径差异；
- 空军/海军起降通路；
- AI寻路死区；
- 1000场镜像AI对局出生位胜率。

若对称地图出生位差超过52/48且样本>1000，地图不得进入排位。

具体矿产公平还必须满足 `MINERAL_RESOURCE_SYSTEM.md`。

## 8. 动态破坏

桥梁三状态：完整/受损/封闭。竞技地图必须有至少一条不可永久切断的备用路线。建筑坍塌采用预制废墟，废墟可改变局部通道但不得随机封死唯一路线。

矿体通常不能被普通攻击永久删除；可攻击采矿、精炼、电力、运输和物流节点。资源产业破坏与恢复按 `MINERAL_RESOURCE_SYSTEM.md` 执行。

## 9. 天气

天气是已知概率事件，赛前地图说明显示可能类型。排位天气不造成>20%的核心属性变化，不直接摧毁单位。战役可使用更强脚本天气。

## 10. AI区域标注

每张地图必须包含：`ZoneResource`、`ZoneChoke`、`ZoneUrban`、`ZoneOpen`、`ZoneNaval`、`ZoneAirfield`、`ZoneHighValue`。AI以这些标注作为高层规划参考，但仍需实时路径/情报判断。

`ZoneResource` 必须能读取 `mineral_id / resource_class / logistics_links / material_access_tag`，使AI能够区分普通工业矿和高价值战略材料节点。