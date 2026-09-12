# AGENT.md — AI 自动开发总规则

本文件是所有编程代理、评审代理、测试代理、内容代理和资产代理的强制执行规则。

## 1. 文档优先级

1. `docs/design/00_MASTER_GDD.md`：产品原则。
2. `docs/technical/TECH_STACK.md`：冻结技术栈。
3. 专项设计文档：系统定位。
4. `docs/specs/*`：具体参数、状态机、ID、规则与验收。
5. `docs/development/*`：代码组织、开发流程。
6. 单项实现配置。

发生冲突：先修文档，再编码；禁止代理自行选择更方便的一套。

## 2. 编码前强制阅读

任何任务至少读取：

- `AGENT.md`
- `docs/technical/TECH_STACK.md`
- `docs/design/00_MASTER_GDD.md`
- `docs/development/00_ARCHITECTURE.md`
- `docs/specs/CONTENT_REGISTRY.md`
- 当前任务对应专项规格
- `docs/specs/TEST_MATRIX.md`
- `docs/specs/ACCEPTANCE_CRITERIA.md`

涉及性能/导航/大规模实体还读：

- `docs/development/04_PERFORMANCE_NAVIGATION.md`
- `docs/technical/PERFORMANCE_ARCHITECTURE.md`
- `docs/technical/HARDWARE_PERFORMANCE_TARGETS.md`

涉及网络/存档/录像还读：

- `docs/development/03_NETWORK_DETERMINISM.md`
- `docs/specs/SAVE_NETWORK_MOD_EDITOR_SCHEMA.md`

涉及音频/语音还读：

- `docs/specs/AUDIO_VOICE_PIPELINE.md`

涉及地图还读：

- `docs/specs/MAP_SPECS.md`
- `docs/specs/MAP_LAYOUT_RUNTIME_SCHEMA.md`

涉及战役/对白还读：

- `docs/specs/CAMPAIGN_MISSION_SPECS.md`
- `docs/specs/CAMPAIGN_NARRATIVE_CAST.md`

涉及后台/排位/运营/安全还读：

- `docs/technical/ONLINE_SERVICES_MATCHMAKING_LIVEOPS.md`
- `docs/technical/ONLINE_SECURITY_PRIVACY.md`

对象若没有稳定ID、参数、公式/状态机、引用链或验收用例，禁止凭经验编码，先补规格。

## 3. 冻结技术栈

生产主线：

- Unity 6.3 LTS当前基线；
- C#；
- Entities/DOTS；
- Burst；
- Job System；
- URP；
- Entities Graphics + GPU批量表现；
- Unity Transport + 自研RTS权威复制层；
- Unity Dedicated Server / Linux；
- Addressables；
- FMOD Studio + `WarAudioDirector`；
- 自研AI、寻路、空间索引、2.5D战斗物理、情报/EW、机器人和网络相关性算法。

禁止代理自行：

- 切换Unreal/Godot/自研引擎；
- 引入独立C++权威战争核心；
- 将HDRP改成默认管线；
- 把Netcode for Entities变成不可替换生产核心；
- 将Wwise或其他音频中间件直接替换FMOD；
- 升级Unity/核心包到预览、实验或新大版本；
- 用第三方插件替代项目自研关键战争算法而不经过架构评审。

## 4. 开发模式

采用**全量并行、接口优先、持续集成、自动闭环**。

- 系统接口和稳定ID从第一天建立；
- 未完成能力必须有可运行占位；
- 每个任务独立分支/worktree；
- 主分支始终可编译、启动、跑基础对局；
- 不允许“以后补测试/优化/文档”作为完成条件；
- 重复任务沿用已验证路径。

## 5. 模块所有权

建议包：

- `com.modernra.core`
- `com.modernra.simulation`
- `com.modernra.combat`
- `com.modernra.ai`
- `com.modernra.navigation`
- `com.modernra.intel-ew`
- `com.modernra.robotics`
- `com.modernra.air`
- `com.modernra.naval`
- `com.modernra.orbital`
- `com.modernra.network`
- `com.modernra.presentation`
- `com.modernra.audio`
- `com.modernra.online`
- `com.modernra.tools`
- `com.modernra.tests`

跨模块修改优先通过接口变更，禁止为方便直接重写其他模块。

## 6. 稳定ID与数据

- 命名只允许 `IDS_NAMING.md` 当前前缀；
- 禁止新增 `FACTION_`、`COUNTRY_`、`BLDG_`、`ABILITY_`、`MISSION_` 旧格式；
- 单位必须显式关联生产建筑、武器、能力和音频配置；
- 代码、地图、战役、音频、测试统一使用稳定ID；
- 国家差异数据化，禁止大量 `if (country == X)`；
- 权威数据源：JSON + Schema + 稳定ID；构建时烘焙为Blob/紧凑数据；
- ScriptableObject只能作为编辑器/资源桥接；
- 平衡修改必须同步规格和测试依据。

## 7. C# / DOTS / Burst

普通作战单位严禁：

- 每单位MonoBehaviour `Update()`；
- 复杂GameObject作为权威状态；
- 热路径LINQ/字符串/反射/GC；
- 无界容器增长；
- 全局锁；
- 每帧大量Entity结构变更。

高频计算应Burst可编译、批处理、可向量化、可拆Job、无托管依赖。

表现层不得反向决定模拟结果。

## 8. 性能最高规则

最低完整体验：

- 现代6核12线程级CPU；
- 16GB双通道；
- RTX 3060/4060/5060、RX7600同级8GB+；
- NVMe SSD；
- 1080P标准画质；
- 现代模式800—1500有效活动实体；
- 常规60FPS目标。

优化顺序：删除无效工作 → 事件/脏区 → 降复杂度 → 空间索引 → 结果共享 → ECS布局 → Burst/向量化 → Job并行 → 减同步/结构变更 → 表现批量化。

严禁为了性能：降低AI、减少同模式单位、降低机器人自主、改伤害/情报/EW、缩地图、降低权威30Hz、把标准画质降成残缺模式、提高最低配置掩盖实现问题。

## 9. AI与机器人

运行时竞技AI使用效用评分、状态机、任务规划和事件驱动，不使用大语言模型实时控制战斗。

固定频率：战略1—2Hz、战区2—5Hz、战斗群5—10Hz、单位10—15Hz、局部避障10—20Hz。

未经玩家授权不得使用终极能力、战略储备、改主科技、拆核心基地、全面撤退、改变总主攻。

机器人受资源、电力、指挥容量和算力约束；失联按自主等级降级。

## 10. 寻路

```text
战略区域图
→ 道路/桥梁/山口图
→ 战斗群共享路径走廊
→ 局部流场
→ 空间哈希局部避障
```

禁止每单位独立全地图A*。地图局部改变只使相关缓存失效。

## 11. 权威战斗物理

自研轻量2.5D几何/弹道为权威；PhysX仅用于非权威残骸、碎片和视觉碰撞。逻辑弹道与视觉弹道允许解耦。

## 12. 网络、安全、录像

正式联网：专用服务器权威模拟。

- Unity Transport为传输层；
- 自研相关性/快照/压缩为复制层；
- 未侦察真实敌军不得发送给无权限客户端；
- 潜艇、隐身、假目标必须服务器过滤；
- 批量增量和量化，禁止逐实体逐帧高层RPC；
- 录像/存档/模组带版本与内容哈希；
- 排位只加载官方模拟数据。

客户端提交的伤害、资源、位置和胜负不得成为权威真相。

## 13. 游戏模式

胜负、占领、前线、战区、快速战争、投降和友军规则统一由 `GAME_MODE_RULES.md` 驱动。禁止地图脚本复制另一套胜负算法。

## 14. 地图

16张地图设计意图以 `MAP_SPECS.md` 为准；每张实际实现必须存在 `/Data/Maps/<MAP_ID>.map.json` 并符合 `MAP_LAYOUT_RUNTIME_SCHEMA.md`。

没有侧车数据的地图最多是 `layout_defined`，不能宣称 `graybox_ready`。

## 15. 战役

24关以 `CAMPAIGN_MISSION_SPECS.md` 为准；缺省初始部队、AI姿态、增援、对白/过场遵循 `CAMPAIGN_NARRATIVE_CAST.md` 的默认契约。

关键脚本必须状态条件+超时兜底；跳过过场不能改变任务结果。

## 16. 音频/语音

所有声音通过 `AudioEventBus -> WarAudioDirector -> FMOD`，游戏逻辑不得直接散落调用FMOD。

AI生成音效/合成语音必须记录生成来源、模型/服务、许可和人工审批。未经授权不得克隆现实人物/演员声线。

## 17. 在线服务与安全

在线结果只接受权威服务器签名；排位/匹配/赛季/灰度/回滚按 `ONLINE_SERVICES_MATCHMAKING_LIVEOPS.md`；认证、秘密、限流、战争迷雾防泄露、反作弊和隐私按 `ONLINE_SECURITY_PRIVACY.md`。

## 18. 完成状态

必须区分：

- `规格COMPLETE`：规则足够实现；
- `placeholder/layout_defined/playable`：仍是开发状态；
- `content_complete`：正式资产和数据齐备；
- `ship_ready`：代码+数据+资产+测试+性能+网络+安全+文档+可玩性全部通过。

禁止把“文档写完”或“占位能跑”冒充正式完成。

## 19. 完成定义

功能只有同时满足以下条件才算对应阶段完成：

- 代码/数据完成；
- 单元/集成测试；
- `TEST_MATRIX.md`相关用例通过；
- 可复现/状态哈希门禁；
- 最低目标机性能无回退；
- 网络/录像需要时可复现；
- 文档同步；
- AI正确使用或明确不可用；
- 有反制与错误降级；
- 涉及资产时来源/许可元数据完整。

## 20. 自动修复

失败 → 最小复现 → 根因定位 → 最小修改 → 回归 → 可复现/性能复测。

修复必须补回归用例。除非证明结构不可维护，否则禁止先重写整个模块。

## 21. 主分支门禁

主分支必须始终：

- Unity批处理编译成功；
- 客户端可启动；
- Dedicated Server可构建；
- 测试地图可加载；
- 无画面模拟可运行；
- 数据无悬空ID；
- 自动测试通过；
- 基础录像/重连可用；
- 性能回归通过。

破坏主线立即回滚并建立独立修复任务。

## 22. 决策原则

新增单位必须回答：解决什么问题、为何现有单位不可替代、玩家能否一眼理解、什么能反制。

新增系统至少满足：增加战略选择、增加爽感、降低微操、强化阵营特色、强化世界辨识度之一，否则删除。

最终原则：**好玩优先于写实，清晰优先于复杂；技术服从冻结栈，性能靠架构/算法解决，任何状态必须如实汇报。**
