# AGENTS.md — Packages

本文件补充根目录 `AGENTS.md`，适用于 `packages/`。

## 包边界

- 保持现有插件化结构和 Service Definition / Provider / Consumer 分工；只有职责确实独立时才拆包。
- 服务包按现有导出约定提供实现；函数插件保持命名导出并与 Loader 约定一致。
- 跨包依赖使用正式 workspace package 名称；不要依赖别的包的私有文件路径。
- 配置、协议和安全约束在最早可确定的位置失败，禁止静默跳过缺失依赖或无效配置。
- 注册、监听和运行时贡献必须有对应释放路径；生命周期、后台任务和进程资源必须能收口。
- 模型可见内容、工具结果、持久状态和用户可见状态应有明确的权威来源，避免多套并行状态。

## 测试

修改插件组合、Loader 行为或产品可见能力时，不能只依赖手工 `ctx.plugin(...)` 单元测试；至少增加或运行覆盖真实组合的测试。底层测试策略参考 `../docs/testing.md`。

工作台相关包变更最终还必须通过根目录：

```bash
pnpm run build
pnpm run workbench:doctor
```

涉及启动组合时，再以空 `DSH_HOME` 冷启动验证。

## 文档

Package README 和 JSDoc 只描述当前契约、配置、失败方式、限制和扩展点。修改这些内容时同步更新，不记录评审历史、迁移过程或已经删除的 Agent Note。

可参考：

- `../docs/architecture.md`：整体架构
- `../docs/development.md`：源码开发约定
- `../docs/testing.md`：测试策略
- `../docs/subsystems/`：底层子系统
- `../README.md`：工作台最终定位与使用方式

`docs/postmortem/` 中仍保留的内容仅作为底层历史故障案例，不是当前规范来源；与现有源码冲突时以源码和当前文档为准。