# 工作台

本仓库是 DeepSeek Harness `0.1.1-rc.2` 的源码内生工作台版本。所有工作台能力直接进入正式源码目录，不依赖 `.workbench`、`overlay` 或用户 Home 覆盖层。

## 原生改造位置

- `apps/cli/config/agent-presets/workbench/`：默认工作台预设与 5 个内置技能。
- `apps/cli/config/workbench/`：TypeScript 语言服务器和本地 MCP 启动器。
- `apps/cli/package.json`：工作台运行依赖。
- `packages/bundle/web-app/cordis.patch.yml`：默认预设、持久全文索引与本地文件 MCP。
- `scripts/workbench-doctor.mjs`：源码/构建静态验收。
- `.github/workflows/workbench-ci.yml`：冷启动和功能链验收。
- `.github/workflows/permanent-toolchain.yml`：直接构建本仓库源码的永久离线产物。

## 默认能力

工作台预设包含原生文件与 Bash、后台任务、持久终端、Code Mode (`both`)、5 个技能、TypeScript/JavaScript 与 C/C++ LSP、目标/计划/TODO、子智能体和工作流入口。Web Host 默认启用持久 SQLite 会话全文索引与 `localfs` MCP。

本地文件 MCP 默认只开放启动工作目录；可通过 `DSH_WORKBENCH_MCP_ROOT` 指定允许根目录。`clangd` 为可选外部依赖；仅在设置 `DSH_CLANGD_BIN` 时启用 C/C++ 语言服务，缺少它不会阻断工作台启动。

## 使用

```bash
corepack enable
corepack prepare pnpm@11.7.0 --activate
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
DSH_WORKBENCH_MCP_ROOT="$PWD" pnpm run workbench:web
```

默认地址：`http://127.0.0.1:3080`。

工作台的本地执行、文件、终端、LSP、MCP、会话和索引能力不要求模型凭据；如果直接在 Harness 网页中让 Harness 自主生成模型回复，仍需另行配置模型提供方。
