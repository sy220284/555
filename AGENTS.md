# AGENTS.md — 工作台源码维护规则

本仓库是工作台 `1.0.0` 的完整源码，基于 DeepSeek Harness `0.1.1-rc.2` / `b150a551b8d465e31e418e1b2eaf5e79bbb7d28e` 改造。根目录就是正式源码，不存在额外覆盖层。

## 核心约束

- 禁止重新引入 `workbench-src/`、`.workbench/`、`overlay/` 或依赖用户 Home 才能生效的必需配置。
- 默认预设必须保持为 `workbench`；正式配置位于 `apps/cli/config/agent-presets/workbench/`。
- 5 个内置技能必须随源码发布：`workbench-ops`、`repo-review`、`repo-quality-gate`、`docs-quality`、`task-journal`。
- 持久终端、Code Mode、本地文件 MCP、TypeScript/JavaScript LSP、SQLite 会话全文索引属于工作台基础能力，修改时必须保持可冷启动。
- `clangd` 是可选外部能力；缺失时不得阻断工作台启动。通过 `DSH_CLANGD_BIN` 显式启用。
- 本地文件 MCP 默认只开放启动工作目录，可通过 `DSH_WORKBENCH_MCP_ROOT` 指定允许根目录。
- 工作台的本地文件、终端、MCP、LSP、会话和索引能力不得强制依赖模型凭据。

## 源码位置

```text
apps/cli/config/agent-presets/workbench/   默认预设与 5 个技能
apps/cli/config/workbench/                 MCP / TypeScript 语言服务启动器
packages/bundle/web-app/cordis.patch.yml   默认预设、SQLite 索引、本地 MCP
scripts/workbench-doctor.mjs               静态验收
.github/workflows/workbench-ci.yml          构建与冷启动验收
.github/workflows/permanent-toolchain.yml   永久离线产物
README.md                                   工作台主文档
```

底层 Harness 架构仍遵循插件化与 Cordis 组合机制。修改核心包前可参考 `docs/architecture.md`、`docs/development.md`、`docs/testing.md`、`docs/subsystems/` 与各 package README；这些文档只作为当前源码的技术参考，代码与实际组合配置是最终事实来源。

`.agents/notes/` 保留上游已经形成的设计决策与历史依据，因为当前技术文档和质量门禁仍引用它们；它们是参考资料，不是工作台新增功能的强制产物。普通工作台修改无需为了流程形式额外创建 Agent Note。

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

随后使用空的 `DSH_HOME` 冷启动 Web，确认 `127.0.0.1:3080` 可访问。修改 MCP、技能发现、LSP、会话索引或默认预设时，还要验证对应功能真实挂载。

不要用“文件存在”代替功能验收。失败必须定位到真实命令、插件或接口结果。

## 包与代码约定

- 保持 ESM 与现有 workspace 边界；跨包优先使用正式 package 依赖。
- 新的模型可见行为优先通过插件、服务或正式组合接入，避免把产品逻辑塞进 agent loop。
- 注册类能力必须可释放，后台任务和持久终端必须有明确生命周期。
- 安全边界、文件范围、外部进程和凭据处理必须显式；禁止提交密钥。
- 改动 package 对外行为、配置或限制时，同步更新对应 package README/JSDoc。
- 测试按改动范围选择；工作台关键链最终以 `workbench-ci.yml` 的完整构建和冷启动为准。

## 文档规则

根 `README.md` 是工作台用户与维护者的主入口，`docs/workbench.zh.md` 是简版技术说明。`docs/` 中保留的 Harness 文档用于理解仍存在的底层架构、配置、工具和子系统；与当前源码冲突时，以源码和工作台主文档为准。

仓库已经清理上游品牌规范、旧贡献流程、旧 README 翻译对和旧基准说明。现有双语技术文档及 `docs/i18n/` 仍由当前文档门禁使用，因此保留；工作台新增主文档不要求复制上游 README 的旧翻译结构。

`LICENSE` 与 `THIRD_PARTY_NOTICES.md` 属于法律/依赖归属文件，不作为普通文档清理对象；依赖变化时按实际许可证要求更新。

## 提交原则

保持改动聚焦。提交前确认没有旧旁挂架构、没有无关运行产物、没有绝对机器路径进入源码。工作台源码变更完成后，以构建、doctor、冷启动和相关功能实测作为完成标准。