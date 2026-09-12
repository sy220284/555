# 技术文档索引

技术目标：30Hz权威模拟、Unity 6/DOTS 数据骨架、分层AI与导航、数千活动实体、服务器权威批量网络同步、可复现录像、可扩展编辑器/模组、自动化质量门禁。

性能原则：**标准画质就是基础产品路径；最低完整体验直接锚定2026主流游戏电脑，性能优先依靠架构、算法、数据结构、Burst批处理、并行和内容预算解决。**

## 上位技术文档

- `TECH_STACK.md`：**冻结技术栈**。Unity 6、C#、DOTS/Entities、Burst、URP、Unity Transport、自研战争核心算法、专用服务器及后台技术边界。
- `PERFORMANCE_ARCHITECTURE.md`：ECS数据布局、Burst/Job、多速率模拟、AI/导航/空间索引、战斗物理、渲染、网络和性能门禁。
- `HARDWARE_PERFORMANCE_TARGETS.md`：最低/推荐/高端硬件与标准画质验收。

## 工程文档

- `BUILD_AND_LAYOUT.md`：Unity package-first目录、程序集、构建目标、依赖和持续集成。
- `CODING_STANDARDS.md`：C#/DOTS/Burst编码、GC、网络、AI和测试规范。
- `SAVE_REPLAY_MODS_EDITOR.md`：存档、录像、模组和编辑器协议。
- `SECURITY_ANTICHEAT.md`：服务器权威、反作弊和内容完整性。
- `OBSERVABILITY_CI.md`：日志、指标、崩溃、持续集成与性能回归。

## 相关开发文档

- `../development/00_ARCHITECTURE.md`
- `../development/03_NETWORK_DETERMINISM.md`
- `../development/04_PERFORMANCE_NAVIGATION.md`

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
+ 自研大规模RTS相关性/快照复制
+ Linux Dedicated Server
+ Addressables
```

核心算法由项目自研：AI、寻路、空间索引、2.5D权威战斗、情报/EW、机器人、网络相关性和状态压缩。

## 消费者性能基线

最低完整体验：

- 6核12线程级 CPU（Ryzen 5 5600 / i5-12400F级或更新同级）；
- 16GB双通道内存；
- RTX 3060/4060/5060、RX7600同级8GB+显存；
- NVMe SSD；
- 1080P标准画质；
- 800—1500有效活动实体现代模式；
- 常规60FPS目标。

高端硬件负责提高分辨率、刷新率、视觉采样精度和战区娱乐规模，不负责补齐基础功能。

任何只能依靠提高硬件门槛、削弱AI、减少同模式单位或明显降低标准画质才能成立的核心能力，默认按架构/算法/代码/资产预算问题处理。