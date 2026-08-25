# 工作台完整源码

本目录是工作台的完整可审计源码树，不是只有补丁或改造说明。

- 上游：deepseek-ai/deepseek-harness
- 上游固定提交：b150a551b8d465e31e418e1b2eaf5e79bbb7d28e
- DeepSeek Harness：0.1.1-rc.2
- 工作台版本：workbench-1.0.0
- 工作台改造目录：`.workbench/`

## 目录说明

- Harness 原始完整源码：本目录除 `.workbench/`、`WORKBENCH_SOURCE.md` 与 `WORKBENCH_SOURCE_MANIFEST.json` 外的全部内容。
- 工作台接管预设、技能、MCP/LSP、脚本、文档和发布工作流：`.workbench/`。
- 永久离线运行产物仍由 555 Release 管理；源码树不提交 `node_modules`、缓存和本机构建垃圾。

## 同步规则

根仓库中的工作台脚本、配置、技能、文档或相关工作流变化时，本目录会由 GitHub Actions 自动重建并提交，避免完整源码树与改造层版本漂移。

## 改造定位

当前工作台采用非侵入式覆盖层设计，尽量不修改上游核心文件。这样既完整保存上游源码，又能清晰定位所有工作台差异。完整改造说明见：

- `.workbench/docs/MODIFICATIONS.md`
- `.workbench/docs/CAPABILITIES.md`
- `.workbench/docs/DEPLOYMENT.md`
- `.workbench/docs/TROUBLESHOOTING.md`
