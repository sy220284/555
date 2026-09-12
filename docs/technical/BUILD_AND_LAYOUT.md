# 构建、目录与依赖规范

技术选型以 `TECH_STACK.md` 为准。

## 1. 语言与平台

- 主语言：C#；
- 高频模拟：Entities/DOTS + Burst + Job System；
- 客户端：Windows 11 x64 首要；
- 专用服务器：Linux x64 首要；
- 渲染：Unity 6 URP；
- 传输：Unity Transport；
- 原生插件只允许用于有明确性能/平台理由的边界功能。

## 2. 推荐仓库布局

```text
/Assets
  /GameContent
  /Presentation
  /UI
  /Audio
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
  /com.modernra.tools
  /com.modernra.tests

/Data
  /Units
  /Buildings
  /Weapons
  /Technology
  /Countries
  /Maps
  /Campaign

/ProjectSettings
/Packages
/docs
```

Packages 内项目包禁止反向依赖 `Assets/GameContent` 的具体场景对象。

## 3. 程序集边界

至少拆分：

- `ModernRA.Core`；
- `ModernRA.Simulation`；
- `ModernRA.Combat`；
- `ModernRA.Navigation`；
- `ModernRA.IntelEW`；
- `ModernRA.Robotics`；
- `ModernRA.AI`；
- `ModernRA.Network`；
- `ModernRA.Presentation`；
- `ModernRA.Tools`；
- `ModernRA.Tests`。

依赖方向：

```text
Core/Data
→ Simulation
→ Combat/Navigation/IntelEW/Economy
→ Robotics/AI/Air/Naval/Orbital
→ Network
→ Presentation/UI/Tools
```

禁止低层引用高层。

## 4. 构建目标

至少生成：

- `ModernRA.Client`：正式客户端；
- `ModernRA.Server`：Linux Dedicated Server；
- `ModernRA.SimRunner`：无画面模拟/回归；
- `ModernRA.BalanceRunner`：AI 对局/平衡农场；
- `ModernRA.ContentValidator`：数据与稳定 ID 验证；
- `ModernRA.ReplayTool`：录像校验与分析；
- `ModernRA.EditorTools`：地图/内容/AI 调试工具。

Runner 可以使用 Unity 无画面批处理环境，但不得加载不必要的表现资源。

## 5. Unity 包版本

正式主线只使用生产可用版本。

当前冻结系列：

- Unity 6.3 LTS；
- Entities 1.4；
- Entities Graphics 1.4；
- Burst 1.8；
- Unity Transport 正式版；
- Dedicated Server 正式版；
- Addressables 正式版；
- URP 对应正式版。

预览/实验包默认禁止进入生产依赖。

## 6. 引擎/包升级

升级必须：

1. 创建独立迁移分支；
2. 固定新的编辑器/包版本；
3. 重新导入全部关键资产；
4. 跑编译和单元测试；
5. 跑服务器/客户端互通；
6. 跑录像/存档兼容；
7. 跑最低目标机性能；
8. 跑 1500 实体战争基准；
9. 验证结果优于或不劣于旧版本；
10. 才允许合并。

AI 代理不得自行升级 Unity 或核心包。

## 7. 数据构建

设计源：JSON + JSON Schema + 稳定 ID。

构建流程：

```text
文本源
→ Schema/ID/引用校验
→ 设计规则校验
→ Unity Authoring/Baker
→ BlobAsset/紧凑Native数据
→ 内容包
```

运行时热路径禁止解析 JSON。

## 8. 资源构建

采用 Addressables 管理异步依赖和地图资源。

客户端与服务器内容必须拆分：服务器不得打入高分辨率纹理、音频、VFX、复杂动画等无用资源。

关键资源支持：

- 预热；
- 区域流送；
- 内容版本；
- 哈希；
- 增量构建。

## 9. 持续集成

使用 GitHub Actions + Windows/Linux 自托管构建机。

主分支合并至少执行：

- 数据/Schema 校验；
- C# 编译；
- Burst 编译检查；
- EditMode/PlayMode 单测；
- 无画面模拟回归；
- 网络烟测；
- 录像重放；
- 性能烟测；
- 内容引用检查。

夜间执行：

- 大量 AI 对 AI；
- 1500/3000 实体压力；
- 长局内存/GC；
- 网络丢包/抖动；
- 战争迷雾泄露；
- 客户端/服务器内容差异验证。

## 10. 缓存与构建加速

- 使用 Unity Accelerator/导入缓存；
- 自托管构建机保留可控 Library 缓存；
- 依赖和 Library 缓存以版本/提交哈希隔离；
- 禁止把本机临时缓存提交仓库。

## 11. 版本管理

- GitHub 为代码、文档、数据主仓库；
- Git LFS 管理大型二进制；
- 大文件启用锁；
- AI 代理独立分支/worktree；
- Unity `.meta` 必须与资产同步提交；
- 禁止复制 GUID 造成资源冲突。

若未来原始 DCC SourceArt 规模导致 Git LFS 锁与仓库性能持续成为瓶颈，可把原始美术源文件独立迁移 Perforce；运行时 Unity 资产、代码、数据、文档仍保持可追溯版本。

## 12. 可复现构建

正式构建必须记录：

- Unity 编辑器版本；
- Package lock；
- Git 提交 SHA；
- 内容哈希；
- Schema 版本；
- 构建配置；
- 服务器协议版本；
- Addressables catalog 版本。

同一发布标签必须能够重新生成兼容客户端/服务器和数据包。