# 最终技术栈与实现基线

本文件冻结《现代红色警戒》的生产级技术选型。若其他技术文档与本文件冲突，先修订文档再编码。

## 1. 最终结论

采用：

**Unity 6 系列生产平台 + DOTS/Entities 数据骨架 + Burst/Job System 高性能计算 + 项目自研战争核心算法 + Unity Transport 上的自定义大规模权威网络同步。**

目标：2026主流游戏电脑在1080P标准画质下完整运行现代模式；标准800—1500有效活动实体，大型/战区扩展3000—5000。

## 2. 引擎与版本

- 当前生产基线：Unity 6.3 LTS；
- 首要客户端：Windows 11 x64；
- 首要服务器：Linux x64 Dedicated Server；
- 正式上线前12—18个月冻结最终引擎大版本；
- 新LTS只在独立迁移分支验证；
- 禁止AI代理自行升级引擎、核心包、渲染管线或引入预览/实验依赖。

## 3. 主语言与模拟边界

- 主语言：C#；
- 高频模拟：unmanaged/blittable数据优先，Burst可编译；
- 不建立独立C++权威战争核心；
- 原生插件仅限平台/第三方边界且必须有性能或功能证明。

`ModernRA.Simulation` 是唯一战争真相源，禁止引用 Camera、Material、音频、UI、VFX、Animator或表现GameObject。

客户端、专用服务器、无画面测试器和平衡模拟器复用同一套规则代码。

## 4. 大规模实体与计算

正式使用：

- Entities/DOTS 1.4系列；
- Burst 1.8系列；
- Job System。

用于：活动单位、建筑/投射物轻量状态、战区/战斗群数据、批处理和Chunk数据局部性。

严禁普通单位采用每单位复杂MonoBehaviour `Update()`、高频托管对象层级、热路径LINQ/字符串/反射/GC分配。

## 5. 项目自研战争核心

Unity负责编辑器、资产、数据骨架和调度；项目长期掌控：

- 分层AI；
- 分层寻路与战斗群共享路径；
- 统一空间网格/哈希；
- 目标与威胁评估；
- 情报、雷达、战争迷雾；
- 电子战/网络战传播；
- 机器人算力与自主等级；
- 轻量2.5D战斗碰撞/弹道；
- 大规模网络相关性、快照和压缩。

关键战争算法不得被单个不可替换第三方插件绑死。

## 6. AI

运行时竞技AI不使用大语言模型。

固定层级：

```text
战略AI 1—2Hz
→ 战区AI 2—5Hz
→ 战斗群AI 5—10Hz
→ 单位轻量控制 10—15Hz
```

局部避障10—20Hz；紧急事件可即时唤醒。

采用效用评分、状态机、分层任务规划、共享黑板、事件驱动和战斗群结果复用。

生成式AI只用于开发辅助：代码、测试、录像分析、关卡/对白/音频候选、数据检查和内容生产。

## 7. 寻路

```text
战略区域图
→ 道路/桥梁/山口图
→ 战斗群共享路径走廊
→ 局部流场/导航场
→ 空间哈希局部避障
```

禁止每单位独立全地图A*。路径缓存按局部地形版本失效，桥梁/堵塞只更新受影响区域。

## 8. 权威战斗物理

使用项目自研轻量2.5D战斗物理：量化位置/方向、简化占位、射线/高度场、解析弹道、爆炸范围、装甲方向、地形遮挡。

Unity PhysX只用于残骸、碎片、装饰碰撞和非权威视觉反馈。逻辑弹道与视觉弹道解耦。

## 9. 渲染与动画

主渲染：URP，不以HDRP作为标准路径。

表现组合：

- Entities Graphics：中近距大多数单位/建筑；
- BatchRendererGroup/间接实例化：超远单位、蜂群、大批量同构对象；
- 少量GameObject：剧情/特殊单位。

特效：VFX Graph；常规材质：Shader Graph + 必要手写HLSL。

大量步兵/机器人采用共享骨架、动画实例化、顶点动画或GPU友好方案；车辆炮塔/传感器优先程序化。

标准画质保留正常材质、主要动态阴影、炮火、爆炸、导弹、天气、机器人动作和建筑损伤。光追、高精实时GI等仅为高端增强。

## 10. 网络

正式联网：**专用服务器权威模拟**。

```text
客户端命令
→ Unity Transport
→ 服务器验证
→ 30Hz权威模拟
→ 情报/空间/重要度过滤
→ 批量增量快照
→ 客户端插值/有限预测
```

未侦察真实敌军、潜艇、隐身、假目标真相和未公开生产/科技状态不得发送给无权限客户端。

复制层由项目自研，支持：相关性、量化、位打包、基线+增量、动态同步频率、可靠命令/非可靠状态分离和周期关键快照。

Netcode for Entities仅作原型/参考，未经项目基准证明不得成为不可替换生产核心。

## 11. 可复现性

- 权威模拟固定30Hz；
- 随机源显式种子/编号随机流；
- 经济、资源、伤害、科技优先整数/定点/量化；
- 并行任务固定阶段归并；
- 同一服务器构建、同一初始快照、同一命令/事件流应重放到一致关键状态哈希。

不要求客户端纯锁步持有完整隐藏世界。

## 12. 专用服务器

Unity Dedicated Server，Linux x64首要。

服务器不加载高分辨率材质、VFX、音频、客户端动画和UI；只保留模拟、AI、导航、简化碰撞/地图、网络、存档、录像和安全所需内容。

## 13. 数据与内容

权威玩法数据：JSON + JSON Schema + 稳定ID。

构建期验证后烘焙为BlobAsset、Native友好紧凑表或运行时二进制包。正式热路径不解析JSON。

ScriptableObject仅用于编辑器/资产桥接，不作为竞技数值唯一真相源。

## 14. 资源加载

采用Addressables正式版：地图/战区区域流送、关键资源预热、客户端/服务器资源分离、版本/哈希、增量构建。

战斗热路径禁止同步磁盘加载。

## 15. UI

- UI Toolkit：主菜单、设置、科技树、生产、战报、编辑器工具；
- 战场海量标记：自定义批量覆盖层，禁止上千复杂独立UI对象；
- AI指挥、情报、EW、后勤、轨道使用数据图层；
- UI通过只读展示模型读取模拟状态。

## 16. 音频与语音

正式音频中间件：**FMOD Studio + 项目自研 `WarAudioDirector`**。

结构：

```text
AudioEventBus
→ WarAudioDirector
→ FMOD适配层
→ 平台设备
```

使用FMOD负责事件、总线、动态混音、虚拟声道、参数驱动和资源分包；项目自己负责大规模战场声音优先级、区域聚类、远景战争层和语义事件。

游戏代码禁止到处直接调用FMOD。具体资产生成、语音、动态音乐、来源授权和性能预算以 `../specs/AUDIO_VOICE_PIPELINE.md` 为准。

## 17. 后台与服务

在线对局与业务后台解耦。

推荐：

- Go：账号适配、组队、房间、匹配、赛季、赛事、结果和统计接口；
- PostgreSQL：事务数据；
- Valkey/Redis类缓存：会话、临时队列、热点；
- ClickHouse：对局遥测和平衡分析；
- OpenTelemetry：统一遥测；
- Prometheus + Grafana：服务与游戏服监控。

详细服务拓扑、排位、灰度、回滚见 `ONLINE_SERVICES_MATCHMAKING_LIVEOPS.md`；安全/隐私见 `ONLINE_SECURITY_PRIVACY.md`。

## 18. 自动测试与模拟农场

构建目标：

- `ModernRA.Client`
- `ModernRA.Server`
- `ModernRA.SimRunner`
- `ModernRA.BalanceRunner`
- `ModernRA.ContentValidator`
- `ModernRA.ReplayTool`
- `ModernRA.EditorTools`

无画面Runner用于AI对战、平衡、可复现回归、导航/机器人/网络压力和内容验证。

## 19. 版本管理与构建

- GitHub：代码、文档、数据；
- Git LFS：大型二进制；
- AI代理：独立分支/worktree；
- GitHub Actions + Windows/Linux自托管构建机；
- Unity命令行批处理构建；
- Unity Accelerator/缓存降低重复导入成本。

若未来原始美术源文件规模导致Git LFS长期成为瓶颈，可把 `SourceArt` 独立迁移Perforce；运行时Unity资产、代码、数据和文档仍保持可追溯。

## 20. 正式依赖原则

当前核心系列：

- Unity 6.3 LTS；
- Entities 1.4；
- Entities Graphics 1.4；
- Burst 1.8；
- Unity Transport正式版；
- Dedicated Server正式版；
- Addressables正式版；
- URP对应正式版；
- FMOD正式生产版本（具体版本在Unity 6.3项目兼容验证后写入package/集成锁文件）。

实验/预览包默认禁止进入生产主线。

## 21. 明确不采用

正式核心不采用：

- Unreal/Mass作为主平台；
- HDRP作为标准基础管线；
- 独立C++权威战争核心；
- 每单位MonoBehaviour/Update；
- 每单位独立全图寻路；
- PhysX刚体作为权威伤害真相源；
- 大语言模型实时控制竞技战斗；
- 纯客户端锁步作为联网安全基础；
- 依靠提高最低硬件或明显降低标准画质解决核心性能问题。

## 22. 最终架构

```text
Unity 6
├─ 编辑器 / 资产 / 平台
├─ URP / VFX / Shader
├─ UI / Addressables
├─ FMOD + WarAudioDirector
│
├─ DOTS / Entities
├─ Burst + Job System
│
├─ ModernRA.Simulation
│  ├─ Combat
│  ├─ AI
│  ├─ Navigation
│  ├─ Spatial
│  ├─ Intel/EW
│  ├─ Robotics
│  └─ Economy/Logistics
│
├─ Presentation
│  ├─ Entities Graphics
│  ├─ GPU批量实例
│  └─ 少量特殊GameObject
│
└─ Network
   ├─ Unity Transport
   ├─ 自研相关性/快照/压缩
   ├─ 客户端插值
   └─ Linux权威服务器
```

## 23. 技术验收

必须同时成立：

- 最低目标机：6核12线程、16GB、8GB显存、NVMe，1080P标准画质；
- 标准现代对局：800—1500有效活动实体，完整AI/机器人/EW/情报；
- 常规60FPS目标，高密度瞬时交战保持可操作；
- 战区扩展3000—5000+实体；
- 专用服务器无渲染运行完整权威模拟；
- 低配与高配游戏规则一致；
- AI、导航、网络、渲染、音频均有量化性能门禁；
- 线上服务、安全、录像和重连可测试、可回滚。

达不到目标时优先优化架构、算法、数据布局、批处理和资产预算，不先提高最低配置。
