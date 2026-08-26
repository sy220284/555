# AGENTS.md — 工作台源码维护规则

本仓库是工作台 `1.0.0` 的完整源码，基于 DeepSeek Harness `0.1.1-rc.2` / `b150a551b8d465e31e418e1b2eaf5e79bbb7d28e` 改造。根目录就是正式源码，不存在额外覆盖层。

## 核心约束

- 禁止重新引入 `workbench-src/`、`.workbench/`、`overlay/` 或依赖用户 Home 才能生效的必需配置。
- 默认预设必须保持为 `workbench`；正式配置位于 `apps/cli/config/agent-presets/workbench/`。
- 5 个内置技能必须随源码发布：`workbench-ops`、`repo-review`、`repo-quality-gate`、`docs-quality`、`task-journal`。
- 持久终端、Code Mode、本地文件 MCP、TypeScript/JavaScript 语言服务、SQLite 会话全文索引属于工作台基础能力，修改时必须保持可冷启动。
- `clangd` 是可选外部能力；缺失时不得阻断工作台启动。通过 `DSH_CLANGD_BIN` 显式启用。
- 本地文件 MCP 默认只开放启动工作目录，可通过 `DSH_WORKBENCH_MCP_ROOT` 指定允许根目录。
- 工作台的本地文件、终端、MCP、语言服务、会话和索引能力不得强制依赖模型凭据。

## 源码位置

```text
apps/cli/config/agent-presets/workbench/   默认预设与 5 个技能
apps/cli/config/workbench/                 MCP / TypeScript 语言服务启动器
packages/bundle/web-app/cordis.patch.yml   默认预设、SQLite 索引、本地 MCP
scripts/workbench-doctor.mjs               静态验收
.github/workflows/workbench-ci.yml          构建与冷启动验收
.github/workflows/permanent-toolchain.yml   永久离线产物
README.md                                   项目首页与快速入口
TECHNICAL.md                                唯一完整中文技术文档
```

底层 Harness 继续使用 Cordis 插件化架构。根 `README.md` 只提供项目入口与快速启动；整体架构、能力分层、工具/配置目录、完整包索引、测试策略、Web 客户端约束、历史设计摘要与维护规则统一查阅根 `TECHNICAL.md`。更细的事实以当前源码、类型、配置和测试为准。

历史 `.agents/notes/` 已收敛进根 `TECHNICAL.md` 的“历史决策收敛索引”并从活动树删除。需要逐行考古时使用 Git 历史；普通修改禁止重新建立平行的 Agent Note 文档树。

## 开发与验收

运行时基线：Node.js `22.19.0`、pnpm `11.7.0`。

涉及运行源码、依赖、工作台预设或正式组合的修改，至少执行：

```bash
corepack enable
corepack prepare pnpm@11.7.0 --activate
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
```

随后使用空的 `DSH_HOME` 冷启动 Web，确认 `127.0.0.1:3080` 可访问。修改 MCP、技能发现、语言服务、会话索引或默认预设时，还要验证对应功能真实挂载。

不要用“文件存在”代替功能验收。失败必须定位到真实命令、插件或接口结果。

## 包与代码约定

- 保持 ESM 与现有工作区边界；跨包优先使用正式 package 依赖。
- 新的模型可见行为优先通过插件、服务或正式组合接入，避免把产品逻辑塞进智能体循环。
- 注册类能力必须可释放，后台任务和持久终端必须有明确生命周期。
- 安全边界、文件范围、外部进程和凭据处理必须显式；禁止提交密钥。
- 改动 package 对外行为、配置或限制时，同步更新公开 JSDoc、测试，以及根 `README.md` 中受影响的系统级说明/索引。
- 测试按改动范围选择；工作台关键链最终以 `.github/workflows/workbench-ci.yml` 的完整构建和冷启动为准。

## 文档规则

根 `README.md` 是项目首页，根 `TECHNICAL.md` 是仓库唯一完整技术文档。两者分工固定：前者只维护项目定位、快速启动和文档入口，后者维护系统级技术事实。禁止重新建立 `docs/`、包级 README、双语镜像或 `.agents/notes/` 决策文档树来重复同一事实。

以下 Markdown 属于功能、测试或法律资产，可以独立保留：各作用域 `AGENTS.md` / `CLAUDE.md`、`.agents/skills/**`、运行时 `SKILL.md`、GitHub 模板、测试快照/夹具、`THIRD_PARTY_NOTICES.md`。它们只承载各自程序用途，不扩展为平行技术手册。

`LICENSE` 与 `THIRD_PARTY_NOTICES.md` 属于法律/依赖归属文件；依赖变化时按实际许可证要求更新。

## 提交原则

保持改动聚焦。提交前确认没有旧旁挂架构、没有无关运行产物、没有绝对机器路径进入源码。工作台源码变更完成后，以构建、doctor、冷启动和相关功能实测作为完成标准。
