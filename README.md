# 工作台（Workbench）

> 基于 DeepSeek Harness `0.1.1-rc.2` 完整源码深度改造的本地执行工作台。
>
> 当前工作台版本：`1.0.0`  
> 上游版本：`0.1.1-rc.2`  
> 上游基线提交：`b150a551b8d465e31e418e1b2eaf5e79bbb7d28e`  
> 默认预设：`workbench`  
> 架构：源码内生集成（`native-source-integrated`）

---

## 1. 这是什么

本仓库已经不再是“原版 DeepSeek Harness + 外置补丁”。

工作台所需的预设、技能、持久终端、语言服务、本地文件 MCP、会话全文索引、代码执行模式、验收脚本与永久离线构建流程，已经直接进入 Harness 正式源码树。

拉取 `555` 后，仓库根目录本身就是完整、可开发、可构建、可运行、可继续修改的工作台源码。

```text
555/
├─ apps/
├─ packages/
├─ native/
├─ python/
├─ vendor/
├─ website/
├─ scripts/
├─ docs/
├─ package.json
├─ pnpm-lock.yaml
└─ .github/workflows/
```

工作台已经彻底删除旧式旁挂结构：

```text
workbench-src/   不存在
.workbench/      不存在
overlay/         不存在
```

`pnpm run workbench:doctor` 会主动检查这些旧目录，如果重新出现会直接判定验收失败。

---

## 2. 设计目标

工作台的定位不是再造一个单独的模型聊天客户端，而是提供一套稳定、可持久化、可扩展的本地执行底座。

核心目标：

1. **完整源码可控**：所有改造都在正式源码树内，可直接审计和继续开发。
2. **模型与执行解耦**：文件、终端、构建、语言服务、MCP、会话、索引等本地能力不依赖模型密钥。
3. **长期项目可恢复**：工作区、会话、全文索引、任务记录和离线产物形成完整恢复链。
4. **安全边界明确**：本地文件 MCP 默认只开放指定工作目录；外部程序按需启用。
5. **修改必须验收**：仓库内置源码医生、完整构建、冷启动与永久产物工作流。
6. **不依赖外置覆盖层**：换环境只需要这份仓库或永久离线包即可重建。

---

## 3. 当前能力总览

```text
工作台
├─ 工作区与会话
│  ├─ 工作区管理
│  ├─ 会话创建 / 历史 / 搜索 / 分叉
│  ├─ 文件引用与附件
│  ├─ 会话投影与统计
│  └─ SQLite 全文索引
│
├─ 本地执行
│  ├─ Bash
│  ├─ 文件读取 / 写入 / 编辑
│  ├─ glob / grep 搜索
│  ├─ 后台任务
│  ├─ 持久终端
│  └─ Code Mode / run_code
│
├─ 代码理解
│  ├─ TypeScript / JavaScript 语言服务
│  └─ C / C++ 语言服务（可选 clangd）
│
├─ MCP
│  └─ localfs 本地文件服务
│
├─ 技能
│  ├─ workbench-ops
│  ├─ repo-review
│  ├─ repo-quality-gate
│  ├─ docs-quality
│  └─ task-journal
│
├─ 项目执行
│  ├─ Goal
│  ├─ Plan
│  ├─ TODO
│  ├─ 子智能体入口
│  ├─ Workflow
│  └─ Ralph 多轮工作流
│
├─ 上下文与安全
│  ├─ 沙箱
│  ├─ 权限控制
│  ├─ 结果裁剪
│  ├─ 大输出落盘
│  └─ 上下文压缩
│
└─ 工程保障
   ├─ workbench:doctor
   ├─ 源码构建与冷启动验收
   └─ 永久离线产物
```

---

## 4. 与原版 Harness 的核心差异

### 4.1 新增正式 `workbench` 预设

位置：

```text
apps/cli/config/agent-presets/workbench/
```

它是正式源码中的一级预设，和 `standard`、`code`、`minimal`、`cordis` 并列。

Web 默认新会话直接选择：

```yaml
default: workbench
```

因此不需要再在 `~/.dsh/.agent-presets/` 中创建用户覆盖预设。

### 4.2 工作台能力直接进入 Web 正式组合

位置：

```text
packages/bundle/web-app/cordis.patch.yml
```

这里正式启用了：

- 默认 `workbench` 预设；
- SQLite 持久会话全文索引；
- `localfs` MCP；
- 工作台代码运行时；
- 工作区、附件、会话投影等 Web 能力。

### 4.3 工作台运行依赖进入正式包依赖

位置：

```text
apps/cli/package.json
pnpm-lock.yaml
```

关键依赖包括：

```text
@deepseek-ai/dsh-lsp
@deepseek-ai/dsh-lsp-stdio
@deepseek-ai/dsh-tool-lsp
@deepseek-ai/dsh-terminal
@deepseek-ai/dsh-terminal-bash
@deepseek-ai/dsh-tool-terminal
@modelcontextprotocol/server-filesystem
typescript-language-server
typescript
```

因此重新安装依赖后，不需要再手工拼接外部依赖路径。

---

## 5. 工作台预设的实际能力

正式配置：

```text
apps/cli/config/agent-presets/workbench/agent.cordis.yml
```

### 5.1 文件与 Shell

默认包含：

```text
tool-bash
tool-fs
tool-fs-search
tool-jobs
```

可用于：

- 浏览项目；
- 读取、创建、修改文件；
- 全仓搜索；
- 编译；
- 测试；
- Git 操作；
- 脚本执行；
- 后台任务管理。

### 5.2 持久终端

包含：

```text
terminal-service
terminal-bash
terminal-tools
```

和一次性 Shell 不同，持久终端会保持：

- 当前工作目录；
- Shell 状态；
- 环境变量；
- 长时间运行的交互式任务。

默认单次终端操作超时：

```text
300000 ms
```

后台任务入口也已经启用。

### 5.3 Code Mode

工作台启用：

```yaml
mode: both
```

因此同时保留原生工具调用和 `run_code` 代码编排能力。

适合把多次文件读取、搜索、命令执行和结果处理组合成一次确定性程序执行。

---

## 6. 语言服务（LSP）

### 6.1 TypeScript / JavaScript

默认内置，无需额外安装。

支持常见：

- `.ts`
- `.tsx`
- `.mts`
- `.cts`
- `.js`
- `.jsx`
- `.mjs`
- `.cjs`

能力包括：

- 悬浮信息；
- 跳转定义；
- 查找引用；
- 类型信息；
- 基础语义导航。

启动器位置：

```text
apps/cli/config/workbench/typescript-language-server.mjs
```

如需覆盖启动器，可设置：

```bash
export DSH_WORKBENCH_TS_LAUNCHER=/path/to/launcher.mjs
```

### 6.2 C / C++

C/C++ 使用 `clangd`，设计为**可选外部能力**。

设置：

```bash
export DSH_CLANGD_BIN=/usr/bin/clangd
```

后即可启用 `.c/.h/.cc/.cpp/.cxx/.hh/.hpp` 等文件的语义导航。

如果系统没有 `clangd` 或没有设置 `DSH_CLANGD_BIN`：

> 工作台基础能力仍然正常启动，C/C++ 语言服务只是不加载。

这避免了可选工具阻断整个工作台。

---

## 7. MCP 本地文件服务

工作台默认启动一个：

```text
localfs
```

对应启动器：

```text
apps/cli/config/workbench/mcp-filesystem.mjs
```

默认授权根目录：

```text
启动工作台时的当前工作目录
```

推荐显式设置：

```bash
export DSH_WORKBENCH_MCP_ROOT="$PWD"
```

然后启动工作台。

如工作目录为 `/mnt/data/project`，MCP 只能访问该授权范围，访问范围外路径会被拒绝。

典型工具包括：

```text
create_directory
directory_tree
edit_file
get_file_info
list_allowed_directories
list_directory
move_file
read_file
read_multiple_files
read_text_file
search_files
write_file
```

如需自定义启动器：

```bash
export DSH_WORKBENCH_MCP_LAUNCHER=/path/to/mcp-filesystem.mjs
```

---

## 8. SQLite 会话全文索引

Web 正式组合默认使用持久 SQLite 索引：

```text
DSH_HOME/storages/session-search.sqlite
```

特点：

- 会话内容可全文检索；
- 重启后可从持久日志恢复；
- 不依赖外部数据库；
- 与 `DSH_HOME` 一起迁移即可保留索引和用户状态。

---

## 9. 内置技能

5 个技能直接放在正式预设内部：

```text
apps/cli/config/agent-presets/workbench/skills/
```

### `workbench-ops`

工作台运行与维护规范。

用于：

- 状态检查；
- 修改后验证；
- 环境恢复；
- 避免破坏冻结依赖；
- 建立确定性执行路径。

### `repo-review`

通用仓库审查。

重点检查：

- 正确性；
- 安全问题；
- 生命周期与并发；
- 接口契约；
- 重复实现；
- 冗余抽象；
- 可以简化的设计。

### `repo-quality-gate`

仓库质量门禁。

根据实际改动选择并执行：

- 格式检查；
- 类型检查；
- 单元测试；
- 集成测试；
- 构建；
- 发布前验收。

### `docs-quality`

文档与技术文字质量检查。

覆盖：

- README；
- 架构文档；
- 设计文档；
- 接口说明；
- 注释；
- 用户可见提示；
- 错误信息。

### `task-journal`

长期任务执行记录。

用于保存：

- 目标；
- 约束；
- 决策；
- 已验证证据；
- 失败路径；
- 检查点；
- 下一步。

解决长任务中断后无法准确恢复的问题。

---

## 10. 目标、计划、待办与工作流

工作台保留并启用 Harness 的项目执行能力：

### Goal

支持目标创建、读取、更新、暂停、恢复和完成。

### Plan

计划模式用于在真正修改前完成代码探索与实施方案。

### TODO

适合实现阶段维护：

```text
已完成
进行中
待处理
阻塞
```

### 子智能体

保留：

```text
subagent
subagent_fork
list_agents
send_message
interrupt_agent
```

### Workflow / Ralph

保留多轮工作流执行框架。

需要注意：

> 子智能体自主推理和多轮智能循环仍然需要 Harness 自己配置可用模型提供方。

本地文件、终端、MCP、语言服务、会话和索引不受这个限制。

---

## 11. 模型凭据与接管模式

工作台已经把“模型能力”和“本地执行能力”分离。

### 不需要模型凭据的能力

以下能力可以独立运行：

```text
工作区
会话
文件
Shell
持久终端
后台任务
Code Mode 基础执行
MCP
语言服务
SQLite 索引
技能发现与加载
插件管理
源码构建
工作台医生
```

### 需要模型提供方的能力

如果希望直接在 Harness Web 聊天框中让 Harness 自主完成：

- 推理；
- 对话生成；
- 自主子智能体；
- 多轮 Ralph 智能循环；

则仍然需要在 Harness 中配置对应模型提供方和凭据。

因此工作台特别适合充当：

> **外部上层智能助手 + 本地 Harness 执行底座**

的执行环境。

---

## 12. 从源码运行

### 环境要求

推荐与永久构建环境保持一致：

```text
Node.js 22.19.0
pnpm 11.7.0
Linux x64
```

根 `package.json` 当前允许：

```text
Node.js ^22.19.0 或 >=24.0.0
```

### 安装

```bash
git clone https://github.com/sy220284/555.git
cd 555

corepack enable
corepack prepare pnpm@11.7.0 --activate
pnpm install --frozen-lockfile
```

### 构建

```bash
pnpm run build
```

### 工作台静态验收

```bash
pnpm run workbench:doctor
```

正常结果：

```text
workbench doctor: OK
```

### 启动

推荐：

```bash
DSH_WORKBENCH_MCP_ROOT="$PWD" pnpm run workbench:web
```

默认地址：

```text
http://127.0.0.1:3080
```

默认只监听本机回环地址。

---

## 13. 推荐启动方式

如果工作台用于处理指定项目：

```bash
cd /path/to/project
export DSH_WORKBENCH_MCP_ROOT="$PWD"

/path/to/555/node_modules/.bin/pnpm \
  --dir /path/to/555 \
  run workbench:web
```

或者从仓库根启动后，在工作台中注册其他工作区。

如果需要 C/C++ 语言服务：

```bash
export DSH_CLANGD_BIN="$(command -v clangd)"
```

然后启动。

---

## 14. 关键源码定位

以后重新部署、排查或继续开发时，优先看这些文件。

### 工作台预设

```text
apps/cli/config/agent-presets/workbench/
├─ preset.yml
├─ agent.cordis.yml
└─ skills/
```

### MCP / 语言服务启动器

```text
apps/cli/config/workbench/
├─ mcp-filesystem.mjs
└─ typescript-language-server.mjs
```

### Web 侧正式集成

```text
packages/bundle/web-app/cordis.patch.yml
```

这里包含：

- 默认 `workbench`；
- SQLite 持久索引；
- `localfs` MCP；
- Web Host 相关集成。

### 正式依赖

```text
apps/cli/package.json
pnpm-lock.yaml
```

### 工作台自检

```text
scripts/workbench-doctor.mjs
```

### 工作台文档

```text
docs/workbench.md
docs/workbench.zh.md
```

### 自动验收

```text
.github/workflows/workbench-ci.yml
```

### 永久离线构建

```text
.github/workflows/permanent-toolchain.yml
```

---

## 15. `workbench:doctor` 检查什么

当前医生脚本会确认：

- 工作台预设存在；
- 5 个技能存在；
- TypeScript 语言服务器启动器存在；
- MCP 启动器存在；
- Web 正式组合存在；
- 工作台文档存在；
- CLI 依赖齐全；
- `terminal-tools` 存在；
- 语言服务配置存在；
- `mode: both` 存在；
- Web 默认预设为 `workbench`；
- SQLite 指向持久路径；
- MCP 已进入正式 Web 组合；
- 架构元数据为 `native-source-integrated`；
- 不存在 `workbench-src`；
- 不存在 `.workbench`；
- 不存在 `overlay`。

因此它既是部署验收，也是以后排查“是否退回旧改造方式”的第一道检查。

---

## 16. 自动验收流程

正式工作流：

```text
.github/workflows/workbench-ci.yml
```

会执行：

```text
安装 Node.js 22.19.0
→ 启用 pnpm 11.7.0
→ pnpm install --frozen-lockfile
→ pnpm run build
→ pnpm run workbench:doctor
→ 使用全新 DSH_HOME 冷启动
→ 检查 http://127.0.0.1:3080
```

这保证验收不是依赖开发机器上的旧缓存或旧 Home 配置。

---

## 17. 永久离线产物

正式永久构建工作流：

```text
.github/workflows/permanent-toolchain.yml
```

发布标签：

```text
workbench-1.0.0
```

Release 包含：

```text
workbench-1.0.0-ready-linux-x64.tar.zst
workbench-toolchain-linux-x64.tar.zst
SHA256SUMS.txt
```

### ready 包

包含：

- 当前完整工作台源码；
- 已冻结安装的依赖；
- 已完成的生产构建；
- 内置工作台预设；
- 5 个技能；
- MCP / LSP 启动器；
- `workbench:doctor`。

### toolchain 包

保存固定工具链：

```text
Node.js 22.19.0
pnpm 11.7.0
```

### 完整性验证

```bash
sha256sum -c SHA256SUMS.txt
```

发布包与源码仓库形成两条恢复路径：

```text
GitHub 源码 → 重新安装 / 构建
永久 ready 包 → 快速离线恢复
```

---

## 18. 离线恢复思路

拥有两个 `.tar.zst` 和 `SHA256SUMS.txt` 后：

1. 先校验 SHA-256；
2. 解压工具链；
3. 解压 ready 包；
4. 使用固定 Node.js / pnpm；
5. 执行 `pnpm run workbench:doctor`；
6. 设置 `DSH_WORKBENCH_MCP_ROOT`；
7. 启动 `pnpm run workbench:web`。

ready 包本身已经包含冻结依赖和生产构建，因此适合沙箱回收、无网环境或快速恢复。

---

## 19. 数据与持久化

运行时代码和用户数据需要区分。

仓库保存：

```text
完整源码
预设
技能
依赖声明
构建脚本
工作流
文档
```

`DSH_HOME` 保存：

```text
会话
SQLite 索引
凭据
工作区状态
用户设置
其他运行时数据
```

推荐单独备份 `DSH_HOME`。

源码仓库可以重建程序，`DSH_HOME` 用于恢复个人运行状态。

---

## 20. 安全边界

### Web 地址

默认：

```text
127.0.0.1:3080
```

仅本机访问。

工作台 Web 当前不应该在没有认证层的情况下直接暴露到不可信网络。

### MCP

推荐每次显式设置：

```bash
DSH_WORKBENCH_MCP_ROOT=/允许访问的项目目录
```

避免无必要扩大文件授权范围。

### 凭据

模型、第三方服务等凭据应继续通过 Harness 凭据系统管理，不应硬编码进仓库。

---

## 21. 常见问题

### 页面能打开，但不能让 Harness 自主回答

检查模型提供方和凭据。

这不会影响文件、Shell、终端、MCP、语言服务和会话管理本身。

### TypeScript / JavaScript 语言服务不可用

确认：

```bash
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
```

并检查：

```text
apps/cli/config/workbench/typescript-language-server.mjs
```

### C/C++ 语言服务不可用

确认系统存在：

```bash
command -v clangd
```

然后：

```bash
export DSH_CLANGD_BIN="$(command -v clangd)"
```

重新启动工作台。

### MCP 无法读取某个目录

查看当前：

```bash
echo "$DSH_WORKBENCH_MCP_ROOT"
```

重新设置为实际项目根目录再启动。

越过授权根目录的访问被拒绝属于正常安全行为。

### 怀疑工作台改造文件丢失

直接运行：

```bash
pnpm run workbench:doctor
```

### 依赖状态异常

优先恢复到锁文件：

```bash
pnpm install --frozen-lockfile
```

不要随意升级依赖后继续把旧构建结果当成有效产物。

---

## 22. 开发修改后的标准验收路径

任何涉及工作台能力的修改，建议至少执行：

```bash
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
```

然后使用新的临时 Home 做冷启动：

```bash
export DSH_HOME="$(mktemp -d)"
export DSH_WORKBENCH_MCP_ROOT="$PWD"
pnpm run workbench:web
```

确认：

```text
HTTP 200
默认 workbench 预设
5 个技能可发现
MCP 正常启动
TypeScript / JavaScript 语言服务可用
SQLite 索引文件可创建
```

修改正式依赖、预设、MCP、语言服务或锁文件后，还应确认永久离线构建工作流通过。

---

## 23. 当前正式工作流

最终仓库只保留两条工作台正式工作流：

```text
.github/workflows/workbench-ci.yml
.github/workflows/permanent-toolchain.yml
```

旧的：

- 完整源码同步工作流；
- overlay 发布工作流；
- 一次性迁移工作流；
- 临时诊断 / 取消任务；

均不属于最终架构，已经清理。

---

## 24. 版本信息

```text
Workbench：1.0.0
DeepSeek Harness：0.1.1-rc.2
上游提交：b150a551b8d465e31e418e1b2eaf5e79bbb7d28e
Node.js：22.19.0（永久构建基线）
pnpm：11.7.0
默认预设：workbench
架构：native-source-integrated
```

版本元数据同时写入根 `package.json` 的 `workbench` 字段。

---

## 25. 上游来源

工作台基于 DeepSeek AI 开源的 DeepSeek Harness 深度改造。

上游项目：`deepseek-ai/deepseek-harness`

本仓库保留完整上游源码历史结构与必要版权、许可证文件，并在其基础上直接集成工作台能力。

如需研究原始 Harness 架构，可继续查看：

```text
docs/architecture.md
docs/development.md
AGENTS.md
```

---

## 26. 许可证

本仓库沿用上游项目的 MIT 许可证：

```text
LICENSE
```

第三方依赖许可证信息：

```text
THIRD_PARTY_NOTICES.md
```

---

## 27. 最终原则

这个仓库今后的维护原则很简单：

> **工作台就是源码本身。**

新增或修改工作台能力时，应直接修改正式源码目录、正式依赖、正式预设或正式 Web 组合。

不要重新引入：

```text
workbench-src/
.workbench/
overlay/
用户 Home 中的必需覆盖配置
```

只有做到“克隆仓库即可看到完整源码、重新安装即可构建、空 Home 即可启动”，才算工作台改造仍然保持完整。
