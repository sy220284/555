# 工作台完整源码

本目录是工作台的完整可审计源码树。

- 上游：deepseek-ai/deepseek-harness
- 上游固定提交：b150a551b8d465e31e418e1b2eaf5e79bbb7d28e
- DeepSeek Harness：0.1.1-rc.2
- 工作台版本：workbench-1.0.0
- 工作台改造目录：`.workbench/`

## 目录说明

- Harness 原始完整源码：本目录除 `.workbench/` 与本文件外的全部内容。
- 工作台接管预设、技能、MCP/LSP、脚本、文档：`.workbench/`。
- 永久离线运行产物仍由 555 Release 管理；源码树不提交 `node_modules`、缓存和本机构建垃圾。

## 改造原则

当前工作台采用非侵入式覆盖层设计，尽量不修改上游核心文件。这样既保留完整上游源码，又能清晰定位所有工作台差异。完整改造说明见：

- `.workbench/docs/MODIFICATIONS.md`
- `.workbench/docs/CAPABILITIES.md`
- `.workbench/docs/DEPLOYMENT.md`
- `.workbench/docs/TROUBLESHOOTING.md`
