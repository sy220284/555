# 实现主干启动状态

本文件记录已经实际进入仓库的实现，不把设计文档冒充代码完成。

## 已落地

- Unity编辑器基线锁到 `6000.3.24f1`；
- 正式非预览依赖写入 `Packages/manifest.json`；
- `ModernRA.Core`：稳定ID类型与前缀校验；
- `ModernRA.Simulation`：DOTS权威状态最小组件与30Hz逻辑时钟；
- `ModernRA.Tests`：稳定ID首批编辑器测试；
- `/Data`：规则集、矿产、通用科技第一批机器可读权威数据；
- `/Data/Schemas`：规则集、矿产、科技JSON Schema；
- `Tools/content_validator.py`：无Unity也可执行的内容门禁；
- GitHub Actions：每次提交自动跑权威数据验证。

## 当前生命周期

工程当前状态只能标记为 `placeholder` / `implementation_bootstrap`，还不是 `playable`。

尚未完成：URP项目资产、基础场景、内容烘焙器、地图灰盒生成器、战斗/导航/AI/EW/网络等正式实现，以及Unity编辑器内完整CI编译验证。

## 下一实现门禁

1. Unity 6000.3.24f1无报错解析包并编译三个程序集；
2. EditMode测试通过；
3. 内容验证器通过；
4. 创建 `MAP_GRAY_RANGE` 最小侧车数据并生成双方初始实体；
5. 无画面运行10000 tick并输出稳定状态哈希；
6. 再开始Combat/Navigation/AI并行实现。

任何后续代理不得绕过这些门禁直接宣称系统完成。
