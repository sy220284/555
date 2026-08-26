# 555 工作台完整技术文档

> 本文件是仓库唯一的人工维护主技术文档。仓库原有分散的架构说明、子系统说明、包说明、用户指南、开发手册、双语文档与历史 Agent Note 已全部读取、去重并收敛到这里。
> 运行时技能定义、测试快照/夹具以及第三方许可证通知属于**可执行或法律资产**，仍以原文件保留，不再视为独立技术文档。

## 0. 当前基线与文档收敛范围

- 工作台版本：`1.0.0`
- 上游 DeepSeek Harness：`0.1.1-rc.2`
- 上游基线提交：`b150a551b8d465e31e418e1b2eaf5e79bbb7d28e`
- 默认预设：`workbench`
- 架构形态：`native-source-integrated`
- Node.js：`^22.19.0 || >=24.0.0`
- 包管理器：`pnpm@11.7.0`
- 仓库自有 Markdown 全量扫描：**2495 份**
- 合并后清理的分散人工文档：**2310 份**
- 保留的 Markdown 功能/测试/法律资产（含本文件）：**185 份**

这次收敛覆盖根目录、`.agents/notes/`、`.agents/skills/`、`docs/`、`packages/`、`apps/`、`examples/`、`native/`、`python/`、`.github/`、`scripts/` 与 `website/` 中的仓库自有 Markdown。`vendor/`、`node_modules/` 等第三方或安装产物不属于本仓库技术文档治理范围。

### 0.1 文档治理原则

1. **当前事实优先。** 运行代码、配置、类型与测试是事实源；本文负责解释整体结构、公开契约和维护路径。
2. **单点说明。** 同一个概念只在本文维护一次，不再恢复中英文双份 README、子系统页、包 README 与决策页组成的多层文档树。
3. **历史决策不丢。** 原 Agent Note 的题名、状态和核心决定压缩到本文“历史决策收敛索引”；需要逐行考古时使用 Git 历史。
4. **可执行 Markdown 不删。** `SKILL.md`、测试快照/夹具中的 `.md` 是程序输入或测试预期，不属于说明文档。
5. **法律文件独立。** `THIRD_PARTY_NOTICES.md` 与 `LICENSE` 保持原样。

---

## 1. 项目定位

`555` 是在 DeepSeek Harness `0.1.1-rc.2` 完整源码上直接改造的本地执行工作台。工作台能力已经进入正式源码树，不依赖 `workbench-src/`、`.workbench/`、`overlay/` 或用户 Home 中的必需覆盖配置。

它的核心目标是把“模型推理”与“本地执行底座”解耦：即使没有模型密钥，工作区、会话、文件、Shell、持久终端、语言服务、模型上下文协议服务、SQLite 索引、插件装载、构建与自检仍可工作；配置模型后，再由同一底座承担自主推理、工具调用、子智能体与工作流。

```text
555/
├─ apps/                 应用入口：命令行与 Web
├─ packages/             227 个工作区包，按能力族分组
├─ native/               本地原生能力，主要是 Linux Landlock
├─ python/               Python 开发工具包与运行时封装
├─ vendor/               固定上游/第三方源码
├─ scripts/              构建、校验、生成与维护脚本
├─ website/              文档站代码（主内容已收敛到本文件）
├─ .agents/skills/       仓库维护技能
├─ .github/workflows/    正式自动化工作流
├─ package.json
└─ README.md             当前唯一人工维护主技术文档
```

### 1.1 工作台新增与强化能力

```text
工作台
├─ 工作区与会话
│  ├─ 工作区持久记录
│  ├─ 会话事件日志、查询、全文索引、分叉
│  ├─ 附件与会话引用
│  └─ 标题、统计、投影与反馈
├─ 本地执行
│  ├─ Bash / PowerShell
│  ├─ 文件读写编辑与搜索
│  ├─ 后台任务
│  ├─ 持久 PTY 终端
│  └─ Code Mode / run_code
├─ 代码理解
│  ├─ TypeScript / JavaScript 语言服务
│  └─ 可选 C / C++ clangd
├─ 扩展接入
│  ├─ MCP
│  ├─ Cordis 插件
│  ├─ Typert 远程调用
│  └─ ACP / SDK
├─ 智能体编排
│  ├─ Goal / Plan / TODO
│  ├─ 子智能体
│  ├─ Workflow
│  ├─ Ralph
│  └─ 计划任务
└─ 工程保障
   ├─ workbench:doctor
   ├─ 构建与冷启动
   ├─ 测试快照
   └─ 永久离线产物
```

---

## 2. 总体架构

### 2.1 Cordis 是运行时骨架

Harness 的插件组合、服务依赖、事件与生命周期由 Cordis 承担。一个插件通常通过 `apply(ctx, config)`、对象形式或 `Service` 类挂载；`inject` 声明依赖，依赖未就绪时插件保持等待，依赖恢复时重新激活。`ctx.effect()` 绑定副作用与清理，使资源归属与插件生命周期一致。

核心约定：

- **服务**：长期能力入口，例如 `ctx.tools`、`ctx.sessions`、`ctx.subagents`、`ctx.storage`。
- **事件**：跨包的一次性通知或可改写流水线；普通通知使用 emit，策略链常使用 waterfall。
- **Fiber/作用域**：决定插件实例、依赖注入、卸载与热重载的边界。
- **注册表**：多实现或多项能力通过注册表聚合，例如模型适配器、子智能体提供方、技能提供方。

### 2.2 能力 seam

大量可替换能力采用三层结构：

```text
服务定义 → 提供方实现 → 消费方/工具
```

例如：

```text
dsh-fs → dsh-fs-local / dsh-fs-sandbox → dsh-tool-fs
dsh-web → Exa / Perplexity / DeepSeek / HTTP Fetch → dsh-tool-web
dsh-code-runtime → worker-thread / Python → run_code
dsh-storage → JSON / SQLite → storage-domain → workspace 等业务包
dsh-subprocess → local / e2b → shell / LSP / PTY / ACP
```

这样替换后端时，模型看到的工具协议与上层业务不需要一起改变。

### 2.3 Profile 与组合包

启动组合由 Profile 管理。Profile 的 `dsh.profile.bundles` 按顺序叠加组合包补丁，之后再叠加 Profile 自己的 `cordis.patch.yml`、Home 级补丁和命令行 `--patch`。

正式组合：

- `@deepseek-ai/dsh-base`：共享核心能力。
- `@deepseek-ai/dsh-headless`：一次性无界面任务。
- `@deepseek-ai/dsh-web-app`：浏览器工作台。

工作台把默认预设设为 `workbench`，正式配置位于：

```text
apps/cli/config/agent-presets/workbench/
packages/bundle/web-app/cordis.patch.yml
```

---

## 3. 启动、安装与运行

### 3.1 环境

推荐固定环境：

```text
Node.js 22.19.0
pnpm 11.7.0
Linux x64
```

根包允许 Node.js `^22.19.0 || >=24.0.0`。

### 3.2 从源码启动

```bash
git clone https://github.com/sy220284/555.git
cd 555
corepack enable
corepack prepare pnpm@11.7.0 --activate
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
DSH_WORKBENCH_MCP_ROOT="$PWD" pnpm run workbench:web
```

默认 Web 地址：

```text
http://127.0.0.1:3080
```

### 3.3 在其他项目目录使用

工作台可以从自身仓库启动，再在界面中登记其他工作区；也可以把当前项目目录作为授权根：

```bash
cd /path/to/project
export DSH_WORKBENCH_MCP_ROOT="$PWD"
/path/to/555/node_modules/.bin/pnpm --dir /path/to/555 run workbench:web
```

### 3.4 C / C++ 语言服务

TypeScript / JavaScript 语言服务随依赖内置。C / C++ 通过外部 `clangd`：

```bash
export DSH_CLANGD_BIN="$(command -v clangd)"
```

未配置 `clangd` 时只关闭 C/C++ 语义能力，不应阻断工作台启动。

---

## 4. Agent、会话、轮次与步骤

### 4.1 会话是事件溯源账本

会话持久化以追加事件为事实源。界面消息、统计、搜索、标题、计划、目标、工具调用状态等都从事件日志或其派生投影中恢复。持久日志与实时控制状态分离：持久事实能够回放，正在执行的瞬时状态由运行时服务维护。

核心层级：

```text
Session
└─ Turn
   ├─ user/message
   ├─ Step 1
   │  ├─ request/*
   │  ├─ assistant/*
   │  ├─ tool/call
   │  └─ tool/result
   ├─ Step 2 ...
   └─ turn/end
```

一个轮次从 `turn/start` 开始，以 `turn/end` 结束；步骤用于表达一次模型请求及其工具执行循环。历史设计要求会话事件保持在轮次边界内，避免回放时出现游离状态。

### 4.2 Agent 句柄

Agent 由注册表和 Agent Loop 创建、恢复与驱动。句柄提供提示词投递、转向、取消、状态读取等控制。新会话可以指定模型、工作目录、预设与权限；恢复会话时以持久日志重建状态。

### 4.3 并行工具调用

`agent-loop` 可通过 `maxParallelToolCalls` 控制同一步中标记为可并行的工具调用数量。并行不改变结果归属：每个工具调用仍有独立身份、事件与最终结果，取消和失败必须按调用分别收敛。

### 4.4 会话查询与投影

会话体系包含：

- 持久化后端：JSONL 与 SQLite；
- 查询：列表、全文搜索、事件搜索、关系追踪；
- 投影缓存：把事件日志转成 UI 和 API 需要的视图；
- 会话标题：首条提示词或模型生成标题；
- 会话引用：把其他会话的稳定快照注入上下文；
- Token 计量：记录请求压力、上下文位置和价格表层；
- 遥测：默认本地，只有明确开启时才通过 OpenTelemetry 输出。

---

## 5. 系统提示词、上下文与模型

### 5.1 系统提示词组装

系统提示词不是单一字符串常量，而是由注册的提供方按顺序贡献段落，再与工具 schema 一起形成请求装配结果。工作区指令、角色、时间、Shell 环境、tmux、技能提示等均可作为独立贡献者加入。

### 5.2 工作区指令

`dsh-agent-instructions` 会从会话工作目录向上寻找项目根，并读取约定的 `AGENTS.md` / `CLAUDE.md` 及本地覆盖文件。该能力有总字节预算和单文件大小限制，防止说明文件无限吞噬模型上下文。

仓库自身清理根 `AGENTS.md` / `CLAUDE.md` 后，并不移除这项通用能力：其他用户项目仍可使用这些文件。

### 5.3 模型适配层

模型服务由注册表统一管理；具体适配器负责协议差异、流式增量、思考内容、工具调用和错误分类。当前主要包括 DeepSeek 路由、通用多提供方适配、重试与测试回放。

自定义 OpenAI 兼容网关最常见的兼容项包括：

- 是否支持 `developer` 角色；
- 最大输出字段使用 `max_tokens` 还是 `max_completion_tokens`；
- 思考内容的协议格式；
- 文本/图片输入模态声明。

### 5.4 上下文压缩

压缩能力独立于 Agent Loop。它结合 Token 计量决定何时压缩，并可通过模型生成摘要；工具结果还可先执行无模型的头/中/尾裁剪。压缩产生持久事件，因此重启与回放不会重新猜测已经发生的压缩。

---

## 6. 工具系统与执行流水线

工具由 `ctx.tools` 注册。每项工具包含名称、说明、JSON 值 schema、执行器、超时与展示信息。模型提交参数后，执行路径大致为：

```text
参数校验
→ tools/pre-execute 策略链
→ 单调守卫/权限约束
→ tools/execute
→ tools/post-execute
→ 工具自有 finalizeContent
→ tools/result 最终通知
→ 会话持久化与 UI 投影
```

### 6.1 为什么要分层

- **工具实现**只负责动作本身。
- **权限、沙箱、超时、文件新鲜度、输出裁剪**作为策略层叠加。
- **展示层**不改变模型实际接收的事实。
- **持久化**记录最终调用与结果，使回放稳定。

### 6.2 Code Mode

工作台预设使用：

```yaml
mode: both
```

所以原生工具调用与 `run_code` 并存。`run_code` 允许模型把多次工具调用编排为一段程序，通过代码运行时暴露的宿主绑定执行，并捕获标准输出与返回值。当前后端包括 worker thread 与 Python 子进程实现。

---

## 7. 文件系统、Shell、子进程、终端与后台任务

### 7.1 文件系统

文件能力拆为抽象服务、具体后端、观察策略和模型工具。主要操作：读取、图片读取、写入、编辑、目录搜索、glob、grep。写入与编辑可以叠加“先观察后修改”的新鲜度守卫，避免基于过期内容盲改。

### 7.2 Shell

Shell seam 统一命令执行请求与结果，Bash/PowerShell 工具可以使用本地后端或沙箱后端。一次性命令与持久 PTY 是两套不同能力：前者更适合确定性脚本，后者适合保持目录、环境变量和交互状态。

### 7.3 子进程

`ctx.subprocess` 是更底层的进程能力：

- 显式可执行文件和参数；
- 标准输入输出策略；
- 受管 `DSH_*` 环境；
- 凭据清除；
- 进程组级终止；
- 有界输出；
- PTY 原语。

Shell、语言服务、ACP、PTY 等都复用它，避免各自重新实现进程生命周期。

### 7.4 持久终端

`ctx.terminals` 管理拥有者隔离的 PTY 会话，支持打开、读取、发送输入、发信号、列举和关闭。工作台默认启用 Bash 持久终端，并提供模型工具封装。

### 7.5 后台任务

长时间命令可以进入后台任务服务；模型通过 `job_list`、`job_output`、`job_kill` 管理。任务归属绑定会话/智能体，清理必须等待子进程真正停稳，不能只发出终止请求就宣告结束。

---

## 8. 沙箱、权限与安全边界

### 8.1 权限体系

权限预设给会话一个统一默认策略；具体高风险动作通过用户审批 seam 请求授权。审批结果和策略变化可以持久化到会话日志，便于审计。

### 8.2 Linux Landlock

本地沙箱可利用 `native/landlock-run` 限制文件系统访问。沙箱执行结果需要区分：真正被内核强制、部分能力不可用、策略拒绝、子进程自身失败。不能把“沙箱未完全生效”误报成命令业务失败。

### 8.3 MCP 本地文件服务

工作台正式 Web 组合挂载本地文件 MCP。授权根默认来自启动时工作目录，建议每次显式设置：

```bash
export DSH_WORKBENCH_MCP_ROOT=/允许访问的项目目录
```

越过授权根的访问应被拒绝。

### 8.4 Web 抓取边界

匿名 HTTP 抓取提供方本身不等于完整 SSRF 防护。部署在能够访问敏感内网的环境时，必须额外限制网络目标或禁用该提供方，不能把“仅允许 HTTP(S)”误认为私网安全策略。

### 8.5 凭据

凭据通过 `ctx.credentials` 与授权服务管理，运行时只向需要的适配器解析实际值。子进程环境使用统一清洗逻辑，避免把宿主秘密无意传给不可信命令输出或外部工具。

---

## 9. 持久化、存储、附件与大输出

### 9.1 会话持久化

会话日志有专用 seam；JSONL 与 SQLite 后端都实现同一会话事实模型。SQLite 同时用于本地全文查询。工作台 Web 正式组合默认持久索引路径：

```text
$DSH_HOME/storages/session-search.sqlite
```

### 9.2 通用存储

不属于会话事件日志的数据走 `ctx.storage`。后端只负责介质，`storage-domain` 提供带 schema、变更事件和命名空间的领域键值接口。Workspace 等上层功能只依赖领域数据形式，不直接操作 SQLite/JSON 文件。

### 9.3 附件

图片附件采用内容寻址引用。会话事件只保存不可变引用和元数据，不保存浏览器对象 URL、临时路径或 base64。这样会话可跨重启、跨客户端稳定回放。

### 9.4 Spill 大输出落盘

工具输出过大时，`spill` 把完整文本持久到会话私有位置，并把有限预览和检索路径返给模型。它与“只在内存中裁剪掉后半段”不同：被省略的内容仍可按路径继续读取。

---

## 10. 工作区、设置与用户状态

Workspace 是规范路径上的持久实体，保存稳定 id、标题及其会话账本。工作区注册表依赖存储领域与会话持久化，以实际 `SessionHeader.cwd` 校验会话归属。

`DSH_HOME` 是个人运行状态根目录，主要保存：

```text
会话日志
SQLite 索引
附件
凭据
settings.yaml
工作区登记
技能/用户配置
其他运行时状态
```

源码仓库与 `DSH_HOME` 应分开备份：前者恢复程序，后者恢复个人状态。

---

## 11. Web 客户端架构

Web 界面采用“宿主 + 浏览器插件”的双半结构。

### 11.1 宿主侧

宿主负责：

- HTTP 服务器；
- 静态前端；
- Typert/API 网关；
- 会话、工作区、设置、凭据等远程能力；
- 客户端插件清单；
- WebSocket/事件下行。

### 11.2 浏览器侧

浏览器侧用 Cordis 装载客户端模块。核心服务包括连接控制、模块表、SessionRuntime、WorkspaceRuntime、SlotRegistry 和 React 渲染器。业务界面通过 slot 注册到布局，不把全部功能硬编码进单一页面组件。

主要 UI 模块覆盖：

- 会话聊天与输入框；
- 侧边栏、搜索、会话树；
- 模型选择；
- 工作区；
- 设置；
- 计划、目标、待办；
- 子智能体与工作流；
- 工具卡片、终端卡片、轨迹详情；
- 附件、反馈、权限、技能、引用；
- 主题与中英文界面语言。

### 11.3 热重载

开发模式下，客户端 bundle 可以重建并通过 HMR 交换插件 fiber；没有 watcher 时 HMR 链路保持空闲，不应改变生产行为。

---

## 12. 子智能体、团队、工作流与 Ralph

### 12.1 子智能体

`ctx.subagents` 是命名提供方注册表，同一上下文可同时挂多个实现。当前实现包括：

- 进程内 spawn；
- 从父会话前缀 fork；
- ACP 子进程；
- DSH SDK 子进程；
- Codex；
- Claude Code。

模型侧工具可启动子智能体、发消息、打断、列举，并由子智能体使用 `report` 汇报。

### 12.2 Agent Team

实验性 Team 能力维护成员表、邮箱和共享任务有向无环图；它提供团队消息、任务创建/领取/更新、等待成员等工具。该能力仍属实验模块，使用时应把持久身份与共享 checkout 边界视为强约束。

### 12.3 Workflow

Workflow 允许模型提交一段编排程序，在独立 worker 中执行，并通过桥接的 `agent()` 调用子智能体。运行过程发布阶段、日志、成员开始/结束和最终结果事件。

### 12.4 Ralph

Ralph 是面向模型的循环工具，建立在 Workflow + Subagent seam 之上，用新鲜子智能体反复执行/评估，适合需要多轮收敛的任务。

---

## 13. Goal、Plan、TODO、问题与计划任务

- **Goal**：持久保存同会话目标，可创建、读取、更新、暂停、恢复与完成。
- **Plan**：把实现前的探索/计划与普通执行区分开；界面和命令都读取持久状态。
- **TODO**：通过事件日志保存结构化待办列表，适合实现阶段进度。
- **用户问题**：`ask_user_question` 允许模型提交结构化选项并接收用户回答。
- **计划任务**：会话内创建、列举、删除未来触发项；它不是跨服务的云定时系统。

---

## 14. 技能系统

技能由 `ctx.skills` 注册，候选元数据与完整定义分开读取；模型先看摘要，再按需要加载 `SKILL.md` 正文。工作台内置技能是正式预设的一部分，不依赖用户 Home 临时注入。

保留的可执行技能如下：

- `.agents/skills/dsh-code-review/SKILL.md`：**Reviewing a DeepSeek-Harness PR**。针对本仓库约束、生命周期、安全边界和质量门禁进行代码审查。
- `.agents/skills/dsh-find-simplifications/SKILL.md`：**Finding DeepSeek Harness Simplifications**。发现可删除、可合并、可降低复杂度的实现与决策。
- `.agents/skills/dsh-merging-stacked-prs/SKILL.md`：**Landing an official GitHub PR stack**。处理依赖式多拉取请求的落地顺序与同步。
- `.agents/skills/dsh-pre-push-checks/SKILL.md`：**DSH Pre-Push Checks**。推送前按改动范围选择最小但足够的检查集。
- `.agents/skills/dsh-prose-standard/SKILL.md`：**DeepSeek Harness Prose Standard**。约束仓库中的技术文字、注释、提示词和诊断信息。
- `.agents/skills/dsh-trim-cot-leakage/SKILL.md`：**Trimming Chain-of-Thought Leakage**。清理像内部推理草稿、审计编号和临时设计会话残留的文字。
- `.agents/skills/record-browser-gif/SKILL.md`：**Record Browser GIF**。录制浏览器/Web 界面交互并生成可复现演示动图。
- `apps/cli/config/agent-presets/cordis/skills/cordis-plugin-development/SKILL.md`：**Develop Dynamic Cordis Plugins**。Cordis 插件开发规则与常见实现路径。
- `apps/cli/config/agent-presets/cordis/skills/editing-cordis-compositions/SKILL.md`：**Editing Cordis compositions**。修改 Cordis 组合配置时的结构与验证规则。
- `apps/cli/config/agent-presets/workbench/skills/docs-quality/SKILL.md`：**Documentation Quality**。检查技术说明、README、注释和错误信息的准确性与可维护性。
- `apps/cli/config/agent-presets/workbench/skills/repo-quality-gate/SKILL.md`：**Repository Quality Gate**。按改动选择格式、类型、测试、构建等质量门禁。
- `apps/cli/config/agent-presets/workbench/skills/repo-review/SKILL.md`：**Repository Review**。仓库级正确性、安全、并发、接口、冗余与简化审查。
- `apps/cli/config/agent-presets/workbench/skills/task-journal/SKILL.md`：**Task Journal**。长期任务的目标、约束、决策、证据、失败路径与检查点记录。
- `apps/cli/config/agent-presets/workbench/skills/workbench-ops/SKILL.md`：**工作台 Operations**。工作台运行、恢复、修改后验证和环境一致性规则。

技能 Markdown 是运行时输入，因此即使其内容已经在本文概括，原 `SKILL.md` 仍必须保留。

---

## 15. Web 搜索/抓取、MCP、ACP、SDK 与远程接口

### 15.1 Web 访问

`ctx.web` 同时抽象 search 与 fetch；搜索提供方可以是 Exa、Perplexity、DeepSeek，抓取由 HTTP 提供方承担。模型只面对 `web_search` / `web_fetch`，提供方替换不会改变工具协议。

### 15.2 MCP

MCP 客户端把外部工具服务器接入工具注册表。工作台默认额外挂载本地文件服务器，但 MCP 本身是通用能力，可配置其他服务器。

### 15.3 ACP

ACP 服务器通过 JSON-RPC stdio 暴露自动化接口，可创建 Harness agent、发送文本/图片提示、处理审批并取消执行。子智能体 ACP 后端也复用这一协议。

### 15.4 Typert 与 API Gateway

Typert 从 TypeScript 类型生成远程调用描述，Host Gateway 负责分发，浏览器/SDK 通过同一约定调用。业务包依赖生成的远程外观，不直接绑定传输实现。

### 15.5 Python SDK

Python 侧提供 SDK 与运行时 wheel 封装，用于程序化创建/控制会话。其职责是远程消费 Harness 能力，不复制 Node 端核心状态机。

---

## 16. 插件开发方法

### 16.1 最小插件

```ts
import type { Context } from '@deepseek-ai/cordis'

export const name = 'my-plugin'

export function apply(ctx: Context) {
  // 注册服务、事件、工具或 effect
}
```

需要其他服务时声明 `inject`；需要手工释放资源时使用 `ctx.effect()` 返回清理函数。

### 16.2 配置

插件应提供 TypeScript `Config` 类型，并用运行时 schema 拒绝非法部署值。可调参数进入配置，不要散落硬编码；配置错误应尽早、明确失败。

### 16.3 工具

模型工具至少需要：名称、说明、参数 schema、执行函数。通用策略（审批、沙箱、超时、输出处理）尽量挂在工具流水线，不要每个工具重复实现。

### 16.4 新能力放在哪里

判断顺序：

1. 是否是所有 Agent 都必须依赖的核心主干？是则进入核心服务。
2. 是否存在可替换后端？是则建 seam：定义包 + 提供方 + 消费方。
3. 是否只是 UI 展示？进入客户端 slot/展示包。
4. 是否只是一种部署组合？进入 bundle/profile，而非把开关塞进核心。
5. 是否属于跨项目可复用的底层原语？放入 util 或独立基础包。

---

## 17. 防御性实现规则

原故障复盘与防御性文档收敛为以下规则：

1. **正交结果分别上报。** 不要用一个布尔值混合“动作成功”“策略生效”“清理成功”等不同事实。
2. **约定两侧都验证。** 生产方与消费方共享的协议，两侧都要对非法状态 fail loud。
3. **异步状态不可假装同步。** 生命周期变更、子进程退出、远程连接和持久写入都必须等待真实完成点。
4. **dispose 要完全停稳。** 发出 abort/kill 不等于资源已经释放。
5. **隔离第三方回调异常。** 一个观察者失败不能破坏事件分发器或其他订阅者。
6. **秘密与路径最小暴露。** 不把环境变量、凭据或可预测临时路径传给不可信输出。
7. **文件替换保持平台语义。** 原子写、Windows DACL、符号链接与 unlink 行为都按平台实际语义实现。
8. **回放必须确定。** 已提交到日志的事实不能依赖当前插件是否仍挂载才能解释。
9. **展示不能反向定义事实。** UI 卡片可裁剪、折叠，但原始事件与模型结果必须保持独立。
10. **错误分类保留原因。** 沙箱拒绝、提供方不可用、超时、用户取消和业务失败不能压成一个通用错误。

---

## 18. 测试、构建与验收

### 18.1 标准改动验收

工作台相关修改至少执行：

```bash
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
```

再使用空 Home 冷启动：

```bash
export DSH_HOME="$(mktemp -d)"
export DSH_WORKBENCH_MCP_ROOT="$PWD"
pnpm run workbench:web
```

确认：

```text
HTTP 200
默认 workbench 预设
工作台技能可发现
MCP 正常
TypeScript/JavaScript 语言服务正常
SQLite 索引可创建
```

### 18.2 测试快照为何保留 Markdown

仓库中大量 `.expected.md` 是测试断言，不是文档。它们覆盖 Web UI、系统提示词、Code Mode、工具卡片、子智能体、工作流、计划、目标、模型设置、附件、权限、终端等场景。删除这些文件会直接降低回归测试可信度。

当前快照/夹具主题统计：

- `models-settings`：7 份 Markdown 测试基线
- `subagent-conversation`：7 份 Markdown 测试基线
- `agent-instructions`：5 份 Markdown 测试基线
- `lifecycle-chrome`：5 份 Markdown 测试基线
- `live-interactions`：5 份 Markdown 测试基线
- `queue-actions`：5 份 Markdown 测试基线
- `agent-preset-authoring`：4 份 Markdown 测试基线
- `question-composer`：4 份 Markdown 测试基线
- `seeded-history`：4 份 Markdown 测试基线
- `测试夹具`：4 份 Markdown 测试基线
- `agent-preset-selection`：3 份 Markdown 测试基线
- `navigation-panes`：3 份 Markdown 测试基线
- `onboarding-deepseek-config`：3 份 Markdown 测试基线
- `plan-review`：3 份 Markdown 测试基线
- `reference-composer`：3 份 Markdown 测试基线
- `schedule-after`：3 份 Markdown 测试基线
- `settings-chrome`：3 份 Markdown 测试基线
- `skill-load`：3 份 Markdown 测试基线
- `background-job-list`：2 份 Markdown 测试基线
- `code-mode-workspace-context`：2 份 Markdown 测试基线
- `fresh-round-trip`：2 份 Markdown 测试基线
- `message-actions`：2 份 Markdown 测试基线
- `steer-all`：2 份 Markdown 测试基线
- `steering`：2 份 Markdown 测试基线
- `turn-tail-actions`：2 份 Markdown 测试基线
- `web-runtime-context`：2 份 Markdown 测试基线
- `workflow-run`：2 份 Markdown 测试基线
- `access-confirmation`：1 份 Markdown 测试基线
- `advanced-toolchain`：1 份 Markdown 测试基线
- `approval-composer`：1 份 Markdown 测试基线
- `bash-abort-row`：1 份 Markdown 测试基线
- `both-mode-turn`：1 份 Markdown 测试基线
- `code-mode-read-image`：1 份 Markdown 测试基线
- `code-mode-round`：1 份 Markdown 测试基线
- `code-mode-turn`：1 份 Markdown 测试基线
- `cold-blank-session`：1 份 Markdown 测试基线
- `composer-draft-scroll`：1 份 Markdown 测试基线
- `composer-tab-geometry`：1 份 Markdown 测试基线
- `conversation-column-overflow`：1 份 Markdown 测试基线
- `cordis-tool-round`：1 份 Markdown 测试基线
- `declared-reasoning`：1 份 Markdown 测试基线
- `details-session-lifecycle`：1 份 Markdown 测试基线
- `feedback-command`：1 份 Markdown 测试基线
- `fs-glob-sampling`：1 份 Markdown 测试基线
- `goal-bar`：1 份 Markdown 测试基线
- `goal-command-presentation`：1 份 Markdown 测试基线
- `goal-multi-turn-actions`：1 份 Markdown 测试基线
- `lsp-definition`：1 份 Markdown 测试基线
- `markdown-cjk-strong`：1 份 Markdown 测试基线
- `markdown-images`：1 份 Markdown 测试基线
- `markdown-inline-code-links`：1 份 Markdown 测试基线
- `markdown-wide-table`：1 份 Markdown 测试基线
- `math-rendering`：1 份 Markdown 测试基线
- `message-feedback-layout`：1 份 Markdown 测试基线
- `onboarding-usable-provider`：1 份 Markdown 测试基线
- `persistent-pwsh-tool-turn`：1 份 Markdown 测试基线
- `plan-narrow-viewport`：1 份 Markdown 测试基线
- `plugin-config`：1 份 Markdown 测试基线
- `product-subagent-codex`：1 份 Markdown 测试基线
- `pty-tools`：1 份 Markdown 测试基线
- `pwsh-terminal`：1 份 Markdown 测试基线
- `pwsh-tool-turn`：1 份 Markdown 测试基线
- `read-image`：1 份 Markdown 测试基线
- `session-query-spill`：1 份 Markdown 测试基线
- `sidebar-scrollbar`：1 份 Markdown 测试基线
- `sidebar-subagent-activity`：1 份 Markdown 测试基线
- `skill-invocation-policy`：1 份 Markdown 测试基线
- `skill-tool-row`：1 份 Markdown 测试基线
- `skill-user-invoke`：1 份 Markdown 测试基线
- `stats-paged-history`：1 份 Markdown 测试基线
- `subagent-continuable`：1 份 Markdown 测试基线
- `subagent-continuable-inheritance`：1 份 Markdown 测试基线
- `subagent-interrupt`：1 份 Markdown 测试基线
- `subagent-list-agents`：1 份 Markdown 测试基线
- `subagent-report`：1 份 Markdown 测试基线
- `text-turn`：1 份 Markdown 测试基线
- `trajectory-virtualization`：1 份 Markdown 测试基线
- `web-fetch`：1 份 Markdown 测试基线
- `web-search-round`：1 份 Markdown 测试基线
- `workspace-management`：1 份 Markdown 测试基线

### 18.3 `workbench:doctor`

自检脚本验证正式预设、5 个工作台技能、语言服务启动器、MCP 启动器、Web 组合、依赖、`mode: both`、默认 `workbench`、持久 SQLite 配置和旧旁挂目录不存在。文档收敛后，它只要求根 `README.md` 作为技术文档，不再要求 `docs/workbench.zh.md`。

---

## 19. 自动化与永久离线恢复

正式工作台流程以源码构建/冷启动和永久工具链产物为主。永久产物基线：

```text
workbench-1.0.0-ready-linux-x64.tar.zst
workbench-toolchain-linux-x64.tar.zst
SHA256SUMS.txt
```

恢复路径：

```text
源码仓库 → 安装锁定依赖 → 构建 → doctor → 冷启动
永久 ready 包 → 校验 SHA-256 → 解压固定工具链与 ready 包 → doctor → 启动
```

离线包解决程序恢复，`DSH_HOME` 备份解决个人会话与状态恢复。

---

## 20. 工具目录

以下是当前生成工具目录中注册的模型可见工具。参数细节以对应工具包源码中的 schema 为最终事实源。

- `@deepseek-ai/dsh-tool-ask-user`：`ask_user_question`
- `@deepseek-ai/dsh-tools`：`run_code`
- `@deepseek-ai/dsh-plan-mode`：`exit_plan_mode`
- `@deepseek-ai/dsh-tool-bash`：`bash`
- `@deepseek-ai/dsh-tool-pwsh`：`pwsh`
- `@deepseek-ai/dsh-tool-cordis`：`cordis_define`、`cordis_inspect_list`、`cordis_inspect_query`、`cordis_inspect_self`、`cordis_run`、`cordis_stop`、`cordis_undefine`
- `@deepseek-ai/dsh-tool-bash-persistent`：`bash`
- `@deepseek-ai/dsh-tool-pwsh-persistent`：`pwsh`
- `@deepseek-ai/dsh-tool-str-replace-editor`：`str_replace_editor`
- `@deepseek-ai/dsh-tool-fs`：`edit`、`read`、`read_image`、`write`
- `@deepseek-ai/dsh-tool-fs-search`：`glob`、`grep`
- `@deepseek-ai/dsh-tool-terminal`：`terminal_close`、`terminal_list`、`terminal_open`、`terminal_read`、`terminal_send`、`terminal_signal`
- `@deepseek-ai/dsh-tool-goal`：`create_goal`、`get_goal`、`update_goal`
- `@deepseek-ai/dsh-schedule`：`schedule_create`、`schedule_delete`、`schedule_list`
- `@deepseek-ai/dsh-tool-lsp`：`lsp`
- `@deepseek-ai/dsh-tool-ralph`：`ralph`
- `@deepseek-ai/dsh-tool-skill`：`skill`
- `@deepseek-ai/dsh-tool-session-query`：`session_event_read`、`session_event_search`、`session_event_trace`、`session_search`、`session_trace`
- `@deepseek-ai/dsh-tool-subagent`：`subagent`
- `@deepseek-ai/dsh-tool-subagent-control`：`interrupt_agent`、`list_agents`、`send_message`
- `@deepseek-ai/dsh-tool-subagent-report`：`report`
- `@deepseek-ai/dsh-tool-jobs`：`job_kill`、`job_list`、`job_output`
- `@deepseek-ai/dsh-experimental-tool-agent-team`：`followup_task`、`interrupt_agent`、`list_agents`、`send_message`、`spawn_teammate`、`team_task_create`、`team_task_get`、`team_task_list`、`team_task_update`、`wait_agent`
- `@deepseek-ai/dsh-tool-todo`：`todo_write`
- `@deepseek-ai/dsh-tool-workflow`：`workflow`
- `@deepseek-ai/dsh-tool-web`：`web_fetch`、`web_search`

---

## 21. 可加载插件配置总览

下表从原生成配置目录提取。`配置键`只列顶层字段；嵌套结构、默认值和运行时校验仍以对应包的 `Config` 类型与 schema 为准。

| 插件 | 注入依赖 | 顶层配置键 |
|---|---|---|
| `@deepseek-ai/dsh-acp` | `agents` | `provider`、`model`、`stream` |
| `@deepseek-ai/dsh-acp-demo` | — | `provider`、`model`、`maxParallelToolCalls`、`persona`、`toolOrder`、`tools`、`dshHome`、`sessionTitle`、`persistenceRoot`、`packChunks`、`persistenceCompression`、`workspaceContext`、`skills`、`toolBash`、`jobs`、`toolJobs`、`goals` |
| `@deepseek-ai/dsh-agent-default-model` | — | `provider`、`model` |
| `@deepseek-ai/dsh-agent-instructions` | — | `dshHome`、`projectRootMarkers`、`maxBytes`、`maxSourceBytes`、`instructionFileCandidates`、`localInstructionFileCandidates` |
| `@deepseek-ai/dsh-agent-loop` | `agents`、`sessions`、`llm`、`tools`、`systemPrompt` | `maxParallelToolCalls`、`agents` |
| `@deepseek-ai/dsh-agent-presets` | `loader` | `default`、`roots`、`includeUserRoot`、`path`、`trust` |
| `@deepseek-ai/dsh-agent-spine-demo` | — | `agents`、`maxParallelToolCalls`、`includeHarnessIdentity`、`includeRuntimeContext`、`persona`、`toolOrder`、`tools`、`dshHome`、`sessionTitle`、`workspaceContext`、`skills`、`toolBash`、`jobs`、`toolJobs`、`invariants`、`goals`、`enabled`、`registry`、`filesystem`、`tool`、`domain` |
| `@deepseek-ai/dsh-agent-tool-presentation` | `tools` | `mode` |
| `@deepseek-ai/dsh-attachment-local` | — | `dshHome`、`maxImageBytes`、`maxImagesPerMessage`、`maxMessageImageBytes`、`maxImagePixels`、`maxImageDimension`、`normalizedImageMaxDimension`、`normalizedImageMaxBytes`、`imageCompressionConcurrency` |
| `@deepseek-ai/dsh-bash-local` | `subprocess` | `cwd`、`timeoutMs`、`maxTimeoutMs`、`maxOutputBytes`、`maxSpillBytes`、`graceMs` |
| `@deepseek-ai/dsh-bash-sandbox` | `subprocess`、`sandbox`、`sandboxPolicy` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-client-connection` | `webServer` | `trustedHosts`、`maxRequestBodyBytes` |
| `@deepseek-ai/dsh-client-hmr` | `clientModules`、`webServer` | `pollIntervalMs` |
| `@deepseek-ai/dsh-code-runtime-worker-thread` | — | `computeMs`、`maxWallMs`、`maxOutputBytes`、`maxOldGenerationSizeMb` |
| `@deepseek-ai/dsh-compaction-basic` | `llm`、`tokenMeter`、`sessions` | `modelPolicies`、`auto`、`thresholdRatio`、`retainRatio`、`retainTokens`、`summarizationProvider`、`summarizationModel`、`maxTokens`、`compactionRetries`、`maxOverflowRetries`、`provider`、`model` |
| `@deepseek-ai/dsh-compaction-tool-result-pruner` | `tokenMeter` | `thresholdChars`、`headChars`、`tailChars` |
| `@deepseek-ai/dsh-cordis-host-runner` | `tools` | `vmTimeoutMs` |
| `@deepseek-ai/dsh-credentials-local` | — | `path`、`dshHome`、`watch`、`debounceMs` |
| `@deepseek-ai/dsh-e2b` | — | `apiKey`、`cwd`、`timeoutMs` |
| `@deepseek-ai/dsh-experimental-agent-team` | `agents`、`sessions`、`sessionPersistence`、`subagents` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-experimental-tool-agent-team` | `agents`、`agentTeams`、`tools`、`systemPrompt` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-file-reference-local` | `agents` | `maxResults`、`maxEntries`、`excludedDirectories` |
| `@deepseek-ai/dsh-fs-local` | — | `cwd`、`diffBasisMaxBytes` |
| `@deepseek-ai/dsh-fs-sandbox` | `sandboxPolicy` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-goal` | `agents` | `defaultMaxGoalRounds` |
| `@deepseek-ai/dsh-headless` | `agentDefaultModel`、`agents`、`sessions` | `task` |
| `@deepseek-ai/dsh-hooks-claude-code` | `shell` | `configPath`、`pluginRoot`、`projectDir`、`defaultTimeoutMs`、`stderrSummaryMaxChars` |
| `@deepseek-ai/dsh-hooks-codex` | `shell` | `configPath`、`model`、`defaultTimeoutMs`、`stderrSummaryMaxChars` |
| `@deepseek-ai/dsh-host-apiproxy` | `agentDefaultModel`、`agents`、`attachments`、`directoryPicker`、`llm`、`sessions`、`subagents`、`sessionQuery`、`tools`、`userQuestions`、`workspaceRegistry` | `nativeOpen`、`sessionExportCompressionLevel`、`coldBlankProbeMaxBytes` |
| `@deepseek-ai/dsh-host-directory-picker-browse` | — | `maxEntries` |
| `@deepseek-ai/dsh-host-frontend-static` | `webServer` | `distIndex` |
| `@deepseek-ai/dsh-host-webserver` | — | `host`、`port` |
| `@deepseek-ai/dsh-invariants` | — | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-jobs-local` | — | `maxConcurrentJobsPerOwner` |
| `@deepseek-ai/dsh-llm-deepseek` | `llm` | `apiKeyEnv`、`baseURL`、`thinking`、`reasoningEffort`、`maxTokens`、`defaultContextWindow`、`models`、`streamIdleTimeoutMs`、`maxRequestFilesBytes`、`maxInlineRequestImageBytes`、`maxImagesPerRequest`、`imageOffloadByteQuantum`、`inlineImageOffloadByteQuantum`、`imageOffloadCountQuantum`、`filesApiTimeoutMs`、`fileExpiresAfterSeconds`、`fileRefreshMarginSeconds`、`fileQuotaCleanupBatch`、`retryPolicy`、`id`、`name`、`description`、`contextWindow`、`inputModalities`、`imagePixelBudget`、`imageMaxBytes`、`imageDetail` |
| `@deepseek-ai/dsh-llm-pi-ai` | `llm` | `providers`、`apiKeyEnv`、`displayName`、`api`、`baseURL`、`models`、`modelOverrides`、`compat`、`defaultContextWindow`、`defaultMaxTokens`、`defaultInput`、`headers`、`reasoning`、`thinkingBudgets`、`cacheRetention`、`transport`、`timeoutMs`、`websocketConnectTimeoutMs`、`streamIdleTimeoutMs`、`maxRequestImageBytes`、`requestImagePixelBudget`、`requestImageMaxBytes`、`retryPolicy`、`id`、`name`、`contextWindow`、`maxTokens`、`input`、`reasoningEfforts`、`supportsStore`、`supportsDeveloperRole`、`supportsReasoningEffort`、`supportsUsageInStreaming`、`maxTokensField`、`requiresToolResultName`、`requiresAssistantAfterToolResult`、`requiresThinkingAsText`、`requiresReasoningContentOnAssistantMessages`、`thinkingFormat`、`chatTemplateKwargs`、`supportsStrictMode`、`cacheControlFormat`、`supportsLongCacheRetention`、`supportsEagerToolInputStreaming`、`supportsCacheControlOnTools`、`supportsTemperature`、`forceAdaptiveThinking`、`allowEmptySignature`、`supportsStrictTools` |
| `@deepseek-ai/dsh-llm-replay` | `llm` | `file`、`overrideFile`、`childFiles`、`providers`、`paceMs`、`id`、`name`、`models`、`retryPolicy`、`description`、`contextWindow`、`inputModalities`、`defaultMaxTokens`、`reasoningEfforts`、`defaultReasoningEffort` |
| `@deepseek-ai/dsh-llm-retry` | `agents` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-lsp-stdio` | `fs`、`lsp`、`subprocess` | `servers`、`command`、`extensionToLanguage`、`args`、`env`、`initializationOptions`、`configuration`、`maxMessageBytes`、`maxStderrBytes`、`maxDocumentBytes`、`shutdownTimeoutMs`、`killGraceMs` |
| `@deepseek-ai/dsh-mcp-client` | `tools` | `transport`、`serverName`、`command`、`args`、`env`、`cwd`、`toolCallTimeoutMs`、`failOnStartupError`、`reconnect`、`url`、`headers`、`enabled`、`initialDelayMs`、`maxDelayMs`、`maxAttempts` |
| `@deepseek-ai/dsh-message-feedback` | `storageDomain`、`sessionPersistence`、`sessions` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-permission-presets` | `shell`、`approval`、`sessions` | `presets`、`defaultPreset`、`sandbox`、`approval`、`name`、`description` |
| `@deepseek-ai/dsh-persona` | `systemPrompt` | `text`、`complete`、`includeRuntimeContext` |
| `@deepseek-ai/dsh-plan-mode` | `tools`、`systemPrompt` | `section` |
| `@deepseek-ai/dsh-pwsh-local` | `subprocess` | `cwd`、`timeoutMs`、`maxTimeoutMs`、`maxOutputBytes`、`maxSpillBytes`、`graceMs`、`pwshPath` |
| `@deepseek-ai/dsh-pwsh-sandbox` | `subprocess`、`sandbox`、`sandboxPolicy` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-repeat-tool-reminder` | — | `thresholds`、`include`、`exclude`、`argumentsPreviewChars` |
| `@deepseek-ai/dsh-sandbox-local` | — | `runnerCommand`、`runnerFailureSignatures`、`probeTimeoutMs` |
| `@deepseek-ai/dsh-sandbox-policy` | — | `mode`、`workspaceRoot` |
| `@deepseek-ai/dsh-sdk-jsonrpc-server` | `agents` | `maxTokensAsSuccess`、`input`、`output`、`exit` |
| `@deepseek-ai/dsh-session-persistence-jsonl` | `sessions` | `root`、`packChunks`、`compression`、`preparedSessionCacheSize`、`writeBatchMaxDelayMs` |
| `@deepseek-ai/dsh-session-persistence-sqlite` | `sessions` | `path`、`journalMode`、`busyTimeoutMs`、`preparedSessionCacheSize`、`writeBatchMaxDelayMs` |
| `@deepseek-ai/dsh-session-projection-cache` | `storageDomain`、`sessionProjections`、`sessionPersistence`、`sessions` | `writeEveryEvents`、`writeIntervalMs` |
| `@deepseek-ai/dsh-session-query-sqlite` | `sessions` | `path`、`openAt`、`journalMode`、`defaultLimit`、`maxLimit`、`snippetChars`、`persistedInspectConcurrency` |
| `@deepseek-ai/dsh-session-reference` | `sessionQuery` | `maxReferences`、`candidateLimit`、`maxReferenceBytes` |
| `@deepseek-ai/dsh-session-telemetry-otel` | `sessions` | `mode`、`exporter`、`processor`、`shutdownTimeoutMillis` |
| `@deepseek-ai/dsh-session-title` | `sessions` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-session-title-all-prompts-llm` | `sessionTitle`、`llm`、`sessions` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-session-title-first-prompt-llm` | `sessionTitle`、`llm`、`sessions` | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-settings-file` | — | `path`、`dshHome`、`watch`、`debounceMs` |
| `@deepseek-ai/dsh-shell-env` | — | `dshHome` |
| `@deepseek-ai/dsh-skill` | — | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-skill-filesystem` | `skills` | `providerName`、`includeDefaultRoots`、`dshHome`、`agentsHome`、`customSkillDirs`、`watch`、`watchUsePolling`、`watchStabilityThresholdMs`、`watchPollIntervalMs`、`watchMaxProjects`、`watchFollowSymlinks`、`bundledSkillDir` |
| `@deepseek-ai/dsh-spill-local` | — | `root` |
| `@deepseek-ai/dsh-spill-policy` | `tools` | `maxInlineBytes` |
| `@deepseek-ai/dsh-storage-domain` | `storage` | `backend`、`routes` |
| `@deepseek-ai/dsh-storage-json` | `storage` | `root` |
| `@deepseek-ai/dsh-storage-sqlite` | `storage` | `path`、`journalMode` |
| `@deepseek-ai/dsh-subagent-acp` | `subagents`、`subprocess` | `providerName`、`command`、`args`、`cwd`、`permission`、`env`、`disposeEofGraceMs`、`disposeGraceMs` |
| `@deepseek-ai/dsh-subagent-claude-code` | `subagents`、`subprocess` | `providerName`、`env`、`permissionMode`、`disposeGraceMs` |
| `@deepseek-ai/dsh-subagent-codex` | `subagents`、`subprocess` | `providerName`、`env`、`permissionMode`、`disposeGraceMs` |
| `@deepseek-ai/dsh-subagent-dsh-sdk` | `subagents` | `providerName`、`command`、`args`、`cwd`、`provider`、`model`、`maxTokens`、`env`、`shutdownTimeoutMs`、`disposeEofGraceMs`、`disposeGraceMs` |
| `@deepseek-ai/dsh-subagent-fork-in-process` | `subagents` | `providerName` |
| `@deepseek-ai/dsh-subagent-spawn-in-process` | `subagents` | `providerName` |
| `@deepseek-ai/dsh-subprocess-e2b` | `e2b` | `pollMs` |
| `@deepseek-ai/dsh-system-prompt` | — | `includeHarnessIdentity`、`includeRuntimeContext`、`persona`、`toolOrder` |
| `@deepseek-ai/dsh-terminal-bash` | `terminals`、`sandboxPolicy`、`subprocess` | `backendType`、`shellDialect`、`shellPath`、`shellArgs`、`rows`、`cols`、`scrollbackLines`、`scrollbackMaxBytes`、`maxReadBytes`、`pollIntervalMs`、`exactProbeAfterMs`、`idleSilenceMs`、`handoffGraceMs`、`timeoutMs`、`disposeGraceMs` |
| `@deepseek-ai/dsh-time-context` | `agents` | `timeZone`、`refreshIntervalMs` |
| `@deepseek-ai/dsh-tmux-context` | `agents` | `refreshIntervalMs` |
| `@deepseek-ai/dsh-token-meter` | — | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-tool-bash` | `tools`、`shell`、`systemPrompt`、`shellEnv` | `enableRunInBackground` |
| `@deepseek-ai/dsh-tool-bash-persistent` | `tools`、`terminals` | `backendType`、`timeoutMs`、`maxOutputChars`、`description` |
| `@deepseek-ai/dsh-tool-fs` | `tools`、`fs`、`systemPrompt` | `readLimit`、`readMaxLineLength`、`readMaxBytes`、`readStreamMinSize` |
| `@deepseek-ai/dsh-tool-fs-search` | `tools`、`systemPrompt`、`subprocess` | `sampleOverCapGlobResults`、`globMaxResults`、`grepMaxMatches`、`grepMaxLineBytes`、`searchMetaMaxBytes`、`rawOutputMaxBytes`、`graceMs`、`stderrMaxBytes`、`timeoutMs` |
| `@deepseek-ai/dsh-tool-goal` | `agents`、`goals`、`tools`、`systemPrompt` | `blockedAfterConsecutiveRounds` |
| `@deepseek-ai/dsh-tool-jobs` | `tools`、`jobs`、`systemPrompt` | `waitTimeoutMs`、`maxWaitTimeoutMs`、`completionDelivery`、`maxConsecutiveWakes` |
| `@deepseek-ai/dsh-tool-lsp` | `tools`、`lsp`、`systemPrompt` | `maxLocations`、`maxResultChars`、`timeoutMs` |
| `@deepseek-ai/dsh-tool-pwsh` | `tools`、`shell`、`systemPrompt`、`shellEnv` | `enableRunInBackground` |
| `@deepseek-ai/dsh-tool-pwsh-persistent` | `tools`、`terminals` | `backendType`、`timeoutMs`、`maxOutputChars`、`description` |
| `@deepseek-ai/dsh-tool-ralph` | `tools`、`workflowEngine`、`subagents`、`systemPrompt` | `subagentProvider`、`maxRounds`、`maxHandoffChars`、`maxResultChars` |
| `@deepseek-ai/dsh-tool-session-query` | `tools`、`systemPrompt`、`sessionQuery` | `maxSearchResults`、`searchTimeoutMs` |
| `@deepseek-ai/dsh-tool-skill` | `agents`、`tools`、`skills` | `catalogDescriptionMaxLength` |
| `@deepseek-ai/dsh-tool-str-replace-editor` | `tools`、`fs` | `maxOutputChars`、`description` |
| `@deepseek-ai/dsh-tool-subagent` | `tools`、`subagents`、`systemPrompt` | `provider`、`toolName`、`enableRunInBackground`、`backgroundMode`、`agentOptions`、`persona`、`toolFilter`、`maxDepth` |
| `@deepseek-ai/dsh-tool-subagent-report` | `subagents`、`tools`、`systemPrompt` | `reportDelivery` |
| `@deepseek-ai/dsh-tool-terminal` | `terminals`、`tools`、`systemPrompt` | `enableRunInBackground`、`maxResultBytes` |
| `@deepseek-ai/dsh-tool-todo` | `tools` | `allowParallelInProgress` |
| `@deepseek-ai/dsh-tool-web` | `tools`、`web`、`systemPrompt` | `search`、`fetch`、`searchMaxResults`、`searchMaxQueries`、`fetchTimeoutMs`、`searchTimeoutMs`、`fetchMaxOutputChars` |
| `@deepseek-ai/dsh-tool-workflow` | `tools`、`workflowEngine`、`systemPrompt` | `toolName`、`maxResultChars` |
| `@deepseek-ai/dsh-tools` | `systemPrompt` | `mode`、`maxParallelSubCalls` |
| `@deepseek-ai/dsh-typert-loader` | `typert`、`loader` | `packages` |
| `@deepseek-ai/dsh-user-approval` | — | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-web` | — | 无公开配置或配置类型不以普通接口字段表达 |
| `@deepseek-ai/dsh-web-app` | `webServer` | `openBrowser`、`printUrl`、`surfaceContext`、`trustedHosts` |
| `@deepseek-ai/dsh-web-fetch-http` | `web` | `maxUrlLength`、`maxResponseBytes`、`maxBodyChars`、`timeoutMs`、`maxRedirects`、`userAgent` |
| `@deepseek-ai/dsh-web-search-deepseek` | `web` | `apiKey`、`apiKeyEnv`、`baseURL`、`model`、`apiVersion`、`maxTokens`、`maxUses` |
| `@deepseek-ai/dsh-web-search-exa` | `web` | `apiKey`、`baseURL`、`searchType`、`numResults`、`highlightsPerResult` |
| `@deepseek-ai/dsh-web-search-perplexity` | `web` | `apiKey`、`baseURL`、`model`、`maxTokens`、`searchRecency` |
| `@deepseek-ai/dsh-workflow-worker-thread` | `subagents` | `provider`、`maxConcurrentAgents`、`maxTotalAgents`、`maxItemsPerCall`、`syncTimeoutMs`、`disposeGraceMs` |


---

## 22. 会话持久事件目录

下面列出当前持久化事件名。`surface` 表示它直接参与对话表层投影；`log-only` 表示它主要用于状态、审计或派生。

| 事件 | 角色 |
|---|---|
| `agent/inbox/spliced` | `log-only` |
| `agent-preset/selected` | `log-only` |
| `approval/asked` | `log-only` |
| `approval/decided` | `log-only` |
| `approval/policy` | `log-only` |
| `assistant/chunk` | `log-only` |
| `assistant/message` | `surface` |
| `command/done` | `log-only` |
| `command/run` | `log-only` |
| `compaction/end` | `log-only` |
| `compaction/prune` | `log-only` |
| `compaction/start` | `log-only` |
| `compaction/summary` | `log-only` |
| `feedback/record` | `log-only` |
| `goal/change` | `log-only` |
| `hook/invoked` | `log-only` |
| `hook/result` | `log-only` |
| `llm/retry` | `log-only` |
| `llm/retry-started` | `log-only` |
| `permission/preset` | `log-only` |
| `plan/mode` | `log-only` |
| `request/context` | `log-only` |
| `request/header` | `log-only` |
| `sandbox/mode` | `log-only` |
| `schedule/change` | `log-only` |
| `session/end-seed` | `log-only` |
| `session/title` | `log-only` |
| `session/title-llm-request` | `log-only` |
| `step/end` | `log-only` |
| `step/start` | `log-only` |
| `subagent/descriptor` | `log-only` |
| `team/member` | `log-only` |
| `team/message/delivered` | `log-only` |
| `team/message/queued` | `log-only` |
| `team/task` | `log-only` |
| `todo/write` | `log-only` |
| `tool/call` | `log-only` |
| `tool/code-dispatch` | `log-only` |
| `tool/code-dispatch-start` | `log-only` |
| `tool/result` | `surface` |
| `tool-workflow/agent-end` | `log-only` |
| `tool-workflow/agent-start` | `log-only` |
| `tool-workflow/run-end` | `log-only` |
| `tool-workflow/run-start` | `log-only` |
| `turn/end` | `log-only` |
| `turn/start` | `log-only` |
| `user/message` | `surface` |
| `web/deepseek-search-llm-request` | `log-only` |


---

## 23. 227 个工作区包完整索引

本节替代原 `packages/**/README*.md`。每个条目给出源码路径、包名和核心职责；更细的函数/类型契约直接读对应源码入口。

### 23.1 自动化协议（`packages/acp`）

- `acp/acp` · `@deepseek-ai/dsh-acp`：通过 JSON-RPC stdio 提供的仅面向自动化的 ACP（Agent Client Protocol） 服务器。程序化客户端可以创建新 harness agent（智能体）、发送文本／图片提示词、收集已提交的 assistant 文本／图片、按策略响应一次性权限请求并取消工作。仓库中的主要客户端是 `dsh-subagent-acp`。
### 23.2 远程接口（`packages/api`）

- `api/gateway` · `@deepseek-ai/dsh-api-gateway`：为 Host 与 Client 两侧的 Cordis 环境提供 Typert RPC endpoint。Host 入口提供 `ctx.typertGateway`，`@deepseek-ai/dsh-api-gateway/client` 则提供 `ctx.remote`；两者使用同一份生成的 `InvocationDescriptor` 约定，并将业务选择交给 API Remotes，将传输、请求关联、信任和响应封装交给 Connection。
- `api/remotes` · `@deepseek-ai/dsh-api-remotes`：为本应用选定的 Host Remote 能力提供双侧 BFF。Host 入口负责 Agent/Session 身份策略；Client 入口以运行时值形式导入生成的 `/remote` 产物，通过 `ctx.remote.$mount()` 挂载每项贡献，并重新导出对应的声明合并。Client 业务包依赖该外观，而不依赖 Gateway 实现或单独的 Remote 运行时入口。
### 23.3 附件（`packages/attachment`）

- `attachment/attachment` · `@deepseek-ai/dsh-attachment`：持久附件服务边界。`ctx.attachments` 校验并持久提交提供方无关的规范化图片，随后返回可序列化的 `ImageAttachmentRef`；消费方绝不会在会话事件中持久保存浏览器路径、对象 URL、提供方 URL 或 base64。
- `attachment/attachment-local` · `@deepseek-ai/dsh-attachment-local`：这是 `@deepseek-ai/dsh-attachment` 的私有本地实现。对象存放在 `/attachments/v1/objects//`，并通过不透明的 `sha256:` 标识符寻址。每个进程都会把每级祖先目录项同步到文件系统根目录，以此一次性证明 home 已持久化。写入使用私有暂存目录、仅所有者可访问的文件、经过同步的临时文件、原子且排他的硬链接发布，并对发布路径执行目录同步（适用于 POSIX；Windows 依赖文件系统元数据日志），确保已报告的引用能够在崩溃后继续存在。
### 23.4 启动（`packages/boot`）

- `boot/app-boot` · `@deepseek-ai/dsh-app-boot`：供 app bin（`dsh` 与 `dsh-acp-demo`）共用的启动粘合层：每个 bin 都是在这些辅助函数之上构建的精简自执行组合，并以自身诊断前缀参数化。这样，Loader 故障行为只由一处负责，不会在已发布产物之间逐渐分化。
- `boot/cmdline` · `@deepseek-ai/dsh-cmdline`：dsh 启动器交给它所引导应用的那条命令行。启动器只解析属于自己的 flag（`--profile`、`--patch`、配置 dump），并把**其后的一切**原样交给配置树，因此 flag 家族、`--help` 文本和解析错误都由应用自己持有，启动器不必知道它们。
### 23.5 组合包（`packages/bundle`）

- `bundle/base` · `@deepseek-ai/dsh-base`：以 profile 组合包形式交付的共享 dsh 核心：`cordis.patch.yml` 在空的 profile 根之上插入全部基础插件行——模型适配器、共享的 `agent-default-model` 选择、工具、持久化、策略、settings／credentials、遥测与核心 spawn／fork subagent provider——作为每个 profile 的 `dsh.profile.bundles` 列表中的第一层。可选的 Codex 与 Claude Code provider 不属于本包及其生产依赖闭包…
- `bundle/headless` · `@deepseek-ai/dsh-headless`：dsh 一次性任务组合包。`cordis.patch.yml` 直接叠加在 `dsh-base` 之上：提供编码 persona 和工具模式、禁用 HMR（热模块替换）、将 Code Mode 的 worker 作为核心执行能力挂载，并插入本包的 `headless-runner` 插件（配置为 `{task}`，从注入的 `headlessStartup` 提供方解析）。它不挂载任何 Host、HTTP server、Web runtime 或浏览器插件。
- `bundle/web-app` · `@deepseek-ai/dsh-web-app`：dsh 浏览器表层组合包。`cordis.patch.yml` 叠加在 `dsh-base` 之上：设置 coding persona，插入 Web 宿主行（webserver、API 网关、workspace、投影缓存、存储）、浏览器插件名录与始终挂载的客户端插件重载链（`dsh-client-hmr`，在重建 watcher 改写客户端 bundle 之前保持空闲），并挂载本包的 `web-runtime` 粘合插件（配置为 `{openBrowser, printUrl, surfaceContext, trustedHosts}`）。…
### 23.6 Web 客户端（`packages/client`）

- `client/connection` · `@deepseek-ai/dsh-client-connection`：协议消费层：客户端插件的 apply 会挂载 `ctx.connection`（共享 API 客户端 + 当前页面的 loopback 状态 + 可观察且按 generation 生效的 `hostDescription` + 单消费方流循环启动器）；导出表层携带协议约定类型、`AbstractApiClient` 抽象，以及循环的 sink／配置类型。每次就绪握手成功后，都会在 `onConnected` 之前发布完整的 `host.describe` 值；generation 失效或显式 stop 会清空它，因此原生能力消费者不会保留已经断线的判断。…
- `client/hmr` · `@deepseek-ai/dsh-client-hmr`：为通过脚本加载的客户端插件提供热重载。web 组合包无条件挂载该行；没有重建 watcher（`pnpm run dev:web`）改写客户端 bundle 时，轮询观察不到变化，链路保持空闲。
- `client/locale` · `@deepseek-ai/dsh-client-locale`：locale 插件：LocaleRuntime——`zh`／`en` 偏好以 `locale.preference` 存储在 `$DSH_HOME/settings.yaml` 中；若没有显式 Host 值，全新浏览器会暂时使用 `navigator` 请求的语言（按主子标签匹配；若其请求的语言本应用都不提供，则使用 `en`）。Host 读取在插件激活后执行，因此 settings 服务不可用不会阻塞页面；读取结果会实时替换浏览器暂定值。settings API 仅限回环请求，因此远程浏览器的选择仅保留在进程内。`locale/change` 仅在切换语言时触发…
- `client/modules` · `@deepseek-ai/dsh-client-modules`：客户端模块系统：Node 内部 ESM loader 的浏览器端对等实现，以惰性 CJS 表实现。web 外壳挂载 vendored cordis Loader 来治理配置项（fiber 生命周期、inject 等待、update/refresh），并通过其 `internal` 约定注入该包的 `ClientModuleLoader`；vendored 一侧唯一的消费点是 `EntryTree.import`，因此替换 `internal` 恰好只会替换「插件代码如何到达」，不会改变其他内容。
- `client/runtime` · `@deepseek-ai/dsh-client-runtime`：客户端 cordis 启动与不依赖 React 的对象服务：SlotRegistry 包装 SlotCore 并提供 renderer 数据源；SessionRuntime 拥有 Session 对象、列表与 scope 状态，以及供已注册 conversation view target 共用的事件窗口与历史分页。WorkspaceRuntime 依赖 SessionRuntime，拥有 Workspace 对象、列表／操作、默认目标派生，以及 New Session 空会话复用入口（`connectWorkspace`）。…
- `client/ui-agent-preset` · `@deepseek-ai/dsh-client-ui-agent-preset`：agent preset 的各个表层：General 设置中的一行，用于选择新建会话据以组装的 preset；新建会话界面上的一枚 chip，用于选择**下一个会话**的 preset；会话标题旁的一个只读标签；以及一个设置页分区，用于管理名单——复制、删除、默认值，以及通往 preset 自身文件的入口。
- `client/ui-attachment` · `@deepseek-ai/dsh-client-ui-attachment`：对话 UI 的动态附件呈现插件。它通过 `ctx.slots.inject` 等待 conversation 包声明 `conversation.input.attachments` 与 `conversation.message.images`，随后注册输入框草稿图片栏、文档拖放目标、聊天历史图片画廊和原图灯箱。conversation slot 持有方提供附件数据、图片加载、回调及其命名空间翻译器；呈现组件保持纯 props，且不从包入口导出。
- `client/ui-brand-official` · `@deepseek-ai/dsh-client-ui-brand-official`：仅当 `DSH_CLIENT_BUILD_PROFILE` 为 `official` 时，本包才填充 `sidebar.brand.mark`、`sidebar.brand.name` 和 `conversation.hero.brand.mark`。其他构建仍会加载插件，但不注册 occupant，因此显示 shell fallback。
- `client/ui-commands` · `@deepseek-ai/dsh-client-ui-commands`：客户端命令 API（`ctx.commandUi`）：以会话为 key 的命令目录缓存、带 `matchSpace`／`matchEnter` 决策钩子的 `/` 命令 source、三类派发（`execute`／`popupSelect`／`leadingInput`），以及面向业务包的 popupSelect 注册。Web 命令 Agent Note 记录了这项决策。
- `client/ui-conversation` · `@deepseek-ai/dsh-client-ui-conversation`：会话领域：骨架（标题栏／标签页／编辑器／空状态）、聊天视图（分组步骤摘要流、流式尾部隔离与轮次状态）、编辑器 dock（与输入区一同 sticky 的会话统计行）、输入区 dock（队列行加 todo 计划条）、详情壳层，以及按 scope 寻址的 ConversationController。工具展示属于 `ui-tool`。
- `client/ui-deliverables` · `@deepseek-ai/dsh-client-ui-deliverables`：产出文件与可点击文件引用功能的属主。Node 侧向系统提示词 registry 注册最终回复指引；浏览器侧把已完成轮次末尾的产出文件行注册到 chat 视图的 `conversation.chat.turnTail` slot，并将收尾正文中匹配的行内代码引用转换为链接。正式提供的组合中只有 Web patch 加载本包；从 cordis.yml 中删去这一项会同时移除提示词、文件行与正文链接。
- `client/ui-directory-picker-browse` · `@deepseek-ai/dsh-client-ui-directory-picker-browse`：应用内目录浏览界面：浏览式选取交互的浏览器半边。它通过 ui-workspace 的两个 directory-flow 洞（`conversation.hero.workspace.directoryFlow` 与 `sidebar.workspaces.directoryFlow`）装入「选择工作区目录」对话框，经 `ctx.workspaces` 驱动本地 Host 的 `host.listDirectory` 与 `host.createDirectory` 原语。它的 node 对侧是 `dsh-host-directory-picker-browse`…
- `client/ui-directory-picker-native` · `@deepseek-ai/dsh-client-ui-directory-picker-native`：原生目录选择界面：原生选取交互的浏览器半边。它通过 ui-workspace 的两个 directory-flow 洞（`conversation.hero.workspace.directoryFlow` 与 `sidebar.workspaces.directoryFlow`）装入一个无渲染占位者，每次收到 `open` 请求就用 `ctx.workspaces.pickDirectory()` 驱动本地 Host 的操作系统选择框，然后通过 owner 会话回报恰好一个结果——选中的路径、取消、或失败。…
- `client/ui-goal` · `@deepseek-ai/dsh-client-ui-goal`：Goal 界面插件（浏览器端部分）：`GoalBar` 条带是 `conversation.input.dock` composer 上下文堆栈中的第二张独立卡片（order 10，位于 Todo 之后、Queue 之前）。活值经 `useProjection('goal')` 到达——host 计算的全量值由历史尾页播种、由 `session/projection` 帧更新——因此本插件不持有领域 store、不设刷新链、不挂事件监听。slot 注入面只携带四个变更动词（edit / pause / resume / clear…
- `client/ui-input-trigger` · `@deepseek-ai/dsh-client-ui-input-trigger`：输入触发流水线插件：光标处的 `/` 与 `@` 检测（词边界 + guard tier 规则）、分组候选菜单，以及把 pick 路由到已注册 source。`ctx.inputTriggers` 拥有 source roster，并按会话 scope（`sessionOf`）各解析一个 `InputTriggerController`；对话接线层在 controller 上驱动 `track`／`arbitrate`／`onSpace`／`adjudicate`。同一个 controller 还暴露 `toggleSource`…
- `client/ui-jobs` · `@deepseek-ai/dsh-client-ui-jobs`：Web 后台任务特性的归属方：向 `conversation.session.header.actions` 贡献一个条目，列出当前会话可见的 `ctx.jobs` 记录。数据完全来自 `dsh-client-runtime` 从 `session/jobs` 帧折叠出的 `jobsBySession` 列表镜像，因此本包不发任何 RPC，除弹层开合外不持有任何状态。
- `client/ui-layout` · `@deepseek-ai/dsh-client-ui-layout`：外壳插件：三栏 AppFrame（拖动手柄与让步链）加 `ctx.layout` 面板几何服务；它注册到运行时拥有的 `root` slot，并声明 `sidebar`、`conversation`、`details` 和 `conversation.empty`。侧边栏的缩放边界是不可见命中条带，详情栏边界则保留其浮动胶囊；让步期间只有详情栏会收缩并随后自动关闭。关闭的侧边栏仍保留 56px 控制栏，详情栏则关闭到零宽度。该包还提供主题呈现器：它消费解析后的 `ctx.theme` 快照…
- `client/ui-message-feedback` · `@deepseek-ai/dsh-client-ui-message-feedback`：单条消息反馈插件的浏览器侧：一对 Like/Dislike 按钮加一个可选备注，作为 `conversation.chat.assistant-actions` 条带的 `feedback` 条目（order 10）贡献。该条带由 `ui-conversation` 声明，渲染在已定稿助手消息的 IconActions 行内、复制与分支之间，因此控件沿用该行的样式与 hover 行为。备注编辑器本身不在这一行里：它是一个 `role="dialog"` 的浮层，portal 到 `document.body` 并锚定在其触发按钮下方，因此无论编辑器是否打开该行都保持单行…
- `client/ui-model-selection` · `@deepseek-ai/dsh-client-ui-model-selection`：模型选择插件（浏览器侧）：**两个入口共用一份会话级目录**，由 `ModelDirectoryResolver`（`ctx.modelDirectories`）持有。对于普通会话，`/model` popupSelect 贡献项（经 `ctx.commandUi` 注册）与 composer 的具名 `conversation.input.model` slot 都通过同一个 `ModelDirectory` 实例，经 `session.models` 加载会话的建议目录，并经 `session.selectModel` 提交。…
- `client/ui-permission-presets` · `@deepseek-ai/dsh-client-ui-permission-presets`：面向两种不同生命周期的浏览器权限界面。「通用」设置行读取显式暴露的 `permission` Settings 描述符，从 host 的动态 `defaultPreset` enum 中推导选项，并携带描述符的 revision 写入一条 `settings.mutate` 路径操作。它的 observable 经 slot 系统的 `hooks` 格传递，因此 React 钩子由渲染器绑定；推送的失效通知会重新获取描述符。这个值仅在后续会话创建时生效；改变它不会切换当前会话。选择 Full access 时必须先显式确认风险，该行随后才会写入。
- `client/ui-plan` · `@deepseek-ai/dsh-client-ui-plan`：Plan mode 状态徽章，纯浏览器 surface 插件。浏览器侧占用会话声明的 `conversation.input.plan` 单实例 seat（位于 access 模式控件右侧）；node 侧是空 apply（roster 行）。plan 行为本身——`/plan` 命令、边界或空闲即时提交的 `plan/mode` 状态、`plan` 投影单元与 policy 段——归 `@deepseek-ai/dsh-plan-mode` 所有，由 host roster 独立组合。
- `client/ui-primitives` · `@deepseek-ai/dsh-client-ui-primitives`：纯 React 原子组件（零 cordis）：StateDot、DisclosureRow、ic_ds_* 图标、Button/Pill/Menu/Modal/Input、Toast 短时横幅、OnboardingSurface 首次使用接管层（portal 到 body 的遮罩加不透明展示层，在且仅在自身生命周期内保持 `#root` 为 `inert`）、markdown 家族（MessageText/MarkdownText/JsonBlock）、只读 JsonTree 检查器、`useAnchoredMaxHeight` 钩子（把底部锚定的浮层高度收敛到锚点上方的视口空间…
- `client/ui-reference` · `@deepseek-ai/dsh-client-ui-reference`：统一的 Web `@file` 与 `@session` source。对于未加引号的 token，浏览器会同时启动 `fileReferences/list` 和 `sessionReferenceResolver/candidates` Remote 调用，以确定性顺序把文件排在会话之前，并使用注册在 locale 字典中的文件夹、文件与会话标签；各行分别渲染在不可选择的文件与会话分组标题下，不显示重复的原始 `reference` source 标题。任一候选领域的失败都会独立降级。尚未闭合的 `@"…` token 只搜索文件。
- `client/ui-renderer` · `@deepseek-ai/dsh-client-ui-renderer`：负责 React 渲染层的浏览器 Cordis 插件。`dsh-client-web` 渲染不依赖框架的启动页并加载完整的客户端插件名册；所有 entry 激活后，它调用 `ctx.uiRenderer.mount(container)`。本包提供该服务、安装 slot 渲染器、hydrate 现有启动 DOM、在下一次绘制前切换到组装完成的应用，并返回 React 根的卸载 disposer。
- `client/ui-settings` · `@deepseek-ai/dsh-client-ui-settings`：设置领域的底座，本身不含任何呈现内容。它提供 `ctx.settingsScope`——每个偏好设置行绑定自己那份持久化命名空间分区所用的宿主传输层；`ctx.settingsSchema`——设置插件使用的同步 schema 重建、校验与不可变路径编辑服务…
- `client/ui-settings-general` · `@deepseek-ai/dsh-client-ui-settings-general`：设置外壳、无特定功能归属文案与持久化产品引导 namespace。它以触发控件和模态设置面板占用 `sidebar.settings`，把 `settings.section` 账本投影成导航、把 `settings.onboarding` 账本投影成每次只挂载一个步骤的引导流程，并在设置页面上注册所有不属于单一功能的内容：触发器、标题栏与关闭控件内容、本地配置文件操作，「通用」分区及其 `settings.general.item` slot，以及 `settings` 字典。它渲染进的那些 slot 类型归 ui-settings——设置领域底座——所有；只有外壳自身的契约类型放在这里…
- `client/ui-settings-models` · `@deepseek-ai/dsh-client-ui-settings-models`：模型设置与产品引导插件。同一个 client Cordis 插件会注册 Models 页面和两个有序的首次使用弹窗：版本化内测声明，以及按条件显示的 DeepSeek 官方凭据步骤。两个步骤共用同一套弹窗组件，并继续由 `settings.onboarding` 排序。Models 平面把三个协议领域汇聚为一个共享快照：`llm.providers`（可配置提供方目录…
- `client/ui-settings-plugin-inventory` · `@deepseek-ai/dsh-client-ui-settings-plugin-inventory`：Web 设置中的只读**插件列表**标签页。浏览器插件注册一个 id 为 `all` 的本地化 `settings.plugins.tab` 贡献；“插件”分区拥有导航入口与标签栏。插件激活期间不会读取 Remote；首次选择该标签页时才挂载组件，并通过 `api-remotes` 懒调用 `ctx.remote.pluginInventory.list()`。
- `client/ui-settings-plugins` · `@deepseek-ai/dsh-client-ui-settings-plugins`：**插件**设置分区及其**插件配置**标签页。该分区拥有标题与紧凑的标签栏；功能插件通过 `settings.plugins.tab` 贡献页面。本包自己的标签页为每个配置由用户拥有的 Host 插件展示一张可展开卡片。卡片展示插件名称及其管辖范围；就地展开后是绑定到该插件 settings 命名空间的手写控件，每个字段标注用户是否覆盖过它，并提供重置回部署组装值的入口。
- `client/ui-sidebar` · `@deepseek-ai/dsh-client-ui-sidebar`：侧边栏外壳插件：负责品牌行、New Session 操作、布局持有的折叠控件、可感知滚动的区域 seat，以及固定在底部的 Settings seat。ui-workspace 持有渲染到 `sidebar.workspaces` 的 Workspace 与 Session 浏览器；本包既不派生其中的行，也不持有其视图偏好。折叠到布局拥有的 56px 轨道仍属于本地呈现行为。约定：slot 系统标准。
- `client/ui-skill` · `@deepseek-ai/dsh-client-ui-skill`：skill（技能）调用 source 的浏览器端：把 `/` 触发的 `skill` source 注册进 `ctx.inputTriggers`。普通会话的候选来自 `skill.list` RPC，以每次调用的 `ClientSessionContext` 投影中的 `{sessionId}` 寻址，host 从会话 header 解析 `cwd`。宿主提供每一个用户可调用的 skill；`modelInvocable: false` 的条目（即 `disable-model-invocation` skill，此路径是其唯一入口）会以当前语言把仅限用户标记作为描述前缀带上。…
- `client/ui-slots` · `@deepseek-ai/dsh-client-ui-slots`：Slot 注册表纯核心、slot 终端设计：SlotMap 声明合并、SlotCore 上唯一的 `register` 组合 API、四 share 组件 props 类型家族、store seat 类型家族，以及 renderer 安装约定。只使用 React 类型；该包不依赖 React，也不依赖 Cordis。
- `client/ui-subagent` · `@deepseek-ai/dsh-client-ui-subagent`：Web subagent 功能 owner：向 `conversation.session.header.lineage` 贡献当前 title 谱系导航，向会话编辑器链贡献按原因区分的只读替代呈现，并保留注册到 `ctx.inputTriggers` 的既有 `@` 引用 source。
- `client/ui-theme` · `@deepseek-ai/dsh-client-ui-theme`：主题插件：基于 --dsw-* token 基础样式表（静态尺度 + 别名语义层）的 ThemeRuntime。该服务拥有实时主题偏好（`light`／`dark`／`system`），将 `system` 通过 `prefers-color-scheme` 解析为实际主题，并发布不可变的 `ThemeSnapshot`，通过 `theme/change` 事件通知变化；它绝不接触 DOM：ui-layout 的呈现器会应用解析后的快照（`html { color-scheme }`、`body[data-ds-dark-theme]`，以及主题的别名 token 内联变量）。…
- `client/ui-tool` · `@deepseek-ai/dsh-client-ui-tool`：Client 工具展示插件。`ui-conversation` 通过 `conversation.chat.node` 的匹配 key 分发每个已排序的 `tool-call` Conversation Node；本包渲染其中的 root 及其 Code Dispatch 子调用，并把每个原子调用通过 keyed slot `tool.call.toolview` 分发。没有注册的工具名称使用通用卡片。
- `client/ui-trajectory` · `@deepseek-ai/dsh-client-ui-trajectory`：Trajectory 渲染按轮次组织的事件记录表，其中可选择用户、助手、工具和嵌套子工具记录。较粗的分割线标示轮次边界，紧凑的行内标记标识步骤，主记录表仅保留索引、事件和内容；选择记录则会打开局部检查器，查看 token 用量、耗时、输入、输出和计时。可滚动的概述区域默认保持滚动条滑块透明，直到鼠标悬停该区域或其中包含键盘焦点时才显示，同时不改变滚动条预留的几何空间。独立运行的压缩（compaction）请求会按时间顺序显示在自己的 `Between turns` 区段中，而带编号的压缩仍位于其所属轮次内。长记录表打开时定位于当前尾部，用户到达已加载范围顶部时加载一页更早的历史…
- `client/ui-user-questions` · `@deepseek-ai/dsh-client-ui-user-questions`：Web 提问功能插件：其浏览器侧把 `question` 条目注册到会话拥有的 `conversation.composer` 键控 slot 中。其主机侧刻意为空——在那里挂载 `dsh-tool-ask-user` 会把工具放进注册表的**全局层**，而全局层会并入每一个 agent（智能体），无论它由哪个 preset 组装，于是一个「两工具」的 benchmark preset 实际会呈现三个。渲染提问是宿主的 UI 能力，拥有该工具则是 agent 的能力，因此 `tool-ask-user` 行属于需要它的各个 preset（以及没有 preset 的 TUI 组装）。
- `client/ui-workflow-run` · `@deepseek-ai/dsh-client-ui-workflow-run`：这个浏览器插件把持久化的顶层工作流运行重建为独立 Chat 节点。它消费由 `dsh-tool-workflow` 拥有的四类 `tool-workflow/*` Session 事件，注册一个 `ConversationNodeDefinition`，并通过 keyed `conversation.chat.node` slot 渲染，不改变现有工作流工具卡。
- `client/ui-workspace` · `@deepseek-ai/dsh-client-ui-workspace`：共享 Workspace 浏览器与选择器插件。`WorkspaceBrowser` 填充侧边栏的 `sidebar.workspaces` slot，`WorkspacePicker` 则填充页面局部 Session Intent 主视觉区的 `conversation.hero.workspace` slot；两个界面使用同一套 Workspace 菜单和添加流程。
- `client/web` · `@deepseek-ai/dsh-client-web`：Web 启动内核：`new AppWebEntry(el, seams?).run()` 分两个阶段挂载客户端。模块阶段调用 Host 安装的 `window.__ModuleLoader__.create()`，传入 `window.__DSH_BOOT__`、外壳静态模块以及可选测试传输覆盖；facade 接纳 parser 预载的 registration 后返回构造好的模块系统与已解析 manifest。本包随后预取 `immediately` 层级。插件阶段挂载仓库内置的 Cordis Loader，通过 Loader 的 `internal` 接口注入该模块系统…
### 23.7 代码运行时（`packages/code-runtime`）

- `code-runtime/code-runtime` · `@deepseek-ai/dsh-code-runtime`：**`CodeRuntime`**（`ctx.codeRuntime`）定义代码运行时做什么，即针对宿主提供的一组异步绑定运行一段模型编写的程序，并报告 `{ value, logs, error? }`，而不规定如何实现。
- `code-runtime/code-runtime-python` · `@deepseek-ai/dsh-code-runtime-python`：`@deepseek-ai/dsh-code-runtime` seam 的 CPython 子进程实现。与 `@deepseek-ai/dsh-code-runtime-worker-thread` 配套；以全新的 `python3` 子进程取代 Node worker 线程，让模型代码从 TypeScript 换成 Python。
- `code-runtime/code-runtime-worker-thread` · `@deepseek-ai/dsh-code-runtime-worker-thread`：这是 `@deepseek-ai/dsh-code-runtime` seam 的 worker 线程实现：`WorkerThreadCodeRuntime` 会在每次运行中使用一个全新的 Node `worker_threads.Worker`，输入 TypeScript，由宿主侧剥离类型，通过消息端口桥接绑定，输出 `{ value, logs, error? }`。**这是隔离措施，而非安全边界**：其信任立场有意与 bash 等价（参见 Code Mode Agent Note 的 Trust posture 章节）…
### 23.8 上下文压缩（`packages/compaction`）

- `compaction/command-compact` · `@deepseek-ai/dsh-command-compact`：通过 `ctx.compaction` 提供面向用户的 `/compact` 压缩（compaction）控制。该插件通过 `ctx.commands` 注册一个全局命令，因此组合中的每个命令适配器都能发现并执行它，无需模型轮次。排队手动压缩 Agent Note拥有接纳、锁与持久性决策。
- `compaction/compaction` · `@deepseek-ai/dsh-compaction`：**`CompactionEngine`**（`ctx.compaction`）定义压缩（compaction）做什么，即判定历史记录是否过大，并将较早范围摘要为单个表层节点，但不规定如何实现。
- `compaction/compaction-basic` · `@deepseek-ai/dsh-compaction-basic`：**基础压缩（compaction）后端**：`BasicCompactionEngine` 实现 `@deepseek-ai/dsh-compaction` Service Definition，使用可复用的 `ctx.tokenMeter` 压力、token 预算保留与摘要。摘要是直接的一次性 `ctx.llm.stream()` 调用，它会回放会话前缀以复用提供方的 KV Cache（可在 `llm/stream` 处拦截）。
- `compaction/compaction-tool-result-pruner` · `@deepseek-ai/dsh-compaction-tool-result-pruner`：可安全回放、不依赖模型的剪枝服务（`ctx.toolResultPruner`）。它会将超出预算的 `tool/result` 表层节点改写为长度受限的头部、固定省略标记和长度受限的尾部，同时在仅追加会话日志中保留完整原始事件。
### 23.9 上下文（`packages/context`）

- `context/agent-instructions` · `@deepseek-ai/dsh-agent-instructions`：为每个会话加载与 `AGENTS.md` 兼容的工作区指令文件。该插件会将初始的用户全局指令与项目指令链注入持久历史，随后发现嵌套文件，并在成功的文件系统工具调用后报告后续变更或移除。
- `context/file-reference` · `@deepseek-ai/dsh-file-reference`：文件引用发现 seam，以及供宿主驱动的用户界面共享、可在浏览器中安全使用的 `@file` 语法。`ctx.fileReferences.list(agent, query, signal)` 为指定 agent（智能体）返回仅含路径的文件或目录候选；具体提供方负责命名空间访问、排序、缓存和失效处理。同一契约以一元 `fileReferences/list` Remote 方法对外可调（`@Remote` 标注在 Service Definition 上，经保留的末位 signal 参数取消），浏览器消费方直接调用 `ctx.remote.fileReferences.list`…
- `context/file-reference-local` · `@deepseek-ai/dsh-file-reference-local`：`ctx.fileReferences` 的本地文件系统实现。它为每个 agent（智能体）维护一个有界的 `WorkspaceFileSearch`，以该会话的 `cwd` 为根目录；缺少该值时回退到宿主进程的 cwd。查询包含 `/` 时，索引会对直接列出的目录项排序；否则会对有界递归索引进行模糊排序。索引永远不会跟随目录符号链接。
- `context/session-reference` · `@deepseek-ai/dsh-session-reference`：`ctx.sessionReferenceResolver` 会把其他会话准备为有界、只读快照，作为带来源信息、面向模型的上下文。它消费 `ctx.sessionQuery` 与后端无关的 compact 检查点标记；不需要 SQLite FTS。支持跨会话 mention 的宿主可以主动启用该服务。
- `context/time-context` · `@deepseek-ai/dsh-time-context`：可选的持久上下文，包含当前带时区时间、附加到当前开放请求的浏览器时区，以及在模型请求准备期间采样的经过时长。默认组合不启用它；Schedule Web overlay 会挂载它，使模型可以按用户的浏览器时区解释未明确限定时区的日期和时间。决策记录：持久 time-context Agent Note。
- `context/tmux-context` · `@deepseek-ai/dsh-tmux-context`：可选启用的持久上下文，记录本 agent（智能体）进程所在的 tmux session、window、pane，以及该 window 的 pane 树布局。在准备模型请求时每轮采样一次；随附 Web／无头组合不包含它。决策记录见：tmux-context Agent Note。
### 23.10 核心（`packages/core`）

- `core/agent` · `@deepseek-ai/dsh-agent`：Agent 接口、注册表、进程本地发起方作用域，以及 `agent/*` 事件词汇。每个插件（UI、钩子、编排器）都面向此处定义的 `Agent` handle 编程；它不依赖循环，因此循环可以替换。
- `core/agent-default-model` · `@deepseek-ai/dsh-agent-default-model`：该部署默认值供入口在创建尚无会话级模型选择的 Agent 时使用。`AgentDefaultModelConfig` 提供 `ctx.agentDefaultModel`；`dsh --profile headless` 这类直接入口与 ApiProxy 这类由 Host 支撑的入口读取同一服务，而不是分别持有平行的提供方／模型默认值。
- `core/agent-loop` · `@deepseek-ai/dsh-agent-loop`：agent（智能体）的唯一具体实现插件和循环驱动器。其包内部实现满足 `Agent` 接口，并驱动会话、轮次和步骤的生命周期。
- `core/agent-tool-presentation` · `@deepseek-ai/dsh-agent-tool-presentation`：agent preset 用来声明「模型看到的工具是哪一种形态」的那一行：`native`（全部 schema）、`code`（只有 `run_code` 加一份生成的 TypeScript SDK）或 `both`。
- `core/scope` · `@deepseek-ai/dsh-scope`：带作用域的注册原语。`createScope(ctx, key)` 创建一个带标签的 Cordis 上下文，其底层 fiber 拥有通过该上下文进行的每项注册。`scopeOf(ctx)` 读取标签；`scopeTarget(base, key)` 将带作用域的事件路由到键相同的监听器，同时让无作用域监听器保持全局可见。键可以构成可选的父链（`bindScopeParent`）：注册视图沿链**向下**继承——子作用域看得见祖先各层，近者遮蔽远者——事件放行沿链**向上**扩展——标签为祖先的监听器能收到子孙键的事件，反向永不成立。…
- `core/session` · `@deepseek-ai/dsh-session`：事件溯源的会话日志和内存存储。`Session` 是 agent（智能体）全部交互历史的仅追加真源，LLM（大语言模型）消息历史由它*派生*。原始日志之上维护一个 **surface** 层（产生消息事件的有序投影），以便高效派生和压缩（compaction）。
- `core/system-prompt` · `@deepseek-ai/dsh-system-prompt`：系统提示词组装注册表。插件可以贡献有序段、工具 schema 和具名变量。循环在每个步骤组装一次，并将结果渲染为完整的模型提示词。此插件拥有静态 harness 身份和全局部署 persona；agent（智能体）作用域的 persona 会遮蔽全局默认值。
- `core/tools` · `@deepseek-ai/dsh-tools`：工具注册表与执行流水线。工具插件注册各自的 schema 和执行器；agent loop（智能体循环）依次让每次调用经过 `tools/pre-execute`（可扩展的允许／拒绝门禁）→ 已注册的单调守卫 → `tools/execute`（供超时／重试／指标插件使用的环绕分发包装层）→ `tools/post-execute`（检查／替换结果、附加上下文）→ 由工具定义持有的 `finalizeContent` 边界 → 仅观测的 `tools/result` 通知。…
### 23.11 凭据（`packages/credentials`）

- `credentials/authorization` · `@deepseek-ai/dsh-authorization`：授权 Service Definition（`ctx.authorization`）。有些凭据无法配置，只能获取：拿到它意味着与人对话——打开这个页面、粘贴那个码、选一个账号。本 seam 拥有这段对话及其生命周期，但从不拥有协议本身。
- `credentials/credentials` · `@deepseek-ai/dsh-credentials`：凭据 Service Definition（`ctx.credentials`）。一条准则，三个推论：
- `credentials/credentials-local` · `@deepseek-ai/dsh-credentials-local`：文件型凭据提供方：四层来源，一套明确的优先级。
### 23.12 远程执行（`packages/e2b`）

- `e2b/e2b` · `@deepseek-ai/dsh-e2b`：一个 E2B 沙箱的共享生命周期所有者。文件系统与进程管理适配器注入 `ctx.e2b`，等待其唯一的 SDK 句柄，因此处于同一个远程 Linux 工作树与进程环境中。本包固定使用 `e2b@2.29.1`；可选组合见包族索引。
- `e2b/fs-e2b` · `@deepseek-ai/dsh-fs-e2b`：`@deepseek-ai/dsh-fs` 提供方约定的 E2B 实现。它没有配置：先加载 `@deepseek-ai/dsh-e2b`，再用本服务取代 `dsh-fs-local`。该提供方使用所有者的远程 cwd 和 SDK 句柄，因此文件工具观察到的环境与 E2B 后端 Bash 进程相同。
- `e2b/subprocess-e2b` · `@deepseek-ai/dsh-subprocess-e2b`：`@deepseek-ai/dsh-subprocess` seam 的 E2B 实现。先加载 `@deepseek-ai/dsh-e2b`，再用本服务取代 `dsh-subprocess-local`。现有的 Bash、PTY 和 LSP 消费方随后会在共享远程沙箱中执行，无需 E2B 专用的能力包。
### 23.13 示例（`packages/examples`）

- `examples/acp-demo` · `@deepseek-ai/dsh-acp-demo`：ACP（Agent Client Protocol）自动化服务器应用：默认 agent（智能体）主干、客户端通过 `@deepseek-ai/dsh-acp` 创建的 agent、JSONL 持久化，以及语义检查点机制，并通过一个 JSON-RPC stdio bin 对外提供服务。程序化客户端创建新会话；此包不挂载人工交互 UI。
- `examples/agent-spine-demo` · `@deepseek-ai/dsh-agent-spine-demo`：将**默认的不含执行器、不含 UI 的 agent（智能体）主干**作为一个 Cordis 组合包插件。它加载每个 harness agent 所需的固定服务集合，包括本地 skill（技能）提供方，并将循环的 `agents` 列表作为自身配置转发。因此，应用包只需添加入口和可替换后端，就能组合出可工作的 agent。
- `examples/jsonrpc-demo` · `@deepseek-ai/dsh-sdk-jsonrpc-demo`：只包含 bin 的应用，启动外部 `cordis.yml`；其 `jsonrpc` 入口通过按换行分隔的 stdio 为 SDK 客户端提供服务。配置负责组合主干、后端和服务插件。发布的 `dsh-jsonrpc-agent` bin 从配置项目解析裸插件。Python SDK 的 `dsh-jsonrpc-agent-pkg` 单文件可执行运行时改用 `lib/packaged-bin.js`：已打包的裸插件从封闭运行时包树解析，相对插件仍以配置目录为基准。
### 23.14 实验能力（`packages/experimental`）

- `experimental/agent-team` · `@deepseek-ai/dsh-experimental-agent-team`：隐式 Root Agent Teams 领域。`ctx.agentTeams` 在 Lead Session 日志中维护扁平的 Lead／teammate roster、持久 peer mailbox 与共享任务 DAG。Agent Teams Agent Note负责协作和隔离决策；Team 子系统目录记录持久数据的字面形态与服务 API。
- `experimental/tool-agent-team` · `@deepseek-ai/dsh-experimental-tool-agent-team`：`ctx.agentTeams` 的 scoped 模型适配器。它会在每个隐式 Lead 与持久 teammate scope 中安装 Agent Teams 策略和协作工具。scoped Team 定义会覆盖同名的旧全局 continuable-subagent control，因此同时挂载两者的组合必须禁用旧定义。
### 23.15 动态扩展（`packages/extensions`）

- `extensions/cordis-client-runner` · `@deepseek-ai/dsh-cordis-client-runner`：动态双半插件包的浏览器半。host 侧 runner 把每个定义的代码留在进程内存里，并经一条 `cordis/request-run` 事件向打开的页面发问「要不要运行它」；本包回答这个请求、把定义变成活的浏览器插件，并把 `dynamicCordisRunner/retract` 事件变回干净的页面。
- `extensions/cordis-host-runner` · `@deepseek-ai/dsh-cordis-host-runner`：由模型挂载的动态包在 host 侧的那一半：定义注册表、host 半所用的 `node:vm` 沙箱与 fiber 生命周期、invoke handler 表，以及由某个浏览器页面执行的 run 往返。以 `ctx.dynamicCordisRunner` 提供。面向模型的工具在 `@deepseek-ai/dsh-tool-cordis` 中；浏览器半由 `@deepseek-ai/dsh-cordis-client-runner` 装载。
- `extensions/tool-cordis` · `@deepseek-ai/dsh-tool-cordis`：自引用 Cordis 工具集：五个面向模型的工具，操作当前 DSH 进程中的实时运行时。注册表、vm 沙箱与浏览器广播属于 `@deepseek-ai/dsh-cordis-host-runner`（`ctx.dynamic`），本工具集注入它——只装这些工具而不装 runner 的组合永远不会激活它们。沙箱语义、动态包生命周期与组合及既定决策详见工具集 Agent Note。
- `extensions/ui-cordis` · `@deepseek-ai/dsh-client-ui-cordis`：Cordis 动态插件的浏览器半：一个覆盖整个框架的面板，操作 host 持有的全部定义；以及一张只读的 `cordis_define` 卡片，记录某个会话定义了什么。
### 23.16 反馈（`packages/feedback`）

- `feedback/command-feedback` · `@deepseek-ai/dsh-command-feedback`：与触发方式无关的会话反馈，以及面向用户的 `/feedback` 采集。本包导出 `recordFeedback(session, text)`；该函数会追加一个仅写入日志的 `feedback/record` 事件。该插件通过 `ctx.commands` 注册一个全局命令，因此每个已组合的命令适配器都能发现它；随附的 Web 客户端无需模型轮次即可执行。
- `feedback/message-feedback` · `@deepseek-ai/dsh-message-feedback`：本包提供由 Host 拥有、针对单条已完成 assistant 消息的可编辑反馈。它注册 `ctx.messageFeedback`，在 storage-domain 中为每个 Session 持久化一条绑定生命周期的伴随记录（sidecar），并发布 Host `messageFeedback.list`、`messageFeedback.put` 与 `messageFeedback.delete` 一元 Remote 契约。它与不可变的 Session 级 `feedback/record` 事件相互独立，不执行遥测交接。消息反馈伴随记录 Agent Note拥有其设计边界。
### 23.17 文件系统（`packages/fs`）

- `fs/fs` · `@deepseek-ai/dsh-fs`：**`FileSystem`**（`ctx.fs`）定义同一个执行世界中的存储原语，包括解析路径、公开规范化进程路径与文件 URI、检查包含关系、完整或流式读取文本、有界读取原始字节、检查／列出元数据、原子写入和应用字面量编辑，但不规定实现方式。两个变更操作都**可选** 接收版本防护，因此 `ctx.fs` 本身就是完整且不受约束的存储 seam。本包还拥有由工具分派、政策插件监听的 `fs/*` 政策事件词汇。
- `fs/fs-local` · `@deepseek-ai/dsh-fs-local`：`ctx.fs` 提供方约定（`@deepseek-ai/dsh-fs`）的**本地文件系统实现**。它使用宿主文件系统支持十二个 `FileSystem` 原语；将其作为插件加载会填充 `ctx.fs`。
- `fs/fs-observation-policy` · `@deepseek-ai/dsh-fs-observation-policy`：**fs-observation-policy 插件**：它记录观测到的存在或缺失状态，并在 `ctx.fs` 提供方约定（`@deepseek-ai/dsh-fs`）之上增加编辑前读取和带防护的写入/编辑；它通过 `fs/*` 事件门禁参与，**不是**通过方法服务。该插件**不**注册 `ctx.fsPolicy` 服务，也没有公开的 `read`/`write`/`edit`/`resolve` 方法。它是文件系统栈的政策层：不是可替换 seam，而是不应位于 `FileSystem` 提供方基类上的政策。
- `fs/fs-sandbox` · `@deepseek-ai/dsh-fs-sandbox`：`SandboxedFileSystem` 扩展 `LocalFileSystem` 并注册为 `ctx.fs`。它逐字继承全部文本存储机制（解析、stat、读取／流式读取、列出、原子写入、按读取、匹配、写入顺序执行的编辑临界区），只为 `writeText`/`editText` 增加按调用的模式围栏。读取始终直接通过：所有模式都允许读取。
- `fs/tool-fs` · `@deepseek-ai/dsh-tool-fs`：**面向模型的文件系统工具**（`read`、`read_image`、`write`、`edit`）及其**执行器**。这是文件系统栈的消费方层：拥有工具名称、JSON Schema、参数校验、提示词段、**读取窗口逻辑**和结果格式化。它**直接**通过 `ctx.fs` 提供方约定（`@deepseek-ai/dsh-fs`）读取、写入和编辑。新鲜度与观察策略由独立插件（`@deepseek-ai/dsh-fs-observation-policy`）通过 `fs/*` 事件门禁贡献；工具不与其方法耦合。使用施加沙箱限制的提供方时，逐会话执行需要共享沙箱策略服务…
- `fs/tool-fs-search` · `@deepseek-ai/dsh-tool-fs-search`：**面向模型的文件系统发现工具**（`glob`、`grep`）由打包的 ripgrep 二进制支持，而不是由 `ctx.fs` 提供方方法或系统 `rg` 安装支持。普通 Node 部署从 `@vscode/ripgrep` 解析平台二进制；pkg 单文件运行时解析与可执行程序共置的 `-rg` 伴随文件，伴随文件缺失时回退到依赖中的二进制。两种载体均打包 ripgrep，因此注册是无条件的，没有加载期可用性探针。每次调用都通过 `ctx.subprocess` seam 以固定 argv 向量 spawn 解析出的二进制（前缀 `--no-config`…
- `fs/tool-str-replace-editor` · `@deepseek-ai/dsh-tool-str-replace-editor`：基于 `ctx.fs`、面向模型的独立 `str_replace_editor`。它可与持久 Bash、一次性 Bash、沙箱 Bash 或其他终端接口组合。
### 23.18 目标（`packages/goal`）

- `goal/command-goal` · `@deepseek-ai/dsh-command-goal`：面向用户的 `/goal` 控制，基于 `ctx.goals` 实现。该插件通过 `ctx.commands` 注册一个全局命令，因此每个已组合的命令适配器都能发现并执行它，无需模型轮次。用户 goal 命令 Agent Note 负责用户体验与组合决策。
- `goal/goal` · `@deepseek-ai/dsh-goal`：事件溯源的同会话目标状态。该服务在 agent（智能体）的现有会话中保留一个当前待完成目标，同时将继续执行的权限作为进程本地续行启用状态。goal 领域 Agent Note 负责设计理由；goal 类型目录记录具体的数据形状。
- `goal/goal-round-driver` · `@deepseek-ai/dsh-goal-round-driver`：`ctx.goals` 的同会话续行驱动器。它通过公开 `Agent` 与会话服务，把 phase 为 active 且已启用续行的目标转换为连续的 Goal Round；同会话驱动器 Agent Note 记载竞态与生命周期方面的设计理由。
- `goal/tool-goal` · `@deepseek-ai/dsh-tool-goal`：`ctx.goals` 的面向模型控制 API：`get_goal`、`create_goal` 和 `update_goal`。goal 工具 Agent Note 负责权限拆分与 Codex 风格用户体验。
### 23.19 执行守卫（`packages/guard`）

- `guard/repeat-tool-reminder` · `@deepseek-ai/dsh-repeat-tool-reminder`：这是一个仅提供建议的循环中断器，而非面向模型的工具：它不会出现在工具列表中，不会否决或改写调用，只增加一种行为。它监视每个 agent（智能体）的工具调用流，统计以完全相同的规范化参数连续调用同一工具的次数；达到所配置的连续次数时，它会注入逐级增强的提示，要求模型停止重复、重新阅读上一次结果，并改用其他方案或结束任务。究竟是换一种方式重试、收集更多证据还是完成任务，仍完全由模型决定：合理的重复调用既不会延迟，也不会受阻。决策记录见 repeat-tool-reminder Agent Note。
- `guard/timeout-policy` · `@deepseek-ai/dsh-tool-call-timeout-policy`：工具调用超时强制执行器：单个 `tools/execute` 环绕分发监听器，会在 `exec.signal` 上设置单次调用的协作式截止时间；适用于声明了 `timeoutMs` 且声明位于其 `ToolDefinition` 上的工具。该截止时间先到时，它返回结构化 `TOOL_TIMEOUT` 结果。预算从工具自身的声明中读取（`ToolDefinition.timeoutMs`，由拥有该工具的插件设置），因此此插件是**零配置**的。它是 `tools/execute` 包装层的参考实现，也是面向模型工具调用预算的强制执行归属地（超时库 Agent Note）。
### 23.20 钩子（`packages/hooks`）

- `hooks/hook-protocol` · `@deepseek-ai/dsh-hook-protocol`：Claude Code／Codex hook 协议格式（wire format）的**共享核心**。它不是 Cordis 插件：不注册也不注入任何内容。它是一个**库**，提供两个桥接插件（`@deepseek-ai/dsh-hooks-claude-code`、`@deepseek-ai/dsh-hooks-codex`）导入的方言无关原语，使两者都无需重复实现协议中相同的部分。
- `hooks/hooks-claude-code` · `@deepseek-ai/dsh-hooks-claude-code`：一个 Cordis 插件，在 harness 的规范拦截点上运行用户现有 **Claude Code** hook 配置（`hooks.json` 或 settings 文件的 `hooks` key）中受支持的 command hook 子集。它是 hooks 子系统的 **CC 方言**部分，负责桥接中 CC 格式的逐事件 stdin payload、CC 的 env 和 `${CLAUDE_PLUGIN_ROOT}`／`${CLAUDE_PROJECT_DIR}` 替换，以及将 hook 的中性结果映射为 harness 的类型化 Decision。…
- `hooks/hooks-codex` · `@deepseek-ai/dsh-hooks-codex`：一个 Cordis 插件，在 harness 的规范拦截点上运行用户现有 **Codex** hook 配置的受支持子集。它是 hooks 子系统中采用 **Codex 方言** 的一侧。方言无关原语来自 `@deepseek-ai/dsh-hook-protocol`；该桥接负责处理 Codex 形状的 payload、matcher 模式和决策映射。
### 23.21 宿主（`packages/host`）

- `host/apiproxy` · `@deepseek-ai/dsh-host-apiproxy`：所有客户端共用的 API 网关由三部分组成：TypeScript API 约定（`src/api/`，不依赖 Node，可从浏览器导入）、fetch 载体对（`src/fetch/`：宿主侧的 `toFetchHandler`，以及客户端侧的 `AbstractApiClient` 与平台子类）和宿主侧实现（`src/api-proxy.ts`：`createApiProxy` 加上默认导出的 `ApiProxyService` 网关插件，其配置为 `{nativeOpen?, sessionExportCompressionLevel?, coldBlankProbeMaxBytes?}`…
- `host/directory-picker` · `@deepseek-ai/dsh-host-directory-picker`：web GUI 宿主的工作区目录选择是一项能力 seam。抽象的 `DirectoryPicker` 服务（`ctx.directoryPicker`）是其 Service Definition。该服务只提供一个方法：`capability()`，它返回一个可辨识联合类型，说明操作者如何选择目录。后端之间的用户交互不同，不只是实现不同：`{ kind: 'native', pick(signal) }` 在宿主屏幕上打开一个原生 OS 选择器（`-native`）…
- `host/directory-picker-auto` · `@deepseek-ai/dsh-host-directory-picker-auto`：目录选择 seam 的**自适应选择器**：一个只有 node 半侧的插件，在启动时一次性判定宿主处境，并把匹配的双面后端——`-native` 或 `-browse`——作为真实的 Loader 条目挂进内存根树（绝不持久化到配置文件；根树的 `write()` 是 no-op）。由于后端以普通条目的形式到达，其 browser half 被 client 模块表发现的方式与配置行完全相同，因此对判定出的选择，seam 的“一行同时换两面”不变式依然成立。卸载该选择器会再次移除该条目，连同两面一起卸载。
- `host/directory-picker-browse` · `@deepseek-ai/dsh-host-directory-picker-browse`：目录选择 seam 的**应用内浏览后端**：`BrowseDirectoryPicker` 以 `browse` 能力注册 `ctx.directoryPicker`——基于 Node 标准库（跨 OS 适配本就由它承担）提供单层目录列举与子目录创建。宿主屏幕上不渲染任何东西，因此该后端能服务原生后端无法触及的远程客户端。
- `host/directory-picker-native` · `@deepseek-ai/dsh-host-directory-picker-native`：目录选择 seam 的**原生 OS 选择器后端**：`NativeDirectoryPicker` 以 `native` 能力注册 `ctx.directoryPicker`，其 `pick(signal)` 每次调用打开一个原生选择器并解析出所选绝对路径（取消时为 `null`）。平台工具不经 shell 调用：macOS 使用 `osascript`，Linux 使用 Zenity 并以 KDialog 回退；调用方的中止信号会终止原生进程。Windows 在 spawn 的子进程中打开现代 `IFileOpenDialog`——由 koffi 在子进程主线程上驱动的 COM 会话…
- `host/frontend-static` · `@deepseek-ai/dsh-host-frontend-static`：Web 壳的 SPA dist 服务器：一个函数插件（配置为 `{distIndex}`），占据 webserver 的唯一回退席位，并通过显式 index 入口服务已构建的前端目录。`distIndex` 可读时，dist 根目录和配置的 index 路径以 HTTP 200 渲染 `index.html`；其他现有文件直接提供。dist 根目录内缺失或不是文件的目标——包括缺失的配置 index——返回空的 404；越出 dist 根目录的遍历返回 403，未知扩展名按 `application/octet-stream` 提供…
- `host/plugin-inventory` · `@deepseek-ai/dsh-host-plugin-inventory`：当前 Cordis Loader 树的只读 Host 投影。`PluginInventoryGateway` 注册 `pluginInventory` 服务，并发布一个由 Typert 生成的直接 Remote：`pluginInventory/list`。每次调用都直接读取 `ctx.loader.entries()`，跳过结构性的 group 行，再按 Loader 顺序返回其余条目，并且只包含 Loader 条目 id、模块标识、有效启用状态与当前根 Fiber 阶段。
- `host/webserver` · `@deepseek-ai/dsh-host-webserver`：Web HTTP 与 upgrade route 注册插件（默认导出 `WebServer`，配置为 `{host, port}`）：一个在激活时开始监听的 `node:http` 服务器，提供 `ctx.webServer`。`register(route)` 添加具名的 `exact`／`prefix` HTTP route；`registerUpgrade(route)` 添加精确 pathname 的 upgrade route；同一张表内的重复路径会抛错，因为 route 模式是组合层约定，冲突即配置错误；两者返回的 disposer 都会移除注册。…
### 23.22 identity（`packages/identity`）

- `identity/anonymous-user-id` · `@deepseek-ai/dsh-anonymous-user-id`：会话遥测、直接反馈确认与 DeepSeek 提供方请求共用的匿名身份。`getOrCreateAnonymousUserId()` 返回一个限定于单个 harness home 的随机 UUID v4，并以裸行形式持久化到 `$DSH_HOME/.anonymous-user-id`（未设置 `DSH_HOME` 时为 `~/.dsh/.anonymous-user-id`）。OpenTelemetry 后端将其作为 Resource 的 `user.id` 上报；`/feedback` 在确认文本中包含同一个值…
### 23.23 交互（`packages/interaction`）

- `interaction/commands` · `@deepseek-ai/dsh-commands`：由插件负责、供交互式 UI 适配器使用的面向用户命令注册表。插件命令注册 Agent Note定义了其边界与分发约定。
- `interaction/permission-presets` · `@deepseek-ai/dsh-permission-presets`：通过 `ctx.permissionPresets`（`PermissionPresetService`）提供面向用户的权限预设。每个配置名称都会将 `sandbox/mode` 与 `approval/policy` 组成一组；默认项为 `workspace-write`（`workspace-write` + `ask`）和 `danger-full-access`（`danger-full-access` + `never`）。UI 适配器可以将该表作为单个选择器公开，而沙箱执行与审批仍分别消费各自的调节项。
- `interaction/tool-ask-user` · `@deepseek-ai/dsh-tool-ask-user`：模型侧 `ask_user_question` 工具，基于 `ctx.userQuestions` 实现。当模型需要确认、选择结果或缺失的信息才能继续时，它可以借此向用户提出简明问题。
- `interaction/user-approval` · `@deepseek-ai/dsh-user-approval`：与通道无关的一次性审批 seam。`ctx.approval.request(req)` 返回 `allowed-once`、`rejected`、`cancelled` 或 `unavailable`；应答者缺失或失败时会以拒绝方式关闭，授权也只适用于所请求的操作。确切事件签名见 approval.md 的生成区块。
- `interaction/user-questions` · `@deepseek-ai/dsh-user-questions`：用户交互 Service Definition。它定义 `ctx.userQuestions`，供面向模型的工具或权限插件在需要暂停工作并询问人类决定时使用。
### 23.24 后台任务（`packages/jobs`）

- `jobs/jobs` · `@deepseek-ai/dsh-jobs`：后台任务注册表约定（`ctx.jobs`）。抽象的 `JobRegistry` 及其词汇类型在同一份约定下为长时间运行的生产方提供共享 id、owner 隔离、读取、取消、等待、通知和清理；进程局部注册表位于 `dsh-jobs-local`。生产方插件使用其不透明 id namespace 扩展 `JobKindMap`。
- `jobs/jobs-local` · `@deepseek-ai/dsh-jobs-local`：`@deepseek-ai/dsh-jobs` 注册表约定的进程本地实现：`LocalJobRegistry` 把每条记录保存在内存中，按 kind 签发 `-N` id，并且只交出全新快照，从不交出实时状态。作为插件加载后即注册为 `ctx.jobs`。
- `jobs/tool-jobs` · `@deepseek-ai/dsh-tool-jobs`：`ctx.jobs` 的面向模型控制器：三个与 kind 无关的工具、完成通知和一个后台工作提示词区段。加载该插件会附加 `ctx.jobs.start()` 所要求的控制器。
### 23.25 模型（`packages/llm`）

- `llm/llm` · `@deepseek-ai/dsh-llm`：提供方无关的 LLM（大语言模型）词汇与抽象服务。本包定义 agent loop（智能体循环）、会话日志和所有插件共同使用的规范词汇。
- `llm/llm-deepseek` · `@deepseek-ai/dsh-llm-deepseek`：harness LLM（大语言模型）seam 的 DeepSeek chat-completions 适配器：直接 `fetch` + SSE（Server-Sent Events，由 `eventsource-parser` 分帧），将官方协议格式（wire format；真源：API 文档 guides/thinking_mode、guides/tool_calls、api/create-chat-completion）转换为 `StreamChunk` 协议。
- `llm/llm-pi-ai` · `@deepseek-ai/dsh-llm-pi-ai`：基于 `@earendil-works/pi-ai` 的 harness LLM（大语言模型）seam 通用多提供方适配器。一个插件实例拥有一份以路由为键的提供方 profile 字典；每个请求使用 `GenerateOptions.provider` 选择 profile，并针对该路由已配置的 catalog 解析 `GenerateOptions.model`。点名了已安装 pi-ai 提供方的路由会继承其端点、协议格式（wire format）与模型 catalog 作为默认值，并逐字段覆盖；pi-ai 未提供的路由则整体声明出来，因此接入 OpenAI 兼容网关、自建服务…
- `llm/llm-retry` · `@deepseek-ai/dsh-llm-retry`：一个函数插件，通过 agent loop（智能体循环）在已关闭步骤上触发的 `agent/request-error` waterfall（瀑布式事件）应用确切提供方重试策略。它不包装 `ctx.llm.stream()`：每次适配器调用仍是一次提供方尝试，每次重试都会开启新的编号轮次。
- `llm/token-meter` · `@deepseek-ai/dsh-token-meter`：通过单例 `ctx.tokenMeter` 服务进行具备回放感知能力的 token 测量。它从持久日志为每个会话推进一个隔离 fold，因此压缩（compaction）与其他压力敏感插件可以共享计量，无需依赖 `CompactionEngine`。
### 23.26 语言服务（`packages/lsp`）

- `lsp/lsp` · `@deepseek-ai/dsh-lsp`：**LSP 能力 seam**：抽象 `LspService`（`ctx.lsp`）定义 harness 具备哪些语义代码导航能力（转到定义、查找引用、查找实现、悬停），并通过语言服务器提供方实现，不把模型约定绑定到本地子进程。
- `lsp/lsp-stdio` · `@deepseek-ai/dsh-lsp-stdio`：`ctx.lsp` 的**通用 stdio 语言服务器后端**。一个插件实例接受一张命名服务器表，并逐配置项注册一个隔离的提供方。它通过 `ctx.fs` 读取，并通过 `ctx.subprocess` 启动，因此服务器与源文件始终位于已挂载的执行世界中。这是通用主机，而不是语言服务器目录或安装器：部署需要显式配置命令与映射，预设应放在 `cordis.yml` overlay 中。
- `lsp/tool-lsp` · `@deepseek-ai/dsh-tool-lsp`：面向模型的 **`lsp` 工具**，基于 `ctx.lsp`：一个只读工具，通过四种操作执行精确代码导航。它拥有模型 schema、提示词指引、坐标转换、结果限制与格式化，以及 UI 呈现；不导入任何提供方。
### 23.27 模型上下文协议（`packages/mcp`）

- `mcp/mcp-client` · `@deepseek-ai/dsh-mcp-client`：MCP 客户端桥接插件：连接外部 Model Context Protocol 服务器，把它们的工具注册到 `ctx.tools`，使模型能够通过服务器限定名称（`mcp____`）将其作为原生工具使用。
### 23.28 计划（`packages/plan`）

- `plan/plan-mode` · `@deepseek-ai/dsh-plan-mode`：按 agent（智能体）分别记录到日志的 plan 协作状态，提供由部署方配置的引导内容、用于直接进入的 `/plan [message]` 命令、用于直接退出的 `/plan off` 命令，以及经用户评审的 `exit_plan_mode` 退出方式。Plan mode 是软引导；沙箱模式和批准策略各自强制执行限制，且不读写 plan 状态。
### 23.29 preset（`packages/preset`）

- `preset/agent-presets` · `@deepseek-ai/dsh-agent-presets`：按 preset 组装 agent（智能体）。**preset** 是一个目录，其中放置一份 `agent.cordis.yml`；roster 在整个进程内只把它挂载一次（常驻 scope），命名它的每个会话通过把自己 agent 的 scope key 认父到该挂载（`dsh-scope` 的父链）来加入。挂载的工具、提示词段落与投影单元只存在一份，覆盖所有已加入的 agent——其插件本就按 Session/Agent 分键存状态，会话在共享实例内互不串扰——而完全没有 agent 的宿主读取方（冷读记录）也能按 preset id 解析到同一份常驻注册。
- `preset/persona` · `@deepseek-ai/dsh-persona`：把 agent（智能体）人设做成一个可组装的行：它既可以遮蔽部署级人设，也可以拥有完整系统提示词。
### 23.30 runtime-diagnostics（`packages/runtime-diagnostics`）

- `runtime-diagnostics/invariants` · `@deepseek-ai/dsh-invariants`：用于包自有运行时不变量检查的可配置注册表服务。根插件注册 `ctx.invariants`；它不包含产品检查或产品包导入。每个工作区包都发布一个 `./invariant` 配套入口，用于注册其精确 npm 包名。
### 23.31 沙箱（`packages/sandbox`）

- `sandbox/sandbox` · `@deepseek-ai/dsh-sandbox`：进程沙箱 Service Definition。负责定义 `ctx.sandbox` 服务约定（`SandboxProvider`）与 harness 共享的限制词汇：`SandboxMode`（`read-only`／`workspace-write`／`danger-full-access`，仅限文件操作）、`SandboxEnforcement`（`full`／`partial`，针对每种内核 ABI）、`SandboxExecutionPolicy`（每次调用的完整模式及工作区根目录）、`SandboxPolicy`（其中受限制的子集）…
- `sandbox/sandbox-local` · `@deepseek-ai/dsh-sandbox-local`：`dsh-sandbox` seam 的本地实现。它选择并缓存一个平台 runner：Linux 优先选择可工作的 `bwrap`，否则选择 Landlock；macOS 使用 Seatbelt；Windows 使用 ACL 受限令牌 runner。多个候选项会按顺序探测，只有一个候选项时则直接选择。
- `sandbox/sandbox-policy` · `@deepseek-ai/dsh-sandbox-policy`：沙箱策略解析的唯一归属位置：部署默认 `SandboxMode` 与回退根目录，加上每个会话的持久模式覆盖和不可变工作区根目录。每项负责强制执行的能力在每次调用时都会收到一项解析完成的模式与根目录策略；模型在每次请求前会收到当前策略，而不会另收一份能力清单。
- `sandbox/sandbox-windows-acl` · `@deepseek-ai/dsh-sandbox-windows-acl`：面向 harness 沙盒 seam 的 Windows 写入限制沙盒后端：一个 Node.js/koffi 实现的、对 huoyaoyuan/windows-acl-restrict-poc（`10e4dfb`，固定修订版本）机制的移植，挂载为 `@deepseek-ai/dsh-sandbox-local` 链中报告 `enforcement: 'partial'` 的 win32 一级（`workspace-write` / `read-only` 两种模式）；Linux/macOS 后端在同一包中。
### 23.32 计划任务（`packages/schedule`）

- `schedule/schedule` · `@deepseek-ai/dsh-schedule`：`dsh-schedule` 为未来创建的 live 根 agent（智能体）提供 3 个会话范围内的工具，用于管理持久提醒。版本 1 接受正的安全整数 `after_seconds` 延时、显式绝对时间 `at` 目标，以及至少 5 分钟的固定速率 `every_seconds` 间隔。会话事件日志拥有提醒状态；timer、工具值和模型 follow-up 都是该日志的可丢弃投影。
### 23.33 开发工具包（`packages/sdk`）

- `sdk/client` · `@deepseek-ai/dsh-sdk-client`：以子进程方式驱动 DeepSeek Harness 运行时、走 stdio JSON-RPC 的 TypeScript 客户端 SDK——Python SDK（`deepseek-harness`）的设计孪生，共享同一个运行时对端、协议与分层：`DeepSeekHarness` 是高层自有运行 API，`HarnessClient` 是低层协议客户端。包（package）根枚举消费方接口：两层客户端、面向调用方的类型和 `JsonRpcResponseError`；源模块、规范化辅助函数与订阅投递机制不供消费方导入。纯库：不在任何 Cordis 上下文注册…
- `sdk/protocol` · `@deepseek-ai/dsh-sdk-protocol`：DeepSeek Harness SDK 运行时的共享协议格式（wire format）：一个按换行分帧的 JSON-RPC 2.0 传输类，加上协议两端共同使用的具名请求、结果与通知类型。包根枚举协议消费方接口；源模块不支持深层导入。服务端是 `dsh-sdk-jsonrpc-server` 插件；客户端是 `dsh-sdk-client`（TypeScript）与 Python SDK（后者复现这些结构但不导入它们）。纯库——无插件、无 Config、无注册。
- `sdk/server` · `@deepseek-ai/dsh-sdk-jsonrpc-server`：`jsonrpc` 插件通过 stdio 提供以换行符分隔的 JSON-RPC，使进程外 SDK 客户端能够驱动 harness agent（智能体）。`HarnessSdkJsonRpcServer` 负责协议方法和通知；传输与具名协议类型位于 `dsh-sdk-protocol`，与客户端 SDK 共享；`jsonrpc-demo` 提供外围的 `cordis.yml` 应用。
### 23.34 会话（`packages/session`）

- `session/session-checkpoint-policy` · `@deepseek-ai/dsh-session-checkpoint-policy`：已持久化的 agent（智能体）的语义持久性策略。它会在模型适配器收到请求前、顶层工具正文可产生外部副作用前，以及每个 `agent/pre-step` 边界为事件溯源会话创建检查点，使前一响应与有序工具结果在下一个请求前已持久化。
- `session/session-persistence` · `@deepseek-ai/dsh-session-persistence`：会话持久化是一项能力 seam。抽象的 `SessionPersistence` 服务（`ctx.sessionPersistence`）是其 Service Definition。它要求持久化后端持久存储、重新加载和列出会话，但不规定具体存储实现。该 seam 采用与 `dsh-shell` 相同的角色划分（见能力 seam）：本包负责 Service Definition，同级包负责 Service Provider，Consumer 注入该服务。
- `session/session-persistence-jsonl` · `@deepseek-ai/dsh-session-persistence-jsonl`：JSONL 持久会话存储后端：`SessionPersistence` 的一个具体实现（`dsh-session-persistence` seam）。每个会话有一个仅追加的逻辑 JSONL 日志，默认存储为 `.jsonl.zstd`；禁用压缩时使用原始 `.jsonl`。
- `session/session-persistence-sqlite` · `@deepseek-ai/dsh-session-persistence-sqlite`：一个可选启用的 SQLite `SessionPersistence` 提供方。它将符合条件的 `assistant/chunk` 连续段存入打包后的物理行，对大型 payload 选择性应用 Zstandard 压缩，并对来源序列进行 delta 编码，同时恢复完全一致的逻辑 `SessionEvent[]`。随产品交付的组合均不选择它；部署方需显式挂载本包并提供数据库路径。
- `session/session-projection` · `@deepseek-ai/dsh-session-projection`：会话投影 Service Definition 与驱动注册表。它拥有 `ctx.sessionProjections`：该注册表在已提交的会话事件上驱动每个已注册的投影单元，并向载体提供完整的最终值，目前包括 api-proxy 历史尾页和 `session/projection` 推送帧。领域注册的只是纯数学；驱动权归框架。session-projection RFC 记录了设计理由。
- `session/session-projection-cache` · `@deepseek-ai/dsh-session-projection-cache`：持久投影缓存（`ctx.sessionProjectionCache`）：把每个投影单元的状态保存为检查点，基于域数据形态（domain data form）每会话一条记录（`session_projcache` 域——出厂 JSON 后端将其落在配置的存储根目录下、`workspace.json` 旁边）。设计权威：session-projection RFC（persisted projection cache 一节）。
- `session/session-stats` · `@deepseek-ai/dsh-session-stats`：注册 `sessionStats` projection 单元的函数插件：从步边界、流式 chunk、工具配对与已组装的 assistant 消息折叠出全日志会话数字——轮/步计数以及 LLM、工具、首 token、解码墙钟时间——经 session-projection 缝对外提供（registry 快照、变更流，以及每一个 projection 载体：history 尾页、`session/projection` 推送帧、会话列表行）。客户端由此渲染分页与压缩都无法改变的全会话数字；参考消费者是 Web 聊天统计条，其窗口折叠以相同字段名充当无单元时的回退。
- `session/session-telemetry` · `@deepseek-ai/dsh-session-telemetry`：遥测（telemetry）Service Definition 声明 `SessionTelemetrySink` 后端约定，捕获协调器把会话记录传给实现该约定的任意上报 SDK 后端。捕获侧可跟随实时会话事件，也可按需回放权威会话日志前缀。本包调用 `emit()` 后就停止处理：批处理、重试、排队与丢失策略都属于后端自身的 SDK，本包既不规定也不包装。设计依据与被否决的替代方案见复活 Agent Note、反馈门控投递与无缓冲反馈回放。
- `session/session-telemetry-otel` · `@deepseek-ai/dsh-session-telemetry-otel`：遥测（telemetry）seam 的 OpenTelemetry 后端，也是部署方唯一要加载的条目。其 `mode` 决定 seam 是实时跟随会话事件、仅在记录反馈时回放权威日志，还是将遥测留在本地。上传模式会原样组合 OTel JS SDK（`LoggerProvider` → `BatchLogRecordProcessor` → OTLP/HTTP 日志导出器），把每条已交接记录映射到 `logger.emit()`…
- `session/session-title` · `@deepseek-ai/dsh-session-title`：由日志支持的会话标题，提供即时确定性回退与一个可选异步提供方。每次已接受的修订都是仅写入日志的 `session/title` 事件；`foldSessionTitle()` 与 `ctx.sessionTitle.get()` 会选择最新事件，并返回其事件 seq 和时间戳。
- `session/session-title-all-prompts-llm` · `@deepseek-ai/dsh-session-title-all-prompts-llm`：可选的 `ctx.sessionTitle` 提供方，通过 `ctx.llm` 总结所有符合条件的用户消息。它注册 `all-prompts` 节奏，并在每条新用户提示词后启动新 revision，同时使用预置历史与子会话提示词。较新的 revision 会中止并取代旧工作；即使提供方忽略取消，也无法提交陈旧输出。
- `session/session-title-first-prompt-llm` · `@deepseek-ai/dsh-session-title-first-prompt-llm`：可选的 `ctx.sessionTitle` 提供方，通过 `ctx.llm` 总结第一条符合条件的用户消息。它注册 `first-prompt` 节奏，只在全新非 fork 会话首次创建回退时自动运行，并将结果归因于该消息的确切 seq。自动失败会保留回退，之后只能通过 `ctx.sessionTitle.refresh()` 重试。
- `session/session-title-llm` · `@deepseek-ai/dsh-session-title-llm`：由模型支持的会话标题提供方的共享实现策略。它解析辅助路由，将精确选中的用户消息封装为 JSON，记录可分发的确切请求，应用语言感知的标题指令，强制执行输入和输出预算，组合超时与调用方取消，组装流，并返回规范化文本，同时给出确切来源 seq 以及生成该文本时使用的提供方／模型路由。
### 23.35 session-query（`packages/session-query`）

- `session-query/session-log-export` · `@deepseek-ai/dsh-session-log-export`：Web Session 日志下载控制，使用 `dsh-host-apiproxy` 拥有的 Host 流式 ZIP 端点。Host 半包注册 `/export`；浏览器半包在 Session Header 中提供 111×32 的 `Session log` 操作，以及一个供该按钮与斜杠命令共用的下载控制器和弹窗。ZIP 生成、原始 JSONL/zstd 读取、子 Session、附件、背压和 HTTP 错误语义仍由 ApiProxy 下载实现负责。
- `session-query/session-query` · `@deepseek-ai/dsh-session-query`：`SessionQueryEngine` 是组合式抽象 `ctx.sessionQuery` 约定。它对实时 `ctx.sessions` 和可选的动态挂载 `ctx.sessionPersistence` 实现精确会话历史取回、关系跟踪和与提供方无关的过滤；具体后端实现它的两个全文方法。匹配 id 只产生一条记录：实时事件优先，而 `live` 和 `persisted` 会报告两种来源的可用性。如果不可变 header 存在冲突，则以 `SESSION_QUERY_SOURCE_CONFLICT` 失败。
- `session-query/session-query-sqlite` · `@deepseek-ai/dsh-session-query-sqlite`：具体 `ctx.sessionQuery` 提供方。`SqliteSessionQueryEngine` 从 Service Definition 包继承精确读取、跟踪和提供方无关的过滤，并使用 SQLite FTS5 实现其两个全文方法。搜索使用实时优先的逻辑会话语料库，并按每个会话中匹配度最高的事件对跨会话结果分组。
- `session-query/tool-session-query` · `@deepseek-ai/dsh-tool-session-query`：位于 `ctx.sessionQuery` 之上、经工作区授权的模型工具。该 opt-in 包只依赖统一接口，并注册 `session_search`、`session_event_search`、`session_trace`、`session_event_trace` 和 `session_event_read`；已发布的宿主组合默认不挂载它。
### 23.36 设置（`packages/settings`）

- `settings/settings` · `@deepseek-ai/dsh-settings`：用户设置 Service Definition（`ctx.settings`）。一个提供方持有按 namespace 分节的原始文档；插件注册 namespace schema 并读取分层解析值：schema 默认值，然后注册方的组合 `base`（其 cordis.yml entry 配置子集），最后用户文档分节。不挂载提供方时消费方行为不变：仍只按 entry 配置解析，因此任何组合有无 settings 都能工作。
- `settings/settings-file` · `@deepseek-ai/dsh-settings-file`：基于文件的设置提供方。一个 YAML 或 JSON 文档承载全部 namespace 分节；外部编辑经 `ctx.settings` 热发布，`update()` 在写锁下先重读文档再原子写回，保留用户的 YAML 注释、当前未加载插件所拥有的分节，以及任何本进程尚未观察到的磁盘变更。
### 23.37 命令执行（`packages/shell`）

- `shell/bash-local` · `@deepseek-ai/dsh-bash-local`：`@deepseek-ai/dsh-shell` 执行器 seam 的本地 Service Provider，构建在 `@deepseek-ai/dsh-subprocess` 服务之上：`LocalBashExecutor` 每次调用都通过 `ctx.subprocess` 把 `bash -c ` 作为受管进程组 spawn，并负责所有 Bash 层职责（命令默认值补全与上限、超时与取消分类、适合模型的终端环境，以及后台读取时面向模型的 stdout/stderr 合并）。…
- `shell/bash-sandbox` · `@deepseek-ai/dsh-bash-sandbox`：这是使用沙箱能力的 `@deepseek-ai/dsh-shell` 执行器 seam 的 Service Provider。加载它时，应**用它替代** `@deepseek-ai/dsh-bash-local`，并同时加载 `ctx.sandbox` 提供方（例如 `@deepseek-ai/dsh-sandbox-local`）及 `ctx.sandboxPolicy`；默认模式和工作区根目录由后者负责，并与受沙箱约束的文件系统共享这些设置。无需使用替代工具插件；`dsh-tool-bash` 会检测执行器的 `sandboxMode` 能力并添加升权字段。
- `shell/pwsh-local` · `@deepseek-ai/dsh-pwsh-local`：`@deepseek-ai/dsh-shell` 执行器 seam 的本地 PowerShell Service Provider，基于 `@deepseek-ai/dsh-subprocess` 服务：`PwshLocalExecutor` 每次调用以受管进程的方式通过 `ctx.subprocess` spawn `pwsh -NoLogo -NoProfile -NonInteractive -Command `，并负责所有 PowerShell 相关事项——可执行文件解析、命令默认化与上限、超时/取消分类、面向模型的终端环境，以及后台读取的 stdout/stderr 合并。…
- `shell/pwsh-sandbox` · `@deepseek-ai/dsh-pwsh-sandbox`：沙盒消费型的 `ctx.shell` 执行器 seam 的 PowerShell 实现：每条命令以 `pwsh -NoLogo -NoProfile -NonInteractive -Command ` 运行，**经 `ctx.sandbox` 隔离**，选定模式、强制完整性、拒绝事实都盖在每次结算的结果上。它是 `@deepseek-ai/dsh-bash-sandbox` 的 pwsh 孪生…
- `shell/shell` · `@deepseek-ai/dsh-shell`：**`ShellExecutor`**（`ctx.shell`）定义 bash 后端做什么，即运行前台命令与启动后台进程，但不规定如何实现。job id、所有权、收集、取消与通知属于通用 `ctx.jobs` 运行时。
- `shell/shell-env` · `@deepseek-ai/dsh-shell-env`：工具无关的 shell 环境插件：拥有 `ctx.shellEnv` 注册表，管理受信任的、每次执行收集的 `DSH_*` 变量，供模型可见的 shell 工具（`dsh-tool-bash`、`dsh-tool-pwsh`）收集进每次 shell 调用的环境。内置 shell 事实（`DSH_HOME`、`DSH_SHELL=1`、`DSH_SESSION_ID`）归注册表自身所有；其他插件可以注册额外的可枚举事实，注册随插件纤维（fiber）释放，重复所有权或未声明的运行时键会响亮失败。
- `shell/tool-bash` · `@deepseek-ai/dsh-tool-bash`：模型侧 `bash` 工具，注册在 `ctx.shell` 执行器 seam 上。前台执行始终位于该 seam 之后；后台进程句柄会注册到通用 `ctx.jobs` 运行时，并通过 `job_output`、`job_list` 和 `job_kill` 控制；这些工具由 `@deepseek-ai/dsh-tool-jobs` 提供。
- `shell/tool-bash-persistent` · `@deepseek-ai/dsh-tool-bash-persistent`：模型可见的 `bash(command)`，底层复用一个按所有者隔离的 `ctx.terminals` shell。该包拥有工具约定和 shell 复用；PTY 后端与沙箱策略由部署选择。
- `shell/tool-pwsh` · `@deepseek-ai/dsh-tool-pwsh`：注册在 `ctx.shell` 执行器 seam 之上的面向模型的 `pwsh` 工具。面向由 PowerShell 执行器（如 `@deepseek-ai/dsh-pwsh-local`）支撑 `ctx.shell` 的 Windows 组合；工具约定是 PowerShell 方言：原生 `C:\...` 路径与 `$env:NAME` 变量。…
- `shell/tool-pwsh-persistent` · `@deepseek-ai/dsh-tool-pwsh-persistent`：模型侧 `pwsh(command)`，由一个 owner 作用域的 `ctx.terminals` shell 支撑。本包拥有工具契约与 shell 复用；部署方选择 terminal backend（配置 `shellDialect: pwsh` 的 `terminal-bash` 实例）与沙箱策略。它是 `tool-bash-persistent` 的 Windows 对应物：相同的持久状态契约，PowerShell 方言。
### 23.38 技能（`packages/skill`）

- `skill/skill` · `@deepseek-ai/dsh-skill`：纯 agent skill（智能体技能）提供方注册表。
- `skill/skill-badge` · `@deepseek-ai/dsh-skill-badge`：可选的内置 skill（技能）提供方，向 `ctx.skills` 贡献 `dsh-badge`。该 skill 提供官方「powered by dsh」Markdown 片段和随包分发的 PNG，供无法可靠导入远程图片的系统使用。
- `skill/skill-filesystem` · `@deepseek-ai/dsh-skill-filesystem`：`ctx.skills` 注册表的本地文件系统提供方。
- `skill/tool-skill` · `@deepseek-ai/dsh-tool-skill`：面向模型的 skill（技能）目录和 `skill` 工具。
### 23.39 大输出落盘（`packages/spill`）

- `spill/spill` · `@deepseek-ai/dsh-spill`：**`SpillStore`**（`ctx.spillStore`）定义 spill 后端做什么，即持久化某个工具过大的文本，并返回面向模型的定位信息与取回指引；它不规定如何实现。
- `spill/spill-local` · `@deepseek-ai/dsh-spill-local`：`@deepseek-ai/dsh-spill` 存储 seam 的**本地文件系统**实现。它注册为 `ctx.spillStore`，将工具产生的过大文本持久化到私有的会话级文件；定位信息是文件路径，取回指引会告诉模型对该路径使用 `read` 或 `grep`。
- `spill/spill-policy` · `@deepseek-ai/dsh-spill-policy`：**工具结果 spill 策略**：一个 `tools/post-execute` 转换器，用于防止过大的纯文本工具结果进入模型上下文。当最终结果超过 `maxInlineBytes` 时，它会通过 `ctx.spillStore` 保存完整文本，并将面向模型的结果替换为有界的首尾预览、后端定位信息与取回指引。
### 23.40 通用存储（`packages/storage`）

- `storage/storage` · `@deepseek-ai/dsh-storage`：非会话数据的存储中心（`ctx.storage`）：具名后端注册表加已挂载的数据形式设施。中心自身不执行 IO：后端拥有介质，数据形式拥有语义。存储家族概述列出了这些包；领域 KV 存储 Agent Note记录了设计理由。
- `storage/storage-domain` · `@deepseek-ai/dsh-storage-domain`：DeepSeek Harness 存储中心的领域数据形式：在所有已配置的后端注册后，公开可注入的 `ctx.storageDomain` 服务及对应的 `ctx.storage.domain` 投影。一个领域通过 `defineDomain`（zod 记录 schema、从 `z.infer` 派生的类型）声明一次，通过 `DomainFacility.open` 打开，并由具有最终决定权的内存状态提供服务：读取同步执行；写入在每个领域各自的一条链上串行化，先在已路由后端达到持久状态，再更新内存并发出 `domain/changed`。打开领域的消费方负责管理句柄的生命周期…
- `storage/storage-json` · `@deepseek-ai/dsh-storage-json`：存储中心的 JSON 后端：配置根目录下每个单元使用一个人类可读的 `.json` 文件，注册为后端 `json`。设计见领域 KV 存储 Agent Note。
- `storage/storage-sqlite` · `@deepseek-ai/dsh-storage-sqlite`：存储中心的 SQLite 后端：注册为后端 `sqlite`，通过一个数据库提供 `kv` facet；该数据库由 `node:sqlite` 操作，可以是单个文件，也可以是 `:memory:`。设计与取舍见领域 KV 存储 Agent Note。
### 23.41 子智能体（`packages/subagent`）

- `subagent/subagent` · `@deepseek-ai/dsh-subagent`：subagent seam 允许一个 agent（智能体）通过具名提供方把工作委派给子 agent。调用方统一使用 `ctx.subagents` 服务 API；提供方决定子 agent 在当前进程、其他进程，还是通过未来的传输方式运行。
- `subagent/subagent-acp` · `@deepseek-ai/dsh-subagent-acp`：ACP（Agent Client Protocol）提供方会在全新的子进程中运行每个 subagent，并作为 Agent Client Protocol 客户端驱动它。这是 spawn 与 fork 的进程外替代方案：子 agent（智能体）拥有自己的运行时、会话、模型配置和工具。
- `subagent/subagent-claude-code` · `@deepseek-ai/dsh-subagent-claude-code`：本包（package）注册由 Profile 命名、默认名称为 `claude-code` 的 Claude Code subagent 提供方。每次接受运行请求后，它都会在发起委托的会话工作区中调用官方 Claude Agent SDK，让锁定版本的 SDK 选择随包安装的平台 CLI，提交一个自包含的文本任务，并通过共享的 `dsh-subagent` 结果约定返回严格的最终答案或独立的安全失败诊断。
- `subagent/subagent-codex` · `@deepseek-ai/dsh-subagent-codex`：本包注册由 Profile 命名、默认名称为 `codex` 的 Codex subagent 提供方。每次接受运行请求后，它都会在发起委托的会话工作区中使用 `app-server --stdio` 启动官方包内 Codex wrapper，创建一个临时 Codex 线程，提交一个自包含的文本任务，并通过共享的 `dsh-subagent` 结果约定返回选定的最终答案或独立的安全失败诊断。
- `subagent/subagent-dsh-sdk` · `@deepseek-ai/dsh-subagent-dsh-sdk`：SDK 提供方会在全新的子进程中把每个 subagent 作为完整的 DeepSeek Harness 运行时运行，并经由 TypeScript SDK 客户端 通过 stdio JSON-RPC 驱动。它是 `subagent-acp` 之外的第二个进程外后端，差异在协议格式（wire format）和子进程约定：ACP（Agent Client Protocol）后端能驱动任何 Agent Client Protocol agent（智能体）；本后端专门驱动 harness SDK 运行时（`dsh-jsonrpc-agent` bin 或打包后的可执行文件）…
- `subagent/subagent-fork-in-process` · `@deepseek-ai/dsh-subagent-fork-in-process`：fork 提供方会创建一个进程内子 agent（智能体），并以父 agent 已完成的对话轮次作为初始内容。它与 spawn 共用全部运行机制；唯一的行为差异是会话初始内容。
- `subagent/subagent-in-process-driver` · `@deepseek-ai/dsh-subagent-in-process-driver`：本包是两个进程内提供方共用的运行驱动器。spawn 不传入会话初始内容；fork 传入父 agent（智能体）已完成轮次的前缀。其余机制，包括深度、子 agent 创建、可选的子 agent 定制、结果读取、取消和 dispose（资源释放），都在此共用同一套实现。
- `subagent/subagent-spawn-in-process` · `@deepseek-ai/dsh-subagent-spawn-in-process`：spawn 提供方会在当前进程中创建一个全新的子 `Agent`。子 agent（智能体）有自己的会话，看不到父 agent 的对话历史，并复用宿主的 agent 工厂及 LLM（大语言模型）/工具服务。
- `subagent/tool-subagent` · `@deepseek-ai/dsh-tool-subagent`：基于一个已配置 `ctx.subagents` 提供方、面向模型的委派工具。更换提供方只会改变传输，不会改变执行约定。
- `subagent/tool-subagent-control` · `@deepseek-ai/dsh-tool-subagent-control`：可选的全局具名 `send_message`、`interrupt_agent` 与 `list_agents` 工具是 `ctx.subagents` 之上的轻量适配器。绑定提供方的 `@deepseek-ai/dsh-tool-subagent` 实例会为每种传输注册不同的委派工具；这个单独加载的包只注册一次共享控制工具，因此多个委派工具绝不会重复注册全局控制工具。根插件注册 `send_message` 与 `interrupt_agent`，且只要求 `subagents`；可单独加载的 `./list-agents` 插件注册 `list_agents`…
- `subagent/tool-subagent-report` · `@deepseek-ai/dsh-tool-subagent-report`：可选的子级作用域 `report` 工具是 `ctx.subagents.reportFrom()` 之上的轻量适配器。它为每个可继续的进程内子级提供一条返回通道，指向启动该子级的 Agent（智能体），并安装指示子级使用该通道的提示词 section。本包注册的是可继续子级设置贡献，而不是全局工具，因此该工具及其指引只存在于这些子级内部。根 Agent、一次性 subagent、远程 subagent 提供方、同级作用域以及不关联 Agent 的工具执行都不会提供或执行它。安装本包只授予这项子级作用域能力…
### 23.42 子进程（`packages/subprocess`）

- `subprocess/subprocess` · `@deepseek-ai/dsh-subprocess`：子进程 seam（`ctx.subprocess`）是一个执行世界的进程部分。抽象的 `SubprocessRuntime` 公开可执行文件查找、普通受管 `spawn` 和一项终端进程原语；其词汇涵盖原始／收集式 stdio、进程与终端句柄、退出事实、进程树／会话清理，以及受管的 `DSH_*` 环境命名空间。本地实现位于 `dsh-subprocess-local`。
- `subprocess/subprocess-local` · `@deepseek-ai/dsh-subprocess-local`：`@deepseek-ai/dsh-subprocess` seam 的本地 Service Provider。`LocalSubprocessRuntime` 解析本地可执行文件，以显式 stdio spawn 普通 detached 进程树，并通过 `node-pty` 加平台进程检查实现终端进程。该实现没有任何配置：每项处置方式、限制、终端尺寸、宽限期与目录都来自调用方能力 seam（`dsh-bash-local`、`dsh-lsp-stdio` 和 `dsh-terminal-bash`）。
### 23.43 持久终端（`packages/terminal`）

- `terminal/terminal` · `@deepseek-ai/dsh-terminal`：限定所有者范围的持久 PTY seam。`TerminalSessionService` 注册为 `ctx.terminals`，生成不透明的会话 id，通过具名后端路由创建操作，将每个操作限制在完全相同的活跃 `Agent` 内，并在该 agent（智能体）或服务 dispose（资源释放）时等待后端完全停稳。
- `terminal/terminal-bash` · `@deepseek-ai/dsh-terminal-bash`：这是一个基于 `ctx.subprocess.spawnTerminal`、为 `ctx.terminals` 提供的持久 shell 后端。它在共享 `ctx.sandboxPolicy` 下启动交互式 shell，保留有界的逐行输出并检测就绪状态；进程管理提供方则负责 PTY 分配、环境清理、前台进程组、信号发送和完整终端会话清理。因此，同一个 PTY 后端可以与本地或远程执行世界提供方组合。
- `terminal/tool-terminal` · `@deepseek-ai/dsh-tool-terminal`：基于 `ctx.terminals` 提供 6 个面向模型的工具：`terminal_open`、`terminal_send`、`terminal_read`、`terminal_signal`、`terminal_close` 和 `terminal_list`。每项操作都要求提供完全相同的发起 `Agent`，因此即使模型获知另一个 agent（智能体）的 id，也无法操作其终端。
### 23.44 测试支撑（`packages/test-support`）

- `test-support/acp-snapshot` · `@deepseek-ai/dsh-acp-snapshot`：ACP（Agent Client Protocol）快照套件工具包：无密钥快照层（`pnpm run test:snapshot`，见测试策略）背后的共享机制。示例只需场景表和 fixture（测试前置数据）目录就能获得完整快照套件；每项比较/保护机制都位于此处，受每文件覆盖率门禁约束，而不是在每个示例中复制。
- `test-support/agent-loop-testkit` · `@deepseek-ai/dsh-agent-loop-testkit`：为运行具体 `AgentLoop` 的测试共享挂载先决依赖。`mountAgentLoopTestDependencies(ctx, options?)` 按依赖顺序安装 LLM（大语言模型）、会话、系统提示词、工具和 agent（智能体）服务，然后在 agent loop 挂载前返回。
- `test-support/client-runtime` · `@deepseek-ai/dsh-client-test-runtime`：面向客户端功能测试的 jsdom slot 测试运行时：真实 Cordis `Context`、生产 `SlotRegistry` 与 UI 渲染器，围绕带类型的 session/workspace 测试替身组装。功能套件无需逐套件手搭机器即可测遍声明、注册、scope、store、inject、渲染、更新与销毁——且不存在任何生产逻辑的第二份实现。
- `test-support/llm-mock-server` · `@deepseek-ai/dsh-llm-mock-server`：可编脚本的 OpenAI 兼容 HTTP／SSE（Server-Sent Events）服务器，用于在无提供方密钥的情况下测试真实 LLM（大语言模型）适配器、agent loop（智能体循环）和恢复策略。它接受 `POST /chat/completions` 和 `POST /v1/chat/completions`；每个已接受请求按到达顺序消费一个已配置行为。无效的请求方法、路径、Bearer token 和 JSON 不会消费脚本条目。
- `test-support/llm-replay` · `@deepseek-ai/dsh-llm-replay`：用于无密钥快照测试的 LLM（大语言模型）回放插件。它根据已记录的**会话 JSONL** fixture（测试前置数据）重建模型流，使测试无需 API 密钥即可针对固定的模型 transcript（文本记录）启动真实 agent（智能体）。配置 `providers` 后，它会注册仅用于回放的适配器，其模型目录可供测试模型发现功能的场景使用；未配置 `providers` 时，它会安装无需模型发现功能的测试所用 catch-all `llm/stream` waterfall（瀑布式事件）。
- `test-support/loader-smoke` · `@deepseek-ai/dsh-loader-smoke`：用于测试通过 Cordis Loader 启动应用和 `cordis.yml` 的共享子进程 harness。`resolveExampleLaunch` 选择本地 `src` mode（tsx 和根 tsconfig 路径）或 CI `lib` mode（普通 Node 和包导出）；选择依据为显式 mode 或 `DSH_EXAMPLE_MODE`。
### 23.45 待办（`packages/todo`）

- `todo/tool-todo` · `@deepseek-ai/dsh-tool-todo`：面向模型的 `todo_write` 工具：agent（智能体）的完整任务列表，每次调用都会整体替换。
### 23.46 类型化远程调用（`packages/typert`）

- `typert/generator` · `@deepseek-ai/dsh-typert-generator`：TypeScript 项目分析器和模型驱动的 Typert 生成器。在生成任何产物之前，它会先将开发者编写的源类型树转换为独立于编译器的 `FaceModel` 和 `TypeGraph` 数据。静态分析无需 Cordis 即可消费该模型；各产物生成组件均不会接收 TypeScript 抽象语法树（AST）或类型检查器对象。
- `typert/generator/tests/fixtures/remote-model` · `@fixture/remote-workspace`：该包的当前公开契约以源码导出、配置 schema 与测试为准。
- `typert/generator/tests/fixtures/remote-model/packages/domain` · `@fixture/domain`：该包的当前公开契约以源码导出、配置 schema 与测试为准。
- `typert/generator/tests/fixtures/remote-model/packages/remote` · `@fixture/remote`：该包的当前公开契约以源码导出、配置 schema 与测试为准。
- `typert/generator/tests/fixtures/type-model` · `@fixture/workspace`：该包的当前公开契约以源码导出、配置 schema 与测试为准。
- `typert/generator/tests/fixtures/type-model/packages/client` · `@fixture/client`：该包的当前公开契约以源码导出、配置 schema 与测试为准。
- `typert/generator/tests/fixtures/type-model/packages/host` · `@fixture/host`：该包的当前公开契约以源码导出、配置 schema 与测试为准。
- `typert/generator/tests/fixtures/type-model/packages/write` · `@fixture/write`：该包的当前公开契约以源码导出、配置 schema 与测试为准。
- `typert/loader` · `@deepseek-ai/dsh-typert-loader`：生成的 Typert 产物所用的 Loader 集成，仅支持 Node。该插件需要 `ctx.loader` 和 `ctx.typert`；它本身不提供注册表。
- `typert/protocol` · `@deepseek-ai/dsh-typert-protocol`：该包提供不依赖编译器的声明，由业务包、生成的 Typert 产物、宿主网关和客户端 API 共享。它负责 Remote 服务基类、装饰器、显式绑定回退、可通过声明合并扩展的协议映射、调用描述符、编解码器和提供方约定；它不执行 TypeScript 分析，也不注册具体 Cordis 服务。
- `typert/registry` · `@deepseek-ai/dsh-typert-registry`：生成的 Typert 产物所用的运行时注册表。每个注册项包含某个包在一个 face 上的业务反射信息，以及可选的运行时 Zod schema；`ctx.typert` 会以原子方式同时注册两者，并在发起调用的 Cordis fiber 释放时一并移除它们。TypeScript 分析和代码生成由 `dsh-typert-generator` 负责。
### 23.47 基础工具（`packages/util`）

- `util/atomic-write` · `@deepseek-ai/dsh-atomic-write`：零依赖的原子文件替换，供绝不允许在磁盘上留下不完整、被符号链接劫持或权限过宽内容的文件型存储共用：用户设置文档（`dsh-settings-file`）与凭据存储（`dsh-credentials-local`）。
- `util/brand` · `@deepseek-ai/dsh-brand`：`Branded` 名义类型原语：一个微小的**仅类型**包，无运行时代码，也不依赖其他 harness 包；所有负责跨边界 id 的包都会共享它。
- `util/home-paths` · `@deepseek-ai/dsh-home-paths`：DeepSeek Harness 用户数据的共享文件系统路径辅助工具。
- `util/launch-environment` · `@deepseek-ai/dsh-launch-environment`：把本次运行的环境冻结为一份不可变快照，并记住**每个值来自哪一层**。消费方用它而不是 `process.env` 解析面向用户的值，因为各层的可信程度并不相同，而压平后的视图无法区分它们。
- `util/native-command` · `@deepseek-ai/dsh-native-command`：宿主原生 OS 集成共享的**零依赖免 shell `execFile` 运行器**：一次 `runNativeCommand(command, args, signal)` 调用直接 spawn 可执行文件（绝不拼 shell 字符串），以 utf8 捕获 stdout/stderr，把调用方的 abort 传播为子进程终止，并在 Windows 上隐藏瞬时控制台窗口。失败时，调用会以错误拒绝；该错误附带退出 `code` 与两路已捕获输出，调用方无需重跑即可分类（工具缺失、已取消、真实失败）。
- `util/output-retention` · `@deepseek-ai/dsh-output-retention`：一个轻依赖的**保留**库：为必须限制返回上下文量的工具提供有界的面向模型输出。调用方将项或文本分片送入有界对象，然后取回保留的内容和精确的省略元数据。
- `util/timeout` · `@deepseek-ai/dsh-timeout`：超时的**时序与分类**部分：一个零依赖纯函数库（无运行时 harness 依赖），由每个需要限制调用方超时提示、启动 deadline，并在之后区分「已超时」与「已取消」的能力共享。
### 23.48 网络访问（`packages/web`）

- `web/tool-web` · `@deepseek-ai/dsh-tool-web`：面向模型的 web 工具套件 `web_search` 与 `web_fetch`，构建于 web 能力 seam（`ctx.web`）之上。它只负责面向模型的事项：工具名称、JSON Schema、snake_case 参数名称、提示词区段、结果数量上限、结果格式、HTML→markdown 呈现，以及 UI 呈现投影——`presentCall`、`presentResult`（以 `kind: 'search' | 'fetch'` 区分的 `card: 'web'` 结果卡片）…
- `web/web` · `@deepseek-ai/dsh-web`：**`WebRuntime`**（`ctx.web`）定义 harness 具备哪些 web 访问能力（搜索 web、抓取 URL），并通过多个提供方实现，不把模型约定绑定到某个厂商的 API 形状。
- `web/web-fetch-http` · `@deepseek-ai/dsh-web-fetch-http`：一个匿名公共 HTTP(S) `WebFetchProvider`，用于 harness web 能力 seam（`ctx.web`）。它获取具体 URL，返回状态码和长度受限的解码内容。
- `web/web-search-deepseek` · `@deepseek-ai/dsh-web-search-deepseek`：由 DeepSeek 支持的 `WebSearchProvider`，用于 harness web 能力 seam（`ctx.web`）。它调用 DeepSeek 的 **Anthropic 兼容 Messages API**（`POST {baseURL}/messages`），启用原生 `web_search_20250305` 服务器工具，并把 DeepSeek 返回的结构化 `web_search_tool_result` 块映射为 seam 规范化的 `WebSearchResult`。
- `web/web-search-exa` · `@deepseek-ai/dsh-web-search-exa`：由 Exa 支持的 `WebSearchProvider`，用于 harness web 能力 seam（`ctx.web`）。它调用 Exa 的 `POST /search` 端点并请求高亮摘要内容，把扁平 `results[]` 映射为 seam 规范化的 `WebSearchResult`。
- `web/web-search-perplexity` · `@deepseek-ai/dsh-web-search-perplexity`：由 Perplexity 支持的 `WebSearchProvider`，用于 harness web 能力 seam（`ctx.web`）。它调用 Perplexity 的 OpenAI 兼容 `POST /chat/completions` 端点，把生成答案与引用映射为 seam 规范化的 `WebSearchResult`。
### 23.49 工作流（`packages/workflow`）

- `workflow/tool-ralph` · `@deepseek-ai/dsh-tool-ralph`：面向模型的 `ralph` 工具运行固定的前台工作流，把一个不可变目标依次交给多个全新子 agent（智能体）。它展示如何把专用编排策略实现为基于 `ctx.workflowEngine` 和 `ctx.subagents` 的普通插件：不会向 `agent-loop` 添加 Ralph 模式或全新 agent loop（智能体循环），同会话的目标领域也保持独立。策略和暂缓事项由 Ralph Agent Note（agent 决策记录）负责。
- `workflow/tool-workflow` · `@deepseek-ai/dsh-tool-workflow`：面向模型的 **`workflow` 工具**：运行一段扇出 subagent 的 JavaScript 编排脚本，并返回脚本的最终值。本包负责基于 `ctx.workflowEngine` 定义面向模型的 schema 和运行生命周期；脚本解析、执行、上限与取消位于 seam 之后，消费方仍负责面向父级的 schema 和结果包络。
- `workflow/workflow` · `@deepseek-ai/dsh-workflow`：工作流 seam（扩展点，`ctx.workflowEngine`）执行由模型编写、可扇出 subagent 的编排脚本。该 seam 定义脚本、运行、结果、错误和事件契约；引擎负责决定如何隔离并执行脚本。
- `workflow/workflow-worker-thread` · `@deepseek-ai/dsh-workflow-worker-thread`：本包为 `WorkflowEngine` 提供实现，每次运行使用一个 Node worker thread。worker 执行编排脚本；子 agent（智能体）留在宿主上，脚本通过带类型的宿主／worker 协议经由 `ctx.subagents` 访问它们。
### 23.50 工作区（`packages/workspace`）

- `workspace/workspace` · `@deepseek-ai/dsh-workspace`：DeepSeek Harness 的 Workspace 实体注册表（`ctx.workspaceRegistry`）：通过领域数据形式存储持久 workspace 记录、稳定 workspace 顺序和按新到旧排列的候选会话索引。消费方看到 `Workspace` 接口；实体实现保持包私有。

---

## 24. 核心子系统索引

原 `docs/subsystems/` 的 47 个中文子系统页已并入前文。为方便从概念跳到源码，保留如下索引：

| 子系统 | 主要服务/职责 |
|---|---|
| 核心 | Agent 注册、Agent Loop、预设、默认模型、工具定义等主干 |
| Scope | 会话/智能体所有权与资源作用域 |
| Session | 事件日志、消息投影、轮次/步骤事实 |
| Persistence | 会话日志后端与元数据 |
| Session Query | 列表、搜索、事件追踪、关系查询 |
| Session Projection | 从事件日志构建客户端/接口视图 |
| Session Reference | 跨会话稳定快照上下文 |
| Session Title | 会话标题策略 |
| Session Telemetry | 可选 OpenTelemetry 输出 |
| Token Meter | 请求压力与上下文位置计量 |
| System Prompt | 系统提示词和工具 schema 组装 |
| LLM Streaming | 模型适配、路由、流式增量 |
| Compaction | 上下文摘要与工具结果裁剪 |
| Tools | 工具注册、schema、执行流水线、展示元数据 |
| Filesystem | 文件读写编辑、目录/搜索、观察策略 |
| Shell | Bash/PowerShell 抽象与工具 |
| Subprocess | 子进程、stdio、终止、PTY 基础 |
| Terminal | 持久 PTY 会话 |
| Jobs | 后台任务生命周期 |
| LSP | 语言服务器抽象与 stdio 后端 |
| Code Runtime | 模型编写程序执行后端 |
| Sandbox | 命令/文件访问隔离 |
| Approval | 用户审批请求与审计 |
| Permission Presets | 会话权限预设 |
| Credentials | 凭据引用、解析、更新 |
| Settings | 用户设置读写 |
| Storage | 通用存储枢纽、JSON/SQLite 后端、领域数据形式 |
| Workspace | 工作区实体与会话归属 |
| Attachment | 内容寻址图片附件 |
| Spill | 大工具输出落盘 |
| Skills | 技能发现、摘要、加载 |
| Commands | 人类命令注册与解析 |
| User Questions | 结构化提问/回答 |
| Goal | 同会话目标 |
| Plan | 计划模式 |
| Schedule | 会话内计划任务 |
| Feedback | 消息反馈 |
| Subagent | 多提供方子智能体委派 |
| Agent Team | 实验性团队、邮箱、共享任务图 |
| Workflow | 编排脚本与子智能体桥接 |
| Web | 搜索/抓取能力 seam |
| Web Server | HTTP 路由和首页注入 |
| Client Modules | 浏览器插件发现与模块装载 |
| Typert | 类型化远程调用元数据与注册 |
| Extensions | 动态 Cordis 扩展与运行时检查 |
| Invariants | 运行时跨包不变式 |

---

## 25. 历史决策收敛索引

原 `.agents/notes/` 共包含大量中英成对的设计决策、提案、缺陷复盘和流程记录。中文侧的 **739 条实际决策记录**已读取；下面保留每条题名、生命周期和核心内容摘要。正文中的当前事实已经优先吸收到前 24 节，这里承担“为什么曾这样设计”的历史索引功能。

### 25.1 已实现 · 架构（143 条）

- **2026-06-11 · 事件溯源的会话与派生消息历史**：MVP 要求严格的基于事件的追踪，以及完全可回放的会话（严格的基于事件的 trace、logging 系统，会话完全可回放）。
- **2026-06-11 · 微内核——通过 Cordis 事件分类体系实现扩展，唯一具体循环**：产品原则是「一切皆插件」：钩子、/goal、/loop、动态工作流、压缩（compaction）、沙箱、权限、UI、持久化、MCP、skill（技能）都必须能以插件形式编写，无需修改核心。
- **2026-06-11 · 模型边界处的运行时参数校验**：`defineTool`（统一 schema DSL）为工具作者的 `execute(args)` 提供了经 `InferArgs` 映射的类型化参数。但该类型只是对运行时值的编译期声明，而这个值实际上是模型生成的 JSON：没有任何机制强制模型遵守 schema，因此畸形调用（缺少必需键、声明为数字的位置传入字符串…
- **2026-06-11 · 源端拥有的会话不可变性与开发模式不变式**：会话日志需要两种不同的保护：对每条已存储事实的不可变所有权，以及对跨时间和服务约定的事实之间关系的检查。如果将二者混为一个可选的开发插件，生产环境的历史记录将失去保护；如果试图通过 TypeScript readonly 类型同时表达两者，既无法建立运行时边界，也无法描述关系规则。
- **2026-06-11 · 由 dsh-llm 拥有的提供方无关内容块词汇**：harness 需要一套统一的内部消息语言，供 agent loop（智能体循环）、会话日志和所有插件共同使用。
- **2026-06-11 · 结构化错误分类体系**：故障跨越 seam 时只是裸字符串。工具错误被扁平化为一个文本块（name、code 和 stack 全部丢失），导致未来的沙箱/重试插件无法区分 ENOENT 和 EACCES，模型也未能获得本可提供的更具可操作性的反馈。非 Error 的 throw 退化更严重：agent loop（智能体循环）将其包装为 `new Error(String(x))`…
- **2026-06-13 · 以两个 LLM 适配器作为设计验证孪生体**：`dsh-llm` 拥有一套提供方无关的流式词汇：`StreamChunk` 协议（`block-start`、`text-delta`、`reasoning-delta`、`tool-call-delta`、`block-end`、`usage`、`finish`）以及内容块类型（内容块词汇）。如果词汇仅针对单个适配器定义…
- **2026-06-13 · 能力 seam——Service Definition / Service Provider / Consumer 角色**：harness 具有可替换的能力：当前是 bash 执行，未来会有沙箱化／远程执行器和替代模型提供方。一项能力涉及三个关注点，它们以不同速率、因不同原因变化：*约定*（这项能力是什么）、*实现*（它如何运行）、*消费方 API*（模型和其他插件面向什么编程）。将三者捆绑在一个包中会耦合这些变化速率——把本地执行器换成沙箱化执行器时…
- **2026-06-14 · 会话持久化作为基于现有 `SessionEvent` 的抽象服务**：会话此前仅存在于内存中。示例插件 `session-jsonl.ts`（在两个示例中逐字节重复）是只写的遥测：它缓冲 `session/event` 并追加 JSON 行，没有读取/回放路径，没有崩溃安全性（无 fsync、无原子写入、dispose（资源释放）时采用 fire-and-forget 方式排空），没有列表功能，也没有格式版本控制。…
- **2026-06-17 · 文件系统能力 seam——ctx.fs、本地后端与面向模型的文件系统工具**：harness 已有一个具体的 `bash` 能力 seam（`dsh-shell` / `dsh-bash-local` / `dsh-tool-bash`），但文件系统操作当时即将作为面向模型的工具落地，却没有等价的 seam。如果 `read`、`write` 和 `edit` 直接使用 `node:fs`…
- **2026-06-18 · Agent 生命周期与所有权约定**：ACP（Agent Client Protocol）与 tool-bash 的若干限制是同一个所有权约定缺失的症状：插件可以通过 `ctx.agents` 创建或恢复 agent（智能体），但无法独立拥有和 dispose（资源释放）单个 agent，而长时间运行的 bash 任务在执行器中也没有稳定的所有者。ACP 在断连时中止并等待 agent…
- **2026-06-18 · 会话 surface：事件日志上的有序投影**：事件日志是权威数据源，但历史操纵此前没有持久化的共享机制。如果没有这样的机制，上下文压缩（context compaction）等插件会通过顺序敏感的监听器改写派生请求，却不记录每次替换使用了哪些事件。每次新增历史操纵时，还必须修改 `deriveMessages()`。
- **2026-06-18 · 共享持久化写入协调器**：`dsh-session-persistence-jsonl` 与 `dsh-session-persistence-sqlite` 有意在不同存储介质上证明同一份 `SessionPersistence` 约定…
- **2026-06-20 · 后台任务运行时（`ctx.jobs`）与通用任务控制工具**：后台 bash 原本兼有两项职责：bash 执行器既运行进程，又管理 job id、所有权、增量读取、取消、完成监听器和面向模型的控制工具。新增后台 subagent 需要相同的生命周期与交互约定。如果每种长时间运行能力都独立实现该约定，就会重复隔离、清理、通知和提示词行为，还会让模型为每种生产方学习不同的收集与停止协议。
- **2026-06-20 · 在所有应有之处使用 branded ID**：harness 使用 `Branded = string & { readonly [BRAND]: B }` 机制，为 `CallId`（`packages/llm/llm/src/brand.ts`）和 agent（智能体）/会话共享的 `SessionId`（`packages/core/session/src/types.ts`）做 brand 处理…
- **2026-06-21 · LLM 暂时性请求失败的有界恢复**：按提供方配置的请求重试策略在此基础上增加了确切提供方配置与显式无界 mode。本说明继续负责结构化失败事实、失败尝试的恢复边界、normal mode 的暂时性默认值、可见的单次尝试和持久重试状态。LLM（大语言模型）流的终止失败取代了其中关于抛出错误身份和流 sidecar 的机制。
- **2026-06-21 · 对提供方请求强制携带 `User-Agent` 归属标识**：LLM（大语言模型）提供方请求应当标识发出请求的产品。这对提供方侧的技术支持、滥用调查、兼容性调试和流量分析都有价值。在本 Agent Note 之前，harness 只做了部分工作：手写的 DeepSeek 适配器发送了一个手动复制的 `User-Agent` 常量（`packages/llm/llm-deepseek/src/adapter.ts`）…
- **2026-06-24 · Web 能力 seam——稳定的工具覆盖多个提供方**：harness 需要面向模型的 web 工具，但不能将模型约定绑定到某一家厂商的 API 形状上。搜索是当前的压力点：从一开始就同时支持 Exa 搜索和 Perplexity 搜索——两种刻意不同的提供方形状（Exa 返回扁平的 `results[]`，每项包含 `{title, url, highlights, publishedDate}`…
- **2026-06-26 · 将 `dsh-fs-observation-policy` 改为事件门禁插件，而非方法接口**：拆分文件系统 seam Agent Note 在面向模型的工具与 `ctx.fs` 提供方之间放置了 `ctx.fileContext`：`dsh-tool-fs` 注入 `fileContext`，并将每次 `read`/`write`/`edit` 路由到它的方法。这使得 `fileContext` **位于关键路径上且不可省略**。…
- **2026-06-30 · 事件域语义——会话是事实日志，agent 是实时事件通道**：harness 通过 Cordis 事件分类体系扩展 agent loop（智能体循环）（见微内核事件分类体系 Agent Note）。随着该分类体系的增长，三个事件域之间的界限变得模糊：
- **2026-06-30 · 在 bash seam 上支持 stdin 与额外 env**：钩子子系统以 Claude Code 和 Codex 的方式运行外部钩子命令：钩子是一条 shell 命令，通过 **stdin 上的 JSON** 接收事件载荷，并从若干**环境变量**（`CLAUDE_PROJECT_DIR`、`CLAUDE_PLUGIN_ROOT`、`PLUGIN_ROOT`……）读取上下文。…
- **2026-07-02 · 用于工具调用展示的带标签 render-intent 联合类型**：render-intent 联合类型对 UI 传输层仍然有效；其 ACP（Agent Client Protocol）映射已被 ACP 作为仅面向自动化的协议取代。
- **2026-07-02 · 相对文件系统路径按调用方的会话 cwd 解析**：ACP（Agent Client Protocol）桥接层为每个会话提供独立的工作区：`session/new` 将自动化客户端的项目目录记录为 `SessionHeader.cwd`，`dsh-tool-bash` 将每次 bash 调用的 `workdir` 默认设为调用方 agent（智能体）的 `session.header.cwd`（见 ACP 包…
- **2026-07-05 · Subagent 提供方生命周期事件——`subagent/provider-added` / `subagent/provider-removed`**：提示词变量 Agent Note 让 `dsh-tool-subagent` 从其提供方派生面向模型的措辞：`SubagentProvider.inheritsParentContext`（spawn 和 ACP（Agent Client Protocol）为 `false`，fork 为 `true`）同时驱动工具描述和 `prompt` 参数描述…
- **2026-07-05 · Windows 原生持久 JSONL 发布**：`dsh-session-persistence-jsonl` 在首次追加时延迟发布会话日志。POSIX 协议会写入临时文件，对其执行 fsync，将其链接至最终名称，对父目录执行 fsync，然后移除临时链接。对父目录执行 fsync 是持久性约定的一部分：命名空间变更后发生崩溃时，已经提交的最终名称不能丢失，否则调用方会误以为会话日志已经物化。
- **2026-07-05 · 提示词变量与工具指导归属**：组装后的系统提示词存在四个缺陷，同属一类：harness 已知的事实在别处被手工重述，然后漂移。
- **2026-07-05 · 每个 LLM 请求都可从会话日志重建**：请求流水线未能保证前缀稳定性以利用提供方缓存，会话日志也无法重建模型实际看到的内容。日志遗漏了模型、系统提示词和工具 schema，同时允许逐次调用的请求改写。因此缓存行为和回放等价性取决于碰巧加载了哪些插件。
- **2026-07-06 · 共享的超时/截止时间原语，硬终止留给各能力自行实现**：超时处理在各个承载工具的能力之间逐渐分化，而且这种分化并非表面的：同一套逻辑被以三种方式重新实现，各自带有微妙的正确性负担。
- **2026-07-06 · 工具结果保留库**：多个面向模型的工具已经限制其返回的上下文量，但每个工具都拥有不同的局部机制和词汇：bash 保留尾部并提供 spill 文件；web search 限制来源列表；web fetch 限制正文内容；`glob`／`grep` 发现工具需要在行内提供第一页，同时为完整结果集保留精确的省略元数据。…
- **2026-07-07 · 工具调用超时策略作为插件**：超时/截止时间 Agent Note 将计时与分类原语提取到了 `@deepseek-ai/dsh-timeout`，但超时策略仍然附着在各个能力和面向模型的 schema 上。`bash` 暴露了 `timeoutMs`；`web_fetch` 暴露了 `timeout_ms`；`web_search` 没有面向模型的超时参数…
- **2026-07-08 · agent 即注册作用域**：一个应用需要在多个 agent（智能体）之间共享基础设施，同时让每个 agent 拥有自己的工具、提示词贡献、策略和监听器。共享的适配器、持久化和用户界面属于部署层面；而 persona、工具变体或监听器往往只属于某一个 agent。
- **2026-07-08 · 工具输出 spill 策略**：工具输出需要有界的模型可见预览，但部分超大结果仍可能在之后有用。抓取的页面正文或冗长的工具响应不应完整占用下一次模型请求，但模型应能使用现有文件读取工具，在之后查看经过格式化的完整结果。
- **2026-07-10 · 单文件可执行的 SDK 运行时分发（single-exe）**：DeepSeek Harness 需要为 Python 库专门提供一种无需安装 Node、可直接在目标平台运行的 SDK 分发形态：一个单文件可执行程序（下称 exe），通过 stdio 提供 JSON-RPC 对外服务接口（`HarnessSdkJsonRpcServer`，Python SDK 的对端）…
- **2026-07-10 · 调用后压缩压力与上下文溢出恢复**：`agent/pre-step` 运行在最终请求路由之前，也早于 assistant 输出、工具结果、缓冲上下文与 steering（中途引导）的产生。即使它接收已装配提示词与会话前缀，压力视图仍是临时的，因为 `agent/request` 还可以改变路由或调用配置，工具 schema 也没有与这些输入一同冻结。增加字段无法让调用前状态描述已完成调用…
- **2026-07-12 · Agent 作用域运行时设计与正确性**：agent（智能体）作用域约定对贡献者而言很简单：通过 `agent.ctx` 注册，解析出一个全局加单 agent 的视图，仅在 setup 完成后发布，并保持作用域直到工作停止。运行时必须在协作式插件框架、异步创建、可重入监听器、持久化会话提交以及 worker 或进程故障等场景下维护这份约定。
- **2026-07-12 · 共享作用域分层存储**：agent（智能体）作用域机制（决策、运行时设计）让支持作用域的注册表反复呈现同一种形态：一个全局注册层，加上一个与具体 agent 精确对应的层。七个注册门面都采用这一形态：`tools.register`、`tools.restrict` 和 `tools.guard`（位于 `dsh-tools`）…
- **2026-07-14 · 基于提供方路由的 LLM 适配器与通用 pi-ai 后端**：`dsh-llm` 按精确模型名称注册适配器。插件在 Cordis 启动时提供模型列表，`LlmRuntime` 为列表中的每个字符串保存一个适配器，`GenerateOptions.model` 同时选择适配器与提供方模型。两个随附的适配器都只面向相同的两个 DeepSeek 模型时，这种方式可以工作，但它混淆了两个独立决策：由哪个上游提供方承接请求…
- **2026-07-15 · LSP 能力 seam 与面向模型的查询工具**：harness 已具备文本搜索与文件读取能力，但二者都无法识别程序符号。文本匹配无法可靠地区分同名函数、跟踪导入别名、关联接口与具体实现，也无法报告推断类型。因此，agent（智能体）在修改代码前缺少人类通过编辑器语言服务器获得的语义导航能力。
- **2026-07-15 · 回放式 token 计量服务**：上下文压力并不只对压缩（compaction）有用。压缩后端、溢出保护或未来的请求策略插件都可能需要回答同一个问题：持久请求消耗了多少 token？如果把该折叠逻辑留在 `dsh-compaction-basic` 内部，就会重复实现回放逻辑，使未加载压缩的调用方无法使用计量，并诱使调用方复用陈旧的核算结果。
- **2026-07-15 · 基于 AsyncLocalStorage 的发起 Agent 作用域**：harness 中存在两种有用但不同的上下文概念。Cordis `Context` 负责选择服务、注册归属和生命周期；`agent.ctx` 是一个存活 Agent 所拥有的扁平注册作用域。Agent 与会话身份描述的则是异步操作主体。若把根 `ctx.agent` 改成「当前正在运行的 Agent」，就会混淆这两种含义…
- **2026-07-15 · 建议性 LLM 目录与 ACP 会话级模型选择**：目录决策仍然有效。ACP（Agent Client Protocol）会话级模型选择已由 ACP 作为仅面向自动化的协议取代。
- **2026-07-16 · 显式轮次取消能力**：取消是一种生命周期短于 Agent（智能体）驱动器的控制能力。自由文本字符串无法完整区分所有调用方，步骤级控制器也无法中断提示词提交、提示词组装、继续决策或轮次终止策略。持久化 `Error`、`AbortSignal.reason` 或后端私有对象还会向持久化回放暴露不稳定的运行时细节。
- **2026-07-19 · GUI 分层与 RPC 协议——host/client 按能力提供方分层、四象限消息模型与 fetch 载体**：需要提供 UI 对接层，除已有 ACP（Agent Client Protocol）/stdio 基线外，还需要 Web（server）、Electron 等其他产品客户端。我们把它们统一称为 Client。…
- **2026-07-19 · Web 客户端架构——client cordis 插件树、slot 体系与 React-free 对象层**：两端都跑 cordis。host 是一棵 cordis 插件树；浏览器里跑第二棵 client 侧 cordis 树，其中每一项 UI 能力都是插件，由壳静态持有的 loader 动态装载。树内 cordis ctx 承载一切运行时事实（服务、store、会话 scope），React 是纯投影：组件对框架零 import，一切经 props 注入…
- **2026-07-19 · Zstandard JSONL 会话日志**：JSONL 持久化后端会逐字保留每个 `SessionEvent`，其中包括数量庞大的 `assistant/chunk` 记录。原始文本便于检查，但重复的 JSON 键和模型文本会增加存储与 I/O 开销。压缩编码必须保留既有的 append/fsync 提交边界、首次物化时的无冲突发布、崩溃修复以及仅元数据列举；如果每轮都重写整个压缩文件…
- **2026-07-19 · 包拥有的不变式服务约定**：运行时不变式检查跨越会话轨迹、agent（智能体）状态、作用域 dispatch 和请求重建。如果所有检查都放在一个诊断包中，该包就必须导入彼此无关的产品领域词汇，测试也会离开真正的所有者；任何产品包新增或移除检查时，都要修改中央包。
- **2026-07-19 · 有意义的包不变量约定**：包自有不变量服务让发布和注册实现了全覆盖，但最初的生成基线允许空安装器。后续方案又用针对插件名称、注入、effect、服务方法和纯工具库中的固定示例的通用断言替代这些空实现。这些断言虽然让每个 companion 都能执行，却没有提高系统安全性：TypeScript、Cordis 启动、包测试和模块加载测试已经约束这些形状…
- **2026-07-19 · 注册表边界上的协作式工具取消**：每次类型化工具调用都需要一个由调用方持有的取消信号。可选的 `ToolExecutionInput.signal` 允许直接调用方不承担所有权，使每个工具主体中的 `exec.signal` 都成为可选值，也会诱使注册表提供无法表达真实调用方生命周期的后备信号。
- **2026-07-20 · 统一 JSON 值 schema DSL**：工具参数使用一套精简的作者侧 schema DSL，subagent／工作流的结构化输出则使用另一套原始 JSON Schema 子集和校验器。两套词汇在根类型、标量约束和校验方式上并不一致；如果继续沿用这种划分，类型化的规范工具输出约定要么还需重复实现两条路径，要么只能接受部分投影无法强制执行的 schema。
- **2026-07-20 · 规范工具输出约定**：工具主体过去直接编写面向模型的 `ContentBlock[]`，并可选择将其与不透明的 `meta` 包装在一起。因此，Native 模式的 Function Calling（函数调用）虽然拥有可供人阅读的投影，但程序化调用方没有稳定的领域值：Code Mode 会将内容块重新展平为字符串，动态工具会重复定义内容形态，策略也可以替换展示内容…
- **2026-07-20 · 路由模型上下文与压缩策略**：当一个进程把请求路由到不同容量的模型时，压缩（compaction）不能安全地应用同一个全局上下文窗口。相同模型 id 也可能存在于多个提供方下，适配器还可能接受不在建议目录中的动态 id。错误容量要么让压缩触发过晚并造成原本可避免的溢出，要么让压缩触发过早并丢弃有用上下文。
- **2026-07-22 · slot 体系标准——单一 register、props 四份额与框架 store 席位**：范围：Web 客户端 slot 体系的终版设计——UI 插件如何拼合页面、渲染权威落在哪里、组件 props 如何定型、业务活数据住在哪里。周边语境（装载链、对象层、服务）归 Web 客户端架构 RFC 所有，其 slot 各节移交本文。
- **2026-07-22 · 将 agent 投递统一到 send(target × wakeup) 并把注入的上下文合并进 user/message**：agent（智能体）的对外驱动接口逐渐长出三个近乎平行的动词——`send`、`steer`、`inject`——各自带有独立的选项类型、独立的实时事件叙事，以及独立的持久事件。`send` 和 `steer` 都会把一条冻结的 inbox 记录入队并发出 `agent/queued`；`inject` 则绕过 inbox…
- **2026-07-23 · client 插件装载——惰性 factory、Cordis 生命周期与热重载**：host 侧，cordis 插件装载站在 Node 的模块机制之上——require cache 与内部 ESM loader 拥有模块身份与字节。vendored `@cordisjs/plugin-loader` 在这层基座之上实现插件治理与热重载，二者在唯一一道边界相接：`Loader.internal`。
- **2026-07-23 · toolview 溶解——工具行即 per-view keyed slot**：工具环作为独立基础设施已消失：工具行是**各视图为自己声明的 keyed 子槽**，client 全域只剩一种注册模型。上述理由是空的——keyed slot 的 *key 空间*本就运行时开放（SlotMap 声明槽、从不声明 key；ask-user composer 的 `key: 'question'` 即先例）…
- **2026-07-24 · dsh web 的 config-tree boot 与 web 传输分层**：范围：`dsh web` 如何组合（cordis.yml + cordis 之前的 boot 类 + 配置源），以及 web 传输如何跨包分层（网关 / 载体 / 绑定 / 图 / 开发期重载）。浏览器侧装载链归 client 插件装载 note 所有，本组合只是它的供给方。
- **2026-07-24 · 单一 harness home 解析器**：对于"DeepSeek Harness 用户数据存放在哪里"，harness 里存在两套互不一致的约定：
- **2026-07-24 · 将上下文注入与轮次执行分离**：agent（智能体） API 曾用三种相互重叠的方式表示面向模型的补充输入：调用方通过 `SendOptions.contexts` 附加 `HookContext[]`，拦截钩子和工具钩子返回 `additionalContexts`，插件则调用 `agent.inject()`。这些路径最终都把上下文写入同一份模型历史…
- **2026-07-24 · 按项目分组的会话目录**：持久化根目录可以只供一个项目使用，也可以由多个项目共享，还可以是临时目录或集中式目录。对 cwd 进行哈希得到的分桶目录能适用于所有这些部署方式，但开发者无法从目录名辨认项目，因此共享根目录难以浏览。
- **2026-07-24 · 适配器持有的推理强度能力**：推理强度过去只能在适配器中配置，因此对话无法在多次请求之间发现或更改所选模型支持的等级。若将某个适配器的等级联合类型提升到 `dsh-llm`，所有提供方和模型都必须采用一套自身可能并不支持的名称；若改用提供方特有的 options 对象，agent loop（智能体循环）又无法校验最终生效的请求，也无法通过持久化记录准确重建该请求。
- **2026-07-25 · Web client Agent-scope 对等模型与供数通道（agents/scope / blank 复用 / provide）**：范围：client Agent scope（actx）与定向事件、client/host 实体化对等模型、空会话 blank 位与复用（`connectWorkspace`）、逐会话供数通道（`sessions.provide`）…
- **2026-07-25 · Web 命令业务面与装配（ui-commands / ui-skill / ui-subagent）**：范围：命令目录缓存与三型派发（ui-commands）、popup 选择流、skill（技能） / subagent 两个引用源、fixture（测试前置数据）命令路由与装配验收（slash-flow 快照）。承载 wire 见会话作用域 note；触发、菜单和输入机器见输入状态机 note。
- **2026-07-25 · Web 输入状态机、composer slot 与 slash 流水线（ui-conversation input / ui-input-trigger）**：范围：输入状态机（occurrence 表 + claim 看护 + 提交事务）、hub/facade 与发送编排、跨插件输入改写的三个 scoped bail 事件、`/` 与 `@` 触发检测与菜单流水线（ui-input-trigger）、composer 周边 slot 体系。…
- **2026-07-26 · 任务注册表是一个能力 seam（`dsh-jobs` / `dsh-jobs-local`）**：后台任务运行时交付时把 `JobRegistry` 做成了单个具体包：`@deepseek-ai/dsh-jobs` 既拥有每个生产方和控制器面向编程的 `ctx.jobs` 约定，也拥有进程内 Service Provider（内存存储、结算簿记、所有者清理 effect、拆除）。…
- **2026-07-26 · 将打包分片行设为默认 JSONL 布局**：提供方流会产生大量 token 大小的 `assistant/chunk` 增量事件，其重复 JSON 封装可能比载荷本身更大。会话日志必须将每个分片保留为独立的逻辑事件：实时 `session/event` 传递、序号、`sourceEventSeqs`、回放、取消证据和 UI 流式输出都依赖这些边界。
- **2026-07-26 · 进程服务是 bash 执行器之下的独立 seam（`dsh-subprocess` / `dsh-subprocess-local`）**：`dsh-bash-local` 原先把两项因不同原因而变化的能力捆绑在一起：*运行一条 bash 命令*（命令默认值补全、超时分类、对模型友好的终端环境、bash 工具所渲染的 stdout/stderr 合并）与*运行并管理一个子进程*（detached 进程组、附带 spill 文件的有界尾部保留输出、凭据清除与 `DSH_*` 合并次序、SIGTERM…
- **2026-07-27 · dispose 阶梯归其消费方所有，而非 subprocess seam**：`SubprocessHandle.dispose(graces)` 与 `SubprocessDisposeGraces` 把一整套拆卸*策略*——等待 stdin EOF、再 SIGTERM、再 SIGKILL，每一层由调用方提供的时间窗约束——放在了一个其余动词均为单一机制的 seam 上。…
- **2026-07-27 · 编译器无关的 Typert 类型模型**：`dsh-typert-generator` 分别从 host 和 client project 建立 `ts.Program`，只把 compiler node、symbol 和 checker 当作提取工具。分析结束后，所有生成器和扫描器只消费 Typert 自有的 `WorkspaceModel`、`FaceModel` 与 `TypeGraph`…
- **2026-07-28 · web GUI 宿主的能力可辨识目录选择 seam**：web GUI 的「打开本地文件夹」流程被焊死在一种交互上：`host.pickDirectory` 调用编译进 `dsh-host-apiproxy` 的原生 OS 选择器（私有模块，仅测试注入点）。…
- **2026-07-28 · 基于文件系统与进程管理执行世界的可移植消费方**：文件系统与进程管理 seam 使文件访问和普通进程访问具备可替换性，但 PTY 和 LSP 仍直接调用宿主 Node API。因此，即使领域行为没有变化，远程执行提供方看起来仍需要独立的 PTY 与 LSP 包。这些包只会成为浅层适配器：每个包都仅为替换文件与进程操作而复制一个现有消费方。
- **2026-07-28 · 将每条消息创建为带标识的不可变值**：harness 曾存在多种形似消息的表示，各自采用不同的标识规则。agent（智能体）输入只有在 agent loop 接受后才会取得 inbox 关联 id，而持久用户消息、assistant 消息、工具结果和模型请求消息都可能没有标识。因此，提示词准入介于创建消息与建立标识之间；等价内容会在实时事件、持久事件和模型请求之间复制…
- **2026-07-28 · 所有 /api 路由共用一道载体级浏览器信任边界**：Web GUI 宿主以纯 HTTP 提供 `/api`（默认 `127.0.0.1:3080`，支持 `--host 0.0.0.0`），而这个面上有远程代码执行级别的方法——`session.prompt` 驱动的 agent（智能体）可以运行 bash。…
- **2026-07-28 · 用户设置 seam（`ctx.settings`）与文件提供方**：范围：`packages/settings/` 能力族——Service Definition、文件提供方，以及用户设置与 `cordis.yml` 的组合边界。web config-tree note 曾把「profile 写路径」记为延后项；本 seam 就是该写路径的归属。…
- **2026-07-29 · LLM 流的终止失败**：`LlmRuntime` 是一次适配器尝试的规范化边界。它只捕获最终适配器选择、同步分发、iterator 构造与 `next()` 失败，将抛出值转换为不可变 `LlmFailure`，并发出一个终止 `finish`。调用方取消或 `ABORTED` 失败选择 aborted 结束原因；其他适配器失败选择 error 结束原因。…
- **2026-07-29 · dsh 通过 tsx ESM 钩子源码启动**：取代原生 TypeScript 源码启动：Node 移除了该决策所依赖的能力。
- **2026-07-29 · token 用量投影与上下文占用率**：Web 统计行原先从当前已加载的会话节点推导 token 总量。该窗口是分页的，因此滚动会改变总量；压缩（compaction）又会替换可见内容，而不保留其背后的计费用量。持久的提供方计费用量需要一个能同时经受这两者的数据源。
- **2026-07-29 · 按实测聚类重新划分 packages/ 分组**：两级 `packages//` 层级结构（原始决策）自 6 月以来已经漂移：167 个包彼时坐落在 42 个组里，若干组边界已经对不上这些包的实际聚类。
- **2026-07-29 · 请求级 LLM 配置与凭据 seam**：范围：`ctx.settings` 的第一批生产消费方（两个 LLM（大语言模型）适配器插件）、新增的 `packages/credentials/` 能力族，以及 `packages/util/atomic-write` 的抽取。…
- **2026-07-30 · client 文案全量接入 typed locale 席位与不翻译边界**：**注册期文本走 label thunk。** ui-slots 的 list 注册项 `label` 接受 `SlotLabel = string | (() => string)`；owner 投影 ledger 行时必须经 `resolveSlotLabel` 解析（不裸读 `options.label`）…
- **2026-07-30 · follow-up 入队与自有运行边界**：`Agent.followup()` 会标识一条用户消息并将其排入队列，但单次 follow-up 并不拥有随后发生的活动。在 agent（智能体）下一次进入 idle 前，steering（中途引导）、注入的上下文、工具续行、恢复和后续排队消息都可能参与活动。因此，`MessageId` 可以证明消息已获 inbox 准入…
- **2026-07-30 · settings 写路径完整性与观察者生命周期**：范围：`dsh-settings-file` 的写路径数据完整性（操作链、读-改-写、跨进程写锁、diff 形态的 YAML 编辑）与 `dsh-settings` 的观察者生命周期（watch 的 dispose（资源释放）、异步监听器收容、JSON 形态写入边界）。…
- **2026-07-30 · web 配置平面**：范围：请求级 LLM（大语言模型）配置 note 中延后的 wire 面与 web UI——带推送式失效的 `settings.*`/`credentials.*`/`llm.*` RPC 领域、分层且脱敏的 `describe()`、本地设置文档交接、llm 可配置提供方目录与拓扑事件、由 `dsh-client-ui-settings` 持有的 `ctx.…
- **2026-07-30 · 凭据边界、按整份快照发起的请求与原子路由注册**：范围：加固请求级 LLM（大语言模型）配置边界——存下来的凭据落在哪里、谁能读到它，一次请求所用的事实如何保持在同一代，以及一组路由如何在不留空窗的前提下更换。本 note 与 settings 写路径 note 配套：它把那篇 note 的提供方修复套用到 `credentials-local`，并把其中的写锁提升进 `dsh-atomic-write`。
- **2026-07-30 · 命令条目文案分别由条目与 handler 负责**：Web 命令条目由一对落库的命令生命周期事件渲染出 `标题 · 摘要`：标题是由 `command/run` 重建的分派命令行（`/permission workspace-write`），摘要是 `command/done` 的原样 `text`（`Permission preset: workspace-write.`）。两半各自成文、互不知情…
- **2026-07-30 · 种子结束日志边界**：在会话日志中拥有独立开／闭括号的插件无法区分一个已死的标记和一个存活的标记。`compaction/start` … `compaction/end` 就是已发布的实例：当接手一份日志、而它最后的压缩（compaction）事件是一个未配对的 `compaction/start` 时…
- **2026-07-30 · 适配器持有的最大 token 默认值**：`LlmResolvedModelInfo.defaultMaxTokens` 携带一条确切提供方／模型路由的可选单次请求输出上限，该值由适配器配置。`LlmRuntime` 将其校验为正的安全整数，并且仅在调用方省略值时才填入 `LlmCallConfig.maxTokens`。…
- **2026-07-30 · 配置面暴露什么，以及谁有权覆盖什么**：范围：对 Web 配置面的边界加固——哪些 namespace 能抵达协议、哪些调用方能抵达它们，以及一个只持有局部且可能陈旧的视图的编辑器该如何写入，才不会毁掉它看不见的东西。
- **2026-07-31 · Goal 自有的持久事件**：Goal 状态与 inbox 状态具有不同的生命周期。无论相关模型上下文是否获准进入步骤，goal 变更都必须在重启与 fork 后保留；inbox 消息则可能在步骤调度期间被编辑、领取、拒绝或丢弃。把 goal 变更编码到 Round 为 0 的 inbox 消息中，会让队列放置成为领域提交点，并迫使回放对账插入、准入、消息标识、来源元数据与渲染内容。
- **2026-07-31 · code-runtime seam 拥有可移植标识符排除集**：Service Definition 包（`@deepseek-ai/dsh-code-runtime`）以四个具名常量导出可移植标识符排除约定，每个 Service Provider 导入它们而非重新声明：
- **2026-07-31 · the code-runtime-python fd-3 frame protocol**：`src/protocol.ts` 是 wire vocabulary 的 host 侧及其敌意帧编解码：
- **2026-07-31 · 在单一 pre-step 决策前领取 inbox 输入**：循环此前把一个步骤边界拆成提示词准备、提示词准入与串行步骤钩子。准入结果可以保留或丢弃已领取输入，实时队列事件还携带了与持久 inbox 状态重复的数据结构。插件不得不在修改 inbox、改写已提交批次与直接追加会话历史之间选择，而观察方无法依赖一套明确顺序。
- **2026-08-01 · glob/grep 改用打包的 ripgrep 二进制直接 spawn**：取代 bash 承载的 grep/glob 发现工具：v1 决策中明确延期的方案——直接 spawn ripgrep——现在成为实际交付的实现。
- **2026-08-02 · Typert Gateway 定向方法调用**：Host API Proxy 同时承担直接方法调用、带状态交互和 Session 事件流。三者的生命周期、路由语义和客户端编程界面不同，继续共用一个业务导出包会让业务 Service、传输协议、状态机和客户端类型彼此耦合。
- **2026-08-03 · pi-ai 路由是被声明的提供方，而不是 catalog 查表**：提供方路由是一份**声明**，已安装 catalog 是它的默认值。`resolveProfiles` 不再拿路由键去核对 `getBuiltinProviders()`，而是把每条路由解析成一份物化模型列表，外加服务它的 pi-ai `Provider`：
- **2026-08-03 · 会话的 agent 由一份 preset cordis.yml 组装而成**：一个 `dsh` 进程服务多个会话，但决定 agent（智能体）究竟是什么的那套组装——它的工具、人设、提示词段落、委派后端——由启动器所引导的 `cordis.yml` 一次性固定给整个进程。若某个部署希望一个 benchmark 精简 agent 与一个完整编码 agent 并存，就必须跑两个进程…
- **2026-08-04 · 在 Models 页上声明一个提供方**：模型列表是两条流程共用的组件；创建则是它自己的卡片。
- **2026-08-04 · 把凭据存储与用户环境层拆开**：两件工作在 Harness home 下拆成两个文件。
- **2026-08-04 · 浏览器下行 WebSocket 载体**：浏览器真实载体为两类下行流各开一条独立 WebSocket：`/api/events.mux` 只发送 `MuxFrame`，`/api/events.host` 只发送 `HostFrame`。每条文本消息是一份完整的 `ServerRequest` JSON；客户端继续先校验信封，再按路径校验具体 frame union…
- **2026-08-04 · 询问草稿中的提供方端点**：询问以 **settings namespace** 为键，而不是提供方路由：
- **2026-08-04 · 配置来源的统一顺序，以及被发现的文件不得决定什么**：**非机密值走同一条顺序。** 每个本身不是凭据的可配置值都按同一顺序解析；各领域的差别只在于哪些层存在。
- **2026-08-05 · profile 插件组合包取代固定的表层 overlay**：一切都变成 **profile**：即目录 `$DSH_HOME/profiles/`，其中包含一个 `package.json`（pnpm 管理的树外插件 `dependencies`，加上 profile manifest `dsh.profile` 及其有序的 `bundles` 层列表）和一份用户 `cordis.patch.yml`。…
- **2026-08-05 · slot 声明注入与重载生命周期**：客户端插件可能在声明某个 slot 的插件之前或之后向该 slot 贡献内容。Cordis 服务注入无法表达这种依赖：服务只能作为间接的顺序信号；客户端 manifest（元数据清单）的依赖项不会规定激活顺序；即使所有相关服务始终挂载，slot 仍可能消失后重新出现。因此，立即注册会与尚未声明的 slot 形成竞态，而等待无关服务则会耦合本可独立重载的功能。
- **2026-08-05 · 发布前可复用的 Session 准备阶段**：冷历史检查和 agent（智能体）恢复会分别实体化同一份持久会话日志。对于大型压缩日志，每次操作都会重新完整读取、解压、解析、验证、冻结并构造 Session。因此，历史分页可能反复承担冷读成本；如果改为由历史查询激活 agent，读取生命周期又会与缺少自然退出时机的实时 agent 耦合。
- **2026-08-05 · 大型会话 JSONL 恢复流水线**：恢复已存储会话会激活该会话，并在 agent（智能体）运行前物化完整且权威的事件日志。处理大型 JSONL 产物时，这个一次性操作会产生几项不必要的开销：每个独立 Zstandard 帧都会创建并关闭一个解码上下文；解码后的明文会汇总成整份日志的缓冲区和字符串，再进行重复扫描；刚解析出的事件还会进入面向借用值或循环引用值设计的通用快照与深度冻结路径。
- **2026-08-06 · Agent 作用域事件 dispatch 单个 payload 对象**：Agent 作用域事件历来采用位置参数：开头的 `agent` 主体、事件专属字段，以及末尾用于 waterfall（瀑布式事件）/serial 事件的 `next`。新增字段或退役上下文类型（如 `PreStepContext` 与 `RequestFailureContext`）都会迫使跨包重写每个监听器和 emitter，约定也一直分散在参数列表中…
- **2026-08-06 · Web 壳产物的分片拆分与目录布局**：`apps/web/vite.config.ts` 以 `manualChunks` 把壳切成两个初始分片，并以输出命名函数归类目录；整套配置零正则——精确包名 Set、文件名清单、扩展名清单。
- **2026-08-06 · subagent 列表经投影单元读取身份**：重写前的 `SubagentRuntime.listChildren` 对每个 `header.origin === 'subagent'` 的直接 child，每次列表都执行 `listEvents` 加 `readEvent` 两次整日志物化，且每次物化都伴随整日志 structuredClone…
- **2026-08-06 · 应用通过 `ctx.cmdlineArgs` 持有自己的命令行**：profile 落地之后，组合可以安装，命令行却不能。`apps/cli` 仍然声明着 Web flag 家族（`--host`、`--port`、`--dev`、`--workspace-root`、`--trusted-host`）和一次性任务位置参数…
- **2026-08-06 · 经由直接 mdast 渲染器的增量流式 Markdown**：`MarkdownText` 直接渲染 mdast,并在流式期间增量解析:
- **2026-08-07 · 遥测、反馈与 DeepSeek 请求共享匿名用户 id**：OpenTelemetry 后端已在 `$DSH_HOME/.anonymous-user-id` 中持久化一个匿名 UUID。`/feedback` 需要同时报告接收反馈的会话 id 与用户 id，以便运维人员将确认文本与导出的记录相关联。复制该身份或单独生成身份会使报告的用户失去意义…
- **2026-08-08 · Client 工具展示所有权**：Client 运行时已经按 `callId` 配对工具调用/结果事件，并能从 Code Dispatch 事件恢复 root/subcall 拓扑，但 Chat view 曾同时拥有工具在对话流中的放置、递归调用树编排、按工具名称分发、Generic fallback、card model 和第一方工具 renderer。…
- **2026-08-08 · 为会话持久化写入批处理设定上界**：流式响应可能会在短时间内发出大量 `assistant/chunk` 事件。此前，只要空闲队列收到一个事件，持久化协调器就会立即调度一次后端追加。该追加仍在进行时到达的事件会共用一个后续批次，但如果后端速度很快，仍可能产生大量小规模的持久化追加。每次 JSONL 追加都会创建并同步一个 Zstandard 帧或原始格式后缀…
- **2026-08-08 · 基于作用域父链的逐预设常驻挂载**：按会话挂载 preset 让面向模型的注册视图变成按 agent 的，而三个独立的宿主读取方仍然假设它是静态的：冷读 `session.history` 找不到 presenter（每张卡都静默退化成通用渲染器——与「工具本无 presenter」无法区分）、投影块丢掉 preset 注册的键（客户端把缺失键当作能力不存在并**清掉**该行）、Typert 网…
- **2026-08-09 · Client Conversation 业务节点组装与 Chat keyed snapshot**：Client Session 既维护传输窗口、连接状态和待处理交互，也在中心化 transcript fold 中解释 Assistant、Tool、消息、命令、压缩、重试及 turn tail 等业务事件。每增加一种业务节点，都要修改 Session 的 switch、历史 replay、索引、缓存和 React 分组…
- **2026-08-09 · headless 是直接使用核心服务的入口**：`headless` 的产品约定是一个本地任务：最终 assistant 文本写入 stdout，退出状态反映成功与否，成功时 stderr 为空，并且不打开监听端口。包含 Workspace Host 服务、ApiProxy、HTTP、Web 运行时或浏览器插件的组合违背这一约定，也使本地完成状态依赖无关的传输树。
- **2026-08-09 · skill 注册表由宿主持有并按 scope 分层**：agent-preset stack 曾把整个 skill 能力——注册表、本地提供方和 `skill` 工具——搬进每个 preset 的 `isolate` realm，理由是"agent 拥有哪些 skill"属于 agent 平面的选择。这一框架混淆了两个不同的问题：*部署*供给哪些 skill，与*agent*是否消费它们。…
- **2026-08-09 · 独立的 Events 兜底扫描补上 Cordis 表面完备性缺口**：`gen-cordis-catalog` 渲染 Typert host face 投影发现的每个服务与事件，fail-closed 的页面映射（`SERVICE_PAGE`、`EVENT_SCOPE_PAGE`）保证每个被发现的 key 或 scope 恰好落在一个 `docs/subsystems/` 页面上（页面区块机制归按子系统区块决定所有）。…
- **2026-08-10 · Remote 事件投递（ctx.remote.$on）**：Typert Remote 方法调用只覆盖「一次请求一个结果」的定向调用，明确把 Session 事件流与有状态交互留在别处；Host 向消费端的**单向事件推送**因此仍然全部压在遗留的 API Proxy 上。
- **2026-08-10 · Session log 版本机制：单调整数、升级器链、逐事件可忽略标记**：Session log 在发布后必须能升级格式，而最先发布的运行时决定了此后一切的下限：第一个发布版的读取器缺少哪种拒绝和降级行为，用户手里已经装上的副本就永远补不上。发布 issue #1901 的最低要求是老运行时读到新 Session 格式时明确报不支持，而不是读错。…
- **2026-08-10 · What stays host-plane once presets own the agent plane**：逐会话 agent preset 把每一个面向模型的行搬上了 agent 平面，此后的每一处修复都是一个仍按搬迁之前的世界写成的读取点。`tasks` 因为 realm 之外的 preset 行要解析它而搬回宿主；`goals` 因为同样的理由从未离开；而当所有面向模型的工具都变成祖先贡献之后…
- **2026-08-10 · fork 出的 child 保持 one-shot**：fork 与 spawn 的唯一区别是 child 的 Session 会以 parent 已完成轮次的前缀作为初始内容（见 subagent-fork-in-process）。这份初始内容有实打实的 token 成本——继承的历史会在 child 的每次请求中重新发送——而它唯一确定的回报是提供方侧的前缀复用：在提供方与模型相同的前提下…
- **2026-08-10 · 产品 subagent 提供方位于共享 profile 宿主**：Codex 与 Claude Code 提供方约定最初以可独立安装的包交付，由部署环境在通用 subagent 工具旁加载。Agent Preset 后来成为单个 agent（智能体）的模型可见工具的常规责任方，但 preset 不能安全地拥有这些产品提供方：`ctx.subagents` 是进程级注册表，提供方名称在 Host 内唯一…
- **2026-08-10 · 绑定生命周期的消息反馈伴随记录**：现有 `/feedback` 命令记录不可变的 Session 级 `feedback/record` 事件。在 `FEEDBACK_ONLY` 下，该事件可以释放待处理的遥测前缀，因此它不适合作为挂在单条 assistant 消息上的可编辑好评／差评与可选备注的权威来源。消息反馈需要独立的更新与删除语义…
- **2026-08-10 · 被取消的流定稿其已送达前缀**：`ReactLoopAgent.step()` 在消费模型流期间捕捉取消，此时 `BlockAssembler`、已记录的分片 seq 和提供方路由可以确定已送达前缀。循环把该前缀追加为 step 的 `assistant/message`…
- **2026-08-11 · Agent Note：Loader 插值条目 `disabled` 字段**：Windows 平台层（当时是 base patch 旁独立的 `windows.cordis.patch.yml`，现已折入 base 行——见「决策」）在 win32 上禁用 `tool-bash`，但 shipped 预设各自挂载了一行 `tool-bash`。预设行最后组合…
- **2026-08-11 · Trajectory 基于注册式 Conversation Context 组装数据**：Trajectory 曾维护独立的 Session History 数据源，并把完整的已加载 Event 窗口折叠为 Assistant、Tool、消息、Request header 和 Compaction 状态。Chat 已经通过注册式 Conversation Definition 组装相同的 Event 族。两条链路重复实现业务关联与分页行为…
- **2026-08-11 · Windows 上基于 terminal seam 的持久化 pwsh**：harness 在 Windows 上没有持久 shell。持久 `bash` 栈按构造就是 POSIX-only：`@deepseek-ai/dsh-subprocess-local` 在终端分配时直接抛错（`createProcessInspector()` 拒绝 win32）…
- **2026-08-11 · “插件”设置中的功能自有标签页**：插件配置与只读 Loader 清单各自注册了一个顶层 `settings.section`。两者描述同一个“插件”领域，却占据两行导航，把搜索与配置拆成互不相关的页面，也没有给 Settings 外壳一个有原则的聚合方式。若直接合并两者的组件，则会让一个功能插件 import 并拥有另一个功能的数据生命周期。
- **2026-08-11 · 仓库命名约定与预发布重命名清单**：仓库的发展速度曾超过部分名称的演进速度。一些包名描述的是最初的实现，而非所提供的能力。若干类即使实际承担注册表、运行时、引擎、控制器或解析器的职责，名称仍使用 `Service`。部分 `ctx` 键以单数命名注册表，却以复数命名单个引擎。还有一些提供方明明通过可替换的文件系统或子进程服务工作，可以在另一执行环境中运行，名称却使用 `local`。
- **2026-08-11 · 把命令行接缝收窄到既有接口**：应用自有命令行（笔记）交付时带着三条比其消费者所需更宽的接缝：一台 vendored 的内存行激活状态机（`Entry.enableRuntime`，外加从 `dsh-cmdline` 导出的 `enableRow` —— 一个命令行包拥有了 Loader 概念）…
- **2026-08-12 · Agent Note：pi-ai 模型自行声明输入模态，未声明即为文本**：`settings.yaml` 里没有任何写法能把一个手写的 pi-ai 模型描述成接受图片，而适配器对已安装 pi-ai catalog 未描述的每个模型都假定纯文本。部署通过 Web UI 的“添加自定义提供商”卡片新增的模型统统属于这一类，因此一个提供视觉模型的 OpenAI 兼容网关，无论实际提供什么…
- **2026-08-12 · 由插件自己拥有的设置表层**：**注册即暴露。** api-proxy 服务 `ctx.settings.describe()` 返回的每一个命名空间，写入不设门禁。`WEB_SETTINGS_NAMESPACES`、`PRODUCT_SETTINGS_NAMESPACES`、与 `ctx.llm.listConfigurableProviders()` 的并集…
- **2026-08-13 · 会话内容搜索通过 openAt never 以 opt-in 方式交付**：交付的 bundle 之前以启用状态挂载 SQLite 会话查询提供方的全文索引（`openAt: first-search`），因此每个默认部署都携带一个派生 FTS 索引，Web 侧边栏提供内容搜索。一个部署是否需要该索引——它的 node:sqlite 导入、每次搜索的来源对账和派生存储——是部署自身的选择，产品默认不携带它交付…
- **2026-08-13 · 凭据记录与授权 flow**：三个 seam，各自拥有一个问题；所有 pi-ai 概念都藏在 `llm-pi-ai` 内部的适配器背后。
- **2026-08-15 · 客户端壳分层与动态包边界**：Client npm 依赖区段描述安装和开发关系，但不能可靠描述 bundle 内容。把 `dependencies`、`peerDependencies` 或 `devDependencies` 当作隐式 bundler 指令，可能内联本应共享的 React 或 workspace 身份，也可能让构建后的库携带未解析子 import…
- **2026-08-17 · Agent Note：Settings describe 镜像**：一次冷启动的 web boot 在约 200ms 内发出十五次 `settings.describe`，且每新增一个持有偏好设置的客户端插件，该计数再加二。两个机制叠加：`SettingsScopeBinder.bind()` 为每个绑定的 scope 启动一次全量文档读取（产品组合中有六个 scope…
- **2026-08-17 · 客户端渲染与附件呈现的动态归属**：宿主编写的客户端图管理浏览器插件，但三条呈现路径位于其生命周期之外。Web 内核创建 React 根和由外壳持有的组装伪 entry，`ui-conversation` 以包值形式导入附件组件，外壳还导入 ui-theme 的全局样式。因此，禁用、失败或重载某个插件时，并不能同时管理属于该插件的全部渲染与 CSS。
- **2026-08-18 · Client 业务代码使用构建期公开环境变量**：`DSH_CLIENT_*` 是可公开给浏览器业务代码的构建期命名空间。业务代码可用静态点访问 `process.env.DSH_CLIENT_NAME` 选择行为；值只取自构建进程环境，不读取 Vite `.env*` 文件。设置的值在构建时内联为字符串，未设置的值为 `undefined`。
- **2026-08-18 · SQLite 物理分片行压缩**：标量 `session-persistence-sqlite` 后端为每个逻辑 `SessionEvent` 存储一个物理行。提供方流会生成 token 大小的 `assistant/chunk` 事件，并重复轮次、步骤、块、类型和 envelope 字段，因此事务批处理可以减少提交次数，却不能减少行数或重复 JSON payload。逻辑流不能合并…
- **2026-08-18 · 将 Agent Teams 作为私有实验性包孵化**：Agent Teams 的服务与工具约定仍在变化，但它需要使用真实 Session 日志、subagent 生命周期、工具、示例、快照和仓库检查。把这些包放在产品职责组会使其成为 dsh 发布系列成员，并获得与稳定包相同的发布预期。
- **2026-08-19 · Agent Note：拆分会话投影状态与客户端视图**：状态：已实现
- **2026-08-19 · 在 npm 名中标记实验性包**：目录归属、私有 manifest 与发布系列过滤可以阻止实验性包进入发布，但 npm specifier 或 Cordis 配置项无法体现该状态。外观稳定的包名可能被复制到其他组合中，而读者看不出其完整公开约定仍处于实验阶段。
- **2026-08-19 · 结构化 index 注入表（webserver/index-inject 事件）**：注入面事件化、数据化：webserver 声明 `webserver/index-inject` 事件与纯数据行类型 `IndexInjection`（`global`/`script`/`script-src`/`style`/`html`，`head|body` 定位）。想注入的插件订阅事件、往表里 push 行…
### 25.2 已实现 · 功能（186 条）

- **2026-06-14 · 在单个连接上多路复用并发 ACP 会话**：本 Agent Note 写于 ACP 还是编辑器桥接层的时期，动机来自 Zed 的多会话客户端模型。ACP 作为仅面向自动化的协议移除了编辑器接口；多路复用决策本身不变，本 Agent Note 现依照自动化约定陈述它。
- **2026-06-15 · Code Mode——模型针对工具注册表编写 TypeScript**：在注册表的原生呈现方式下，agent loop（智能体循环）将每个可见能力以 JSON Schema 函数定义的形式通告给模型。`ToolRuntime` 将其 schema 贡献给系统提示词组装，组装结果中的 `tools` 落到协议格式（wire format）上（也记录在请求头日志中），模型每步调用一个 `tool-call` 块…
- **2026-06-17 · 文件系统工具 schema——面向模型的读/写/编辑接口形状**：文件系统能力 seam Agent Note 定义了文件系统能力 seam（`ctx.fs`）、包拆分（`dsh-fs`、`dsh-fs-local`、`dsh-tool-fs`，加上 `dsh-fs-observation-policy` 策略插件）…
- **2026-06-18 · 压缩作为能力 seam（抽象约定 + 基础后端）**：长时间运行的 agent（智能体）对话会无限增长。随着事件日志不断累积轮次，派生出的消息历史最终逼近模型的上下文窗口，模型随即在响应中途停止生成（`max-tokens`），或表现退化。**上下文压缩（context compaction）** 是对此的缓解手段：用一段简洁的摘要替换一批较早的历史，保持近期上下文完整。
- **2026-06-21 · subagent 能力 seam**：完整 seam 已交付：`dsh-subagent` 接口与 `dsh-tool-subagent` 消费方；两个进程内后端（`dsh-subagent-spawn-in-process`、`dsh-subagent-fork-in-process`）；嵌套 agent（智能体）快照基础设施（逐会话快照回放）…
- **2026-06-22 · ACP subagent 后端（进程外委派）**：subagent seam（seam Agent Note）的设计使多个后端可以按名称共存于 `ctx.subagents`。进程内后端（`-spawn`/`-fork`）将子 agent（智能体）作为第二个 `Agent` 运行在同一个 Cordis 上下文中：开销低，但子 agent 与父 agent 共享进程、模型客户端和工具。…
- **2026-06-24 · 工作区上下文指令文件**：`AGENTS.md` 等仓库指引应当进入编码会话的有效上下文，使项目约定、构建命令和评审规则无需由用户反复粘贴即可生效。stdio 与 ACP（Agent Client Protocol）产品需要具备相同行为，并按会话 cwd 隔离：全局系统提示词章节会把一个工作区的文件泄漏到另一个仍在运行的 ACP 会话中。
- **2026-06-29 · `todo_write` 工具——将模型任务列表作为事件溯源的会话状态**：harness 为模型提供了 bash 和 subagent 工具，却没有办法记录结构化的任务列表。todo 列表有两个同等重要的用途：引导模型规划多步骤工作并保持当前活跃工作明确；同时为交互式宿主提供实时进度清单。调研的所有参考编码 agent（智能体），包括 claude-code、opencode、codex、oh-my-pi 和 pi…
- **2026-06-30 · SessionStore fork API**：事件溯源的会话日志已经具备 fork 所需的原语：创建一个带有种子事件前缀的新会话，然后像回放一样从该种子日志推导模型历史。这个原语有意保持底层：`ctx.sessions.create(id, { seed, meta })` 接受任何合法种子，但常规的活跃会话分支需要围绕以下问题制定策略：哪些前缀可以被复制、子会话应打上哪些元数据、以及错误如何分类。
- **2026-06-30 · dsh-hook-protocol——Claude Code / Codex 钩子协议格式共享核心库**：钩子子系统提供两个桥接插件：一个运行用户既有的 Claude Code（CC）钩子，另一个运行 Codex 钩子。参考实现（`~/repos/refs/claude-code`、`~/repos/refs/codex`）表明一个决定性事实：**Codex 有意重新实现了 CC 钩子协议的一个子集。** 它的引擎读取相同的 `hooks.json`…
- **2026-06-30 · dsh-hooks-claude-code + dsh-hooks-codex —— Claude Code / Codex 钩子桥接插件**：harness 的扩展面是其类型化拦截点（见拦截扩展点 Agent Note）：所谓「原生钩子」不过是一个普通的 Cordis 插件，订阅 `agent/session-start`、`agent/pre-step`、`tools/pre-execute`、`tools/post-execute`、`agent/turn-stopping`、`subagent…
- **2026-06-30 · 拦截扩展点——钩子编程所面对的类型化 Decision 接口**：harness 需要一套钩子子系统：用户像 Claude Code（CC）和 Codex 那样在生命周期节点扩展或管控 agent（智能体）。驱动本设计的关键视角转换是：**「原生钩子」不是一个包**——原生钩子只是一个普通的 Cordis 插件，订阅规范的生命周期事件。因此真正的产品是一个*强大、类型完备的规范事件接口*…
- **2026-07-05 · Skill 系统——面向 agent 的渐进式指令披露**：agent（智能体）产品已趋同于一种 skill（技能）模式：保持请求提示词精简，仅列出可用的指令包，当模型判定某任务匹配时再加载完整正文。Codex、Claude Code、OpenCode 与 Kimi Code 在细节上各有不同，但都将发现元数据与完整指令分离，使工作区能承载可复用的行为而无需在每个轮次支付全量提示词开销。
- **2026-07-05 · 动态工作流——脚本驱动的多 agent 编排 seam**：harness 可以通过 `dsh-tool-subagent` 将一个任务委派给一个子 agent（智能体），但需要扇出到多个独立部分的工作——跨多文件审计、迁移、多角度调研、对抗式验证——迫使模型逐轮次编排：每个中间结果都落入父上下文，计划无处持久存储，每一步的协调都要消耗一次模型往返。…
- **2026-07-06 · 子进程沙箱——约束 seam、原生 runner、升级机制与按会话模式**：一个编码 agent（智能体）需要如下产品路径：bash 子进程（以及依附其上的钩子命令）默认在受限的文件沙箱下执行；当且仅当沙箱实际拒绝了某个操作时，模型可以为同一操作请求一次用户批准，获批后以更宽的权限重试一次。本设计刻意不声称覆盖所有工具：fs/web/todo 在进程内执行，`execve` 包装层对它们毫无意义（§ 进程内工具）…
- **2026-07-06 · 审批 seam——基于 waterfall（瀑布式事件）应答者的一次性权限决策**：两个调用方需要同一个封闭决策——「这个具体操作可以继续吗？」：`tools/pre-execute` 的 `ask` 决策（包括 Claude-Code 钩子桥的 `permissionDecision: ask`）以及沙箱 Agent Note 中拒绝后的一次性升级重试。一个共享的 seam 使它们无需各自发明独立的结果词汇、通道路由、取消机制和审计轨迹…
- **2026-07-06 · 显式的模型侧工具顺序**：模型侧的工具顺序此前跟随插件注册顺序，而注册顺序取决于相互独立的插件的并发模块加载。这种竞态在 CI 和快照录制中产生了不同的请求头。由于顺序影响请求字节、缓存和持久化的请求头，因此需要一个显式的确定性策略。
- **2026-07-07 · MCP 客户端插件——连接外部 MCP 服务器并桥接其工具**：harness 此前无法消费 MCP（Model Context Protocol）生态中的工具。MCP 是工具服务器的新兴标准——GitHub、文件系统、数据库、代码搜索以及数百个社区服务器都通过 MCP 暴露工具。用户希望将 harness 指向一个或多个 MCP 服务器，让其工具以原生的模型可见工具形式出现，而无需为每个服务器编写胶水代码。
- **2026-07-08 · 后台 subagent 任务**：subagent seam 会返回 `SubagentRun`，但原先面向模型的工具会同步收集每一次运行。因此，各自独立的慢速委派要么一直占用父调用，要么按串行方式运行。
- **2026-07-08 · 自引用 cordis 工具集**：本 harness 中的一切都是 cordis 插件，但运行在该插件运行时内部的 agent（智能体）既看不到也碰不到它：它无法枚举周围的服务和事件，无法在会话中途为自己添加新工具，也无法组合自己发明的能力。赋予模型这种能力值得探索——一个能审视并修改自身运行时的自引用 agent——但这同时引发三个正确性问题，本设计的核心正是回答这些问题…
- **2026-07-10 · SQLite FTS5 会话搜索**：精确读取的 `ctx.sessionQuery` 服务有意不维护派生索引。大规模持久化的历史记录需要全文搜索，而不是每次查询都扫描全部事件；当前的活跃会话则需要一个包含上次持久性检查点之后更新的覆盖层。搜索还需要具体的排序、摘要片段、过滤器、分页、取消以及重建行为。
- **2026-07-10 · 向工具与钩子公开 agent 会话标识和 JSONL 位置**：agent（智能体）可以通过 `session.header.cwd` 识别其工作区，但使用 bash 的模型无法可靠识别当前调用所属的会话，也无法找到记录该调用的持久 transcript（文本记录）。搜索 `./.sessions` 等同于猜测部署配置和 JSONL 布局；自定义根目录、替代持久化后端、恢复、fork，以及并发运行的父子 agent…
- **2026-07-10 · 按单次调用安全性并行执行工具调用**：一条 assistant 消息可以包含多个并列的 `tool-call` 块。尽管模型已经同时请求了这些调用，串行执行仍会叠加各个独立读取和 Web 请求的延迟。
- **2026-07-12 · 配置 subagent 的人设、工具可见性与深度**：一个可复用的 subagent 提供方解决的是「如何运行子 agent（智能体）」的问题，但不同的委派工具需要不同的子 agent 行为。某个部署可能需要评审者人设、仅限研究的工具集，或硬性递归上限，而不必为每种组合创建新的提供方。
- **2026-07-13 · 会话查询关系追踪**：会话关系分散编码在不可变 header、位置式表面操作和已记录的来源事件 seq 引用数组中。消费方如果直接重建这些关系，就必须重复实现语料优先级、表面折叠、格式错误日志的处理、确定性的谱系顺序和克隆。位置替换关系与来源事件引用关系表示不同含义，因此把两者合并为一种通用边类型也会丢失含义。
- **2026-07-14 · 跨家族文件沙箱——统一策略归属、沙箱化 fs 提供方、fs 升级对等**：`SandboxMode` 所声明的语义涵盖文件系统效果，但最初只有 `ctx.shell` 强制执行该策略。fs 工具（`write`/`edit`）在进程内经由 `ctx.fs` 变更宿主文件系统，那里的 OS argv 包装在机制上毫无意义——沙箱 Agent Note § 进程内工具记录了这一点，并把跨家族强制执行留作一个暂缓阶段…
- **2026-07-16 · Harness 层目标式执行**：具体 agent loop（智能体循环）只拥有一个轮次：它排空已接纳输入，执行一个或多个模型与工具步骤，然后停止。大型目标通常需要一项外层策略来开始另一个轮次、保留进度、在达到预算上限时停止，并让人类能够理解其状态。定时提示词、同会话续行和全新 agent Ralph 尝试都会重复工作，但它们并不共享相同的状态、权限、记忆或生命周期。
- **2026-07-16 · 持久化 PTY 会话**：harness 可以运行前台与后台命令、编辑文件和委派工作，但无法跨工具调用延续一次交互式终端对话。每次 `bash` 前台运行都会启动一个新 shell，因此 shell 内的 cwd、导出变量、虚拟环境激活状态、函数、job control 状态和交互式子进程都会随本次调用结束。
- **2026-07-16 · 持久的逐步骤时间上下文**：仅存在于请求中的时钟可以告诉模型当前时间，但在系统提示词中替换这个值会移除先前对时间敏感的推理所依据的证据。在包含多个步骤的轮次中，请求需要保留先前步骤使用的读数。系统必须能在重启后重建请求，自动压缩（compaction）也必须将模型实际收到的同一份时间上下文纳入考量。
- **2026-07-19 · 全新 agent Ralph 工作流工具**：同会话目标会保留对话，让一个 agent（智能体）持续完成持久目标；通用工作流工具则让模型编写扇出编排脚本。两者都不是 Ralph 模式：把同一目标反复交给全新的工作者，以共享工作区作为长期记忆，并且在各 Round 之间只传递一份小型显式交接，直到工作完成或触及限制。
- **2026-07-19 · 同会话 Goal Round 驱动器**：目标领域可以保留目标，模型可见工具也可以变更其生命周期，但两者都不应决定下一个模型轮次何时开始。继续执行驱动器必须把活跃目标状态桥接到普通 agent loop（智能体循环），同时不能向 `dsh-agent-loop` 添加目标专用分支、创建第二段对话，也不能把每个人类轮次都视为自主迭代。
- **2026-07-19 · 持久化的同会话目标领域**：长时间运行的目标会跨越单个提示词、轮次或模型请求。若把该目标视为内存中的循环变量，进程重启时就会丢失；若只存放在 UI 状态中，又无法重建模型行为。若把会话中的每个轮次都视为目标进度，与自动工作无关的人类消息也会消耗预算。
- **2026-07-19 · 插件自有的人类命令注册**：TUI 拥有斜杠命令。如果命令名、帮助文本、自动补全、分派和取消都留在适配器内部，每个新命令都需要修改 TUI，可选插件也无法贡献命令。把斜杠输入当作普通模型提示词同样不安全：用户可见的直接操作可能意外消耗 token，或让模型重新解释未知命令。
- **2026-07-19 · 面向人类的 `/goal` 命令**：同会话目标领域和模型工具提供了状态机与自然语言语义路径，但尚不足以构成面向人类的 UX。用户需要在不询问模型的情况下检查准确的当前阶段与 Round 预算，在不消耗模型轮次的情况下明确暂停或清除工作，并在会话恢复后经过必要的人类决策重新激活已恢复的活跃目标。若在各 UI 中分别实现这些操作，就会重复解析逻辑、导致各界面发生偏差…
- **2026-07-19 · 面向模型的同会话目标工具**：持久目标领域有意把生命周期动词提供给插件，而不直接提供给模型。模型仍然需要一个小型控制 API，用于发现当前目标、根据人类意图创建目标并改变其生命周期。仅靠提示词指导无法确定是谁授权了一次变更：subagent、注入的插件消息、陈旧的模型轮次或恢复后的会话都可能产生相同的工具参数。
- **2026-07-20 · Code Mode 的类型化工具返回值**：Code Mode 过去会把每个嵌套工具的结果从 `ContentBlock[]` 重新投影为一个字符串。这样虽然保留了适合人类阅读的 Native 呈现，却丢失了工具已经生成的规范结果：程序只能从自然语言中提取 job id 和动态挂载 id；结构化搜索与工作流结果失去原有形态；非文本块则变为占位符。生成的 SDK 可以描述参数…
- **2026-07-20 · dsh CLI 与来自 Harness home 的个人配置 overlay**：开发者自己的偏好——TUI 使用哪个提供方和模型、个人凭证、私有的适配器路由——除了改动已提交的文件之外无处安放。要把 TUI 示例指向个人的 Anthropic 代理 Opus 路由，只能在工作区里改 `examples/tui-agent/cordis.yml` 和 `.env`，既有提交密钥的风险，又要在每个 checkout 里重复一遍。…
- **2026-07-21 · 加载全部指令候选并按目录去重**：agent-instructions 插件在每个目录中为每个候选列表只解析出一个胜出文件：`instructionFileCandidates` 中第一个存在的名字赢得基础槽位，本地覆盖层再追加一个胜出者。但 `AGENTS.md` 与 `CLAUDE.md` 经常共处同一目录。在多数仓库里其中一个是另一个的符号链接，因此内容完全相同…
- **2026-07-21 · 可继续的后台 subagent**：本记录已由可继续的 subagent取代——后者以一个持久 Session 加至多一个进程内 Activation（驻留期）替换了其基于 Task 的 activation 模型、路由、取消和持久性语义。其服务放置与提供方功能策略此前已由将 subagent 控制合并到 subagent 服务和以意图命名的 subagent 继续执行操作取代。…
- **2026-07-21 · 基于日志的会话标题**：会话需要一个面向用户的简短标题，编辑器、终端或查询消费方才能有效呈现它。成本最低的实现可以从第一条提示词派生标题，质量更高的实现则可以让模型处理第一条提示词或整个对话。这些策略在延迟、成本、路由和重试行为上各有不同，但所有消费方都需要一个持久的真源。
- **2026-07-21 · 跟随符号链接指向的指令文件**：agent-instructions 插件在解析前用 `ctx.fs.lstat` 探测每个指令候选，拒绝任何末段的符号链接，从而使仓库自有的链接无法把指令加载指向工作区之外的内容。这条「不跟随」不变式挡住了一个有意为之、且受支持的配置：用户若把 `$DSH_HOME/AGENTS.md`（或某个项目的 `AGENTS.md`）符号链接到别处保存的一个规范指令…
- **2026-07-21 · 跨会话引用**：Web 用户需要把另一场对话中的相关工作带入一条新消息，但不恢复、不 fork，也不让源 transcript（文本记录）对当前会话拥有权威性。harness 已经提供准确的会话枚举与原始事件检查，但若每个宿主都独立解析日志，就会重复实现压缩（compaction）折叠、来源过滤、大小限制、错误行为和持久化。…
- **2026-07-21 · 默认的本地指令覆盖层**：被 git 忽略的个人指导文件（`AGENTS.local.md` / `CLAUDE.local.md`）是 Claude Code 的一项约定，用于存放刻意不提交、每位开发者各自的覆盖内容。agent-instructions 插件每个目录只加载一个候选…
- **2026-07-22 · Agent Note（agent 决策记录）：持久化 subagent 目录与 list_agents**：可继续的后台 subagent 会公开稳定的 child id，并将重建数据持久化在该 child 的会话中，因此 `send_message` 无需任何列表查询操作即可恢复已知 child。发现功能有两类需求不同的消费方：UI 可以同时展示一次性工作和可继续对话，而模型只应收到适合使用 `send_message` 的 child。…
- **2026-07-22 · Web 多模态图片输入与持久附件**：在此变更之前，Web 输入区仅接受文本：`InputBar` 接收字符串草稿，`ConversationController.send()` 创建文本内容，宿主再把该内容转发给 agent（智能体）。用户无法粘贴图片、在发送前查看图片、提交仅含图片的提示词，也无法从历史记录中恢复已发送图片。
- **2026-07-22 · 显式指定 Web 绑定地址**：即便浏览器与服务器运行在同一台机器上，`dsh web` 也会绑定所有网络接口。因此，本地使用会在操作者未明确选择的情况下暴露一个未经身份验证的开发服务器；另一方面，远程容器和局域网浏览器场景仍需要一种受支持的方式来接受非环回连接。
- **2026-07-23 · Web UI 权限预设与审批应答**：Web 承载层启动的是一个不受限的 agent（智能体）：`bootHost` 组合了 `dsh-bash-local` 与 `dsh-fs-local`，因此每个 Web 会话都以完整文件访问权限运行，既无审批通道…
- **2026-07-23 · Web todo 展示——快照副作用通道 + 两个渲染面**：`todo_write` 把 `todo/write` 的整份列表快照追加进会话日志；TUI 渲染一块常驻的 plan 面板（自动化专用的 ACP（Agent Client Protocol）桥接刻意不做 todo 呈现）。Web 客户端把这个事件整个丢弃了：host mux 流本已转发每一个会话事件…
- **2026-07-23 · Web 对话中安全的 assistant Markdown**：Web 对话通过会话事件、历史回放与流式累积保留 assistant Markdown 源文本，但其最末端的文本原语会按字面渲染源文本。若修改共享原语，用户消息与 steering（中途引导）消息也会被格式化；若在运行时中解析，则会把呈现状态混入不依赖 React 的会话投影。
- **2026-07-23 · 设有强制脱敏点和 OTel 后端的会话遥测 seam**：每个想把 harness 会话接入可观测性体系的部署方都得手写一个会话日志消费方：订阅、生命周期交接、以及最难的脱敏——原始日志携带文件内容与命令输出，可能内嵌凭据。遥测 seam 和 OTel 后端曾在 `session-telemetry-otlp-rfc` 分支（PR #222/#231）上完成过一版…
- **2026-07-24 · Web 对话输入区的会话模型选择**：Web 对话需要一项由 Host 提供、可见且可更改的会话模型选择。如果照搬 TUI 的呈现方式，或在浏览器中硬编码 DeepSeek 模型，就会让模型发现逻辑和步骤边界语义分散到不同前端中。响应运行期间发生的切换还需要一个原子边界：提示词变量与请求路由不能观测到不同的选择。
- **2026-07-24 · 逐提供方请求重试策略**：同一进程可能把模型请求路由到可靠性和成本约束各不相同的提供方。单一的瞬态错误分类器与有限重试预算无法表达这种部署需求：大多数提供方只需有界恢复，但其中一个提供方必须持续重试每次模型请求失败，直到请求成功或调用方取消。
- **2026-07-24 · 面向模型的会话查询工具**：统一的 `ctx.sessionQuery` 服务对优先使用实时数据的会话日志提供精确读取、过滤、关系追踪与全文搜索，但模型无法直接使用该服务。若把提供方请求类型交给模型，还会暴露不稳定的分页游标、受信任的语料范围、存储形态的时间值，以及更适合程序化消费方而非模型推理的结果记录。大型追踪与事件负载另有输出大小问题，但若在该消费方内部解决…
- **2026-07-25 · Session 列表浏览与 Workspace 手动排序**：Workspace UI 完整产品流交付了分组 session 列表的首个形态，并把 Rename、拖拽排序等操作明确划出当期范围。设计稿（figma 239-10458 及关联画面）随后补齐了这些交互：列表要能切换成不分组的平铺视图、session 行悬停要出详情卡与操作菜单、workspace 要能改名、组内 session 要能手动排序。
- **2026-07-25 · Workspace UI 完整产品动线**：Domain KV storage 与 Workspace entity定义了 Workspace 的持久实体、路径规范和有序 Session 账本，但没有定义 Host 接线、历史数据初始化或 GUI 动线。GUI 同时呈现 Workspace 和 Session；用户进入 New Session 后必须能够立即输入…
- **2026-07-25 · 进程内 subagent 策略继承——子 agent 在父级的沙箱覆盖项下启动**：沙箱与审批覆盖项都是按会话的日志折叠。进程内 subagent 会获得一个新会话，因此 spawn 子 agent（智能体）过去会回退到部署默认值，fork 子 agent 则只能看到其已完成轮次前缀中的切换。因此，委派可能放宽已经切换到 `read-only` 的父级。
- **2026-07-26 · Code Mode 的 UI 基础——run_code 的 description 参数，以及与原生同等保真的分发日志**：范围：让 UI 能以与原生工具调用相同的保真度渲染 Code Mode 轮次的宿主侧约定变更，即其他 Code Mode UI Agent Note 赖以构建的基础。传输设计归 Code Mode 基础所有；模型可见的 `description` 参数、携带完整内容的 `tool/code-dispatch` 载荷…
- **2026-07-26 · Code Mode 的 chat 渲染——子调用作为父行之下的原生行**：范围：Web chat 视图如何渲染一个 `run_code` 轮次，即 Code Mode UI 栈的客户端侧部分，构建在宿主侧基础之上（携带完整内容的 `tool/code-dispatch`、必填的 `description` 参数）。本篇所依托的 slot 模型归 toolview 溶解所有。
- **2026-07-26 · Code Mode 的实时分发生命周期，以及复用原生约定的并行执行**：范围：`tool/code-dispatch-start` 事件、Web chat 中每个子调用的运行状态，以及桥接层调度器对原生并发约定的复用。构建在宿主侧基础与 chat 子调用行之上；原生约定本身归并行工具调用 Agent Note 所有。
- **2026-07-26 · 允许同时存在多个 `in_progress` todo**：原始 `todo_write` 设计在 `execute` 和持久日志不变式中都强制每个列表至多一个 `in_progress` 任务。该不变式假设工作是顺序进行的，但 harness 会运行真正并行的工作（通过委派工具启动的并发 subagent、后台 bash 命令、工作流扇出），而一个只能标出单个活跃任务的列表无法表示这种情况。…
- **2026-07-26 · 将 Code Mode 子分发结果的持久化副本纳入 spill 机制**：范围：用既有的 spill 实现限制 `tool/code-dispatch` 事件的内容。宿主侧基础 Agent Note 有意接受了不设上限的日志，并把 spill 支持留到本次更改；实时并行 Agent Note 定义了该监听器处理的事件对。
- **2026-07-27 · Skill 目录热刷新**：skill（技能）摘要是模型的路由输入，但本地 skill 可在会话启动后新增、消失或重命名。IDE、Git 操作、shell 命令和其他进程都可以修改 `.agents/skills`，而不经过 harness 文件系统工具。仅在启动时构建目录，会让模型无法获知新 skill，并且仍能调用已删除的名称。反之，如果把每次指令正文编辑都视为目录修订…
- **2026-07-27 · TypeScript SDK 客户端与 SDK subagent 后端**：stdio JSON-RPC 对外服务接口（`@deepseek-ai/dsh-sdk-jsonrpc-server`，见单文件可执行 Agent Note）当时只有一个客户端：Python SDK。想要同样「把 harness 作为子进程驱动」能力的 TypeScript 消费方——仓库测试、自动化…
- **2026-07-27 · Web session fork 操作**：本决策中的消息资格部分由已完成轮次尾部决策收紧；共享运行时操作、注入归属、标题处理和同级列表决策仍然有效。
- **2026-07-27 · Web subagent 目录与用户继续交互**：由会话支撑的 subagent 具有持久化身份、持久化 transcript（文本记录）与直接 child 目录，但普通会话谱系无法将它们与 fork 区分开，也无法证明其描述符 mode 与继续执行授权。否则，绑定到 agent（智能体）的通用 Host 操作可能在其直接 parent 继续执行 owner 之外恢复或驱动 child。
- **2026-07-27 · Web 历史会话搜索**：Web 侧边栏会展示会话标题及其 Workspace 归属，但无法根据只出现在消息中的词语检索历史对话。在浏览器中扫描历史记录，需要附加或加载每个会话，重复实现现有的索引搜索服务，也会让冷态持久化会话的检索既缓慢又容易遗漏。产品还需要一条可预测的故障路径：派生索引不可用时，不得抹去客户端能够在本地计算出的标题匹配结果。
- **2026-07-27 · Web 文件与会话引用**：Web 输入框已有可复用的斜杠命令／引用触发流水线，但它的 `@` source 只是不会产生实际作用的 subagent 标签文本。Web 需要由宿主提供工作区路径发现和结构化跨会话快照，同时避免在浏览器中扫描宿主文件系统或把会话身份绑定到显示标签。
- **2026-07-27 · tmux 位置上下文**：运行在 tmux 内的 agent（智能体）无法告诉模型自己身在何处：进程占据哪个 session、window、pane，以及 window 如何布局。当用户操作多个 pane 时，希望模型能对自身位置有所定位，从而让「下方的 pane」「这个 window」之类的指令得以解析。位置必须以持久、可重建的上下文形式送达模型，而非在原地被改写的系统提示值…
- **2026-07-27 · 删除 Workspace 注册记录**：Workspace 注册已有代码目录，使 GUI 能够为目录命名，并对其会话排序。该记录没有说 Harness 创建或拥有该目录，会话日志也是独立的持久化对象。若将行内 Delete 操作视为递归删除源码或删除会话，就会破坏该记录所有权边界之外的数据。
- **2026-07-27 · 原生工作区目录选择器**：桌面端 GUI 在添加现有工作区时要求用户输入绝对路径。相比使用操作系统原生选择器选取目录，这种操作速度更慢，也更容易出错。GUI 由本地 Web 载体提供，因此打开原生对话框也会形成一条特权边界，普通远程请求不得越过这条边界。
- **2026-07-27 · 轨迹检查记录表**：轨迹视图需要在同一视口内清晰呈现正文、机器载荷、token 用量、计时数据和嵌套工具活动。此前堆叠式的轮次与步骤卡片虽然保留了层级，却在重复界面框架上耗费了太多垂直空间；完全扁平化的表格又会抹去因果结构，而这种结构正是轨迹视图的价值所在。角色配色还可能借用成功与警告语义，使视觉装饰与运行时状态无法区分。
- **2026-07-28 · SDK 最大输出 token 数**：Python 与 TypeScript SDK 可以选择提供方和模型，却无法限制对话模型输出。即使评测宿主要求固定输出预算，运行时仍会省略 `GenerateOptions.maxTokens`，由提供方默认值控制。`compaction-basic.maxTokens` 只限制压缩摘要调用，不能承担这一职责。
- **2026-07-28 · Web 终端卡片：bash 渲染意图抵达浏览器**：bash 工具的调用与结果都声明 `card: 'terminal'`（渲染意图联合类型）：调用视图携带命令、一段可选的模型撰写描述以及工作目录，结果视图携带输出、退出码与终止信号。该视图早已抵达浏览器——host、connection 与 runtime 把它投递到 `ConversationSnapshot` 的 `callView`/`resultVie…
- **2026-07-28 · `/feedback` 命令**：用户在会话中途发现问题时，没有地方记下这个观察。告诉模型会浪费一个轮次、改变用户原本进行的对话，并把这条评论埋进派生历史，使后续读者无法找到它。写到会话之外则会丢失让它有意义的上下文：属于哪个会话、处于哪个时点、针对哪项工作。
- **2026-07-28 · 下一轮次开始时清空 todo 计划条**：`todo_write` 在会话日志中存储完整列表快照，交互式宿主把最新列表渲染为计划条（web TodoPanel 经 `todos` 投影，TUI Plan 面板）。一个轮次结束后，该条仍留在下一用户轮次的屏幕上——上一任务已完成或已放弃的清单。读者把计划条理解为「本轮次正在做什么」，因此跨轮次的陈旧列表是错误的产品生命周期。…
- **2026-07-28 · 可继续的 subagent**：本记录取代可继续的后台 subagent中由 Task 支撑的继续执行管理器。它保留将 subagent 控制合并到 subagent 服务确立的单一 `ctx.subagents` 服务，以及以意图命名的 subagent 继续执行操作确立的 `followup` 操作。
- **2026-07-28 · 在工具调用中用系统应用打开文件**：聊天工具行把整行摘要当作点击目标，点击后打开右侧 details 面板，并带有整行悬停背景。对文件系统工具而言，有用的动作是用操作系统默认应用打开所涉文件，而不是在侧栏里查看原始工具载荷。
- **2026-07-28 · 模型与用户彼此独立的 skill（技能）调用策略**：skill 注册表最初将发现操作视为模型目录：`ctx.skills.list()` 会移除禁止模型调用的 skill，而 `ctx.skills.get()` 仍是不过滤内容的可信 loader。该设计足以支持由模型发起的加载，却无法表示与 Claude 兼容的四类 skill：仅向用户公开、仅向模型公开、同时向两者公开，或者两者均不公开。…
- **2026-07-28 · 跨 workspace 会话恢复**：共享 CLI（命令行界面）配置提供 Harness home 下的同一个会话根目录，选择器获得 workspace 范围，交接过程携带目标目录。
- **2026-07-29 · Ask-question Web 呈现**：Web GUI 已经可以通过 `QuestionComposer` 的输入区接管收集回答，但其周边的会话记录呈现在三个方面是错的。待回答的问题会渲染两次：一次是输入区接管，一次是早于接管存在的只读 `PendingCard` 占位卡片。…
- **2026-07-29 · 持久 Bash 与字符串替换编辑器工具**：部分部署需要只调用一次的 Bash schema，同时要求 shell 状态跨模型轮次保留；另一些部署需要与终端选择无关的 Claude 风格 `str_replace_editor`。把两个工具绑在一起或按某个基准命名，会阻碍复用并模糊配置归属。
- **2026-07-29 · 目录选择交互的自适应默认值**：目录选择 seam 把交互形态做成了 `cordis.yml` 的切换点，但随附的组合仍必须固定一个后端：处处用 `-browse` 意味着本地操作者永远得不到 OS 选择器，处处用 `-native` 则弄坏所有远程部署。…
- **2026-07-30 · DeepSeek 官方首次使用凭据配置**：web 配置平面让提供方设置与凭据可以实时编辑，但首次使用的用户仍会进入空白对话 Hero；当随产品提供的 `deepseek-official` 路由缺少凭据时，界面没有给出可采取操作的说明。Models 页能修复该状态，但要求用户自行发现这个入口会削弱首次使用引导。界面不得混淆凭据缺失与适配器缺失：浏览器可以为现有凭据引用存入值…
- **2026-07-30 · Read card — the read tool's structured line window reaches the client**：`read` 工具返回规范化输出对象 `{ path, offset, lines: [{ number, text }], totalLines }`，但它的展示层把这个结构压平了。`presentCall` 声明为 `GenericCallView`（`kind: 'read'`，一个跟随定位）…
- **2026-07-30 · Web diff 卡片 —— write/edit 渲染意图抵达浏览器**：`DiffBlock` 是一个 `ui-primitives` 组件，把文件改动渲染为内联 diff 表面，write/edit 调用的两个 Web 渲染点都通过它消费 diff 渲染意图：chat 工具行的行体和详情面板的 Output 区。…
- **2026-07-30 · Web result card — a structured render intent for web_search and web_fetch**：向 `ToolResultView`（`packages/core/tools/src/presentation.ts`）新增一个 `card: 'web'` 结果分支，它是以 `kind: 'search' | 'fetch'` 字段作判别的联合 `WebResultView = WebSearchResultView | WebFetchResultVie…
- **2026-07-30 · Web result 卡片前端 —— 在浏览器渲染 web 渲染意图**：`WebBlock` 是一个 `ui-primitives` 组件,渲染一次已完成的 web 检索,web 调用的每个 Web 渲染点都通过它消费 `web` 渲染意图:键控的 chat 工具行（`web_search`/`web_fetch`）、`GenericToolCard` 渲染点兜底,以及详情面板的 Output 区。…
- **2026-07-30 · Web 中的远程 Markdown 图片**：assistant Markdown 可以使用标准图片语法引用图表和截图，但 Web 渲染器会把每张图片替换为斜体替代文本。因此，即使目标地址是绝对 HTTP(S) URL，也无法获得普通的 Markdown 图片行为。
- **2026-07-30 · Web 工具行统一展开交互与 trajectory Inspect**：聊天视图的工具行交互已经分裂成多种方言：ToolRow 通过前导图标切换展开、且仅限有 args body 的调用，bash 示例有自己的一套展开方式，todo / ask-question 行只能展开原始 args，单文件工具完全不可展开，而调用的 OUTPUT 只能通过详情面板查看。…
- **2026-07-30 · Web 搜索卡片 —— grep 与 glob 的 render intent 到达浏览器**：`SearchBlock` 是一个 `ui-primitives` 组件，把一次已完成的搜索渲染成两种形态之一，`grep`/`glob` 调用的 Web 渲染点都通过它消费搜索 render intent。…
- **2026-07-30 · Web 读取卡片前端 —— 读取工具的行窗口以带行号、语法高亮的形式渲染**：`ReadBlock` 是一个 `ui-primitives` 组件，把一次读取结果渲染成带行号、可选语法高亮的文件视图，读取的两个 Web 渲染点都通过它消费读取渲染意图：聊天工具行（常驻在摘要行之下）与详情面板的 Output 区段。…
- **2026-07-30 · 使用单一持久锁实现排队手动压缩**：自动压缩（compaction）可以保护上下文窗口，但交互用户还需要一种确定性方法，在压力策略触发前压缩累积的历史。把 `/compact` 作为提示词文本发送会消耗一个模型轮次，还会让会话模型重新解释一项直接控制操作。在某个 UI 内实现该功能，则会重复命令发现、生命周期日志记录、取消与后端策略。
- **2026-07-30 · 可继续 subagent 报告工具**：可继续的进程内 subagent 能够接收 parent 后续发来的消息、保留后代、结算并冷恢复，但基础生命周期无法让它们将选中内容发送给直接 parent。child 的完整输出已可从持久化会话中重建，因此缺失的能力是显式投递，而非结果存储。
- **2026-07-30 · 将 Web 已排队消息转为活动轮次的 steering（中途引导）**：Web composer 原本会在 agent（智能体）运行期间把所有 Enter 提交作为 Queue 入队。QueueDock 已经为每条待处理消息提供可寻址的行，持久 transcript（文本记录）也已能把消费后的 steer 事件渲染为用户样式气泡，但 Web 既没有连接这两个界面的操作…
- **2026-07-30 · 当前沙箱策略上下文**：沙箱策略已经强制执行并记录每个会话的文件操作模式，但新的模型请求并不包含这一状态。在 `read-only` 下的 Web 会话中，write 与 edit schema 仍然可见，因此模型会声称自己能够写入，直到一次被拒绝的调用后才发现事实并非如此。执行 `/permission danger-full-access` 后，下一个请求带有批准策略变更…
- **2026-07-30 · 搜索渲染意图——grep 与 glob 产出结构化搜索卡片**：`grep` 与 `glob` 返回结构化的 canonical 值——`grep` 是扁平的 `{ matches: [{ path, lineNumber, line }] }`，`glob` 是 `{ paths: string[] }`——但每个 UI 只见过它们面向模型的渲染文本：`grep` 把匹配按文件头分组、每行 `Line N:`…
- **2026-07-30 · 版本化 GUI 欢迎引导**：GUI 的凭据引导从 DeepSeek 专用的就绪状态检查开始，但内部测试通知适用于每位用户，即使凭据已经配置，也必须先于提供方设置显示。若把两者作为独立浮层处理，多个对话框可能同时出现；仅存于进程内的关闭标记既无法区分通知已完成确认还是窗口在确认前已关闭，也无法在文案有意修订后重新显示一次通知。
- **2026-07-30 · 计划审阅是一次决定，不是一道题**：`exit_plan_mode` 通过 `ctx.userQuestions.ask()` 把写好的计划交给用户审阅，而这正是 `ask_user_question` 使用的同一个 seam。在 Web GUI 上…
- **2026-07-31 · Code Mode 语言分发与 Python SDK 渲染器**：Code Mode 只生成一种 SDK 形态：TypeScript。`ToolRuntime` 为 `tools:sdk` 段硬编码了 `renderToolsSdk`，且 `requireCodeRuntime` 会拒绝任何 `ctx.codeRuntime.language !== 'typescript'`。引入 CPython 后端后…
- **2026-07-31 · GUI Full access 风险确认**：在 Web 客户端的权限选择器中切换到 `danger-full-access` 只需一次点击，且预设以 Title Case 机器名 `Danger Full Access` 展示。Full access 会减少确认步骤，允许 agent（智能体）执行敏感操作、修改文件或运行外部命令，误点即在毫无刻意确认环节的情况下启用了最危险的预设。
- **2026-07-31 · dsh web 组合默认挂载会话遥测（OTel 上报）**：遥测 seam 与 OTel 后端（revival Note）自完成以来从未接入任何部署组合：没有 roster 行、没有开关、没有节奏口径，内部部署对用户会话的可观测性为零。需要一个部署决策：哪些 surface 上报、报到哪、什么节奏、怎么关、CI 怎么隔离。
- **2026-07-31 · 从 web UI 打开产出的文件**：范围：完成的轮次以其产出文件收尾的那一行、读得出是链接的文件路径链接，以及 Host 打开器对浏览器可渲染文档优先选用默认浏览器。经决定不在范围内：以 HTTP 提供工作区文件，以及为不在 Host 机器上的客户端提供预览。
- **2026-07-31 · 会话归档（注册表级全局集合）**：Sidebar workspace 浏览区的会话行菜单里，「Delete session」一直是纯视觉占位（无 handler）。产品口径定为**归档**而非删除：会话日志与 workspace 记账都不动，只把该会话从所有分组视图（workspace 分组、Ungrouped、搜索、平铺列表）里隐藏。…
- **2026-07-31 · 全新浏览器打开的设置语言由浏览器决定**：**暂定 locale 先经浏览器、再经 `FALLBACK_LOCALE`（`en`）解析；显式 Host 偏好会实时替换它。** `packages/client/locale/src/client/index.ts` 中的 `resolveInitialLocale()` 在服务构造时运行，并表达浏览器／回落顺序。随后…
- **2026-07-31 · 已交付界面的 workspace-write 默认值**：已交付的终端和浏览器界面在两套不同的无约束组合下暴露相同的编码工具。Web 挂载了沙箱与权限服务，却选择 `danger-full-access`；TUI 则直接挂载不受限的本地 bash 与文件系统提供方。因此，在用户主动选择这类权限之前，全新的编码会话就能修改其同 UID 进程可达的任意路径。
- **2026-07-31 · 已交付组合中的默认 Web 搜索**：该 harness 已具备完整的 Web 能力体系：提供方注册表、DeepSeek、Exa 和 Perplexity 搜索提供方、本地抓取、稳定的面向模型工具，以及结构化结果呈现，但已交付的 `dsh web` 组合没有挂载其中任何一项。除非部署提供自定义覆盖层，否则模型无法发现最新信息。…
- **2026-07-31 · 拉平交付的工具清单**：两个交付的 `dsh` surface 提供着不同的工具，而没有任何记录说明为什么。会话检查点、工具结果裁剪、goal 工具和 Ralph 在 `tui.cordis.yml`；`tool-todo` 以及后来的 web 搜索在 `web.cordis.yml`。两个 surface 都没有会话搜索、字符串替换编辑器和重复工具守卫，尽管这三者都已成包存在…
- **2026-07-31 · 新会话的权限 Settings 默认值**：Web「通用」设置页将「权限」显示为禁用的骨架控件，尽管 `dsh-permission-presets` 已经拥有 preset 表和当前会话的切换路径。Settings seam 可以持久化由插件拥有的值，但 Web Settings API 只暴露可配置 LLM（大语言模型）提供方的 namespace。更重要的是…
- **2026-07-31 · 第三方记忆 MCP 示例**：直接集成某个提供方会使该提供方的 API、配置、健康状态行为和工具语义成为 DSH 的一部分。对于已经可以通过 MCP 表达的功能，这会让产品接口过于庞大，而且每接入一个记忆系统都需要重复同样的适配工作。用户需要的是一种精简、可检查的方式，在保留通用 MCP 边界的同时，选择启用一个外部记忆服务器。
- **2026-07-31 · 遥测匿名用户 id（$DSH_HOME/.anonymous-user-id）与 OTel Resource 的 user.id**：session telemetry 已默认挂载（默认挂载 Note），但 OTel Resource 只有 `service.name`/`service.version`，没有任何用户级标识——接收端无法按用户聚合、无法数活跃用户。此前唯一相关口径是一条未实现的「hostname/本机 IP 哈希派生 user.id」裁定。…
- **2026-08-01 · Goal 命令输入投影**：面向用户的命令在模型轮次之外执行，并持久化为 `command/run` 与 `command/done`。Web transcript（文本记录）此前只渲染结果行。因此，在新会话中，`/goal` 会清空编辑器并成功完成，但页面仍停留在空白 Hero；只有后续对话内容激活 Chat 后，结果才会显示。若处理器追加普通 `user/message`…
- **2026-08-01 · PowerShell 执行器与 pwsh 工具**：harness 在每个平台只说一种 shell 方言：`bash`。Windows 主机只能通过 WSL 或 Git-Bash 垫片运行它，而交付的 `dsh-bash-local` 执行器仅限 POSIX（硬编码 `bash`，进程组语义是 POSIX 的）。Windows 路线图——让主机默认 `pwsh`…
- **2026-08-01 · Windows 默认改用 pwsh**：harness 交付的执行画像在每个平台都是 bash 优先。Windows 主机必须安装 bash 垫片（WSL 或 Git-Bash），或退回到仅 POSIX 的 `dsh-bash-local` 行为（硬编码 `bash -c` argv、进程组语义）；面向模型的 bash 工具教的是 bash 方言。…
- **2026-08-02 · Agent Note：Web 思考尾部滚动 —— 折叠态 reasoning 跟随实时输出**：Web Think 行在结算与流式 block 中都把 reasoning 首行渲染成折叠摘要。首行一旦出现，之后每个 reasoning delta 只会改变隐藏的正文。于是快速模型在思考时看起来静止，用户必须展开完整思维链才能确认输出仍在推进。产品事项表已经要求“thinking：滚动展示思维链更新、可展开”；当前行只满足了后半项。
- **2026-08-02 · Win32 文件夹选择器迁至 koffi 子进程**：Windows 目录选择器的主层此前是围绕 WinForms `FolderBrowserDialog` spawn 出的 PowerShell 脚本：只有恰好安装了 PowerShell 7 的机器才有现代对话框；一处回归——PowerShell 6 可解析却没有 WinForms（退出码 1 而非 `ENOENT`，5.1 回退永远不会触发）…
- **2026-08-02 · pwsh 工具与 bash 对齐**：首个 Windows 原生基础交付的 `dsh-tool-pwsh` 是刻意最小的画像——仅前台（每次调用都启动新进程；无持久 PTY 会话）、受管环境只有三个硬编码 `DSH_*` 键、以及一个未声明就偏离 bash 工具的 marker 故事（「恒打 `[exit code: N]`」）。…
- **2026-08-02 · 会话搜索工具不是交付默认项**：交付清单决策把 `tool-session-query` 设为共享 `cordis.patch.yml` 的默认行，于是交付的 TUI 与 Web surface 把这五个会话搜索工具（`session_search`、`session_event_search`、`session_trace`、`session_event_trace`、`session_e…
- **2026-08-03 · Web search 来源卡片改为滚动而非折叠**：`web_search` 结果卡片（`WebBlock`，`packages/client/ui-primitives/src/WebBlock.tsx`）此前用首尾折叠渲染它的来源列表：超过 `maxSources` 数量（详情面板为 16，聊天行经由 `CHAT_WEB_MAX_SOURCES` 为 8）时…
- **2026-08-03 · Web 轮次运行时长与悬停显示的时间附属元素**：Web 聊天界面会显示消息的到达时间，却不显示 agent（智能体）处理这条消息花了多久。长轮次除静态活动标签外没有任何实时进度信号，轮次结束后也无法从 UI 中还原实际耗时。与此同时，始终可见的时钟行给每条消息都增加了视觉噪音。
- **2026-08-03 · 受防护变更错误在模型边界追加恢复指令**：受防护的 `write` 与 `edit` 失败以只陈述条件、不给出唯一正确恢复方式的消息到达模型：`FS_STALE_VERSION`（「file changed since it was read」）与 `FS_NOT_OBSERVED`（「edit requires reading … first」）。…
- **2026-08-04 · Claude Code 与 Codex subagent 后端**：命名的 `ctx.subagents` 注册表让父 agent（智能体）无需了解子 agent 的运行方式即可委派工作，但 harness 需要通往真实 Codex 与 Claude Code 产品的第一方路径。每条路径都必须向产品交付一项自包含任务，让它在父会话的工作区中执行，返回最终回答或明确的失败或取消结果，并且不留下任何受管的产品进程。
- **2026-08-04 · Web transcript 标出上下文来源、召回与 steering**：生产方向模型侧对话补充的一切内容，进入 Web transcript（文本记录）后只剩两种匿名形态。每一条已记录的非用户 `user/message`——skill（技能）目录、运行时快照、经过对账的 `AGENTS.md` 指令、guard 提示、subagent 汇报、跨会话快照——都塌缩成同一行 `上下文注入`…
- **2026-08-04 · Web 斜杠命令模糊发现**：Web 命令菜单要求按命令名前缀匹配，因此用户只记得关键字母却不记得其准确位置时，就无法发现命令。扩大菜单的匹配范围可使命令更易发现，但命令执行仍必须保持精确匹配和确定性：近似输入行绝不能执行相近命令。
- **2026-08-04 · Web 轮次与窗口级延迟/吞吐指标**：Web 聊天已经记录了逐步骤的 LLM（大语言模型）计时（`stepStartTime`／`firstTokenTime`／`completedTime`）和逐步骤 usage，trajectory 视图也按步骤展示它们，但聊天界面既回答不了「这一轮响应有多快」，也回答不了「这个会话跑得有多快」：assistant 页脚只显示轮次实际耗时…
- **2026-08-04 · Web 输入区共享宽度轴与控制行打磨**：Web 会话列的各个区域各自独立设定尺寸：transcript（文本记录）列、输入卡片、todo/goal/queue 停靠卡片、ask-question/approval/plan-review 接管卡片各自硬编码 max-width（736/752/776/800px 等变体）与各自的侧边内边距。这些区域在全宽下彼此漂移几个像素…
- **2026-08-04 · 侧边栏的滚动条跟随指针**：侧边栏的会话列表只要有几个会话就会溢出，从那一刻起它的滚动条就一直画在那里——所处的这一列大部分时间都是静止的，而列表行自己的操作按钮只在悬停时才出现。它是侧边栏里唯一始终常驻的构件，而在有人真的伸手去操作它之前，它不提供任何可操作性。产品诉求（2026-08-04）是只在指针位于侧边栏内时才绘制它，并留一小段拖尾，避免指针路过时它一闪而灭。
- **2026-08-05 · Durable Agent Teams over continuable children**：每个普通运行时 Root 都是一个隐式 Team 的 Lead，Team id 等于该 Root 的 `SessionId`。Team 没有 creation event：Lead pseudo-row 由身份直接存在，持久状态从第一条 member、message 或 task event 开始。roster 是扁平结构…
- **2026-08-05 · Web 预览版产品徽标**：Web 空状态没有标明产品处于预览版阶段。用户可以在未看到产品尚未正式发布的情况下进入主会话界面；若改用部署设置，则会把面向整个产品的生命周期决策误表述为操作者的选择。
- **2026-08-05 · composer 上下文占用圆环与启发式组成明细**：Web 聊天的统计行把上下文占用率作为一个行内数字（`Context N% of X`）挤在计费分组之间。它回答了「有多满」，却回答不了「被什么占满」：没有任何地方展示窗口在系统提示词、工具 schema 与对话之间如何分配，而单行统计行也容纳不下这种明细。可用的数字还分属两套口径——来自 `contextPressure` 的提供方精确计费的提示词规模…
- **2026-08-05 · pwsh UI presentation matches bash**：`dsh-tool-pwsh` 的 `presentResult` 现在逐调用镜像 `dsh-tool-bash`：完成的前台结果是 `terminal` 卡，输出正文为去 marker 的渲染文本，退出状态 pill 为解析出的 `exitCode`/`signal`；后台 ack 与 `isError` 结果保持通用 `console` 围栏卡片…
- **2026-08-05 · 反馈门控的会话遥测**：会话遥测原本只有一种已挂载行为：每条已接受记录都立即进入上报后端。部署方需要两种更严格的策略，且不替换插件：只有用户记录反馈时才释放该会话的遥测，或禁用上报并仍向用户说明反馈的去向。该策略必须保留遥测 seam 在记录抵达后端之前脱敏的边界。
- **2026-08-05 · 持久、仅限 Session 内的提醒**：在对话中创建的提醒必须始终归属于确切的那个 Session，并且跨进程重启存活。进程本地 timer 或 inbox 项无法提供这种持久性，而全局 scheduler 或私有数据库又会引入第二套身份、持久化和生命周期系统。
- **2026-08-05 · 按 agent 的工具呈现方式，以及 `code` 预设**：agent preset 已经能按会话组装一个 agent 的工具，却管不了这些工具以何种**形态**抵达模型。Code Mode——一个 `run_code` 工具加一份生成的 TypeScript SDK，用一段程序替代一串调用——此前是宿主 `dsh-tools` 那一行上的部署级 `mode` 字段。一个部署要么所有会话都跑 Code Mode…
- **2026-08-05 · 由生产方声明的上下文形态**：每一条已记录的非用户 `user/message` 都通过同一个内容区渲染：把整条消息序列化成内联 JSON。读者展开一行，看到的是 `{ "content": [ { "type": "text", "text": "…\n\n…" } ], "source": { … } }`——转义把唯一值得读的东西（面向模型的散文）压成了一行…
- **2026-08-06 · Continuable subagent 当前轮次中断**：一个正在运行的 continuable subagent 无法在不销毁它的前提下被停止。继续执行管理器只在整个 Activation 拆除（结算、drain、scoped drain）内部取消子 Agent，`send_message`／`subagent.prompt` 只能增加工作，而 Web composer 的 Stop 按钮被刻意限制在普通会话。…
- **2026-08-06 · MCP client auto-reconnect with bounded backoff**：MCP 客户端在插件加载时仅连接一次。stdio 服务器崩溃或被终止后，其已注册的工具仍然可见，但每次调用均以 `Not connected` 失败，直到人工编辑配置触发 HMR（热模块替换）重载，或重启 Host——v1 明确推迟了重连机制。长时间运行的 Host（ACP 自动化、Web）不能因为子进程死亡就被重启；而对于 stdio 传输…
- **2026-08-06 · Settlement delivery belongs to the continuation manager**：可继续后台委派是模型唯一一种能够发起、却无法抵达终点的异步操作。其他每一种形态都有取回原语或返回值：后台 bash 命令与一次性后台 subagent 都通过 Task 结算，`job_output(wait: true)` 可以阻塞等待；workflow 与前台 subagent 会把结果返回给调用方。可继续后台 child 只返回它持久化的 id…
- **2026-08-06 · Web skill 工具行**：Web transcript（文本记录）通过通用后备行渲染 `skill` 调用，使已加载的指令集看起来像一次未知工具调用，尽管 Skill（技能）已是产品中的一等概念。通用行还会在结果旁暴露 JSON 参数的外层结构，围绕用户真正需要的唯一标识增加了噪声：已加载的 skill 名称。
- **2026-08-06 · Web 安装 manifest 元数据**：Web 构建产物已有文档标题和 favicon，却没有可供浏览器发现稳定安装身份、启动边界或安装后呈现方式的 manifest（元数据清单）。添加这类元数据也可能暗示应用并不具备的能力：service worker 会让人以为应用提供离线约定，而单一语言或调色板取值会错误描述这个能够解析浅色与深色主题的双语 UI。
- **2026-08-06 · 侧边栏会话完成提醒点**：`SessionManager` 持有客户端侧的完成提醒集合，与待交互位并列：非当前会话发生 running→idle 边沿时点亮其提醒；`select()`/`selectSubagent()` 消费掉提醒；重新开始一轮运行会熄灭提醒并在再次完成时重新点亮；会话被移除时清理提醒。…
- **2026-08-06 · 内置 dsh 徽章 skill**：Cordis 教程的各个页面都使用官方「powered by dsh」徽章，但交付的 CLI（命令行界面）既没有用于在其他位置应用同样署名的可复用指令，也没有可显式选择加入的提供方。
- **2026-08-06 · 可继续 child 的返回通道是一项义务**：可继续后台 child 拥有自己的 Session，因此它写在那里的任何内容都不会到达启动它的 agent。report 工具为该 child 提供了一条返回通道，却把它呈现为若干选项之一：schema 里写着「可调用零次或多次」，child 的提示词中没有任何地方要求它调用该工具…
- **2026-08-06 · 基于解析后主题的颜色元数据**：Web 客户端可以独立于操作系统偏好解析主题，因此 manifest（元数据清单）中单一的 `theme_color` 值或带媒体条件的静态元数据可能与显式选择的 Light 或 Dark 不一致。此时，无论是已安装页面还是普通页面，其周围的浏览器界面都未必与应用界面一致，尽管布局呈现器已经拥有解析后的 document 调色板。
- **2026-08-06 · 空输入时 Cmd/Ctrl+Enter 将 Web 排队消息全部插话**：主会话运行时，用户用普通 Enter（或在 busy-Enter 偏好为 Queue 时）输入的消息会在 Web 队列里累积。把它们灌进当前轮次需要逐条点击「插话发送」按钮；而输入框草稿为空时没有任何键盘手势——输入机对空草稿直接拒绝，Enter 与 Cmd/Ctrl+Enter 都是空操作。排队消息一多，逐条插话是明显的多点摩擦…
- **2026-08-07 · 反馈确认中的会话共享披露**：`/feedback` 命令会记录一个仅写入日志的 `feedback/record` 事件并确认用户，但确认文本没有携带关于会话去向的持久信息：挂载了会话遥测（`FULL`、`FEEDBACK_ONLY` 或 `DISABLED`）的部署无法告知用户其反馈和会话是否离开了进程，确认文本也没有回显接收会话的 id。命令插件无法读取共享策略…
- **2026-08-07 · 未选择 Workspace 时从编辑器打开现有选择器**：Session scope 决策会在 Workspace 存在前保留同一个常驻编辑器，但 textarea 处于禁用状态，只有较小的 Workspace chip 能打开选择器。用户首次点击最显眼、也最熟悉的输入区域时，界面不会响应，尽管同一界面已有继续操作的入口。
- **2026-08-07 · 行内代码文件提及可打开其命名的文件**：范围：引导最终回复以行内代码点名主要输出文件，再把这些 token 链接到本轮变更的文件。不在范围内：识别普通正文中的路径、链接成功修改位置中不存在的文件，以及流式或轮次中途消息里的提及。
- **2026-08-07 · 默认模型跟随选择器**：会话模型选择器与部署默认值是同一项偏好的两个层次。如果选择器只影响其所在会话，下一个空白会话可能选择不同模型，用户却没有途径使默认值与选择器一致。如果默认值位于 Host 网关内部，直接创建 Agent 的入口只有依赖 Host 或复制状态才能共享它。
- **2026-08-08 · Web 后台任务展示**：`ctx.jobs` 已经承载了 harness 在后台启动的全部长时工作——`bash`、`pwsh`、`pty-send`，以及一次性后台 subagent——但它唯一的读者是模型。`dsh-tool-jobs` 暴露了 `job_list`、`job_output` 和 `job_kill`，除此之外没有任何东西观察这个注册表。
- **2026-08-08 · Windows sandbox rung: raw ACL restricted tokens over mxc and AppContainer**：最初的沙箱决策将 `PLATFORM_CHAINS.win32` 留空，因此交付的 Windows profile 因不存在隔离执行器而退化为 danger-full-access。win32 档必须约束沙箱词汇表中的两种文件效果模式——`read-only`（不显式授予任何可写根目录）与 `workspace-write`（允许写入工作区根目录及后端定义的临…
- **2026-08-08 · llm-pi-ai 的按模型推理声明**：在声明式提供方 catalog（[[2026-08-03-pi-ai-declared-provider-catalog]]，它刻意把推理排除在可配置字段之外）之下，手工声明的 pi-ai 路由，其模型物化出来就带着 `reasoning: false`…
- **2026-08-08 · pre-step 手势边界上的用户显式 skill 调用**：`disable-model-invocation: true` 的 skill（技能）在设计上就是仅限用户的：它绝不进入面向模型的目录，`skill` 工具也拒绝加载它。它唯一正当的入口是一次显式的用户手势——而 web 客户端此前没有这个入口。`skill.list` 过滤到模型与用户的交集（把仅限用户的 skill 挡在菜单之外）…
- **2026-08-09 · 并行 subagent 委派**：想要扇出的模型会把多个 `subagent` 调用合并进同一条 assistant 消息：这个批次本身就是并行意图。委派工具此前没有声明 `isConcurrencySafe` 分类器，按安全侧原则设计的调度器（并行工具调用 Agent Note）便把每个前台委派都当作独占屏障：GUI 里显示九张卡片，却只有一个子 agent（智能体）在运行…
- **2026-08-10 · Chat 中的持久工作流运行**：普通工作流工具行拥有模型调用与最终工具结果，但这两条记录无法说明哪些成员真正开始、如何分组、各成员是完成、失败还是取消，也无法说明进程停止时哪些工作尚未结束。实时 `workflow/*` 事件只存在于当前进程，因此刷新或稍后重新打开 Session 会丢失运行历史。
- **2026-08-10 · Plugin configuration in the web settings page**：三个分节、分层解析与暂存保存表单依然有效。Host 白名单与无键卡片列表已被由插件自己拥有的设置表层取代：每一个已注册的命名空间都被服务，卡片以它所编辑的命名空间为键。
- **2026-08-10 · Web 会话日志导出——宿主流式 ZIP 下载**：状态：implemented
- **2026-08-10 · 创造模式引导以介绍动效落在预设 chip 上**：预设的创作发生在创造模式 session 内部，但设置分区没有把这条路径讲清楚。创建入口游离在名册分组之外；自定义分组在没有成员时整个消失；点击入口后用户被抛到新会话屏幕，没有任何标记说明发生了什么变化：暂存的预设 chip 渲染得和用户亲手挑选时一模一样。用户反馈看不懂流程已经移动，也不明白即将开始的 session 正是构建预设的地方（#2184）。
- **2026-08-10 · 可继续 subagent 策略继承——持久化子日志拥有委派时快照**：自进程内策略继承决策以来，一次性进程内驱动器一直会把父级的沙箱／审批覆盖项注入其子级，但可继续路径从未这样做：`SubagentContinuationManager` 的物化只应用子级组合与 Activation（激活）设置注册表。默认组合包把两个委派工具都配置为 `backgroundMode: continuable`，因此在默认部署中…
- **2026-08-10 · 基于既有 seam 的最小 read_image 工具**：多模态附件工作为用户上传建立了完整的持久路径，但模型无法查看磁盘图片。`read` 按约定拒绝二进制内容，因此被问到截图或渲染图表的 agent 要么失败，要么使用有损的变通方法。PR #598 的独立尝试把工具与循环级路由作用域、按路由控制 schema 可见性和新的会话日志概念放在一起。这些能力不是发布一条带图片且已记录的工具结果所必需的。
- **2026-08-10 · 被委派的 subagent 以钉定为 `'never'` 的审批策略运行**：被委派的子 agent 发起审批请求时无人可问。在交互式父级（`'ask'`）之下，后台子 agent 的升级请求会变成一个任何产品界面都不展示的挂起问题——subagent 会话不进入 Web 侧边栏，父级的 `list_agents` 只报告普通的 `running`／`idle`…
- **2026-08-10 · 遥测必须显式启用**：DeepSeek Harness 有两路出站遥测数据流。在内测阶段，共享基础配置挂载了带内建生产 endpoint 的遥测，两路数据流默认上报以帮助诊断上报的问题：会话 OTel 后端在省略 `mode` 时可能导出完整会话内容、工具数据、提示词和工作区路径，而 dsh-sdk 启动器数据流则无条件外发。因此，全新安装无需部署方明确选择便允许向外上报。
- **2026-08-11 · Agent Note：消息反馈的 Web 界面**：PR #2217 交付了持久化的消息反馈 sidecar 及其三个 Host Remote 方法，但它明确只做后端：没有任何客户端包消费 `messageFeedback.list`、`put` 或 `delete`，因此 Web GUI 无法记录评价。…
- **2026-08-11 · Background job completion wakes an idle owner**：`tool-jobs` 对模型承诺「任务完成时你会在会话内收到通知——不要忙轮询，也不要 sleep 等待」。这个承诺只在模型仍在工作时成立。完成经由 `agent.inject()` 交付，它只向 next-step inbox 追加而不预留 driver，因此在轮次结束之后才结算的任务会把通知搁在那里，直到某件无关的事情唤醒 agent。…
- **2026-08-11 · DeepSeek 请求用户与会话身份头部**：当调用方提供 `GenerateOptions.sessionId` 时，直连 DeepSeek 请求已携带 `x-deepseek-harness-session-id`，让提供方侧支持与诊断可以关联同一对话中的多个轮次。但请求缺少跨会话的稳定身份，而 harness 已为遥测与反馈持久化匿名用户 id。另行生成 id 会破坏关联…
- **2026-08-11 · Web `/export` 共用流式 Session ZIP 下载**：`@deepseek-ai/dsh-session-log-export` 注册 Web 专用的 `/export` 用户命令，并提供浏览器 `ctx.sessionLogDownload` 控制器。该命令记录普通的 `command/run` 和 `command/done`；`command.execute` 返回成功结果后…
- **2026-08-11 · Web 附件展示经附件原子组件对齐 DeepSeek Chat**：Web 输入框的图片界面缺乏基本可用性（用户反馈，issue #2248）。删除按钮以 `top/right: -6px` 挂在 72px 缩略图外侧，被附件栏的 `overflow-x` 盒子裁切，点击经常落空；预览只能双击打开，除了 tooltip 没有任何提示这个操作；附件栏超出输入框宽度时在胶囊内部直接出现原生横向滚动条…
- **2026-08-11 · Workspace 侧边栏顺序与折叠**：Session 很多的 Workspace 会占满整个侧边栏，把其他 Workspace 挤出可见范围。紧凑列表需要有界的默认高度，同时仍要提供到达每条 Session 的明确入口。侧边栏还需要面向活动时间的顺序，但 `WorkspaceView.sessionIds` 是持久的手动记账，不能被 Session 活动改写。
- **2026-08-11 · minimal profile 使用裸双工具运行时**：Web `minimal` preset 与独立 JSON-RPC minimal 组合对外提供持久 `bash` 和 `str_replace_editor`，但支撑服务与目标训练运行时不一致。两者都挂载上下文压缩，而 Web preset 继承宿主的沙箱文件系统，JSON-RPC 组合则挂载 `fs-sandbox` 和文件系统策略。因此…
- **2026-08-11 · 可收起的提问卡片**：在提问卡片头部（现有的"放弃整组问题"按钮旁）增加收起/展开切换按钮。收起时隐藏选项主体和底部操作区，只保留一条头部（eyebrow、标题、两个图标按钮），用户仍能看到"有未答问题"的信号；展开后恢复完整卡片。
- **2026-08-11 · 可继续委派采用后台优先**：可继续 child 已经具备持久化 id、独立轮次、后续消息以及由管理器负责的结算通知。如果把省略的 `run_in_background` 视为前台，模型就必须在每次调用时重复写出 `true`，才能得到这套生命周期。这样也会掩盖真正有用的调度判断：只有当 parent 的下一步动作需要 child 结果时，parent 才应等待。
- **2026-08-11 · 工作流运行的状态驱动 disclosure**：持久工作流 Chat 节点会在同一位置从运行前缀更新为终态记录。renderer 必须提示新工作、异常结果和正常完成，同时不能在普通更新中反复覆盖用户回收对话空间的选择。
- **2026-08-12 · Agent Note：整页图片拖放、上限投影预检与缩略图平铺**：状态：implemented
- **2026-08-12 · `dsh web` 打开已就绪页面**：Web 应用的命令提供方为普通调用解析出 `openBrowser: true`，为 `--no-open` 解析出 `false`。组合包把该值传给自己的 `web-runtime` 行；部署仍可显式替换该行的完整配置。运行时在激活期间对继承的 `SSH_CONNECTION` 与 `SSH_TTY` 采样一次，只要其中一项非空就会跳过浏览器交接…
- **2026-08-12 · 产品 one-shot subagent 使用通用后台 Job**：Codex 与 Claude Code 提供方已经能够运行一项自包含任务并返回一个最终回答，而 `dsh-tool-subagent` 也已经能够把任意 one-shot 提供方接入通用后台 Job 运行时。随附产品工具行禁用了这条路径，因此即使委托与 agent 的下一步操作彼此独立，agent 也只能等待产品回答。
- **2026-08-13 · 共用弹窗的产品引导**：首次使用引导混用了两种交互：产品背景说明占满整个视口，凭据提示则先把用户带进「设置」，之后才能输入密钥。一个很短的有序流程因此像两个互不相关的界面，引导 UI 的归属也分散在多个包中。产品仍需要在提供方配置之前显示版本化的测试阶段声明，但恢复它不能增加第二个独立浮层，也不能改变 Host 的设置与凭据边界。
- **2026-08-15 · 产品 subagent 使用 Profile 选择的非交互权限**：每个产品提供方分别拥有自己的 Profile 级 `permissionMode` 值。两个 Config 字段有意使用各产品的原生名称，而不是共享的受限／自动／完全抽象。提供方会为该插件实例的每次运行固定已解析值。subagent 工具 schema 与 `SubagentStartRequest` 都不包含权限字段，因此模型或单次委派无法改变它。
- **2026-08-17 · Command image-attachment envelope**：提交信封被端到端建模，每条命令路径要么整体消费它，要么响亮拒绝。
- **2026-08-17 · web_search 支持一次传入多个查询**：面向模型的 `web_search` 工具原来只接受单个 `query`。在同时把内部搜索后端以 MCP 方式暴露的部署中，模型更倾向于使用 MCP 搜索工具，因为它能一次传入多个关键词；模型也常常在调用原生 `web_search` 后觉得结果不够，再补一次 MCP 搜索。
- **2026-08-18 · Web UI abbreviates POSIX home paths as `~`**：`host.describe` 把宿主账户的 `home` 作为必填字段上报。Client 与 Host 一同发布，因此该字段是必填而不是可选。ApiProxy 在 describe 时用 `homedir()` 填入。
- **2026-08-18 · pi-ai Wire-Compatibility Surface in llm-pi-ai**：每个 pi-ai compat 类型一张漂移门禁——以 `Record` 为键——把每一个上游字段分类为 `offer` 或 `withhold`。去重后三十个字段，开放二十个。分界线在于私有 URL 能推出什么：凡是无法从未识别端点推断的，部署方必须能够说出口；而 pi-ai 已安装 catalog 为具名厂商设定的字段保持扣留…
- **2026-08-18 · 产品 subagent 公开有界结构化失败事实**：每个产品提供方分别拥有从锁定版本官方错误联合、当前操作和受管进程结果到一行固定安全诊断的映射。`SubagentResult` 保持不变：消费方仍接收现有的有界 `diagnostic` 字符串，而且不解析其中由产品私有的字段。
- **2026-08-18 · 产品 subagent 命名实例**：Profile 可以用多个配置项挂载同一个 Cordis 插件包，但 Codex 与 Claude Code 产品提供方此前会把每个配置项都注册到一个固定产品名称下。因此，第二个配置项会在其独立权限模式、环境或进程释放设置可用前因名称重复而失败。根据这些设置隐式派生名称会建立第二套身份规则，而在工具调用期间选择提供方会让模型输入决定部署权限。
- **2026-08-19 · Agent Note：Web markdown 表格按列数填充消息列，宽表突破列宽**：`MarkdownText` 把每个 GFM 表格都按自然宽度渲染（`.tableScroll table { width: max-content; max-width: max-content }`，`packages/client/ui-primitives/src/markdown/MarkdownText.module.css`）…
- **2026-08-19 · 高缓存命中率的小数显示**：Web 会话统计行会把所有非空缓存命中率舍入为整数。真实比率超过 99% 后，显示会隐藏后续提升；比率达到 99.5% 时，即使仍有未缓存输入或缓存写入，也会显示为 100%。
- **2026-08-20 · Multi-line answers in the question composer**：两种问题形状都写入同一个 `AnswerField`：一个 ``，与一个渲染「草稿 + 结尾换行」的隐藏镜像 `` 共享同一个 CSS grid 单元格。
- **2026-08-20 · 统一规范化附件、请求版本与提供方文件**：图片路径有两个显式版本。附件后端拥有提供方无关的持久规范化附件。每条支持图片的模型路由拥有确定性请求策略，附件后端从该附件派生并缓存确切请求版本。会话历史只包含规范化附件引用；内联字节和提供方文件 ID 都是瞬时请求投影。
### 25.3 已实现 · 缺陷修复（94 条）

- **2026-07-19 · Windows 原子文件替换期间保留 DACL**：原子写入在 POSIX 上以 `0o700` 保护暂存目录、以 `0o600` 保护临时文件，但 Windows mode 位只呈现实际 DACL 的合成只读视图。在目标文件的父目录下创建暂存目录和临时文件，并且只依赖继承的 DACL，足以满足新建文件的需要…
- **2026-07-20 · 在变更前绑定 JSONL 会话身份**：JSONL 查找会根据请求的会话 id 在各个项目目录中选出物理日志，而解析得到的 `SessionHeader` 会提供后续修复和追加操作使用的元数据。如果这两个事实没有绑定，为会话 A 选中的日志就能声明会话 B 的 id 或 cwd，并将修复或后续追加重定向到 B 的路径。当同一个编码后 id 出现在多个项目目录中时，项目扫描也必须给出确定的结果。…
- **2026-07-20 · 在每个诊断边界渲染错误 cause 链**：TUI 连接不可达的 DeepSeek 端点时，失败只显示一条 `fetch failed` 通知，没有任何进一步细节。两个独立缺口共同造成了这个死胡同：
- **2026-07-20 · 配置热重载不得杀死或降级正在运行的应用**：vendor 中的 Cordis 生命周期和 Loader 插件提供可等待、带补偿的配置事务，并在 vendor/README.md 中记录为本地修改第 6、8、9 条。
- **2026-07-21 · 摘要调用回放对话前缀以复用 KV Cache**：自动压缩（compaction）在对话中途触发，恰好在循环用最后一个已路由请求（`system` + `tools` + 派生历史）预热了提供方的 KV Cache 之后。随后默认摘要器发出一个*独立的*辅助请求，其前缀与那个已预热请求没有任何共享部分：一个专门的摘要器 `system` 提示词…
- **2026-07-21 · 语义会话检查点**：持久化机制会缓冲所有同步 `session/event`，直到 agent loop（智能体循环）执行最后的轮次检查点才写入。一个轮次是正确的对话事务，但作为唯一的崩溃恢复点过于粗粒度：如果在耗时的模型请求或工具调用期间发生硬崩溃，整个进行中的轮次都可能丢失，其中包括识别已尝试操作所需的请求封套。系统还会使用同一种不作区分的中断错误，修复没有结果的工具调用…
- **2026-07-22 · 从扁平化的消息文本中分类 pi-ai 传输层截断**：一次 TUI 运行的模型连接在流式输出中途断开，只浮现出一条 `terminated` 通知，而一个被截断的 Anthropic 响应则浮现出 `Anthropic stream ended before message_stop`。…
- **2026-07-24 · Python SDK 递归会话通知**：Python SDK 过去通过将每条通知的 payload 与根会话 ID 直接比较来过滤轮次通知。直接子 agent 的生命周期通知因 parent ID 指向根会话而能够通过，但孙级生命周期通知与所有后代 `session.event` 都会被拒绝。JSON-RPC 服务器仍会发出这些通知，因此它们会堆积在底层全局队列中…
- **2026-07-24 · 空模型补全是可重试的 EMPTY_RESPONSE 失败**：提供方偶尔会返回一种退化的 completion：流本身格式完好，以终止性的 `stop` 结束，却没有任何内容块——没有文本、没有推理（reasoning）、没有工具调用。如果适配器把这种形态映射为成功的 `{kind: 'stop'}` 结束，主循环就会记录一条空的 `assistant/message`，并把该轮次以 `completed` 结束。…
- **2026-07-27 · 稳定快照刷新中的易变值**：ACP（Agent Client Protocol）快照比较会归一化生成的 UUID、cwd 别名、spill locator、嵌入的事件时间和省略字节数，但刷新写回会持久化本次生成的原始值。因此，即使比较约定将两份日志视为相等，一次行为未发生变化的刷新仍会用新的随机值或宿主特有的路径写法改写 fixture（测试前置数据）。
- **2026-07-27 · 跨目录树采样超出上限的 glob 结果**：用户询问工作区包含什么内容时，一个 agent（智能体）把某个子文件夹描述成了整个项目。该工作区有 22 个顶层条目和 11,485 个文件。`glob {"pattern":"*"}` 匹配到 10,030 条路径，但内联显示的 100 条路径全部位于一棵近期解压的子树中，因此模型完全没有看到其余 21 个条目。
- **2026-07-28 · Web GUI 改动在现有 URL 上闭环**：Web agent（智能体）既无法识别承载当前会话的 GUI，也不知道用户正在查看哪个 URL。运行时上下文决策提供前一项事实，但 GUI 编辑仍然没有可执行的验收目标：源码编辑、产物构建、监听中的进程与用户已打开的页面只是互不关联的观察结果。仓库提供的入口让错误的替代方案显得合理…
- **2026-07-28 · Web agent 获得显式运行时上下文**：CLI（命令行界面）共享 base 配置了空的部署 persona，Web overlay 没有替换它，而 Web 启动器既未添加源码提示词段，也未添加交互界面提示词段。会话 header 会记录工作目录，供工具与持久化使用，但模型提示词既不说明该目录，也不标识 DeepSeek Harness Web GUI。因此…
- **2026-07-28 · 加载消息标识机制引入前持久化的会话**：带标识的不可变消息变更将四种持久化事件载荷替换为完整消息值。现有的 v0 JSONL 和 SQLite 会话仍保留紧邻该变更之前的形状：用户事件和 steering（中途引导）事件直接携带 `content`/`source`，assistant 事件携带 `content`/`provenance`…
- **2026-07-28 · 滚动条 token 有了消费方，工作区列表预留出滚动条空位**：`design-platform.css` 在亮色与暗色两套调色板中都声明了四个 `--dsw-alias-scrollbar-*` token（`bg-l1`、`bg-l2`、`hover-l1`、`hover-l2`），而客户端里没有任何一条规则读取它们。定义了却无人消费的 token 构不成主题：所有滚动区域渲染的都是浏览器自带的滚动条…
- **2026-07-29 · Web 图片准入的原子性**：包含图片的提示词准入与 `session.selectModel` 都会跨越异步模型查询和附件查询。没有统一的排序点时，包含图片的提示词可能在支持图片的目标上通过校验，并发选择却设置了纯文本目标。选择也可能在准入已经开始、持久消息事件尚未发布时改变路由。
- **2026-07-29 · Web 详情栏遵循当前会话生命周期**：详情入口由会话作用域拥有，而其首选网格宽度由根作用域拥有。选择不同会话时，系统会替换详情内容，却不会关闭根作用域的该首选宽度，因此新 owner 会继承陈旧的查看几何信息。hero 和其他未选中状态不会渲染会话作用域的详情；其轨道需派生为零宽度，但不能因此在比较中成为伪 owner。
- **2026-07-29 · 人类可读记录投影追加来源的事件**：终端与宿主历史网关都把模型可见的 surface 当作 transcript（文本记录）。一次成功的压缩（compaction）会用一个检查点节点替换一段 surface 范围，因此该替换一落地，终端就丢弃了它所遮蔽的每条消息——那些是用户已经读过的对话——并在此后任何替换到来时重新执行这次破坏性重建。…
- **2026-07-29 · 固定标题栏，sticky 编辑器位于 transcript（文本记录）滚动容器内**：活跃会话列把滚动拆成两段：聊天（以及 trajectory）视图自有 `overflow-y: auto`，编辑器栈则作为该滚动容器的兄弟节点坐在下方。指针落在统计行或输入区上时，滚轮打在不可滚动区域上因而毫无效果——只有指针在消息列表上时 transcript 才会移动。草稿变长时更糟：textarea 本身也是滚动容器，编辑器上的滚轮可能被截在那里。…
- **2026-07-29 · 按 GitHub Actions runner 隔离 pnpm 设置**：`pnpm/action-setup@v4` 的安装目标目录默认为 `~/setup-pnpm`，并会在设置期间替换该目录。自托管 CI 故障切换在同一个 VM 用户下运行六个 GitHub Actions runner 服务，因此并发作业会共用同一目标目录。在复现运行中，三个作业在 73 毫秒内进入 pnpm 设置…
- **2026-07-30 · Composer 上下文堆栈顺序**：Goal、Todo 与 Queue 独立注册到同一个 `conversation.input.dock` 列表，但各自的注册顺序与间距规则没有编码组合矩阵。因此，渲染器将 Todo 放在 Queue 和 Goal 之前，而 Queue 与 Goal 都带有用于 composer 边界的负外边距。三者同时出现时，Queue 与 Goal 相接…
- **2026-07-30 · 在提供方限制覆写上下文 diff 基础**：`dsh-fs-local` 会在 `FsWriteOutcome.before` 中返回完整旧文件，供消费方生成覆写上下文 diff。这个仅用于展示的预读没有上限：大文件覆写可能分配整个旧文件；而仅检查较早的路径 stat 也无法真正实施上限，因为外部进程可以在 stat 与读取之间替换文件或扩大文件。即使旧文件很小…
- **2026-07-30 · 多选题自定义答案组合**：用户交互结果的词汇分别通过不同字段携带选中的选项标签和可选的自定义文本，但最初的语义要求每个问题的这两个字段互斥。对于多选题，打开自定义答案或输入文本会丢弃用户已选中的标签。TUI 只返回自定义文本，而 Web 宿主会拒绝同时保留两个字段的客户端响应。
- **2026-07-30 · 审批接管面板与输入框共用同一文本高度上限**：审批面板是一次 composer 接管：当一次沙箱越权申请处于等待状态时，它在 composer 容器中取代 InputBar，展示模型给出的理由、与之配对的命令，以及一行拒绝／允许按钮。这两段文本都是长度不受限的模型输出，而卡片当时没有任何高度上限。命令一长——而这正是现实中的常见形态，因为越权申请针对的就是沙箱刚刚拒绝的那条命令…
- **2026-07-30 · 悬浮弹层的指针宽限期**：工作区浏览器行弹出的两种弹层都处于指针无法抵达的位置。`HoverCard` 在指针离开锚点的第一个 `pointerleave` 上就关闭，其卡片还设置了 `pointer-events: none`；但卡片位于锚点右边缘外 8px 处，因此通往卡片的每条路径都要穿过既不属于锚点也不属于卡片的区域…
- **2026-07-30 · 浏览器会话是按日志顺序投影的人类对话记录**：`TranscriptAdapter` 取代 `FoldAdapter`，并且从不查询 surface 顺序。它按日志顺序投影原始窗口：每个 append 来源的 surface 事件（`isAppendSurfaceEvent`）落在它自己的日志位置上，外加每次落地的压缩检查点一个 `CompactionSummaryNode` 标记。…
- **2026-07-30 · 源码 checkout 路径不定义工作目录**：`harness:source` 提示词段遵循源码位置决策，但原有措辞把 checkout 称为「你自己的源代码」，却没有区分该路径与会话 workspace。在 persona 不声明 `{{cwd}}` 的普通 TUI 配置中，这可能是系统提示词开头附近唯一固定的绝对路径。因此…
- **2026-07-31 · Web 停止操作保留待处理 Queue**：Web 停止按钮调用 `session.cancel`，后者映射到广义 `agent.cancel({ kind: 'user' })`。在活动轮次期间，普通 composer 提交已经被接纳为可独立寻址的 Queue 入队项。用户只想停止当前生成时，广义取消却会丢弃所有入队项，混淆了轮次中断与 Queue 的显式删除操作。
- **2026-07-31 · composer 的两层文本共用同一个滚动容器**：composer 的文本由两层叠放绘制（见 InputBar）：`` 持有值、选区与光标，但它自己的字形以 `color: transparent` 渲染；用户看到的每一个字符都由其下的 `[data-input-backdrop]` 层绘制，该层同时承载 claim token 高亮、chip 与提示影子文本。…
- **2026-07-31 · fail-loud 在退出前释放终端**：配置校验失败的 `dsh` 启动会打印诊断信息，然后把用户丢回一个损坏的 shell：输入不可见，下一条命令还会被残留文本弄乱：
- **2026-07-31 · fork 锚点向下取整到事件 seq**：在已停止的助手消息上点 fork 毫无反应——没有子会话，没有报错，也没有任何可见变化。
- **2026-07-31 · 压缩检查点使用英语工程文体**：压缩（compaction）检查点会成为下一次模型请求中持久存在的前缀。当多语言对话使压缩器以对话语言保留叙述性材料时，检查点可能引入大量代码、工具输出和既有推理（reasoning）前缀中均未出现的语言内容。该语言随后会在后续压缩周期中持续存在，并影响对话模型的推理文体。
- **2026-07-31 · 恢复选择器只折叠标题**：打开 TUI `/resume` 选择器时，会在一个无界 `Promise.all` 中对每个列出的会话调用一次 `sessionQuery.readSession()`。每次调用都会在 `SessionCorpus.load()` 内部重新列出整个持久化存储（O(N²) 次列表查询）、读取并解压完整日志、通过 `Session` 构造函数对每个事件做回放验证…
- **2026-07-31 · 接纳 basename 相同的 Workspace**：Workspace 的身份由其稳定 id 和规范目录路径确定，标题则是可变的显示元数据。然而，只要新规范路径按 basename 派生出的标题与另一个 Workspace 相同，注册表就会拒绝该路径。因此，`/a/xx` 和 `/b/xx` 等常见目录布局无法同时出现在 Web UI 中，尽管领域设计早已允许标题重复…
- **2026-08-01 · 拒绝运行时中归属于其他 agent 的 subagent 向人类发起交互**：一次性 subagent 调用 `ask_user_question` 时可能无限阻塞。该调用会等待人类回答，但子级没有由自身独立拥有的人类交互通道，因此子级无法完成，等待其完成的父级也会随之停滞。
- **2026-08-02 · Agent Note：Goal Round 收尾消息**：每个自主 goal 都以一条面向用户的收尾消息结束，而非一张裸工具卡片，代价是每个 goal 生命周期一次模型请求。`concludeTurn()` 保留其 loop 语义，但在 subagent 结构化输出之外失去了唯一的一方调用者。快照场景现在可以通过 `{{fromRequest:...}}` 脚本化只在运行时才存在的值…
- **2026-08-02 · Todo 优先的 composer 上下文顺序**：composer 上下文堆栈将 Goal 渲染在 Todo 之前，但 Harness 设计稿把当前任务计划排在进行中的目标和待处理 Queue 之前。Todo 还把 Queue 包装层的 776px 宽度用作自身的可见卡片宽度，而 Goal 和 Queue 面板则渲染在共享的 752px 卡片列上。结果既颠倒了预期的信息层级…
- **2026-08-02 · 消息 fork 操作要求消息位于已完成轮次尾部**：Web 会话把分支操作挂到每个轮次中最后一个文本非空的 assistant 节点上。如果后面还有工具结果、被中断的推理（reasoning）节点或终态错误，这些行也不会接管操作，因为它们没有内容文本 IconActions。因此，分支图标可能出现在 assistant 响应下方，而同一轮次的更多行仍位于其后。…
- **2026-08-03 · Agent Note：HMR 初始扫描使失败的启动死锁为静默的 exit 13**：状态：已实现
- **2026-08-03 · Web 与 headless 的有界信号关闭和重复信号强制退出**：默认挂载遥测后，`dsh web` 与 headless 命令（现为 `dsh --profile headless`）新增了 SIGINT/SIGTERM 处理器，使进程退出时可以排空 Cordis 插件树，而不是丢弃排队中的遥测数据。每个处理器都使用单向布尔闩锁（latch），并且只有在 `ctx.fiber.dispose()` 结算后才退出。…
- **2026-08-04 · 会话列为每个视图预留同一条滚动条槽**：composer 座位在组件树中只有一个节点、一个位置，但它究竟对齐到哪条边，取决于当前展示的是哪个视图标签页。
- **2026-08-04 · 会话列只在一个轴上滚动**：当中间列被拉窄——无论是拖窗口还是拖侧边栏——hero 态的整条会话列下方就会出现一条横向滚动条。溢出的元素是 hero 的装饰性背景椭圆：`.heroGlow` 的宽度取 hero 盒子的 `1051/776`，好让它的模糊在 userSpace 中随输入卡片一同缩放；这也意味着只要列比它窄，它就会伸出列外。
- **2026-08-04 · 加载 react-loop 重构前格式的会话**：react-loop 简化在保持 `SESSION_FORMAT_VERSION` 为 0 的同时更改了持久事件。该变更基线所存储的会话包含 steering（中途引导）事件 `steering/message`，以及 `turn/start.trigger` 字段…
- **2026-08-04 · 大规模历史记录的溯源信息通过扫描处理，不做参数展开**：一条已定稿的 assistant 消息可以通过 `sourceEventSeqs` 引用数十万个流式分片。历史记录分页使用 `Math.min(event.seq, ...sourceEventSeqs)` 查找消息组的首个事件，因此，有效会话可能超出 JavaScript 引擎的函数参数数量上限…
- **2026-08-05 · 上下文仪表看不见压缩**：composer 的上下文仪表的圆环、百分比与 `~已用 / 容量` 标题都取自 `contextPressure.pressureTokens`，即提供方报告的最新提示词规模。这个数字只在某个请求报告用量时才会移动…
- **2026-08-05 · 工作区新建会话复用了 cwd 匹配但未入账的空白会话**：在侧边栏某个工作区分组的 `+` 上创建会话时，有时会进入一个新会话，但侧边栏把它显示在「未分组」而不是点击的那个工作区下——「进入了新会话，但工作区没有被选中」。故障只出现在注册在 CLI（命令行界面）运行目录（即 `defaults.cwd = process.cwd()`，实际场景里就是 harness 检出目录本身）上的工作区…
- **2026-08-05 · 轮次尾部 IconActions 要求轮次已完成**：assistant IconActions 此前只从已定稿的 transcript（文本记录）推导：每个轮次中最后一条含内容文本的 assistant 拥有该行。这个量只有在轮次关闭后才稳定。轮次仍在产出步骤时，模型在工具调用前写下的叙述就是当时该轮次的最后一条内容 assistant，于是它在工具执行期间取得该行，等下一步的文本落定又把它交出去。…
- **2026-08-06 · Agent Note：首次使用引导的接管界面框架移入步骤自身**：状态：已实现
- **2026-08-06 · `list_agents` uses `ready` for resumable children**：`list_agents` 把可继续 child 的进程驻留状态投影为 `running | idle | complete`。`complete` 读起来像一项终态工作，且结果就在某处，但底层事实只表示没有驻留的 Activation：对话完好无损，`send_message` 可以继续它，而且它对 child 的结果不作任何断言。…
- **2026-08-06 · 可恢复的提供方凭据生命周期**：Models 编辑器横跨互相独立的 settings 与凭据 RPC 领域。之前它先提交提供方 settings，再存储 API 密钥，却一直保留卡片打开时的 revision 和原始子树。如果凭据写入失败，重试会用陈旧 revision 重放已提交的 settings 变更，并产生冲突，导致用户无法从同一张卡片完成第二个阶段。…
- **2026-08-06 · 在 API Key 进入 HTTP header 之前校验其格式**：一个含有 HTTP header value 无法承载的字符的 API Key，曾被每个配置入口接受，直到构造请求时才失败——离引发它的那个字段已经很远。
- **2026-08-06 · 将 bwrap 与宿主 PID 命名空间隔离**：bwrap 后端挂载了全新的 `/proc`，但保留宿主 PID 命名空间。因此，受约束命令可以看到宿主进程，并沿 `/proc//root`、`/proc//fd`、`/proc//cwd` 等 procfs 魔法链接进入宿主进程的挂载视图。当访问控制允许跟随其中某条链接时，该路径便可越过 profile 对宿主根目录的只读绑定挂载…
- **2026-08-06 · 未计价的表层替换以中性方式折叠**：`contextPressure` 与 `contextBreakdown` 两个投影只维护一份滚动累计的表层 token 总量，外加至多一条待结算的影子价格（shadow price）声明，因此其持久化检查点在会话整个生命周期内保持 O(1)。…
- **2026-08-06 · 窄视口下 Plan chip 点击区域回归测试**：外部报告 dsh-external/issues#107（内部聚类为 deepseek-harness#1406）测得视口宽度在 760px 到 850px 之间时 Plan 控件与模型选择器发生重叠，模型选择器覆盖 Plan 控件的点击区域，导致在 800×720 下无法用鼠标退出 Plan 模式。其验收清单要求增加浏览器回归测试…
- **2026-08-06 · 经由 observed-top ledger 的读者滚动归因**：ChatView 的贴底跟随此前只把滚轮／触控板手势识别为读者输入：钉在底部（floor）期间，一个没有对应滚轮位移的滚动事件会被视为程序化滚动并被拉回底部。因此触控平移、拖动原生滚动条与键盘翻页都无法离开流式 transcript（文本记录）的底部，在手机上尾部实际上被锁死。…
- **2026-08-06 · 通过 Host settings 持久化 Web 用户偏好**：Web 的 Appearance、Language 和繁忙态 Enter 偏好原本存在浏览器 `localStorage` 中。浏览器存储以 origin 为作用域，因此换一个端口重新打开 `dsh web` 会选中另一个存储分区并丢失选择，即使两个进程使用同一个 DSH home。这些是用户级产品偏好…
- **2026-08-07 · Code Mode 塌缩执行器而非仅通告面**：`mode: 'code'` 只塌缩了通告面，没有塌缩执行面。`wireSchemas()` 只向模型发送一个工具——`run_code`——但执行器通过 `get()` 解析所有调用，而 `get()` 返回完整的可见工具表外加保留的传输工具。模型一旦发出原生工具名（`write`、`read`、`bash`、`subagent` 等）…
- **2026-08-07 · 锁存取消收敛窗口内到达的唤醒请求**：`Agent.cancel(cause, { keepInbox: true })` 在触发 abort 信号后立即返回，但活动 driver 可能尚未收敛到 `idle`：LLM（大语言模型）流拆除、工具取消与 `turn/end` 落盘都会在 `abort()` 返回后异步展开。在该窗口内到达的唤醒 send 被放入 `next-turn`…
- **2026-08-09 · 损坏的 preset 是名单行，不是空缺**：文件成为唯一的组装编辑器之后，手动编辑造成的损坏有两种形态，且都要拖到最糟的时刻才暴露。`agent.cordis.yml` 解析不了的 preset 在名单上是一张完全正常的行——可选择、可复制、可设为默认——直到下一个会话尝试挂载才失败；一旦被设为默认，所有新会话都无法启动。组装文件被整个删掉的目录则从名单上消失…
- **2026-08-09 · 文件系统中的缺失是一种观测，带防护的创建绝不执行替换**：事件门控的文件系统策略最初只把成功读取和变更记录成目标版本。如果某个会话读取文件后，外部命令将其删除，第一次带防护的变更会正确地因陈旧而失败，但按指示执行的重新读取会在发出 `fs/observed` 前返回 `FS_NOT_FOUND`。因此，旧的存在版本会一直保留：写入仍不断选择 `replaceIfVersion`，提供方仍不断拒绝缺失目标…
- **2026-08-10 · Child agents join their parent's preset composition**：工具与提示段的可见性沿 `dsh-scope` 的父链继承，而 agent 的 scope key 铸造出来时没有父。逐会话 agent preset 把所有面向模型的行搬到了 agent 平面，并让 `AgentPresets.mount()` 成为绑定那条父链的唯一途径——调用点在 api-proxy 的会话创建、恢复与 fork 路径上。…
- **2026-08-10 · minimal preset 拥有完整的 RL agent 组合**：随附 Web 配置同时由两个位置定义与 Claude SWE 兼容的 RL agent（智能体）：进程级 `core-web.cordis.yml` patch，以及逐会话的 `minimal` preset。agent preset 成为 agent 组合边界后…
- **2026-08-10 · 会话行的标识判定纳入 preset**：`SessionManager.buildListSnapshot` 按值对列表行做记忆化：一次 wire 刷新会铸造全新的 summary 对象，因此与缓存项相等的行会被替换为缓存实例，下游每一个 `SessionListItem` memo 才能持续命中。它声明的约定是「每个字段都相同就复用缓存对象」，而那段比较是手写枚举字段的…
- **2026-08-10 · 插件激活前的主题引导**：Web 壳在浏览器侧插件树激活前呈现 `Loading plugins…`。ui-theme 的 token 样式随动态客户端 bundle 到达，因此不依赖框架的加载页使用私有的明暗回退配色。如果不提前写入 `color-scheme` 与 `body[data-ds-dark-theme]`，持久化偏好为深色时，该页面仍会先按浅色回退绘制…
- **2026-08-10 · 斜杠目录跟随空会话的 preset 切换**：preset 把决定 `/` 菜单内容的那些行搬走了。Web 组装禁用了宿主面的 `skill-filesystem`、`tool-skill`、`plan-mode` 和 `command-compact`，改由 preset 提供，因此一个会话有哪些命令和技能，是它自身组成的属性，而不是部署的属性。
- **2026-08-10 · 用同一条选取规则在空终止消息后保留子代理输出**：当 `max-tokens` 步骤只组装了工具调用块时，agent loop（智能体循环）会追加一条空内容的 `assistant/message`，因为 `BlockAssembler.blocks()` 会丢弃被截断的工具调用；这条消息仅记录 usage。三个消费方独立选取子 agent 的输出，并把这条 usage 记录当成输出。…
- **2026-08-11 · 创作 preset 的 agent 自行挂载校验其组装**：`cordis` preset 随包发布 `editing-cordis-compositions`，它是 agent 创作 preset 时唯一的指导来源。其中四条陈述与事实不符，而分量最重的两条恰好指向该 skill 自称「最容易让人栽跟头的规则」。
- **2026-08-11 · 宿主退出时同步清理受管子进程**：`LocalSubprocessRuntime`在自身 Cordis effect中安装一个同步 Node `exit` listener。只有正常 dispose结算后，同一 effect才移除该 listener。异步清理仍在等待时，普通和 terminal handle继续保留在服务已有的存活集合中，因此更短的外层退出上限仍能看到并强制终止它们。…
- **2026-08-11 · 有界后台任务准入**：模型可以在不同工具调用和后续回合中启动后台 Bash、PowerShell、PTY 操作与一次性 subagent。agent loop 的 `maxParallelToolCalls` 只限制单个步骤中尚未返回的调用；每个后台生产方会立即返回 job id，因此反复启动会让仍存活的进程或子工作无限增长。
- **2026-08-11 · 自有运行的结束原因报告**：Python SDK 消费方需要简洁地判断自有活动区间如何进入 idle。要求每个消费方扫描原始 `turn/end` 事件会重复协议知识，而通用的成功状态会丢失 token 上限与模型错误之间的区别。
- **2026-08-11 · 预设卡片截断自身描述，而不是由描述决定整份名单的高度**：preset 自行发布 `description`，长度不限，而设置分区把名单渲染为卡片网格。描述只有 `min-height` 没有上限，网格则以 `grid-auto-rows: 1fr` 排布行——该取值让每一个隐式行等高，而不只是承载高卡片的那一行。因此一条长描述决定了整份名单的高度：自定义组里放入一条 250 字的描述后…
- **2026-08-12 · Agent Note：修复 pwsh 终端 overlay 的重复 loader 冲突**：把 overlay 对 `tool-pwsh` 的 `insert` 替换成顶层按 id override：
- **2026-08-12 · First-run readiness reads every provider, and the setup card closes**：一个谓词回答两处界面真正需要的事实。`providerUsable(row)` 在路由已注册进适配器注册表（`entry.active`）、且其解析后 profile 所指名的凭据已存储时为真；不指名任何引用的 profile 走提供方自己的认证路径，没有 settings 地址的存活路由亦然，因此二者都不欠这个页面一把密钥。
- **2026-08-12 · 用 unlink 删除过期的 profile 回退链接而非 rmSync**：`healProfilesModuleFallback` 在安装位置迁移时会把 `$DSH_HOME/profiles/node_modules` 中的条目重新指向新目标，而 Windows 主机上这些条目是 junction。`ensureSymlink` 原先用 `rmSync(link)` 删除过期条目…
- **2026-08-12 · 聊天流展示 max-tokens 结束的轮次**：新增 `turn-max-tokens` 会话节点 Definition，匹配 `reason.kind === 'max-tokens'` 的 `turn/end`，在该轮位置生成一条持久聊天行：warning 状态的 StateDot、本地化标题，以及说明已截断输出会保留、发送“继续”可在新一轮接着输出的指引。节点只从持久会话事件推导…
- **2026-08-12 · 覆盖视图的 composer 座位改为补偿滚动条宽度，不再预留滚动条槽**：composer 标签页滚动条槽预留 让会话列滚动容器无条件预留一条滚动条槽，使 composer 座位在 Chat 与带 composer 覆盖的视图中测得相同宽度。代价由每个覆盖视图承担：视图内容列比列右边缘窄 8px，因为滚动容器为一条它从不绘制的滚动条预留了槽——trajectory 台账由视图内部自己的滚动容器滚动，外层盒子从不滚动。
- **2026-08-12 · 解析 Microsoft Store 的 pwsh 别名**：`resolvePwshPath` 声称 Store 安装经 PATH 解析，但它的存在性探测用的是 `existsSync`，会对候选做 stat、从而跟随重解析点。Store 的 `%LOCALAPPDATA%\Microsoft\WindowsApps\pwsh.exe` 是 app execution alias…
- **2026-08-12 · 递归删除前先解链 fixture junction**：install-lefthook 与 translation-pairing 的 fixture 把仓库真实的 `scripts/`、`node_modules` 和 tsx 包目录用 junction 链进 fixture 树，让 installer 探测能穿透解析。…
- **2026-08-12 · 通过 sessionStats 投影提供全会话统计条数字**：Web 聊天统计条的每个非 token 数字都折算自 `StatsLine` 已加载的会话窗口（`deriveStats` 遍历 `chat.legacy.nodes`）：「N 轮 · M 步」计数、LLM 与工具墙钟时间、TTFT／吞吐平均值。历史按每页 50 条消息分页…
- **2026-08-13 · Agent Note：反馈备注编辑器以浮层悬浮在对话记录上方**：备注编辑器完全不进入行的 flex 布局。它是一个浮层：一张固定定位的面板，portal 到 `document.body`，其坐标来自备注触发按钮的矩形。行保持其单行图标与备注触发按钮，因此没有任何东西需要围绕编辑器收缩、换行或回流，任何地方都不需要 `order` 或换行。portal 出会话列也逃出了列的 `overflow` 裁剪…
- **2026-08-13 · Agent Note：可配置提供方目录不再提供仅以 OAuth 认证的提供方**：模型设置页把 `openai-codex` 当作普通 pi-ai 路由提供出来，配的还是每个 pi-ai 提供方共用的那句占位文案：填入 API 密钥，或留空使用环境认证。照此配置后发送消息，本轮以 `Provider is not configured: openai-codex` 失败，并被适配器归入兜底的 `PI_AI_ERROR`。
- **2026-08-13 · Safari textarea 软换行收缩恢复**：composer 把光标与选区留在透明的原生 textarea 中，由 backdrop 绘制可见字形，并由隐藏的镜像层决定完整草稿高度。因此，单滚动容器决策依赖 textarea 不持有可滚动溢出：每次草稿提交后，它的 `scrollHeight` 与 `clientHeight` 相等，`scrollTop` 为零。
- **2026-08-13 · 有界验证冷空白会话**：`dsh-host-apiproxy` 注册 `sessionListMetadata` 投影，其中包含 `blank` 与 `lastPromptAt`。已附加摘要直接用同一组函数折叠实时日志。`blank` 只在 `turn/start` 时从 true 单调变为 false…
- **2026-08-15 · 回放状态与组装内容按构造对齐**：pi-ai 为每个响应记录一个从提供方原生消息投影而来的不透明回放数据，而 `BlockAssembler.blocks()` 会另行从 `max-tokens` 响应中丢弃工具调用，因为被截断的调用不能安全执行。持久化的 assistant 消息因此把变换后的内容与描述未变换原生块清单的元数据存在一起。…
- **2026-08-15 · 持久 bash 保留后端的受控提示符**：后端拥有自己的提示符协议并自行修复：受控 `PROMPT_COMMAND` 在打印标记后重新设定 `PS1`，因此任何 shell 内的提示符覆盖——本工具从前的初始化、模型命令、被 source 的脚本——都存活不到下一个提示符。这同时保护了无法报告前台状态的提供方：在那里，确切的提示符文本是唯一的就绪证据。
- **2026-08-17 · Subagent report 先于其结算通知**：可继续 child 可以显式上报选中内容，之后还会产生一条由管理器撰写且无条件投递的结算通知。报告投递曾使用 `Agent.followup()` 并进入 parent 的 `next-turn` 队列，而面向运行中 parent 的结算投递使用 `Agent.steer()` 并进入 `next-step`。…
- **2026-08-18 · Tool-row file-open failures stay visible**：工具行路径点击已经通过聊天视图注入的 `openFile` 调用 `host.openPath`。inject 吞掉了每一次 Host 或操作系统拒绝，因此缺少桌面打开器、远程或非回环载体、或 Host 无法交接的路径，都会让该行看起来像成功。读者看不到原因，也无法再试一次。
- **2026-08-18 · 轨道搜索在展开点击到达 document 时保持展开**：收起侧边栏的轨道搜索按钮会置位轨道手势标志（`searchOnExpand`）、展开搜索控件（`searchExpanded`）并请求侧边栏展开——设计意图是列滑开后让用户直接落在已聚焦的搜索输入框里。但在真实浏览器中这个手势从未完成：侧边栏展开了，搜索框却保持关闭且未聚焦。
- **2026-08-19 · DeepSeek reasoning passback on every reasoned turn**：`serializeAssistant` 对每个内容携带推理的 assistant 轮次都发出 `reasoning_content`，与是否有工具调用无关。没有推理块时仍然不发出该字段，因此非思考轮次的行为不变。
- **2026-08-20 · Terminal turn errors survive same-turn retry history**：Web 的 `turn-error` Definition 一旦发现所属轮次携带任何 `llm/retry` 事件，就永久抑制自身节点。这条规则编码的是有界 LLM 请求恢复最初交付的重试模型：当时重试会关闭失败轮次并开启下一个编号轮次——带重试历史的轮次只可能是中间失败，其事实已经落在重试行上，而耗尽后的终态失败落在一个没有重试事件的后续轮次里。
- **2026-08-20 · 显式 Web index 路径与静态资源未命中的 404**：无条件 SPA 回退会让每个未匹配的 GET 或 HEAD 请求看起来都成功。失效的普通链接，以及缺失的 JavaScript、样式表、source map 或 manifest，都会收到状态码为 200 的 HTML 外壳，导致浏览器、缓存与监控无法区分有效页面入口和缺失资源。
- **2026-08-20 · 输入框引用装饰按草稿顺序序号取 key**：输入框 backdrop 把草稿渲染成一组片段：纯文本字符串、开头的 claim token 标记、每个结构化引用一个元素、每个纯文本引用范围一个标记。React 按 key 协调这个数组。
- **2026-08-20 · 输入框的编辑自带它所作用的范围**：输入机器靠一个编辑范围来对齐引用 occurrence：范围之前的条目右移，之后的条目不动，被范围相交的条目失去结构化身份、以普通草稿文本留在原地。最后一条是"在引用内部编辑"的刻意含义。
- **2026-08-21 · DeepSeek Files 解析失败时恢复图片请求**：Files 仍是首选传输方式。每张请求图片的文件解析都有可配置的 `filesApiTimeoutMs` 时限，默认一分钟。stream idle 时限默认为五分钟，因此 Files 时限通常会为内联回退留出时间。部署也可以把 stream idle 时限设得更短，让它先终止请求。每次成功解析都会刷新外层 idle watchdog。…
### 25.4 已实现 · 简化（48 条）

- **2026-06-19 · 移除可变的会话摘要**：会话持久化 seam 将会话的日志外元数据拆分为 `dsh-session` 拥有的两种类型：一个不可变的 `SessionHeader`（`version`、`id`、`createdAt`、`cwd?`、`parentSession?`），在创建时一次性写入…
- **2026-06-20 · 保留单一公开停止原语**：公共 `Agent` handle 暴露了两种相互重叠的在途工作停止方式：仅针对步骤的 `abort()` 和感知队列的 `cancel()`。前者保留已排队输入，后者原本只暴露广义默认行为，该行为会清除已排队和 steering（中途引导）工作，同时中止活动轮次。…
- **2026-06-20 · 停止将持久化边界镜像为 agent 事件**：循环在 `SessionEvent` 中记录规范 transcript（文本记录），同时还发出一组并行的实时 `agent/*` 边界镜像事件：`agent/turn-start`、`agent/turn-end`、`agent/step-start` 和 `agent/step-end`。这些镜像迫使消费方在同一持久事实的两个真源之间做选择。…
- **2026-06-20 · 将仅用于追踪的会话事实折叠进承载实际功能的事件**：会话事件词汇中包含一些一等事件，它们不属于可回放的对话历史，在生产环境中几乎没有消费方。`usage` 已经作为模型流分片存在，之后循环又追加了一个独立的 `usage` 事件。`error` 与 `turn/end { kind: 'error', message, code }` 中的循环失败原因重复…
- **2026-06-20 · 统一 agent id 与会话 id**：一个存活的 agent（智能体）/会话对需要使用同一 identity 完成注册表路由、事件溯源和持久化。让 factory 接受相互独立的 `agentId` 和 `sessionId` 输入，会允许任何生产路径都无法使用的配对，同时迫使每个消费方为同一生命周期在两个名称之间选择或转换。
- **2026-06-26 · 拆分文件系统 seam——提供方文本变更操作与 `dsh-fs-observation-policy` 插件**：文件系统能力 seam中的文件系统能力目前让一个抽象 `FileSystem` 服务同时负责两项不同工作：
- **2026-07-04 · 收紧 hook-protocol 约定——dialect、被丢弃的字段、双重默认值与 lib 拥有的 `hook/result` 语义**：`dsh-hook-protocol`/bridge 约定中有四部分没有遵守 subagent observe/enrich Agent Note 记下的准则——后者因缺少消费方而删除 `agentType` 生命周期字段，以下各项没有通过同一检验：
- **2026-07-12 · 简化会话日志表示**：会话日志维护着两种表示，其机制复杂度超出了消费方的实际需求：一个伪链表 surface 和自定义的请求头增量。
- **2026-07-17 · 删除普通 send 的隐式批处理**：假设调用方连续两次调用 `Agent.send()`，先提交消息 A，再提交消息 B。隐式批处理可能只因为驱动器读取队列时两条消息都在等待，就把 A、B 放进同一个轮次。调用方明明调用了两次，agent loop（智能体循环）却悄悄把它们变成一个工作单元。
- **2026-07-20 · 注入内容逐字投影，去除 XML 封套**：两类注入的会话内容在渲染进模型 transcript（文本记录）时被包在 XML 封套里：`steering/message` 包成 `…`，`context/message` 包成 `…`（后者有一个 `'raw'` 退出选项可跳过封套）。这些封套意在告诉模型「这是注入内容，不是用户在说话」。
- **2026-07-20 · 移除 stdio 和 Echo agent**：DeepSeek Harness 在 TUI 和 Headless coding agent 之外，还提供了两个重复的产品 agent（智能体）。面向行的 stdio agent 使用混合的提示符/输出协议，同时重复实现终端交互与非交互执行。Echo 则以无需联网的 mock 模型加一个教学工具重复实现 Headless…
- **2026-07-22 · plan 专用协作状态**：产品只交付了 `plan`，首个 plan mode 实现却引入了通用的具名模式注册表。`ModeConfig.modes`、定义名称校验、`ctx.modes.list()`、已退役定义的回退逻辑，以及测试中合成的 `review` 模式，都只为支持假想中的未来协作模式而存在。…
- **2026-07-23 · ACP 作为仅面向自动化的协议**：ACP（Agent Client Protocol）桥接层已经变成第二套交互式产品 UI。它将持久事件转换为编辑器卡片、终端元数据、diff、计划、标题、推理（reasoning）、命令、模式、模型和权限选择器、会话导航以及面向人类的询问。这些职责与 TUI 和 Web 客户端重复…
- **2026-07-23 · 将实时持久化归并到单个刷写控制器**：有界写入批处理决策取代了本 Agent Note 中的即时调度节奏。单控制器归属、失败保留、按 id 串行化、退役和完全停稳的资源释放决策仍然有效。
- **2026-07-24 · 围绕可观察状态机收拢 agent loop 事件**：agent loop（智能体循环）曾将其控制流暴露为大量 Cordis 事件。`pre-step` 和 `post-step` 两个独立检查点分列步骤前后，`session-prefix` 和 `step-result` 分别变换请求消息与响应消息，`request-error` 决定失败的请求是否在当前轮次内重试…
- **2026-07-26 · 将 subagent 控制合并到 subagent 服务**：公开操作集合由以意图命名的 subagent 继续执行操作进一步细化，并由可继续的 subagent再次细化——后者保留这一个合并后的服务，同时移除提供方 `resume` 派发和基于 Task 的继续执行生命周期。
- **2026-07-27 · 按意图命名的 subagent 继续执行操作**：当前基于 Activation 的实现由可继续的 subagent负责。它保留本记录命名的 `followup` 操作，返回已接受的 `MessageId`，使用裸 `Agent` 参数作为确切的在线直属父级权限，并将提供方对可继续 child 的参与限制为 `prepareContinuable`。
- **2026-07-27 · 请求错误重试动作**：模型请求恢复由 `agent/request-error` 内部决定，却通过 `Agent.retry()` 传达。这个公开命令只在一个狭窄的 waterfall（瀑布式事件）窗口内和空闲时有效，在其他运行状态下会被拒绝，并要求 `ReactLoopAgent` 在 waterfall 结果旁保留一个可变的重试窗口。恢复插件是仅有的生产调用方…
- **2026-07-28 · 本地 JSON 树渲染器**：轨迹检查记录表使用的只读 JSON 检查器需要提供紧凑的对象和数组预览、供复制操作使用的明确数组路径、固定展开与可折叠两种根节点模式，以及键盘导航。`react-json-view-lite` 既不提供自定义节点渲染，也不提供行标识；要通过该依赖满足这些要求，就必须使用包管理器为编译后的发布文件打补丁，并遍历 DOM，从可见标签中还原数据路径。…
- **2026-07-28 · 移除纯日志事件的合成轮次**：会话存储曾暴露 `appendOutOfBand()`，让插件可以在没有 agent（智能体）轮次运行时发布延迟到达的纯日志事件。该方法会用 `turn/start` 和 `turn/end` 包住事件，再将其刷写。这保留了「每个持久事件都必须位于轮次内」的旧规则，却让同一个标识符既表示模型循环执行，又表示仅持久化更新。
- **2026-07-29 · 一份共享 base 配置加各 surface 的 overlay**：`dsh` 交付了两棵完整的配置树，其中有 43 个共享配置项。`apps/cli/cordis.yml` 以 74 个平铺配置项组合 web surface，而 TUI 启动的是 `examples/tui-agent/cordis.yml`——其中单独一行 `@deepseek-ai/dsh-tui-demo` 挂载了十二个插件…
- **2026-07-29 · 简化 Web 图片输入第一版**：首个持久化 Web 图片输入切片在引入按顺序接收多张图片的必需能力时，也引入了由 CLI（命令行界面）挂载任意提供方、输出模态发现、替代文本、提供方无关的视觉 token 定价，以及没有跨包（package）消费方的浏览器生命周期 API 等推测性表面。保留这些推测性表面会把尚未选择的未来行为变成公共契约，并使初始能力更难评审和维护。
- **2026-07-30 · 将 agent 路由保留为私有实现**：公开的 `Agent.send()` 方法暴露了具体循环实现的路由矩阵，但生产调用方只使用语义明确的 `followup()`、`steer()` 和 `inject()` 操作。第四种组合，即 `next-turn` 配合 `wakeup: false`，除测试外没有消费方。将这项潜在能力保留为公开接口…
- **2026-07-31 · 不依赖具体能力的沙箱策略上下文**：当前策略上下文最初通过受强制执行家族与可升权家族两个独立注册表来映射运行时组合。后端、工具与示例中的六个调用点会贡献 `filesystem`、`bash` 或 `terminal`；策略服务保留 token 集合，以便独立释放各项注册，对两个注册表的内容求交集并排序，在每次生命周期变化时使提示词组装失效，并且需要测试覆盖所有家族组合。
- **2026-07-31 · 添加 Workspace 的唯一路径**：两处 Workspace 表层——侧边栏区头的 `+` 与会话主视觉区的 chip——都提供了两条获得 Workspace 的路径：**打开本地文件夹…** 拉起组合的目录流程，**新建工作区** 接收一个名称并创建 `/`。两者重叠：浏览占用者自带 **新建文件夹** 能力，因此「选一个目录」本就覆盖了「建一个目录」。…
- **2026-07-31 · 移除 user 消息的编辑存根**：user 气泡的 IconActions 行在复制和分支旁边还有一个编辑按钮，但其背后什么都没有：该控件没有点击处理、没有 client 侧变更，也没有 host 侧重新发送已编辑消息的操作。用户找到它时，看到的是一个产品无法兑现的可供性。
- **2026-08-03 · 从交付的 dsh 配置中省略运行时不变式**：`@deepseek-ai/dsh-invariants` 与各包拥有的 `./invariant` 伴随插件是可选的开发诊断。交付的 TUI 挂载了该服务和四个有状态伴随插件，而交付的 Web 配置树省略了这些条目，导致两个产品 surface 的诊断成本和失败行为不同。即使始终启用的产品边界仍负责会话验证与不可变历史…
- **2026-08-04 · 删除 Windows PowerShell 选择器回退**：原生目录选择器的 win32 分支在 koffi `IFileOpenDialog` 子进程之下保留了一条两级 PowerShell 回退：先 `pwsh.exe`，再 `powershell.exe`（Windows PowerShell 5.1），两者运行同一个主动启用 `SetProcessDPIAware` 的 WinForms 脚本。…
- **2026-08-04 · 移除 TUI 包**：移除隐式的 `dsh` 终端应用后，`@deepseek-ai/dsh-tui` 不再拥有任何已交付的组合。该包仍包含终端渲染器、交互式命令与问答适配器、扩展浮层、快照 fixture（测试前置数据）、已打补丁的 `pi-tui` 依赖，以及仍将 TUI 宣称为受支持应用接口的 SDK 脚手架。保留这整套能力意味着继续维护一个产品规模的前端…
- **2026-08-06 · user 与 steering 气泡移除分支操作**：每个 user 气泡和已消费的 steering（中途引导）气泡都渲染分支控件，受已完成轮次尾部决策的门禁约束。在这些气泡上，该门禁实际上是永久性的：开轮的 user 消息后面必然跟着本轮自己的节点，已消费的 steering 消息按构造就处在轮次中间，因此只有当轮次结束时该消息之后一个节点都没有——即在第一个模型事件之前就取消——控件才可能启用。…
- **2026-08-06 · 无缓冲反馈遥测**：仅反馈遥测必须只在记录反馈后上传会话日志前缀。若在触发前为每个已投影事件保留一份已深拷贝、已脱敏的记录，就会复制权威会话日志；对于长期运行但从不记录反馈的会话，这份副本会无限增长。
- **2026-08-08 · 仅复制的 preset 创作，与通往 preset 文件的入口**：agent-preset 设置页带着一个网页 YAML 编辑器：`agentPreset.write` 接收任意组装文本，页面是一个没有补全、高亮或 diff 的文本域，形状检查依赖 Loader 自己的 `entryListSchema`——其方言含 `!!js`，所以「过了形状检查的文本」在下一次挂载时仍是任意代码。作为编辑器很弱，作为能力很宽…
- **2026-08-08 · 移除独立的 CLI demo**：在 `dsh --profile headless` 成为产品的一次性命令后，`@deepseek-ai/dsh-cli-demo` 仍是承担同一工作的第二个应用包。它另行拥有一套可执行文件、参数语法、应用组装、取消生命周期、文本／JSON／stream-JSON 输出约定、构建产物、配套文档和测试套件。两个入口组装的树也不相同…
- **2026-08-09 · 对话式 Schedule 交付**：Schedule 已经通过将普通的 agent（智能体）后续轮次排入队列来交付到期提醒。第二条持久 Web 回执通过 Schedule 投影、持久化成功事件、Host 历史记录与 live 伴随数据、客户端同序号升级、通用事件视图 slot 和专用渲染器表示同一次提醒触发。…
- **2026-08-09 · 显式 Schedule 时区边界**：隐式本地 `at` 输入把浏览器事实变成了共享产品状态。在 Session 创建时捕获默认时区，需要增加新的 Session header、create／resume／fork 冲突规则、JSONL metadata、SQLite migration、client 创建 plumbing、Host 比较…
- **2026-08-09 · 有界固定速率 Schedule**：用户需要简单的重复提醒，但持久、仅限 Session 内的提醒最初采用的周期层把固定间隔和日历表达式当成一个通用子系统。它增加了 Cron 语言与求值器、时区敏感的发生时点搜索、tzdata 回放规则、跨记录的 300 秒准入门控、持久化的门控证据、延迟交付字段，以及门控耗尽状态。即使所请求的行为只是“每 N 秒重复一次”…
- **2026-08-09 · 移除专用 repository 插件路径**：repository 插件路径与 profile 组合包路径重复实现了第三方包的安装和组合。它增加了 `.dsh-plugin` manifest（元数据清单）、生成的包装层、准备工作可执行文件、第二套 Git／包缓存、Loader 内置项，以及 repository 专用的 skill（技能）和 MCP 适配器。…
- **2026-08-10 · 无需托管安装器的源码运行**：仓库自带的源码安装器可以提供稳定的启动器、相互隔离的 staging worktree、原子升级、回滚存储，以及用于个人定制的共享维护工作流。与此同时，仓库还必须在包管理器之外负责第二套生命周期：安装宿主依赖、提示输入凭证、接管检出、管理符号链接归属、协调 staging 分支、处理升级恢复，以及持续保持安装器与随附维护 skill（技能）的兼容性。
- **2026-08-10 · 移除 steering（中途引导）插话标注**：上下文来源与 steer 标识决策给每个持久与待处理的 steering 气泡加上了 `插话` / `Interjection` 标注，让 transcript（文本记录）能说明哪条右对齐气泡打断了正在运行的轮次。这个标注重复了消息流已经呈现的事实：steering 气泡位于轮次中途、夹在被它打断的助手内容之间，而开轮提示位于轮次边界。…
- **2026-08-10 · 通用 preset 只提供一套编辑工具**：`standard`、`code` 和 `cordis` preset 同时提供 `read`/`write`/`edit` 文件系统工具与 `str_replace_editor`。两套接口在常规文件查看和编辑上重叠，导致每次请求都携带额外的工具 schema，却没有增加独立的默认能力。…
- **2026-08-11 · parseCmdline 运行 program 自己的 commander action**：`parseCmdline(ctx, program): void` 只把 commander 的控制流适配到启动器：它解析不可变的 `cmdlineArgs` 快照，并把 help、version、解析错误与 action 的拒绝转换为一次 `ctx.appExit` 请求。应用代码——commander 语法表达不了的校验…
- **2026-08-11 · 将文档根路由指向快速开始**：单独的文档首页会重复产品首页所维护的产品定位和功能摘要。这些重复声明需要同步与评审，却不能帮助读者查阅技术操作说明。
- **2026-08-11 · 移除 SDK 项目工具链**：仓库曾包含一套从未发布且没有消费方的开发者项目产品。`@deepseek-ai/create-sdk` 用于生成可编辑的 Cordis 项目；`@deepseek-ai/dsh-scripts` 提供 `dsh-sdk` 的开发、构建、启动、配置和插件安装命令；`@deepseek-ai/dsh-helper` 协调功能定义与多文件项目编辑…
- **2026-08-12 · Trim the Agent Teams read and lifecycle surface**：Team 服务保留独立的产品职责：持久具名 roster、Lead-log mailbox 与 task DAG。它不会与通用 subagent catalog 或 task service 合并。
- **2026-08-12 · 将源码启动与仓库构建分离**：TypeScript 源码启动器无需在每次调用前完成整个仓库的构建。Web 界面则需要已构建的前端与 Client plugin 产物。由同一个包脚本同时负责这两项操作，会让重复启动 TUI、无头模式和 Web 时都承担全仓库构建延迟，也会掩盖浏览器产物何时刷新。
- **2026-08-12 · 生产 dsh 排除产品 subagent 提供方**：`@deepseek-ai/dsh` 会获得 `@deepseek-ai/dsh-base` 的依赖闭包。如果 base 包含 Codex 与 Claude Code subagent 提供方，每次生产安装都会下载可选的产品集成代码与大型平台 CLI 载荷，即使用户并未使用任一集成。
- **2026-08-13 · 移除首次启动内测声明**：GUI 每次首启都会先显示占满视口的内测声明：内部测试的定位表述，加上通过 `DSH_TELEMETRY_MODE` 开启 Session Log 上传的说明。会话遥测在 mode 未设置时已解析为 `DISABLED`（遥测默认关闭），因此引导流程中关于遥测的全部内容就是一段教用户如何开启的提示，而内部测试的定位表述本身也不应出现在发布版本里。
- **2026-08-19 · 删除 knip.json 中失效与重复的 workspace 条目**：`knip.json` 携带了大量不产生任何作用的 workspace 条目。其中一些指向已经不复存在的包，另一些与 `packages/*/*` 通配默认完全重复。这两类都让文件变大——790 行——并显现出配置已经超出了它所描述的包：读者无法分辨哪些条目在保护真实行为、哪些是惰性的。
### 25.5 已实现 · 流程（74 条）

- **2026-06-11 · 以机械质量门禁取代行文约定**：本记录中的钩子/CI 对称设计已由快速本地 Git 钩子取代；CI 仍是执行完整检查的路径。
- **2026-06-11 · 将 Cordis 以源码形式收录，而非作为 NPM 依赖**：DeepSeek Harness 构建于 Cordis 框架之上。本仓库启动时，Cordis core 处于 4.0.0-rc.6（一个候选发布版本）；harness 依赖框架内部实现（fiber 生命周期、dispose（资源释放）、waterfall（瀑布式事件）分发），其确切行为直接关系到 agent loop（智能体循环）的正确性保证。
- **2026-06-16 · 使用 pnpm 替代 Yarn 4 作为包管理器**：本仓库最初使用 **Yarn 4** 搭配 `node-modules` 链接器。这是一个刻意保守的选择：行为类似 npm 的扁平布局，同时享有 Yarn 的 workspaces 和 `yarn constraints`。它能正常工作。但 Yarn 4 源自 Plug'n'Play 的血统，使得 `node-modules` 链接器成为非主流模式…
- **2026-06-17 · TSC 优先构建与编译器单一归属**：根项目拓扑由一个 solution 根文件统辖两个 aggregate program；见 solution 根文件 Agent Note。Host 生成 Remote 约定后再编译 Client 的当前命令顺序见 API Remotes 构建 Agent Note。本文确定的 tsc-first 职责保持不变。
- **2026-06-18 · Markdown 交叉链接有效性检查**：本仓库的文档通过相对路径互相链接：`topic`、`the cookbook`、`architecture.md`。此前没有任何机制验证这些目标是否存在。重命名或移动文件会静默破坏所有指向它的链接，且在读者点击之前不可见。doc-sync（文档同步门禁）强制执行已经将两类文档漂移的检查自动化（无法编译的代码块、陈旧的事件分类体系表）…
- **2026-06-20 · 子系统目录与 `ts type-equiv` 漂移门禁**：试图理解 harness 的读者可以在 architecture.md 中找到它的*行为*（服务图、会话/轮次/步骤生命周期、事件分类体系），却找不到一个统一描述其*词汇*的地方，也就是这些行为所传递的数据结构。类型定义只存在于源码中，散落在 `packages/*/src/types.ts` 各处…
- **2026-06-20 · 通过路径编码的子目录对 Agent Note 进行分类**：仅按生命周期组织的 Agent Note 目录树（`proposed/` / `implemented/` / `rejected/`）无法记录每个文件包含哪一*类*决策。读者浏览某个生命周期时，如果不逐一打开文件，就无法区分新功能、移除项或工具策略变更。
- **2026-07-02 · 生成的工具 schema 目录（启动并采集）**：仓库此前没有一份统一的参考文档来记录实际暴露给模型的工具名称、描述与 JSON Schema。源码声明分散各处且在运行时组合，而既有的 Cordis 参考和子系统页面覆盖的是接线与词汇，而非工具。
- **2026-07-02 · 通过配对兄弟文件与配对门禁实现双语文档**：本仓库的文档语料会被公司内外的人和 agent（智能体）以中英两种语言阅读。在没有机制的情况下纯靠手工维护第二语言，正是译文腐烂的根源：一侧持续演进，另一侧默默失实，而没有门禁能够发现。对于这类不变式，本仓库一贯的做法是将其编码为机械检查（见质量门禁与 doc-sync（文档同步门禁）强制），因此双语政策随附一道门禁一起交付。
- **2026-07-04 · 文档结构、层级与预算**：尽管已有写作指导，常设文档仍不断累积重复规则、反复讲述的事故、重复的包映射，以及陈旧的 Agent Note 摘要。该指导也未明确文档在层级中的位置如何限定其内容范围，以及按顺序引导读者学习的内容与面向查阅的材料有何不同。仅靠评审无法阻止这种增长，因此仓库需要在文档分类体系之外再配一套可自动执行的预算。
- **2026-07-05 · Agent Note 的统一受门禁约束的文件内格式**：Agent Note 的路径编码了生命周期和类别，但文件内容仍混杂着不同标题、状态格式、ADR 与提案模板，以及已实现记录中的提案阶段章节。作者会复制随手找到的相邻文件，而生命周期迁移可能跳过必要的改写，因为没有门禁强制执行文件内约定。
- **2026-07-06 · 导出 JSDoc 门禁**：Cordis JSDoc 完整性门禁使得 Cordis 接口上的参数和返回值不可能缺少文档——`interface Events` 成员和 `ctx.` 服务类——但这只覆盖插件作者可导入接口的一小部分。AGENTS.md 中的规则「每个导出（以及非显而易见的方法）都必须有解释语义的 JSDoc」在其他地方仍只是由评审检查的文字约定…
- **2026-07-06 · 将 Node LTS 引擎下限提升至 22.19**：根 `engines.node` 范围中的 Node 22 分支是对安装后工作区的约定，而不仅仅是 harness 源码直接调用的运行时 API 的约定。它不得低于工作区在该分支上安装的依赖包所声明的 `engines.node`；否则 `pnpm install --engine-strict` 会在一个已宣传的 LTS 版本上失败…
- **2026-07-06 · 并行 pre-push 门禁**：本记录中的本地钩子部分已由快速本地 Git 钩子 取代。有界门禁调度器和包级 `publint` 并行机制仍用于 CI、`doc-sync` 和显式本地命令。
- **2026-07-10 · 每个包 README 中受门禁保护的「已知限制」章节**：文档标准规定限制项归属包 README。没有统一结构时，章节缺失便无法区分“经审计确认没有限制”与“忘记编写文档”，不同的标题还会妨碍全仓库搜索。
- **2026-07-12 · 包的模型体验约定**：包 README 可以解释 API 和运行时机制，却不回答主导 agent harness（智能体框架）行为与成本的问题：该包的哪些内容会进入模型请求、在什么条件下进入、这些 token 会保留多久，以及后续请求是否会保留可复用的 KV Cache 前缀。在插件架构中，这种遗漏尤其难以审计。消费方可能把后端结果转为工具消息，策略插件可能以错误取代成功结果…
- **2026-07-13 · 将权威文档投影到网站**：仓库需要一个可导航的文档网站，但不能让网站目录成为第二个文档源。把包指南、架构页面或生成目录复制到网站专用目录树，会使两份副本发生漂移；让 VitePress 直接指向仓库根目录，又会把公开 URL 和导航与内部文件布局耦合。仓库相对链接在网站上也需要指向不同位置：已发布页面应留在站内，源文件和未发布的贡献者文档则应指向 GitHub。
- **2026-07-14 · 基于 TypeScript Program 的语义门禁**：仓库门禁有时需要判断 TypeScript 语法本身不携带的事实：接收者是否为 Cordis `Context`、哪些具体事件名会进入转发辅助函数、声明合并是否改变了事件签名。
- **2026-07-19 · Web 样式体系——token 框架与工程约束**：GUI 无设计师供给，样式由 agent 编写并 review；没有一套机器可检查的 token 体系与编码规范，颜色/圆角/动效会在组件间字面量漂移，暗色主题会长成组件内散落的条件分支。
- **2026-07-19 · 无需生成索引即可发现 Agent Note**：提交到仓库的 Agent Note 索引，会重复记录每个文件的生命周期／类别路径、文件名日期和 H1 已经编码的事实。任何分支只要添加、移动或重命名彼此无关的 Agent Note，都会重写同一个生成文件，因此该产物会成为可预见的合并冲突热点。
- **2026-07-19 · 每项实质性变更都必须附带 Agent Note**：如果只在决策被认为持久、有争议且出人意料时才记录 Agent Note，实质性变更就可能在没有保存决策依据的情况下落地。代码和测试能展示改动内容，却无法稳定保留某种方案胜出的原因、被放弃的备选方案，以及维护者接受的成本。
- **2026-07-20 · GUI 测试体系——三层结构**：沿架构天然的测试钩子切分为三层，自底向上：
- **2026-07-21 · 跨平台串行 CI 参考流程**：拉取请求工作流将必需检查合并到专用的 Linux 和 Windows 作业中。这些作业仍不应成为唯一的完整性判定基准：如果其门禁清单或依赖图存在缺陷，即使必需聚合结果保持绿灯，也可能漏掉部分工作。
- **2026-07-22 · 产品优先的根 README**：根 README 是仓库的产品入口。其产品优先的结构和既有语气仍然有效，但随着运行时不断扩展，具体入口和能力声明会逐渐陈旧。重写事实仍然正确的章节，会扩大评审范围，也会丢弃已经行之有效的措辞。
- **2026-07-22 · 以 solution 根文件统辖两个聚合 program**：GUI 拆分引入了第二个聚合 program（`tsconfig.client.json`，见分层 RFC），根 `tsconfig.json` 则继续兼任宿主侧聚合，`tsconfig.build.json` 还是第三份手工维护的全量 emit 图。三处账本并行，造成四个具体的不对称：
- **2026-07-22 · 基于实证选用 GitHub 托管大型运行器**：高度分片的 CI 拓扑通过把主 Node 工作分散到 40 个 Linux 作业、把 Windows 工作分散到 9 个作业来达到延迟目标。大多数门禁本身的耗时短于代码检出、运行器设置、缓存恢复和依赖安装这些准备阶段，因此反复执行多轮设置既增加成本，也带来延迟波动。一次托管运行中最慢的 Linux 作业用时 49 秒…
- **2026-07-22 · 快速本地 Git 钩子**：agent（智能体）已经会运行能够覆盖自身改动的测试和检查，而提交、推送与 CI 可能分别重复其中范围越来越广的子集。因此，全量 pre-push 套件会拖慢每次推送，放大与当前改动无关的本地偶发失败，而且 CI 紧接着再次运行完整矩阵时不会提供新信号。
- **2026-07-23 · 拉取请求 CI 的可移植恢复边界**：分配到组织自有运行器标签的拉取请求必需作业，在 GitHub 无法为这些池分配运行器时会持续排队。工作流本身有效，GitHub 标准托管作业仍能通过，但 `all checks passed` 始终无法启动，原本健康的拉取请求因此无法满足分支保护要求。
- **2026-07-23 · 经校准的翻译提示词 v4 约定**：自动生成对侧文件需要一份稳定的提示词，能够复现经人工评审的译文所确立的语体和修正方式。注入通用说明文档，会让这份经校准的模型输入随着面向人类或 agent（智能体）的指导发生变化，而未经封装的响应无法分别承载草稿、自检内容和修正后的文档。普通的类 XML 分段标签还会与用于说明这些标签的合法 Markdown 内容发生冲突。
- **2026-07-26 · CI 故障切换手册 — 托管池 → 自有池**：CI 中三个必需的 Linux 工作作业（`node 24 / static`、`node 24 / coverage`、`node 24 / snapshots and artifacts`）运行在托管的企业级 32 核池上；聚合它们的必需判定作业（`all checks passed`）运行在标准 `ubuntu-latest` 上…
- **2026-07-26 · web client 的语法高亮——同步细粒度的 shiki**：范围：web client 唯一的一套语法高亮体系——依赖裁决、单例形态、token 表约定与各消费表面。本篇是 Code Mode UI 堆叠 PR（Pull Request）链的第五个 PR；chat 子调用行 Agent Note交付了 `run_code` 程序正文，而本体系存在的意义正是让它可读。样式的基本规则由 Web 样式体系裁决规定。
- **2026-07-26 · 优先选用持续维护的依赖，而非手写实现**：harness 手写了大量基础设施，而成熟的外部包早已提供同等能力。其中一部分是有意为之——以源码形式收录的 Cordis（引入 vendor 的决策）、孪生 LLM（大语言模型）适配器、作为配置 schema 标准的 schemastery——但相当大一部分是在一种未经言明的「避免新依赖」下意识作用下逐渐累积而成的：仓库级的外部依赖清单始终很小…
- **2026-07-26 · 基于简报的最小化翻译更新**：双语配对约定早已规定对侧文件按最小幅度更新：把被改的一侧与其上次确认状态做 diff，据此修补对侧文件，绝不整篇重译；但仓库内置的工作流让每次更新都付出整篇文档级别的开销。负责翻译的 subagent 在动手处理一个两行的 diff 之前，要先加载完整的指导语料（guidance corpus）…
- **2026-07-26 · 增量更新 PR 的 base 分支**：将 PR（Pull Request）的 base 分支当前顶端提交合入 PR 分支的过程中，base 分支可能继续前移。若改从新的顶端提交重新开始，就会丢弃已经完成的冲突解决和验证工作。重写已经推送的合并还会抹去可供评审的历史记录。
- **2026-07-26 · 将未来指导价值较低的 Agent Note 冻结在活跃记录集合之外**：implemented Agent Note 作为当前决策记录持续维护，因此活跃记录集合中的每个路径、符号、默认值、译文、围栏代码块、包引用和出站链接都会形成维护义务。当决策依据可以指导未来工作时，这项成本合理；但对于已经收尾的 UI 细节、小型修复、已被取代的实现机制，或当前权威依据已转移到别处的流程历史，这项成本并不值得。…
- **2026-07-26 · 经由 pnpm/action-setup 提供 CI 的 pnpm**：除 `landlock-run.yml` 外，每个安装 pnpm 的工作流都曾用 `corepack enable` 手工提供 pnpm，其中五个还各自重复着一套手写（hand-rolled）的缓存设置——`pnpm store path --silent >> $GITHUB_OUTPUT`、再加上以 `pnpm-lock.yaml` 为缓存键的 `acti…
- **2026-07-27 · Dependabot 版本更新采用 30 天冷却期**：来自包注册表的依赖与 GitHub Actions 依赖都需要定期更新机制。每个新版本一经发布便立即采用，会增加受到遭入侵的版本和早期回归影响的风险；但完全依靠手动更新，又会导致依赖版本差距持续扩大。以源码形式纳入仓库的 Cordis 不能当作注册表依赖处理，而共用一份锁文件的工作区必须通过同一棵包树更新。
- **2026-07-27 · 显式报告仓库变更范围**：pre-push 工作流需要取得相对于实际基准的 diff，但按 `origin/` 构造引用存在两类问题：对于第一次推送前跟踪 `origin/master`、尚无同名远端分支的新 worktree 分支，该引用无法解析；对于 PR（Pull Request）以另一功能分支为基准的堆叠分支，该引用会错误描述基准。…
- **2026-07-27 · 让 Lefthook 安装限定于各 worktree**：每次运行 `pnpm install` 都会执行根目录的 `postinstall`，其中的 `install-lefthook.mjs` 会调用 `lefthook install --force`。若无额外配置，关联的 Git worktree 共用同一仓库的默认钩子目录…
- **2026-07-28 · 按子系统生成的 cordis-surface 区块**：一个子系统的文档过去分散在三个归属：手写的 subsystems 页面（介绍、数据结构、动词）、平铺生成的 `docs/cordis-catalog/services.md` 中属于它的 `ctx.` 切片，以及平铺的 `docs/cordis-catalog/events.md` 中属于其事件作用域的切片。shell.md 的读者必须再打开两份文档…
- **2026-07-29 · 使用 Oxlint 作为仓库 linter**：仓库的自有源码需要类型感知的 TypeScript 正确性规则、一致的格式，以及文件内重复逻辑检查。ESLint 通过 JavaScript 解析器、项目服务和多个插件提供这些检查，但在本地迁移基线上，一次无问题的 lint 运行约需 1 分钟，并且需要 8 GiB Node 堆、CI 结果缓存和单独调优的 ESLint 并发度。
- **2026-07-30 · CI 消费方独立构建**：大型运行器拓扑将静态门禁清单和构建后消费方清单分配给不同作业，但二者共用的构建由静态作业负责。静态作业要等所有静态门禁完成后才上传生成的目录树，消费方作业则在恢复该目录树前声明了作业级依赖。基于编译输出的快照与发布校验确实需要完整构建，但不依赖运行时依赖闭包检查、文档生成、模块图验证或 Knip。
- **2026-07-30 · verify-cordis-config 对配置中插件的源码面解析实施门禁**：`apps/cli/config/tui.cordis.yml` 新增了 `@deepseek-ai/dsh-tui/prompt` 配置项，却没有对应的 tsconfig `paths` 映射。通用的 `@deepseek-ai/dsh-*` 通配符会把 `tui/prompt` 整体代入其 `/*/src` 候选路径，而这些路径全都不存在…
- **2026-07-30 · 生成的第三方声明**：本仓库开源需要披露所依赖的第三方软件及各自的许可证。这份披露必须完整，必须随依赖变化保持为真，还必须给出读者用得上的信息：哪些包最终会进到用户机器上，哪些只用于构建和测试。
- **2026-07-31 · 覆盖率豁免重型套件**：`ci-coverage` 聚合拆成两个并行 gate，全部测试仍然执行，只有重型套件不再交插桩税：
- **2026-08-02 · GitHub 原生堆叠与可选 PR rebase**：仅以 base 分支表示的依赖 PR（Pull Request）链没有官方的堆叠身份。要让它落地，就必须逐个手动合并 PR、保留中间分支、调整每个子 PR 的 base，并重新查证这条链是否仍然完整。GitHub 原生的堆叠 PR 功能则会承载顺序，对每一层应用 trunk 规则和 CI，并负责自底向上的合并与 base 调整。
- **2026-08-03 · 按包锚定的子系统页面与精简的分组 README**：子系统目录最初用主干-vs-seam 规则界定首页范围：如果循环在每个轮次都持有、派生、流式传输或记录某个类型，它就是「核心」。该规则选择的是类型而非包，因此当目录增长到四十多页后，首页变成了跨包大杂烩：LLM（大语言模型）对话词汇排在 agent（智能体）约定之前…
- **2026-08-06 · 仓库内 Landlock 发布**：`@deepseek-ai/node-addon-landlock-run` 源码已经与其 DeepSeek Harness 消费方一同位于 `native/landlock-run` 下，但此前仍保留独立的 pnpm workspace 和锁文件，并依赖一个独立仓库发布到 npm。Harness 包使用 npm 注册表中的固定版本…
- **2026-08-06 · 文档站点自带图片**：`scripts/project-doc-site.ts` 会把发布 manifest（元数据清单）未收录的仓库相对目标一律改写成 GitHub 地址，对图片而言就是 `https://raw.githubusercontent.com////`。站点构建不拷贝任何文件：`srcDir` 是用完即弃的 `.generated` 树…
- **2026-08-06 · 覆盖率未达标时输出精确未覆盖位置**：per-file 100% 覆盖率门禁失败时，vitest 只输出文件级错误行（`ERROR: Coverage for lines (…) does not meet global threshold (100%) for `）——知道哪个文件没达标，不知道差在哪几行。内置 `text` 报表虽有 Uncovered Line #s 列…
- **2026-08-08 · API Remotes 生成约定的有序构建**：Host 的 `@Remote` 方法需要先由 Typert 生成 `/remote` 声明和运行时贡献，Client 的 `api-remotes/src/client/index.ts` 才能通过类型检查并打包这些贡献。若根构建先把 Host 与 Client 两张 Project Reference 图一起交给 tsc…
- **2026-08-08 · Wine 与原生 Windows 双通道拉取请求 CI**：拉取请求必需的 Windows 判定既需要快速的 win32 工具链信号，也不能让聚合流程等待稀缺的 Windows 容量。Wine 提供这项关键路径信号，但它运行在 Linux 内核与区分大小写的 ext4 之上，采用 hoisted 依赖布局，且无法证明 NTFS、DACL、ConPTY、崩溃持久性或原生进程行为。原生串行参考流程停用期间…
- **2026-08-08 · 浏览器 GIF 保留单一证据链**：浏览器演示的分镜可以由每张都真实的截图组成，却无法证明这些截图来自同一次真实执行。复用应用全局状态可能引入旧设置或旧会话；录制自动化可能误将不同模型运行的画面合并；聊天 transcript（文本记录）可能显示降级处理成功，却没有揭示触发降级的工具拒绝。按无障碍名称进行模糊匹配，还可能误把提示词回显或后代文本当成预期结果。
- **2026-08-08 · 统一 GitHub 标签分类体系**：PR（Pull Request）标签回答两个相互独立的问题：工作带来哪一类变更，以及会对哪些持久的仓库领域产生实质影响。混用这两个维度，或同时保留同义的无前缀标签与带命名空间的标签，都会使查询含义模糊；封闭的领域清单则会迫使新领域归入不准确的类别。
- **2026-08-08 · 自动组合翻译配对记录**：一份双语一致性记录包含两侧 Markdown 文件的精确 blob hash。因此，当两个分支分别更新同一已确认配对的不同部分时，即使 Git 能干净合并两侧 Markdown 文件，记录中的两行 hash 仍会发生冲突。选择任一侧都会留下陈旧 hash；手工重新生成记录则会重复执行一项确定性操作，并阻止本可自动完成的合并。
- **2026-08-08 · 轻量化日常文档翻译**：日常双语编辑会自动选用完整的翻译 skill（技能）。即使经过基于简报的更新优化，一次小的文档改动仍可能加载专用工作流、生成简报、把行文翻译委派给 subagent，并另行执行一轮核验。这种编排耗费的时间、上下文和模型 token 比直接翻译改动文本本身还多，而且 skill 的自动发现机制还会在普通文档处理轮次中暴露该工作流。
- **2026-08-09 · verify-md-links 校验 fragment 锚点，消除最后一类死链**：`verify-md-links` 只证明相对链接的目标文件存在，从不检查 `#fragment`，文档标准以一条人工规则补偿：重命名标题前自己 grep 锚点。一次语料扫描发现 15 条链接的 fragment 在目标中没有对应锚点——三种衰变模式：链接写下后标题被改写（`#security-and-authority-are-explicit-non-go…
- **2026-08-09 · 仅使用 Oxlint 的修复工作流**：仓库 linter 迁移保留了一次仅用于格式化的 ESLint 调用，因为当时认为 Oxlint 的 JavaScript 插件桥接层只能用于校验。固定版本的 Oxlint 工具链能够执行 `@stylistic/eslint-plugin` 提供的安全修复，因此单独的格式化器重复引入了配置边界、命令启动过程…
- **2026-08-09 · 具体表述说明执行者和记录的事实**：仓库行文使用了抽象的类别名称，但读者需要知道的具体事实各不相同。同一个名称可能指替换操作引用的早期事件 seq、生成消息的提供方和模型、提供上下文的调用方、提供某行配置的文件，或构建某个二进制文件的 CI 任务。读者必须查看代码，才能知道句子承诺的是哪项事实。
- **2026-08-09 · 将中文 contract 术语统一为「约定」**：中文文档对英文 `contract` 的译法在「契约」与「约定」之间不一致，有时甚至出现在同一文件或段落中。术语表规定使用「契约」，而经过评审的增量复校选择了更符合工程语境的「约定」。若术语表与语料继续分裂，无论选择哪一种译法都会违反仓库术语规则，后续翻译也会再次引入分歧。
- **2026-08-09 · 引用已提交的产物，绝不引用设计会话序号**：大型设计与评审会话会留下工作速记：决策序号、审计条目代号、计划章节编号、任务与栈序号、评审人裁定。这些速记在会话 transcript（文本记录）还开着时读起来顺理成章，一旦关闭就什么也解析不到。一次全仓库审计发现该模式集中在 `packages/client`：裸写的 `(decision 12/16/19/20/21)` 引用中只有决策 21 有已提交的归…
- **2026-08-10 · 三条独立序列的私有 NPM 发布**：这个仓库有三组互不相干的可发布包，却没有任何发布通道把它们送上 registry。
- **2026-08-10 · 把 vendored Cordis 重命名进 @deepseek-ai scope**：`vendor/` 下的九个包此前保留上游 npm 名（`cordis`、`cosmokit`、`schemastery`、`@cordisjs/plugin-*`）。这个前提在发布时不成立：每个 harness 包都把 `cordis` 声明成 peer dependency…
- **2026-08-10 · 由事件直接指定的 PR 评审状态命令**：Issue 所在 Project 中的状态记录了解决工作的下一步由谁负责。PR（Pull Request）的汇总评审状态可以回答 GitHub 是否认为该 PR 可合并，却无法表示这次交接：作者修复代码并重新请求评审后，先前的 `CHANGES_REQUESTED` 评审仍可能继续生效。
- **2026-08-11 · Python 公开发布工作流**：Python SDK 由一个平台无关的客户端 wheel 包和三个原生运行时 wheel 包组成，它们必须使用同一版本，并作为一组可安装。public PyPI 上传会立即公开包元数据和文件，无法替换已上传的同名文件；如果精确版本的运行时依赖尚未到达，还会产生暂时不可用的 SDK。私有仓库需要在不向外发布任何产物的情况下，执行完整的原生构建与验证流程。
- **2026-08-12 · 文档站导航与仓库 chrome**：参考侧边栏把 43 个子系统页排在了所有其他分组之前：VitePress 配置中的 `sectionOrder` 既没有为子系统分组、也没有为承载 Python SDK 页的分组声明位置，`indexOf` 返回 `-1`，于是它们排到了所有已排序分区的前面。点击 `参考` 导航项落在架构页，而该页自己的侧边栏条目是 62 条中的第 44 条…
- **2026-08-12 · 用文件名标明 client 测试的编译面**：`packages/client/*/tests/` 同时存放两个编译面的测试。多数覆盖某个 Client 包的浏览器半边，属于 `tsconfig.client.json`；少数覆盖拆分包的 Host 半边——载体的 node 半边 spec——只能在 `tsconfig.host.json` 里类型检查…
- **2026-08-13 · 按发布序列区分 npm access:vendored 框架与 native 包公开发布**：access 是每条发布序列的属性,不是整个 scope 的属性:
- **2026-08-13 · 校验已发布文档的 fragment**：`docs:build` 及其 MPA 变体会在 VitePress 生成 `website/.dist` 后运行 `verify-doc-site-fragments`。该校验器解析每个生成的 HTML 页面，按照 VitePress clean URL 解析每个内部 fragment 链接…
- **2026-08-17 · README 资产通过专用仓库发布**：公开中文 README 嵌入了 3 张社区二维码。使用仓库相对路径时，每次替换都依赖源码变更以及独立的公开仓库发布流程，即使图片字节并未改变产品代码或文档文字。
- **2026-08-18 · 单 job 分区覆盖率**：原生 Windows 覆盖率是拉取请求完整清单中反馈最慢的路径。把插桩套件保留在单个 Vitest 进程内并只使用 1 个 worker，可以避开较大进程内 worker 池曾出现的 worker 丢失和 Node 24 CJS lexer 故障，但一次失败可能超过 14 分钟才会显现，而且门禁调度器会在子进程结束前扣住输出。
- **2026-08-18 · 双语文档链接本地化**：目标属于活跃双语语料时，仓库相对文档链接跟随源文件 locale：英文源使用目标 `.md`，中文源使用其 `.zh.md`。两侧保持相同的语义目标以及完全相同的 query/fragment 后缀。该范围内缺少对侧属于配对完整性错误，不得回退；外部 URL、图片、纯页内 fragment 与范围外目标保持不变。语言切换行是显式跨 locale 例外。
- **2026-08-20 · Agent Note：文档站的纯 Markdown 孪生页与 llms.txt**：`vitepress build` 结束时向构建输出发射每个已发布路由的纯 Markdown 孪生页。`emitRawMarkdownPages` 复用填充 `website/.generated/` 的同一趟 manifest 加投影器流程，但以原始页面内容写入 `/`：不带 `editSource`/`outline` 投影 frontmatter…
- **2026-08-21 · Agent Note：文档站从发布 tag 发布**：`docs-pages.yml` 只声明 `workflow_dispatch`，并从 `dsh-v*` tag 发布，这正是 `release-publish.yml` 为 npm 采用的结构：发布是从发布 tag 出发的显式动作，绝不作为拉取请求检查出现。
### 25.6 已实现 · 测试（14 条）

- **2026-06-11 · 对协议形态代码进行基于属性的测试**：属性测试套件首次运行即发现了 BlockAssembler 重复 `block-end` 的真实 bug。
- **2026-06-19 · ACP 快照测试——一次录制 / 确定性回放**：单元测试不会覆盖组装后的完整 agent（智能体）子进程及其 ACP（Agent Client Protocol）自动化协议格式，而真实 API 测试不具确定性且受密钥门控。因此，即使单元测试覆盖率检查通过，Loader 接线、后端行为和协议输出仍可能回归，默认导出事故复盘（postmortem）已经证明了这一点。
- **2026-06-19 · 在 CI 中对外部 DeepSeek API 运行真实 API e2e 测试**：根据策略，harness 高度依赖真实 API 测试：docs/testing.md 指出，无密钥套件证明的是管线，而非产品；ACP（Agent Client Protocol）inject 事故复盘（postmortem）则是常设证据——178 项无密钥测试保持绿色时，真实 ACP 客户端会话却立即崩溃。…
- **2026-06-22 · 嵌套 agent 的逐会话快照回放**：快照层（`pnpm run test:snapshot`）会启动真实 `acp-agent` 子进程，通过 `dsh-llm-replay` 回放已记录会话，并将规范化后的自动化协议输出 + 重新持久化的会话日志与已提交预期输出进行 diff。大多数场景通过这条真实进程边界测试组装后的后端行为。
- **2026-06-22 · 持久化 seed 边界以确保 fork 子会话回放正确路由**：逐会话快照回放 Agent Note使快照层能够表达嵌套 agent（智能体）形状：一个父项加上每个进程内 subagent 的一份记录日志，每份日志都按调用会话作为键，以独立脚本回放。它曾指出（§ 范围，最后一个项目符号），fork 快照「只是未来很容易添加的一项，并非键控缺口」。这一判断对 fork 子会话而言是错误的——问题不在键控，而在*脚本派生*。
- **2026-07-22 · 让受支持平台的测试聚焦语义**：单元测试与覆盖率测试套件会在 Windows、macOS 和 Linux 上运行，但平台无关行为可能被平台特有的 fixture（测试前置数据）掩盖。字面 POSIX 路径在 Windows 上会变成相对于驱动器的路径；带主机名的 `file:` URI 在 Windows 上可能是有效的 UNC 路径…
- **2026-07-24 · Web GUI 的无密钥浏览器 e2e 车道**：Web GUI 以一条真实组装链交付——chromium 页面 → client 插件 bundle → HTTP 单次 RPC + 两条 SSE（Server-Sent Events）流 → `toFetchHandler`/apiproxy → host 端的 agent loop（智能体循环）、工具与 JSONL 持久化——却没有任何测试无密钥且确定性地…
- **2026-07-25 · 可脚本控制的 LLM 协议层故障服务器**：适配器单元测试使用本地 HTTP 服务器对各类提供方故障逐一分类，重试测试则使用进程内的脚本化 `LlmAdapter` 证明已关闭步骤的恢复能力。这两个边界都无法提供可复用的服务器，以便同时运行交付版本的 HTTP 适配器、agent loop（智能体循环）和重试策略；开发者也无法仅修改现有应用的 base URL 与 API key…
- **2026-07-30 · Web 浏览器预期输出的必需 CI 门禁**：无密钥 Web 浏览器 e2e 车道只由本地 `pnpm run test:web` 运行，PR CI 不比较 `apps/web/tests/snapshots/**/*.expected.md`。因此，改变用户可见 Web 输出的 PR 可以在漏刷预期输出时保持绿色；后来任意分支显式运行 `DSH_SNAPSHOT=refresh`…
- **2026-07-30 · 在 Vitest 中将浏览器存储交由 jsdom 管理**：受支持的 Node 版本范围包含会预留进程级 `globalThis.localStorage` 的版本。未设置 `--localstorage-file` 时，Node 26 将该属性暴露为 `undefined`；Vitest 检测到这个预留键后，不会用 jsdom 的隔离 `Storage` 对象覆盖该属性。因此，组件测试套件尚未验证产品行为便会失败…
- **2026-08-03 · 推理（reasoning）分片的逐帧累计发布与浏览器压力验证**：长推理流会连续产生大量 `assistant/chunk`。这些原始事件必须逐个完成排序、日志记录和 `PartialAccumulator` 折叠，以保持重放保真度和最终内容完整；但 React 只需要看到当前累计结果，不需要观察同一浏览器帧内的每个中间态。
- **2026-08-12 · 必需的 Python 运行时拉取请求验证**：普通拉取请求 CI 会针对 fake 运行时对端执行完整的 Python SDK pytest 套件，而 Node 快照使用不同的客户端与预期输出。真实 Python 客户端、打包后的 JSON-RPC 可执行文件、exe 专用快照、发布形态 wheel 包与干净安装只在可选的单文件可执行程序工作流或 Python 发布工作流中汇合。因此…
- **2026-08-13 · Agent Note：Python 极简组合的模型可见快照**：Python 通道从未比对极简组合实际展示给模型的内容。动态运行时上下文以 user 消息进入历史，因此 mock 模型"system 角色消息等于部署 persona"的断言看不见它；而进阶可执行文件快照会把每个请求头中已组装的系统提示词换成占位符、把每个工具 schema 换成其名称。…
- **2026-08-18 · Agent Note：会话快照 envelope 投影**：签入仓库的会话快照曾复制每一条正文记录的持久化 envelope。普通行的单调 `seq` 与墙钟 `time`，以及打包行的 `seq0` 与 `time0`，会让一次局部事件插入重新编号或计时后面的大段内容，即使其 payload 完全没有变化。这些字段是运行时持久日志所必需的，但在快照中反复出现会让评审 diff 主要描述存储机制，而不是行为变化。
### 25.7 提案 · 架构（10 条）

- **2026-06-16 · 事件词汇的运行时 schema（Zod 与 merge-extensible-map 模式之辩）**：harness 将其核心词汇——内容块、消息来源、结束原因、轮次触发器、轮次结束原因与会话事件——建模为 **merge-extensible map**：一个 TypeScript `interface`（如 `SessionEventMap`、`ContentBlockMap`），插件通过声明合并对其扩展…
- **2026-07-19 · 工具可达能力 seam 中的必填取消**：已经实现的工具注册表取消约定让每个工具主体中的 `exec.signal` 成为必填值，但许多从这些工具主体可达的异步能力接口仍接受可选信号。因此，工具可以满足自身类型，却在下一次同进程调用时意外丢失取消。
- **2026-07-24 · 领域 KV 存储能力 seam 与 workspace 实体**：host 侧唯一的持久化面是 session 事件日志（`packages/session/session-persistence`：仅追加、一 session 一文件）。凡是"不属于某个 session"的信息就没有落盘处，眼下有两个真实需求：
- **2026-07-25 · Client Settings、Locale 与 Theme 分层**：浏览器端已有的 Settings 直接写在 Sidebar 内，语言和主题也由组件本地状态直接改 DOM。这使 Settings 无法由独立插件扩展，偏好状态没有稳定的跨插件服务约定，主题注册表同时承担状态与呈现职责。
- **2026-07-27 · 会话投影与命令生命周期日志记录**：三个在途的 web 功能——todo（#497）、goal（#527）、plan mode（#587）——都要从会话日志推导按会话的状态并呈现到浏览器客户端，而三者各自发明了一套同样的机制：
- **2026-07-28 · 存储根目录落点与派生介质恢复**：持久投影缓存（决策记录，已作为 `dsh-session-projection-cache` 落地）暴露了它所依托的存储基座的两个缺口。二者都是 domain-KV 栈（设计）的属性而非缓存自身的问题，且都首先咬到缓存——因为它是这条栈上第一个*派生*介质。
- **2026-07-29 · 在会话索引中记录最后活动**：一个冷会话（已持久化、未附加）对「用户上次是什么时候在这里发出 prompt」没有权威的已存储答案。`dsh-host-apiproxy` 从可选 projection cache 的 `lastPromptAt` 提供 `updatedAt`，缺失时回退到 `createdAt`，Web 客户端按该值为 Session 树排序。…
- **2026-08-08 · Cordis Host/Client 动态插件运行体系**：模型需要在不修改仓库源码、不重新构建应用、不刷新浏览器的前提下，临时扩展当前 DSH 进程。扩展既可能运行在 Host 的 Node.js 进程，也可能运行在 Client 浏览器页面，还可能由 Host 取数、Client 展示，共同组成一个插件。
- **2026-08-08 · composer 链选举的语义阶段**：浏览器的 `conversation.composer` 链先按一个全局数值 `priority` 对所有候选项排序，再选出第一个返回匹配项的选择器。问题采用默认优先级 `0`，审批采用 `1`，一次性或父级不可用时使用的只读 subagent composer 采用 `-10`。因此，选中一次性 subagent 历史记录后…
- **2026-08-10 · 将简单的一元 API Proxy 调用迁移到业务 Remote 服务**：Host API Proxy 仍承载许多一元方法。这些方法的实现仅执行服务查找、参数投影、一次业务调用和响应投影。尽管 Typert Remote 调用已经允许业务包承载此类调用，这种做法仍会在业务服务、API Proxy 接口、Zod schema、路由表、客户端 stub 和 Client 调用方之间重复定义同一约定。
### 25.8 提案 · 功能（4 条）

- **2026-06-30 · 工具执行前输入重写——一致性设计**：拦截扩展点 Agent Note 将 `tools/pre-execute` 定义为一道针对执行的允许/拒绝/询问门禁，此时执行的身份标识已受保护、参数已被深度冻结。Claude Code 的 `PreToolUse` 钩子还提供了 `updatedInput`，因此忠实的桥接需要一个显式的重写机制。…
- **2026-07-06 · 可回溯压缩：索引检查点、状态检查点与会话内历史回溯**：压缩（compaction）对模型的当前上下文不可逆。模型看到的摘要没有指向被其遮蔽内容的引用，因为 `shadowedRange` 只存在于仅写入日志、模型不可见的 `compaction/summary` 事件上，也没有工具能让模型重新读取被遮蔽的区段。即使仅追加日志仍保存每一个字节，摘要器丢弃的内容也对模型不可用。…
- **2026-07-08 · 交互式侧会话与合并回写**：用户可能希望在不改变当前会话主上下文的前提下，探索一个来自活跃会话的问题。现有原语无法提供这种产品形态：会话存储 fork 创建的是一个未绑定的会话，而 fork subagent 是模型驱动的任务，其 transcript（文本记录）会折叠为一条工具结果。两者都不能给用户一个独立的对话，也都不能在父会话中同时记录结论和产生该结论的侧会话。
- **2026-08-04 · 用于结构化会话交互的 Task Surface**：有些任务很难通过交替发送文本消息来完成。比较多个选项、调整计划顺序、审阅表格，或填写一小组关联字段，都更适合在一次结构化交互中处理。目前，agent（智能体）可以描述这类交互，但若不增加永久的产品组件或生成可执行的客户端插件代码，就无法要求 Web 客户端渲染这类交互。
### 25.9 提案 · 缺陷修复（1 条）

- **2026-08-20 · 隔离无法读取的历史附件**：已接纳的 `ImageAttachmentRef` 会留在持久历史中，因此在被压缩替换前都会参与之后的每次请求。引用对象丢失、完整性校验失败或无法读取时，`AttachmentStore.readImage()` 会返回 `ATTACHMENT_NOT_FOUND`、`ATTACHMENT_CORRUPT` 或 `ATTACHMENT_READ_FAILED`…
### 25.10 提案 · 简化（2 条）

- **2026-07-04 · 裁剪无用的公开与结果接口**：若干包根导出、结果字段和便利方法没有生产消费方。它们之所以存活，要么是因为测试通过公开入口导入了内部实现，要么是因为某个类型预期了一个从未出现的调用者。每一项单独看都很小，但合在一起，它们扩大了 SDK 约定、生成的 catalog、文档和回归矩阵，却没有支撑任何已交付的路径。
- **2026-07-19 · 让 JSON-RPC 完成结果与传输方向单一化**：JSON-RPC 桥接层把两个端点都建模为对称的对等端，但实际协议具有固定方向。共享传输层（现为 `dsh-sdk-protocol`，由服务端与 TypeScript SDK 客户端共用，后者行使出站请求/入站通知方向）仍实现着没有任何端点使用的两个半边：服务端发起的请求与客户端发起的通知。Python SDK 发送请求并接收响应或通知…
### 25.11 提案 · 流程（7 条）

- **2026-06-11 · API extractor 报告**：文档块类型检查与事件分类体系两部分已交付（doc-sync（文档同步门禁）强制）；剩余的 API 报告部分作为独立提案被推迟。
- **2026-06-11 · 供应链检查与 vendor 漂移验证**：vendor manifest（元数据清单）（见引入 vendor 的决策）在提交时仅在*正向*强制执行（vendor 变更 ⇒ manifest 更新），但没有任何机制验证 manifest 的*声明*：即 vendor/ 确实等于上游指定 SHA 的内容加上所记录的修改。此外，少量真正的 NPM 依赖也没有安全公告监控或更新节奏。
- **2026-06-11 · 架构一致性——依赖规则与适配器套件**：目前有两项架构保证仅存在于行文中：（1）没有任何组件依赖具体的 agent loop（智能体循环）包（微内核承诺）；（2）每个 LlmAdapter 都正确遵循分片协议。二者都应由机制强制执行（质量门禁原则）。
- **2026-06-20 · 通过发现机制获取包清单，而非维护静态列表**：包与门禁清单在 TypeScript project references、包文档、CI 描述和 Knip 覆盖项中反复出现。大多数只是重述包布局、manifest（元数据清单）数据或聚合命令内容。因此每新增一个包都会产生本可避免的同步点。
- **2026-07-13 · dsh-code-review 的定期人工评审维护**：`dsh-code-review` skill（技能）记录需要评审人判断的失败模式，但一次性审计重复开展起来成本高昂，作用域也容易不一致。把每条评论都当作教训会让检查清单不断膨胀；把合并、讨论串已解决或作者回复「已修复」视为采纳证据，则会把最终代码可能并未落实的反馈提升为规则。维护流程需要足够的证据和独立评审，以便在证据不足时按不采纳处理…
- **2026-07-26 · 移除打包会话 fixture 分支迁移器**：仓库的默认写入器和快照检查会使会话 fixture（测试前置数据）保持规范打包行布局。在永久强制机制之外仍保留 `pnpm run migrate:packed-session-fixtures`，唯一原因是让携带旧版 fixture 改动的在途分支可以合并当前 `master`，并在不重新录制模型输出的情况下通过机械转换收敛。
- **2026-08-04 · 以产物为先的 NPM 基线发布**：monorepo 中可运行的源码并不能证明发布后的包可运行。workspace link、TypeScript paths、tsx 源码加载和工作树里残留的 `lib/` 都可能补上发布 tarball 中缺失的文件或依赖。即使现有构建产物测试使用普通 Node，它仍直接读取工作树中的 `lib/`…
### 25.12 提案 · 测试（2 条）

- **2026-06-11 · 变异测试作为覆盖率的制衡手段**：逐文件 100% 覆盖率门禁（质量门禁决策）证明每一行代码在测试中都被*执行*了，但不能证明如果该行出错，任何断言会注意到。在 agent（智能体）编写测试的场景下，覆盖率压力可能产出「执行但不断言」的测试。变异测试衡量的正是覆盖率无法衡量的：测试套件是否能*杀死*被刻意注入的缺陷。
- **2026-06-11 · 确定性测试、回放不变式 fixture 与竞态压力测试**：若干 agent loop（智能体循环）测试通过 `setTimeout(30)` 睡眠来同步——这是一笔不稳定性债务，浪费 agent 的重试周期，还可能掩盖时序 bug。另外，我们的核心架构承诺（任何会话日志回放后都能得到相同的派生历史）目前只在两个测试中断言，但在*所有*测试中断言的成本极低。此外，inbox 唤醒竞态只被手动验证过一次…
### 25.13 已否决 · 功能（1 条）

- **2026-07-26 · 在构建 Windows 沙箱启动器之前先评估 landstrip**：沙箱决策将 `PLATFORM_CHAINS.win32` 留空，并计划用「AppContainer/受限令牌（restricted-token）家族的一个约束运行器，按 `node-addon-landlock-run` 模板从其独立仓库发布」来填充——一个估计约 1,500 行、需要自研编写并维护的新仓库（landlock-run 子树约为 1,460 行…
### 25.14 已否决 · 简化（10 条）

- **2026-06-20 · 仅持久化组装后的 assistant 消息，不存储流式分片**：当前的规范会话日志会持久化模型流式输出的每一个 `assistant/chunk`。会话持久化 Agent Note选择这一方案是为了 token 级回放保真度和连续的 `seq`，但其代价日益增长：JSONL fixture（测试前置数据）被大量微小的增量记录占据，快照场景通过对分片事件分组来回放模型…
- **2026-06-20 · 加载时截断被中断的最终轮次**：当前的持久化约定会保留已持久写入但从未关闭的最终轮次。加载时，`interruptedTurnClosers()` 扫描尾部，为未应答的工具调用合成 error `tool/result` 事件，在步骤处于打开状态时追加 `step/end`，追加 `turn/end { kind: 'interrupted' }`，并要求后端持久提交这次修复。…
- **2026-06-20 · 将持久化接口合并进 dsh-session**：`dsh-session-persistence` 是一个 Service Definition 包，其核心概念已经由 `dsh-session` 拥有：`SessionHeader`、`SessionEvent`、`SessionId`、`session/event` 与 `session/flush`。…
- **2026-06-20 · 移除 bash 完整输出 spill 文件**：`dsh-bash-local` 在内存中保留有界的输出，并将大体量的 stdout/stderr 流写入私有临时 spill 文件。这要求一个私有目录、随机创建仅所有者可访问的文件、关闭失败处理、基于字节偏移的增量读取、有损读取报告、在面向模型的文本中渲染路径，以及清理纪律。当输出被截断时，该工具会告知模型去读取一个本地 spill 路径。
- **2026-06-20 · 移除持久化的步骤边界事件**：会话日志存储了 `step/start` 和 `step/end` 事件，尽管每个步骤级事件本身已经携带 `{ turn, step }`：assistant 分片、assistant 消息、工具调用、工具结果、用量和错误。`deriveMessages()` 忽略步骤边界，ACP（Agent Client Protocol）在 UI 层面也忽略它们…
- **2026-07-12 · 将工作流收缩至已使用的前台核心**：工作流能力在前台执行用于编排 subagent 的 JavaScript，但它同时携带了一套无人消费的进度观测系统。没有任何生产环境的监听器订阅六个 `workflow/*` 事件中的任何一个；监听器仅存在于工作流测试中。尽管如此，seam 定义了 run/phase/agent（智能体）outcome 载荷…
- **2026-07-12 · 裁剪 skill 注册表中未使用的接口**：skill（技能）服务的嵌入式运行时子系统中，`ctx.skills.register()` 没有任何生产调用方。它引入了一个保留的 `runtime` 提供方名称、一套运行时 map/rank/source、重复策略、缓存键中的第二个 revision、规范化逻辑、dispose（资源释放）函数以及相应测试——而所有已交付的 skill 都使用提供方约定。…
- **2026-07-19 · 将唯一的压缩后端并入服务包**：压缩（compaction）目前拆分在两个包中：`@deepseek-ai/dsh-compaction` 拥有一个含两个方法的抽象服务和共享类型，`@deepseek-ai/dsh-compaction-basic` 拥有唯一的完整提供方。交付配置只加载 basic 包，除了该提供方外，没有生产包独立消费 Service Definition 包。
- **2026-07-26 · 2026-07 NIH 审计否决的依赖替换**：一次仓库级的「Not Invented Here（非我发明）」审计（2026-07-26，十路并行普查，覆盖每个包分组、scripts/、native/、vendor/ 边界、python/、测试基础设施与 CI）对每一处手写接口面追问同一个问题：在依赖政策之下…
- **2026-07-26 · 用 node:timers/promises 替代手写的可取消休眠**：三个包手写了用 promise 包装的定时器，而 `node:timers/promises` 内置模块早已提供同等能力；其他包（`dsh-llm-mock-server` 的 `pause()`、`dsh-lsp-stdio`、`dsh-acp-snapshot`）已经在使用该内置模块，因此这些手写副本同时也是一处一致性缺口：
### 25.15 已归档 · 架构（14 条）

- **2026-06-11 · 使用自定义类型化工具 schema DSL 替代 schemastery**：Archived: 2026-07-26
- **2026-06-11 · 工具 schema 是系统提示词组装的一部分**：Archived: 2026-07-27
- **2026-06-15 · 每个会话事件都封闭在一个轮次内**：Archived: 2026-07-28
- **2026-06-20 · 将包重组为模块化层级结构**：Archived: 2026-07-27
- **2026-06-20 · 将示例应用提取为独立包**：Archived: 2026-07-26
- **2026-07-02 · 结果时刻的 applied-hunk diff 用于文件变更**：Archived: 2026-07-27
- **2026-07-03 · 为文件系统 seam 添加直接目录列举能力**：Archived: 2026-07-26
- **2026-07-05 · Windows 写入权限语义：继承 DACL，而非权限模式位**：Archived: 2026-07-26
- **2026-07-22 · 由 effect 持有的 TUI 交互扩展**：Archived: 2026-08-04
- **2026-07-23 · 统一会话查询服务**：Archived: 2026-07-26
- **2026-07-24 · 通过单个 Commander 适配器解析 `dsh` 的 argv**：Archived: 2026-07-26
- **2026-07-27 · dsh-tui 聊天通道模块拆分**：聊天通道内聚的子机制从 `createTuiChat` 中抽出，迁入 `src/chat/`，每个都是接收显式依赖包的工厂，而非闭包捕获入口作用域：
- **2026-07-28 · dsh 原生 TypeScript 源码启动**：Archived: 2026-08-07
- **2026-07-28 · 统一的 TUI 呈现与导航**：终端 UI 逐步积累了多套彼此干扰的呈现规则：调色板角色互为别名，或在浅色终端中颠倒强调层级；工具卡片的框架、输出和退出标记重复或争夺注意力；注入上下文被当作 XML 解析，无法可靠折叠；`/resume` 即使能通过启动器访问其他工作区，也会排除不属于当前工作区的会话。每个症状看似局部…
### 25.16 已归档 · 功能（54 条）

- **2026-06-14 · Agent Client Protocol（ACP）支持——从外部编辑器驱动编码 agent**：Archived: 2026-07-26
- **2026-06-18 · 富 ACP bash 渲染——通过 `_meta` 约定实现终端卡片**：Archived: 2026-07-26
- **2026-06-25 · ask-user 提问能力**：Archived: 2026-07-27
- **2026-06-30 · Subagent 生命周期丰富化——lastAssistantMessage（仅观察）**：Archived: 2026-07-26
- **2026-07-07 · plan mode——记录到日志的逐 agent 会话模式**：Archived: 2026-07-26
- **2026-07-07 · 会话前缀——派生历史之前的仅请求消息**：Archived: 2026-07-28
- **2026-07-08 · 重复工具调用守卫插件**：Archived: 2026-07-27
- **2026-07-09 · 由 Bash 支持的 grep 与 glob 发现工具**：Archived: 2026-07-27
- **2026-07-10 · 精确会话查询服务**：Archived: 2026-07-27
- **2026-07-14 · 可选时间上下文插件**：Archived: 2026-07-26
- **2026-07-17 · 独立的全屏 TUI 入口**：Archived: 2026-08-04
- **2026-07-20 · 启动 slogan 取代配置化的 TUI 欢迎语**：- `examples/tui-agent/cordis.yml` 不再配置 `welcome`；该配置键保留给需要固定、确定性副标题的部署与 fixture（Code Mode overlay 和所有快照/脚本化 fixture 都保留各自的欢迎语）。 - `welcome` 未设置时…
- **2026-07-20 · 在 Windows 上支持 TUI**：Archived: 2026-08-04
- **2026-07-21 · /reload 命令按需重读 loader 配置**：`dsh-tui` 增加一个**实验性、仅供开发**的 `/reload` 斜杠命令：遍历 `ctx.loader.entries()`，对每个文件后端的子树（`Include`）调用 `refresh()`——即 HMR 监听器配置变更分支所走的同一条代码路径，改为手动触发、不依赖监听器。未变化的文件是无操作（`Include.read` 做内容比较）…
- **2026-07-21 · TUI skill slash command**：`@deepseek-ai/dsh-tui` 前门拥有一条 `/skill: [instructions]` 命令。提交时它加载指定的 skill，并投递一个文本块作为用户轮次——空闲时用 `agent.send()` 发送、运行中用 `agent.steer()` 中途引导，与普通编辑器输入遵循同一规则。…
- **2026-07-21 · TUI 启动横幅品牌渐变**：Archived: 2026-07-26
- **2026-07-21 · TUI 状态行标示排队中的 steering 消息**：agent（智能体）的收件箱（inbox）才是权威的 steering 队列，但 TUI 无法观测它，因此徽标是从公开的 `agent/queued` 与 `steering/message` 事件重建出的实时计数，而非对队列本身的投影。
- **2026-07-21 · TUI 页脚展示会话缓存命中率**：页脚在 `↑ ↓` 之后追加 `cache %`，该比率是计费输入 token 中由提供方缓存承接的占比。
- **2026-07-21 · dsh 告知 agent 其自身源码所在位置**：`dsh` 启动器（`apps/cli/src/tui.ts`）从它自身的模块 URL 计算 harness 检出根目录——`fileURLToPath(new URL('../../..', import.meta.url))`…
- **2026-07-21 · 产品级 TUI 会话恢复**：`/resume` 使用 TUI 现有的交互式浮层接口，但以占满 viewport 的选择页呈现，而不是居中弹窗。这个扁平页面把搜索框、workspace 作用域行、候选项和快捷键页脚放在稳定的屏幕区域，只有当前行使用强调色。搜索编辑器紧跟搜索图标起始，并输出 pi-tui 的光标标记，因此终端输入法的组合文本会锚定在输入框中。查询非空时…
- **2026-07-21 · 从首条消息自动命名终端**：- `TuiConfig` 新增布尔字段 `autoTitle`（默认 `false`）。开启后，TUI 会在全新会话的首条用户消息之后发起一次后台模型调用，并用一个简短的、模型生成的标签替换终端标题；静态 `title` 是替换前的初值，也是兜底。 - 该标签是模型概括，而非对提示词的截断。…
- **2026-07-21 · 横幅回归，无边框**：- `HeaderComponent` 及其从左到右的扫入动画回归，但以**无边框**方式渲染：没有 `╭─╮`/`╰─╯` 边角，也没有 `│` 侧边。每一行都是一个前导空格加上经 `truncateToWidth` 裁剪的内容，因此扫入的宽度裁剪永远不会撕裂转义序列，也不绘制任何固定边框。扫入大约经过 24 帧完成，每帧间隔 15 ms。…
- **2026-07-21 · 横幅整体扫入；副标题行移除**：- 删除 slogan 库、`pickStartupSlogan` 和打字机动画。`welcome` 未设置时横幅直接**没有副标题行**——只有标题和模型/会话详情。`welcome` 配置保留给想要固定副标题的部署与 fixture，无动画、逐帧确定地渲染。…
- **2026-07-21 · 移除启动横幅**：- 删除 `HeaderComponent`、扫入动画及其生命周期接线。TUI 直接挂载进 transcript；启动时分隔线之上不渲染任何东西。 - 模型名移入页脚状态行的左段（` ↑tokens ↓tokens`），会话使用的模型因此始终可见，而不只是启动时。会话 id 不再显示——它存在于会话日志和 `./.sessions` 文件名中…
- **2026-07-21 · 自动标题默认开启，恢复时重新推导**：- `autoTitle` 默认**开启**（`z.boolean().default(true)`，`resolveTuiConfig` 以 `?? true` 与之对齐）。带有 `llm` 服务与 agent 提供方/模型的部署无需选择性开启即可在每个会话获得模型制作的窗格标题；不具备它们的部署保留静态标题，因此在调用无法运行处，默认开启是惰性的。…
- **2026-07-21 · 运行状态行展示轮次阶段与已用时长**：Archived: 2026-07-26
- **2026-07-22 · 停靠式 Web 目标条**：Archived: 2026-08-07
- **2026-07-23 · TUI 文件引用自动补全**：Archived: 2026-08-04
- **2026-07-23 · TUI 状态检查模型请求输入**：Archived: 2026-08-04
- **2026-07-23 · Trajectory 步骤单元格与轮次列表 chrome**：`@deepseek-ai/dsh-client-ui-trajectory` 拥有展示型 trajectory 列表 chrome：
- **2026-07-24 · New Session clears onto the empty-state launch**：`SessionsService.clear()` 清除持久化选中项与 `list.current`。顶层侧栏创建入口（无 cwd 的 `onCreate()`——New Session 与 New Workspace）调用 `clear()`，使 `AppFrame` 渲染 `conversation.empty`。…
- **2026-07-24 · TUI QuestionDialog 以多行方式渲染选项**：Archived: 2026-08-04
- **2026-07-24 · TUI shell 提示符编辑器**：Archived: 2026-08-04
- **2026-07-24 · TUI 提示符主题组合可变的插件值**：Archived: 2026-08-04
- **2026-07-26 · trajectory 与 waterfall 视图中的 Code Mode 子调用**：Archived: 2026-07-28
- **2026-07-27 · Assistant timing line renders after the message body**：**把标签与计时拆开；计时作为消息的末行渲染。**
- **2026-07-27 · Dim-gray pulse for the running prompt glyph**：运行字形是一种暗灰色，在回合开始时淡入，运行期间持续脉动，回合结束后淡出，随后恢复为普通的 `>` 光标。它从不使用强调色。
- **2026-07-27 · Fixed `Tool / <name>` header for tool-call cards**：表头是固定的 `{ring} Tool / ` 框架，采用单一扁平的状态色——不加粗、不加下划线、不变暗——因此整行的颜色保持一致。`Tool` 是字面常量；`` 是原始工具名。分隔符是 ASCII 的 `/`。环形标记在调用挂起时为 `○`，落定后为 `●`…
- **2026-07-27 · 用户消息气泡下方的 IconActions**：Archived: 2026-07-27
- **2026-07-28 · `dsh meta` 以 harness 检出为 workspace 启动 TUI**：`dsh meta` 在任意目录下都以 harness 检出为 workspace 启动普通 TUI。
- **2026-07-28 · `dsh migrate`/`dsh upgrade` 以 skill 播种首轮**：Archived: 2026-08-03
- **2026-07-29 · TUI 隐藏模式把一个轮次的 assistant 步骤折叠为一条消息**：Archived: 2026-08-04
- **2026-07-29 · Web 消息 IconActions 与时钟**：Archived: 2026-08-07
- **2026-07-29 · 为待处理队列项提供编辑与移除操作**：Archived: 2026-07-31
- **2026-07-30 · Web composer stats detail and input-zone polish**：**统计行经由新的 `footer` owner prop 渲染进 InputBar 的宽度列内，并扩展为设计稿的分组细节行；composer stack 拥有唯一的 6px 节奏；座位以固定 36px 的 token 绑定渐变淡出消息流；「回到底部」控件跟随实时的 `--dsh-composer-height`…
- **2026-07-30 · Web 上下文注入展开项**：Archived: 2026-08-07
- **2026-07-30 · dsh --dump-config 打印合成后的配置树**：`dsh --dump-config` 和 `dsh web --dump-config` 把合成后的条目列表——基础配置、界面覆盖层、再叠 `--config` 或个人覆盖层，恰好是该界面启动时组装的那些层——以 YAML 打印到 stdout 后退出，不启动任何东西。…
- **2026-07-30 · 版本化 TUI 首次运行欢迎页**：Archived: 2026-08-03
- **2026-07-30 · 用于 transcript 细节状态的 /details 命令**：`dsh-tui` 在其他 agent 作用域命令旁注册 `/details`。裸 `/details` 打开 `DetailsDialog`：一个居中的键盘开关，每个维度一个条目——`Tool cards` 与 `Reasoning`——显示实时值：Tab 循环高亮条目并立即应用变更，对话框背后的 transcript 即是预览…
- **2026-07-30 · 终端中的实时独立压缩进度**：Archived: 2026-08-04
- **2026-07-31 · 卡片工具行通过同一个 ToolRow 折叠**：Archived: 2026-08-07
- **2026-07-31 · 实验性子命令由 `--experimental` 或 `DSH_EXPERIMENTAL=1` 把守**：`dsh experimental-meta` 改为 `dsh meta`，`dsh experimental-upgrade` 改为 `dsh upgrade`。二者只有在调用时传入各自的 `--experimental` 标志、或环境中带有 `DSH_EXPERIMENTAL=1` 时才会运行；否则命令在 stderr 上明确报错并以退出码 1 结束…
- **2026-07-31 · 悬浮卡片激活时复制主要值**：Archived: 2026-08-07
- **2026-08-08 · `dsh run` 负责一次性 headless 执行**：Archived: 2026-08-10
### 25.17 已归档 · 缺陷修复（19 条）

- **2026-07-20 · 保证 Code Mode 结果卡片内容完整**：Archived: 2026-07-26
- **2026-07-22 · 侧边栏折叠后保留控制栏**：Archived: 2026-07-26
- **2026-07-23 · TUI 通用卡片的 Markdown 渲染**：TUI 先用共享的 Markdown 主题渲染通用卡片的结果内容，再应用卡片的头尾行数限制。终端卡片和 diff 卡片保留各自专门的纯文本渲染器；通用卡片的原始输入仍按字面显示，因为它代表的是工具参数，而非展示器撰写的行文。
- **2026-07-23 · demo:web 构建客户端插件的打包产物**：`demo:web` 在 `npm run build:web` 之前先运行 `npm run build`，使插件的 `lib/client.js` 打包产物在 `dsh web` 提供它们之前已经存在。…
- **2026-07-23 · thinking 行使用单一展开目标**：Archived: 2026-07-26
- **2026-07-24 · TUI 为每种轮次结束 kind 呈现原因**：Archived: 2026-08-04
- **2026-07-26 · Intent draft echoes in the same tick**：`SessionManager.updateIntent` 在 `updatePendingPrompt` 之后调用 `this.notifier.notifyNow()`，从而在与变更事件相同的 tick 内刷新列表快照。这符合 Notifier 的通道规则：当某个用户手势的受控输入正是从该快照渲染时，对它的直接回显使用 `notifyNow`…
- **2026-07-27 · TUI diff 卡片重复打印文件路径**：`diffLines` 新增 `showPath` 参数；当一个 diff 卡片只有一个 diff、且生效标题（`resultView?.title ?? callView.title`）已包含该 diff 的路径时，`ToolCardComponent.renderBody` 抑制每文件表头。多文件 diff 卡片保留全部每文件表头。…
- **2026-07-27 · TUI 步骤计时跟在该步骤最后一条消息之后**：Archived: 2026-08-04
- **2026-07-27 · 工具卡片的单行字段以内联方式渲染**：单行卡片字段改用 `displayInlineText`（将 `\n` 转义为字面量 `\x0a`）而非 `displayText`：包括卡片标题、terminal 卡片的 `description` 与 `cwd` 元数据行，以及待执行的 `$ ` 回显。每个字段都严格保持在一行内，因此多行命令不再会换行并与相邻行冲突。…
- **2026-07-27 · 提问 composer 的选项行是滚动内容，而非空间不足时的吸收方**：Archived: 2026-08-07
- **2026-07-28 · Web 对话 UI 视觉优化**：Archived: 2026-08-07
- **2026-07-30 · TUI 模型上下文解析在适配器注册竞争时延后重试**：TUI 模型控制器把上下文窗口解析中的 `NO_ADAPTER` 拒绝视为瞬态状态而非错误：静默搁置这次解析，并在下一次 `llm/adapters-updated` 提交时重新解析——这是 `LlmService` 本就在每个路由提交点发出的无载荷注册表通知。若某次提交仍缺少该路由，等待会被再次搁置，因此无关的拓扑变化保持沉默。…
- **2026-07-30 · Web 详情栏默认关闭**：Archived: 2026-08-07
- **2026-07-31 · TUI diff 上下文行保持中性**：Archived: 2026-08-04
- **2026-07-31 · 空白会话打开期间保持 hero 可见**：Archived: 2026-08-07
- **2026-08-03 · TUI 长会话渲染开销：共享步骤耗时扫描与卡片行缓存**：Archived: 2026-08-04
- **2026-08-10 · 网页图标随配色方案切换**：Archived: 2026-08-10
- **2026-08-12 · 收起侧栏的上方控件共用同一进入动画**：轨道落位时，四个 36px 上方控件从同一个左对齐布局开始，共用一段 `150ms` 动画，从 `translateX(49px)` 移动到最终 10px 内边距。外壳把位移分别应用于侧栏切换、新建会话，并只对 Workspace 区域应用一次，因此添加与搜索会继承同一路径，不产生嵌套变换。透明度使用同一条动画时间线。
### 25.18 已归档 · 简化（28 条）

- **2026-06-20 · 从持久化 seam 中移除无用方法**：Archived: 2026-07-26
- **2026-06-20 · 移除未被消费的 LLM 组装便捷接口**：Archived: 2026-07-26
- **2026-06-20 · 移除未被消费的 `llm/adapter-change` 事件**：Archived: 2026-07-26
- **2026-07-02 · 停止将 token 流镜像为 agent 事件**：Archived: 2026-07-27
- **2026-07-04 · 从 fs seam 中移除只写字段与一个无效的路由旋钮**：Archived: 2026-07-26
- **2026-07-04 · 共享应用 bin 的启动胶水代码，而非维护两份副本**：Archived: 2026-07-26
- **2026-07-04 · 将 stdio UI 辅助模块折入 stdio 应用**：Archived: 2026-07-26
- **2026-07-04 · 移除 `GenerateOptions.prefill` 与 `ToolSchema.strict`——无端到端可用路径的请求旋钮**：Archived: 2026-07-26
- **2026-07-04 · 移除 `agent/steering` 镜像 emit**：Archived: 2026-07-26
- **2026-07-04 · 移除 `image` 内容块，直到有路径能真正处理它**：Archived: 2026-08-19
- **2026-07-04 · 移除未被消费的 web 观测接口——`providers-change` 事件与 status 方法**：Archived: 2026-07-26
- **2026-07-04 · 裁剪不可达的 ACP 桥接层表面——品牌配置项与 kind 嗅探回退**：Archived: 2026-07-26
- **2026-07-04 · 裁剪无生产者的词汇变体（块缓存提示、`agent` 消息来源、`continuation` 轮次触发器）**：Archived: 2026-07-26
- **2026-07-12 · 移除无消费方的 skill 提供方事件**：Archived: 2026-07-26
- **2026-07-12 · 裁剪 web seam 中未使用的字段**：Archived: 2026-07-26
- **2026-07-19 · 撤销独立的 subagent mock 包**：Archived: 2026-07-26
- **2026-07-19 · 每个会话只使用一个表层管理器**：Archived: 2026-07-26
- **2026-07-20 · 退役 readline 前端与 repl-agent 示例**：Archived: 2026-07-26
- **2026-07-21 · Drop the TUI `/cancel` slash command**：`/cancel` 已移除。取消运行中的轮次是一项仅由键位绑定提供的能力（`Esc`，或运行中的 `Ctrl+C`），状态行提示与 `/help` 快捷键清单已对其作出说明。`baseCommands` 自动补全条目、`/help` 命令行、编辑器提交处理函数中的 `case '/cancel'` 分支…
- **2026-07-21 · Ship the TUI without `todo_write`; keep it a one-line opt-in**：tui-agent `cordis.yml` 不再加载 `tool-todo`；`todo_write` 改为可选启用。`code-mode.cordis.yml` 覆盖配置继承基础组合，因此它生成的 SDK 同样不再包含 `todo_write`。…
- **2026-07-22 · TUI 标题来自 session-title 服务**：Archived: 2026-07-27
- **2026-07-26 · 把门禁脚本统一到已有依赖与内置模块上**：Archived: 2026-07-27
- **2026-07-26 · 用 eventsource-parser 替换 llm-deepseek 中手写的 SSE 解析器**：Archived: 2026-08-07
- **2026-07-26 · 用 turndown 替换 tool-web 的正则 HTML 转 markdown 转换器**：Archived: 2026-08-07
- **2026-07-27 · 无 gutter bar 的可复制 TUI transcript**：transcript 不再带任何逐行前缀。消息仅通过以角色色渲染的粗体带下划线角色标题和空行分隔，而这两者本就由终端在每个块前后自动插入。下划线让每个角色获得清晰的视觉分带，且无需背景填充，因此在任何终端配色下都可读，也绝不会进入剪贴板：
- **2026-07-30 · 侧边栏缩放不显示胶囊**：Archived: 2026-08-07
- **2026-07-31 · Web UI 去掉 steer 入口与插话 chrome**：Archived: 2026-08-07
- **2026-08-03 · 显式配置的 dsh 入口**：Archived: 2026-08-08
### 25.19 已归档 · 流程（20 条）

- **2026-06-11 · Doc-sync 强制**：Archived: 2026-07-26
- **2026-06-11 · 使用 tsdown 替代 dumble 进行 JS 打包**：Archived: 2026-07-27
- **2026-06-20 · 生成的 Cordis 事件与服务目录**：Archived: 2026-08-07
- **2026-07-03 · 面向维护者与 SDK 用户的文档关系图索引**：Archived: 2026-07-26
- **2026-07-04 · 生成式持久化日志事件目录**：Archived: 2026-07-27
- **2026-07-04 · 针对 Cordis 对外服务接口的 JSDoc 完整性门禁**：Archived: 2026-07-27
- **2026-07-06 · 并行 GitHub CI 门禁**：Archived: 2026-07-26
- **2026-07-06 · 生成式插件配置目录**：Archived: 2026-07-27
- **2026-07-17 · 在 CI 中从构建后的 lib 运行示例**：Archived: 2026-07-27
- **2026-07-20 · 生成 Cordis 核心 API 参考文档**：Archived: 2026-07-27
- **2026-07-21 · doc-sync 走门禁调度器**：Archived: 2026-07-26
- **2026-07-22 · `docs/cordis-tutorial` 下的 Cordis 实操教程文档**：Archived: 2026-07-27
- **2026-07-22 · 在检出目录内运行时安装脚本跳过克隆**：Archived: 2026-07-26
- **2026-07-23 · 个人集成分支维护 skill**：Archived: 2026-08-10
- **2026-07-23 · 浏览器演示 GIF 录制**：Archived: 2026-07-26
- **2026-07-26 · GUI PR 的 GIF 证据与 assets 分支发布**：Archived: 2026-07-27
- **2026-07-27 · 在 Linux runner 上用 Wine 运行 Windows 阻断门禁**：Archived: 2026-08-08
- **2026-07-31 · 安装器把已有检出接管进受管布局**：检出内模式仍然绝不克隆、绝不修改工作树，但现在它会无条件地把该检出**接管**进受管布局。不存在退出选项：一套布局服务于所有安装。
- **2026-08-04 · PR 到 Issue 的状态仅向前投射**：Archived: 2026-08-10
- **2026-08-08 · 由评审驱动的 Issue 生命周期触发器**：Archived: 2026-08-10
### 25.20 已归档 · 测试（8 条）

- **2026-06-20 · 使用 `session.jsonl` 作为唯一的快照会话日志产物**：Archived: 2026-07-26
- **2026-06-22 · 记录 fork 与混合 spawn+fork 快照场景**：Archived: 2026-07-26
- **2026-07-04 · 将 acp-agent 回放配置改为单一来源**：Archived: 2026-07-26
- **2026-07-04 · 钩子快照矩阵——覆盖两种 bridge 的端到端预期输出测试**：Archived: 2026-07-26
- **2026-07-06 · 在单个快照场景中固定请求头内容**：Archived: 2026-07-26
- **2026-07-08 · 将 ACP 快照套件提取为支持包**：Archived: 2026-07-27
- **2026-07-18 · TUI 语义终端状态快照**：Archived: 2026-08-04
- **2026-07-26 · 采用 execa 替换手写的测试子进程管道代码**：Archived: 2026-08-07

---

## 26. 文档清理后的仓库约定

本次收敛后：

```text
人工维护技术文档
└─ README.md                         唯一主技术文档

保留但不视为人工技术文档
├─ .agents/skills/**/SKILL.md        仓库维护技能
├─ apps/cli/config/**/SKILL.md       运行时内置技能
├─ **/snapshots/**/*.md              测试快照
├─ **/fixtures/**/*.md               测试夹具
├─ THIRD_PARTY_NOTICES.md            第三方许可证通知
└─ LICENSE                           许可证
```

本次从整理前的仓库树中清理分散人工 Markdown 共 **2310 份**，包括旧双语 `docs/`、包级 README、`.agents/notes/` 决策树、旧贡献者说明、旧示例/原生/Python README 等。`.agents/notes/` 的配对元数据与归档清单也随整棵历史文档树一并清理，避免留下无正文的元数据残骸。

今后新增公共能力时，优先更新源码类型/测试，并在本文件相应章节维护说明；不要重新恢复大规模分层 Markdown 文档体系。

---

## 27. 快速排错

### Web 能打开但模型不能回答

检查模型提供方、模型 id 和凭据。文件、Shell、终端、MCP、语言服务、会话管理本身不应因此失效。

### TypeScript / JavaScript 语言服务不可用

```bash
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
```

并确认 `apps/cli/config/workbench/typescript-language-server.mjs` 存在。

### C / C++ 语言服务不可用

```bash
command -v clangd
export DSH_CLANGD_BIN="$(command -v clangd)"
```

### MCP 不能读取目录

```bash
echo "$DSH_WORKBENCH_MCP_ROOT"
```

重新设置为实际项目根目录再启动。

### 怀疑工作台改造文件缺失

```bash
pnpm run workbench:doctor
```

### 依赖异常

```bash
pnpm install --frozen-lockfile
```

不要在锁文件之外随意升级关键依赖后继续复用旧构建产物。

---

## 28. 最终维护原则

> **工作台就是正式源码；技术说明只有这一份。**

新增能力直接进入正式源码目录、正式依赖、正式预设或正式组合；文档只在本文件维护系统级解释。运行时技能与测试 Markdown 继续作为程序资产存在，但不再复制一套说明文档。
