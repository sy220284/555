# 实现主干启动状态

本文件只记录已经实际进入仓库的实现，不把设计文档或接口空壳冒充可玩功能。

## 已实际入库

### 工程与依赖

- Unity编辑器基线：`6000.3.24f1`；
- 正式依赖写入 `Packages/manifest.json`：Entities、Entities Graphics、Burst、Transport、Addressables、性能测试包；
- `.gitignore` 已加入Unity生成目录规则。

### 代码包

- `ModernRA.Core`：稳定ID与前缀校验；
- `ModernRA.Simulation`：权威热状态、30Hz常量、固定步进组30Hz配置；
- `ModernRA.Combat`：装甲、武器、伤害事件契约；
- `ModernRA.Navigation`：导航层、路径走廊、导航目标、局部避障契约；
- `ModernRA.AI`：AI授权等级、禁令、更新节拍、玩家覆盖代次；
- `ModernRA.IntelEW`：六级情报、四级EW、目标签名；
- `ModernRA.Robotics`：A1—A4自主、链路、算力分配；
- `ModernRA.Network`：命令包头、快照包头、复制可见性；
- `ModernRA.Tests`：稳定ID、30Hz、自主等级等首批编辑器测试。

### 权威机器数据

- `/Data/Rulesets/rulesets.json`：五种标准规则集；
- `/Data/Minerals/minerals.json`：7类矿产/回收资源；
- `/Data/Technology/common_technology.json`：首批通用科技与征服材料绑定；
- `/Data/Maps/MAP_GRAY_RANGE.map.json`：首张地图侧车，当前仅为机器布局数据，尚未达到 `graybox_ready`；
- `/Data/Schemas/`：rulesets、minerals、technology、map首批Schema。

### 自动门禁

- `Tools/content_validator.py`：检查稳定ID、旧前缀、规则集、矿产、征服材料绑定密度、地图引用/资源/区域一致性；
- `.github/workflows/content-validation.yml`：PR和主分支自动数据门禁。

## 当前生命周期

项目整体状态：**`implementation_bootstrap`**。

部分模块达到 `code_contract_ready`，首批数据达到 `data_defined`；仍然没有任何核心模式达到 `playable`。

## 已知尚未完成

- Unity 6000.3.24f1真实打开、包解析和全程序集编译；
- Unity生成 `.meta` 的正式整理与提交；
- URP项目资产/渲染设置；
- FMOD实际集成包与工程；
- 数据烘焙器/Blob生成；
- `ModernRA.SimRunner` 与10000 tick状态哈希；
- `MAP_GRAY_RANGE`灰盒生成器与实体出生；
- 资源采集、移动、战斗、感知、AI、机器人、网络的完整系统实现；
- Dedicated Server实际构建；
- 正式表现/音频/内容资产。

## 下一门禁

必须按顺序闭环：

1. 真实Unity编辑器解析项目并编译全部现有程序集；
2. EditMode测试全部通过；
3. 内容验证器通过；
4. 建立无画面 `SimRunner`，相同种子/命令运行10000 tick得到一致状态哈希；
5. 根据 `MAP_GRAY_RANGE.map.json` 生成最小灰盒和双方初始实体；
6. 完成基础矿产采集/资源入账；
7. 完成共享导航和移动；
8. 完成第一种武器/伤害闭环；
9. 再逐层接入情报、AI、机器人/EW和网络。

任何后续代理不得跨过这些门禁直接宣称“项目可玩”或开始大规模正式资产生产。
