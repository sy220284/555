# 构建、目录与依赖规范

技术选型以 `TECH_STACK.md` 为准。

## 1. 语言与平台

- 主语言：C#；
- 高频模拟：Entities/DOTS + Burst + Job System；
- 客户端：Windows 11 x64首要；
- 专用服务器：Linux x64首要；
- 渲染：Unity 6 URP；
- 传输：Unity Transport；
- 音频：FMOD Studio适配层；
- 原生插件只用于有明确性能/平台理由的边界功能。

## 2. 推荐仓库布局

```text
/Assets
  /GameContent
  /Presentation
  /UI
  /Audio
    /SFX
    /Voice
    /Music
    /Ambience
    /FMOD
  /Maps

/Packages
  /com.modernra.core
  /com.modernra.simulation
  /com.modernra.combat
  /com.modernra.ai
  /com.modernra.navigation
  /com.modernra.intel-ew
  /com.modernra.robotics
  /com.modernra.air
  /com.modernra.naval
  /com.modernra.orbital
  /com.modernra.network
  /com.modernra.presentation
  /com.modernra.audio
  /com.modernra.online
  /com.modernra.tools
  /com.modernra.tests

/Data
  /Units
  /Buildings
  /Weapons
  /Abilities
  /Technology
  /Countries
  /Rulesets
  /Maps
  /Campaign
  /Audio
    audio_registry.json
    voice_registry.json
    music_registry.json
    source_provenance.json

/ProjectSettings
/Packages
/docs
```

Packages内项目包禁止反向依赖 `Assets/GameContent` 的具体场景对象。

## 3. 程序集边界

至少拆分：

- `ModernRA.Core`
- `ModernRA.Simulation`
- `ModernRA.Combat`
- `ModernRA.Navigation`
- `ModernRA.IntelEW`
- `ModernRA.Robotics`
- `ModernRA.AI`
- `ModernRA.Network`
- `ModernRA.Presentation`
- `ModernRA.Audio`
- `ModernRA.Online`
- `ModernRA.Tools`
- `ModernRA.Tests`

依赖方向：

```text
Core/Data
→ Simulation
→ Combat/Navigation/IntelEW/Economy
→ Robotics/AI/Air/Naval/Orbital
→ Network
→ Presentation/Audio/UI/Tools
```

`ModernRA.Online` 只处理客户端在线服务适配/会话，不得进入权威战斗模拟规则。

## 4. 构建目标

至少生成：

- `ModernRA.Client`：正式客户端；
- `ModernRA.Server`：Linux Dedicated Server；
- `ModernRA.SimRunner`：无画面模拟/回归；
- `ModernRA.BalanceRunner`：AI对局/平衡农场；
- `ModernRA.ContentValidator`：Schema/ID/引用验证；
- `ModernRA.ReplayTool`：录像校验与分析；
- `ModernRA.EditorTools`：地图/内容/AI调试工具。

Runner不得加载不必要的表现/音频资源。

## 5. Unity包版本

正式主线只使用生产可用版本。

当前冻结系列：

- Unity 6.3 LTS；
- Entities 1.4；
- Entities Graphics 1.4；
- Burst 1.8；
- Unity Transport正式版；
- Dedicated Server正式版；
- Addressables正式版；
- URP对应正式版；
- FMOD正式生产版本，具体集成版本在Unity 6.3兼容验证后锁定。

预览/实验包默认禁止进入生产依赖。

## 6. 引擎/包升级

升级必须：创建迁移分支 → 固定版本 → 重导关键资产 → 编译/单测 → 客服互通 → 录像/存档 → 最低目标机性能 → 1500实体基准 → 音频集成 → 内容导入 → 无回退后才合并。

AI代理不得自行升级Unity或核心包。

## 7. 数据构建

设计源：JSON + JSON Schema + 稳定ID。

```text
文本源
→ Schema/ID/引用校验
→ 设计规则校验
→ Unity Authoring/Baker
→ BlobAsset/紧凑Native数据
→ 内容包
```

运行时热路径禁止解析JSON。

地图必须从 `/Data/Maps/<MAP_ID>.map.json` 生成规则数据；模式从Rulesets数据生成；音频稳定ID从 `/Data/Audio` 注册表生成强类型句柄。

## 8. 资源构建

采用Addressables管理异步依赖和地图资源。

客户端与服务器资源分离：服务器不得打入高分辨率纹理、FMOD音频库、VFX、复杂动画等无用资源。

关键资源支持：预热、区域流送、内容版本、哈希、增量构建。

## 9. FMOD构建

- FMOD Studio工程与Unity集成版本必须记录；
- Bank按公共/阵营/地图/战役章节分包；
- 音效事件ID由稳定注册表生成/校验；
- 不允许代码引用裸FMOD字符串作为唯一键；
- 服务器构建剔除全部Bank；
- CI检查缺失事件、重复ID和来源元数据。

## 10. 在线后台代码

实时后台服务建议独立 `/Services` 仓库或本仓库明确子目录，使用Go模块隔离：

```text
/Services
  /gateway
  /account
  /party
  /matchmaking
  /session
  /result
  /rating
  /telemetry
```

如果与Unity同仓，必须独立构建和依赖，不允许Unity客户端依赖服务端内部实现。

## 11. 持续集成

主分支合并至少执行：

- 数据/Schema/稳定ID校验；
- C#编译；
- Burst检查；
- EditMode/PlayMode单测；
- 无画面模拟；
- 网络烟测；
- 录像重放；
- 性能烟测；
- 地图侧车检查；
- 音频事件/来源检查；
- 内容引用检查；
- 秘密/依赖漏洞/许可证扫描。

夜间执行AI农场、1500/3000/5000压力、90分钟长稳、网络故障注入、战争迷雾泄露、音频压力和线上服务集成回归。

## 12. 缓存与构建加速

- Unity Accelerator/导入缓存；
- 自托管构建机保留可控Library缓存；
- 缓存按版本/提交哈希隔离；
- 禁止本机临时缓存入库。

## 13. 版本管理

- GitHub为代码、文档、数据主仓库；
- Git LFS管理大型二进制；
- 大文件启用锁；
- AI代理独立分支/worktree；
- Unity `.meta` 必须同步提交；
- 禁止复制GUID。

若SourceArt规模长期压垮Git LFS，可将原始美术源迁移Perforce；运行时资产、代码、数据、文档仍保持可追溯。

## 14. 可复现构建

正式构建必须记录：Unity版本、Package lock、FMOD集成版本、Git SHA、内容哈希、Schema版本、协议版本、规则集版本、Addressables catalog版本、后台服务版本。

同一发布标签必须能够重新生成兼容客户端、服务器和数据包。
