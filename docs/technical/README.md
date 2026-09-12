# 技术文档索引

技术目标：30Hz权威模拟、数据驱动内容、分层AI与导航、数千活动单位、批量网络同步、可复现录像、可扩展编辑器/模组、自动化质量门禁。

性能原则已经冻结为：**正常画质是基础路径；低配性能优先依靠架构、算法、数据结构、批处理和内容预算解决，不把优化成本转嫁为明显低画质、弱AI、少单位或缩水规则。**

主要文档：

- `BUILD_AND_LAYOUT.md`：构建、目录、平台与依赖边界。
- `CODING_STANDARDS.md`：代码、确定性、并发和错误处理规范。
- `PERFORMANCE_ARCHITECTURE.md`：数据导向、任务系统、分层AI/导航、空间索引、弹道、渲染、内存与性能门禁的完整架构规范。
- `HARDWARE_PERFORMANCE_TARGETS.md`：最低/主流/高配目标机、标准画质定义和硬件验收边界。
- `SAVE_REPLAY_MODS_EDITOR.md`：存档、录像、模组和编辑器协议。
- `SECURITY_ANTICHEAT.md`：服务器校验、反作弊和内容完整性。
- `OBSERVABILITY_CI.md`：日志、指标、崩溃、持续集成与性能回归。

相关开发文档：

- `../development/00_ARCHITECTURE.md`
- `../development/03_NETWORK_DETERMINISM.md`
- `../development/04_PERFORMANCE_NAVIGATION.md`

消费者性能基线：

- 最低目标：现代4核8线程、16GB、4GB显存、SSD，1080P标准画质完整运行标准1V1。
- 主流目标：现代6核12线程、16GB、8GB显存、NVMe，1080P/1440P标准高质量路径60FPS。

任何只能依靠提高硬件门槛才能成立的核心功能，默认先按架构、算法或资产预算问题处理。
