# 构建、目录与依赖规范

## 语言与平台

核心模拟建议 C++。渲染后端优先 DirectX 12 / Vulkan 抽象；平台层不得渗入 Simulation。

## 构建目标

至少拆分：`GameClient`、`DedicatedServer`、`SimulationTests`、`ContentValidator`、`MapEditor`、`ReplayTool`、`BalanceRunner`。

## 依赖方向

低层：Core/Math/Serialization → Simulation → Gameplay/Combat/Navigation/Intel → AI/Network → Presentation/Tools。禁止低层引用高层。

## 第三方依赖

所有依赖固定版本、记录许可证、哈希和升级原因。模拟关键依赖升级必须跑确定性基线。

## 构建可复现

CI 固定编译器/SDK 版本；发布构建保存完整依赖清单、内容哈希、提交 SHA 和数据 schema 版本。

## 目录规则

代码、数据、测试同模块对应；禁止把所有配置堆入一个巨型文件。正式内容与测试占位内容使用不同命名空间和打包规则。