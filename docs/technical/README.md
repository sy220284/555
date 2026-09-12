# 技术文档索引

技术目标：30Hz服务器权威模拟、Unity 6/DOTS数据骨架、分层AI与导航、数千活动实体、权威批量网络同步、FMOD大规模战场音频、可复现录像、可扩展编辑器/模组、在线服务、安全与自动化门禁。

性能原则：**标准画质就是基础产品路径；最低完整体验锚定2026主流游戏电脑，性能优先依靠架构、算法、数据结构、Burst批处理、并行和内容预算解决。**

## 上位技术文档

- `TECH_STACK.md`：冻结技术栈。Unity 6、C#、DOTS/Entities、Burst、URP、Unity Transport、自研战争核心、FMOD、专用服务器与后台边界。
- `PERFORMANCE_ARCHITECTURE.md`：ECS数据布局、Burst/Job、多速率模拟、AI/导航/空间索引、战斗物理、渲染、网络和性能门禁。
- `HARDWARE_PERFORMANCE_TARGETS.md`：最低/推荐/高端硬件与标准画质验收。

## 工程与线上

- `BUILD_AND_LAYOUT.md`：目录、程序集、构建目标、依赖和CI。
- `CODING_STANDARDS.md`：C#/DOTS/Burst编码规范。
- `ONLINE_SERVICES_MATCHMAKING_LIVEOPS.md`：账号、组队、匹配、排位、赛季、赛事、灰度和回滚。
- `ONLINE_SECURITY_PRIVACY.md`：认证、传输、反作弊、秘密、供应链、隐私和事故响应。
- `SAVE_REPLAY_MODS_EDITOR.md`：存档、录像、模组、编辑器。
- `SECURITY_ANTICHEAT.md`：安全摘要，详细执行以ONLINE_SECURITY_PRIVACY为准。
- `OBSERVABILITY_CI.md`：日志、指标、持续集成、性能和线上健康门禁。

## 相关执行规格

- `../specs/AUDIO_VOICE_PIPELINE.md`：FMOD + WarAudioDirector、音效/语音生成与来源追溯。
- `../specs/GAME_MODE_RULES.md`：五种游戏模式执行规则。
- `../specs/MAP_LAYOUT_RUNTIME_SCHEMA.md`：地图运行时数据与灰盒生成契约。
- `../specs/SAVE_NETWORK_MOD_EDITOR_SCHEMA.md`：网络/重连/录像数据协议。
- `../specs/TEST_MATRIX.md`：完整自动测试门禁。

## 当前冻结技术基线

```text
Unity 6.3 LTS
+ C#
+ Entities/DOTS 1.4
+ Burst 1.8
+ Job System
+ URP
+ Entities Graphics + GPU批量实例
+ Unity Transport
+ 自研RTS相关性/快照/压缩
+ Linux Dedicated Server
+ Addressables
+ FMOD Studio + WarAudioDirector
+ Go / PostgreSQL / Valkey或Redis类缓存 / ClickHouse
```

核心战争算法由项目自研：AI、寻路、空间索引、2.5D权威战斗、情报/EW、机器人、网络相关性和状态压缩。

## 消费者性能基线

最低完整体验：

- 6核12线程级CPU（Ryzen 5 5600 / i5-12400F级或更新同级）；
- 16GB双通道；
- RTX 3060/4060/5060、RX7600同级8GB+；
- NVMe SSD；
- 1080P标准画质；
- 800—1500有效活动实体现代模式；
- 常规60FPS目标。

任何只能依靠提高硬件门槛、削弱AI、减少同模式单位或明显降低标准画质才能成立的核心能力，默认按架构/算法/代码/资产预算问题处理。
