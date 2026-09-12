# 实现主干启动状态

本文件只记录已经实际进入仓库并有可复现证据的实现，不把设计文档、接口空壳或占位冒充可玩功能。详细实跑数据以 `10_FORMAL_IMPLEMENTATION_PROGRESS.md` 为准，阶段门禁以 `../specs/IMPLEMENTATION_TEST_GATES.md` 为准。

## 已实际入库

### 工程与依赖

- Unity 编辑器基线：`6000.3.24f1 (4e7b9b5b6244)`；
- 正式依赖锁定：Entities、Entities Graphics、Burst、Transport、Addressables、性能测试包；
- 10 个 `com.modernra.*` 嵌入包和 10 个程序集已进入结构门禁；
- `Tools/project_structure_validator.py` 已校验精确编辑器版本、正式包版本、依赖图、程序集引用、EditMode 测试程序集属性和 30Hz 固定步配置；
- `Tools/run_unity_g1.py` 与 `.github/workflows/unity-g1.yml` 已建立真实 Unity 批处理编译/EditMode 执行入口。

### 权威规则与 Unity 接桥

- `com.modernra.rules`：纯 C# 确定性权威规则内核，禁止引用 Unity API；
- `ModernRA.Simulation`：30Hz 权威模拟边界；
- `ModernRA.Combat`：装甲、武器、伤害事件契约；
- `ModernRA.Navigation`：共享路径、局部导航/避障契约；
- `ModernRA.AI`：AI 授权、禁令、玩家覆盖代次；
- `ModernRA.IntelEW`：情报/EW 数据契约；
- `ModernRA.Robotics`：A1—A4、自主降级、链路与算力契约；
- `ModernRA.Network`：命令、快照、复制可见性边界；
- `GrayRangeBootstrapSystem`：将权威地图运行数据实体化为 Unity ECS 锚点；
- `AnnihilationRuleBridgeSystem`：按 30Hz 调用统一歼灭规则并镜像经济、单位、建筑、生命、胜负和状态哈希；
- Unity 桥不允许复制坦克造价、伤害、采集速率等第二套规则常量。

### 权威机器数据与地图

- `/Data/Rulesets/rulesets.json`：五种标准规则集；
- `/Data/Minerals/minerals.json`：7 类矿产/回收资源；
- `/Data/Technology/common_technology.json`：首批通用科技与征服材料绑定；
- `/Data/Maps/MAP_GRAY_RANGE.map.json`：首张地图权威侧车；
- `/Data/Schemas/`：rulesets、minerals、technology、map Schema；
- `Tools/runtime_map_codegen.py`：地图侧车 -> 编译期运行数据；
- `Tools/graybox_generator.py`：确定性灰盒清单、区域图、道路、资源、出生、公平性派生；
- 生成代码和地图源由 CI 逐字节/哈希校验，地图源变化后旧生成结果不能混入主线。

### 无画面规则与玩法闭环

- `Tools/ModernRA.SimRunner` 已完成 1000 活动单位、10000 tick、重复 10 次确定性门禁；
- 当前基线最终哈希：`A72CA0F85058C84B`；
- 基础经济已验证：采集、第二采集组递减、采矿单位损失后停止对应吞吐；
- 共享路径与空间索引已进入规则门禁，禁止每单位全地图搜索；
- 1000 实体附近目标查询候选扫描较朴素全量扫描下降约 94.6%；
- 1000 单位自由目标获取/同步伤害战斗已进入空间哈希路径，300 tick 基线哈希：`477FF238CD8346B6`；
- 分频调度已验证：战斗保持 30Hz，单位/传感、战斗群、战区、战略逻辑按冻结频率错峰，3000 实体参考工作量下降约 57%；
- 情报过滤、AI 授权、机器人降级、本机服务器/客户端规则回环已经有可执行门禁。

### 第一局歼灭规则闭环与录像

- `Tools/ModernRA.AnnihilationRunner` 直接读取 `MAP_GRAY_RANGE` 同源运行数据；
- 已跑通 `建设 -> 采集 -> 生产 -> 移动 -> 战斗 -> 基础设施摧毁/恢复 -> 战争体系崩溃判负`；
- 标准参考局自然结束于 tick `1893`，最终哈希 `1979A9B7B4183764`；
- 胜负不要求清空地图，失败方仍可保留无恢复意义的保障建筑；
- 每 120 tick 状态检查点重放通过，共 16 个检查点；
- 已建立可持久化录像文件、地图源哈希/规则集/格式版本绑定和落盘读回重放校验；
- 当前参考录像文件 SHA256：`69cef8e723ae4b3b919f010ec24ddd2a0371e7dd16f07b3f0901a71dbb5c37e0`。

### 自动门禁

- `.github/workflows/content-validation.yml`：数据、嵌入包结构、灰盒、运行地图代码生成、Unity 桥结构等；
- `.github/workflows/simulation-gates.yml`：纯规则构建、10000 tick、歼灭闭环、录像/空间/分频等回归；
- `.github/workflows/unity-g1.yml`：等待合法激活的 Unity `6000.3.24f1` 自托管执行器执行真实 G1；
- 当前结构前置门禁已识别并通过：Unity 精确版本、10 个嵌入包、10 个程序集；
- 当前 Python 自动回归至少 39 项通过，既有 .NET 规则运行器保持 0 warning / 0 error。

## 当前生命周期

项目整体状态仍为 **`implementation_bootstrap`**。

已经通过大量规则层、数据层和自动化门禁，并且 G11 已有完整无画面规则闭环；但 **G1 真实 Unity 编辑器编译/EditMode 尚未关闭**，因此不得升级为 `playable`。

## 当前唯一最高优先级阻断

### G1：真实 Unity 工程编译

仍需一台合法激活 Unity `6000.3.24f1 (4e7b9b5b6244)` 的执行环境实际证明：

1. 所有嵌入包成功解析；
2. `ModernRA.Core/Rules/Simulation/Combat/Navigation/AI/IntelEW/Robotics/Network/Tests` 全程序集编译；
3. EditMode 测试全绿；
4. `FixedStepSimulationSystemGroup.Timestep == 1/30f`；
5. 无 preview/experimental 依赖。

仓库已提供 `Tools/run_unity_g1.py` 与 `unity-g1` 工作流。外部执行环境阻断记录在 Issue #20。静态前置通过不等于 G1 通过。

## G1 关闭后的下一执行链

必须继续按风险顺序推进：

1. 修复真实 Unity 编译暴露的问题；
2. 实际运行 `GrayRangeBootstrapSystem` 并核对地图锚点实体数量/坐标；
3. 建立最低限度灰盒表现、相机、框选和命令输入；
4. 将规则镜像桥逐步替换为正式 ECS 批处理经济/移动/战斗系统；
5. 在最低目标硬件采集 800—1500 实体 P50/P95/P99；
6. 完成 Unity 内可观察、可操作、可录像的第一局歼灭灰盒；
7. G0—G11 全部通过后，才允许生命周期升级为 `playable`；
8. 之后再进入征服矿产科技、动态前线、快速战争和战区扩展。

任何后续代理不得因为无画面规则已闭环，就跳过真实 Unity、玩家输入、性能和完整对局门禁直接宣称项目可玩。
