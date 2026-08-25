# 可接管功能明细与使用方式

“可接管”指 ChatGPT 可以把工作台作为本地执行底座使用。部分能力由 Harness API 管理，部分通过接管预设中的工具实现。

## 状态等级

- ✅：当前已启用并完成真实或冷启动验证；
- 🟡：框架已启用，但若要求 Harness 自己推理仍需要模型凭据；
- ⛔：当前主动关闭。

## 1. 服务与运行态 ✅

能力：启动、停止、重启、状态检查、核心 API 就绪判断、日志查看、插件清单。

```bash
workbench-control.sh start
workbench-control.sh stop
workbench-control.sh restart
workbench-control.sh status
workbench-control.sh verify
workbench-control.sh doctor
workbench-control.sh inventory
```

典型用途：环境被重启后恢复、任务前健康检查、出现异常时定位。

## 2. 工作区管理 ✅

Harness 支持创建、列出、重命名和管理工作区，会话可以绑定工作区。

ChatGPT 接管时的使用方式：

1. 用户指定项目/仓库；
2. ChatGPT 将其放入 `/mnt/data` 下的工作目录；
3. 注册为 Harness 工作区；
4. 后续会话、文件、状态围绕该工作区管理。

适合：多项目并行、长期仓库任务、恢复某个项目上下文。

## 3. 会话与持久化 ✅

核心接口包括：

```text
session.list
session.search
session.create
session.history
session.models
session.selectModel
session.rename
session.fork
session.prompt
session.attachment
session.updateQueue
session.cancel
```

工作台重点利用：

- 会话创建/列表；
- 历史读取；
- 会话搜索；
- 分叉；
- 附件；
- SQLite 全文检索；
- 重启后从持久日志恢复。

## 4. 文件系统 ✅

接管预设提供：

```text
read
read_image
write
edit
glob
grep
```

适合：代码审计、批量修改、配置修复、文档维护、图片读取。

## 5. Bash 与命令执行 ✅

```text
bash
```

可用于：

- Git；
- Python / Node.js；
- 编译；
- 测试；
- 静态分析；
- 文件批处理；
- 服务启动；
- 系统只读审计。

工作台仍受 Harness 沙箱和权限策略约束。

## 6. 持久终端 ✅

```text
terminal_open
terminal_send
terminal_read
terminal_signal
terminal_list
terminal_close
```

与一次性 Bash 的区别：Shell 状态可以跨多次调用保持。

适合：

- 保持当前目录；
- 持续运行开发服务器；
- 交互式命令；
- 多阶段构建；
- 需要连续环境变量的任务。

实测包括跨命令保持目录/环境变量、正常关闭和敏感环境清理。

## 7. 后台任务 ✅

```text
job_list
job_output
job_kill
```

适合：长编译、测试套件、扫描、服务进程、后台子任务。

## 8. Code Mode ✅

接管预设启用 `run_code`，呈现方式为 `both`。

用途：把多次文件读取、搜索、命令和结果处理组合成一次程序化执行，降低多轮工具调用成本。

适合大仓库批处理、重复校验、结构化分析。

## 9. Skills 技能 ✅

### `chatgpt-takeover-ops`

用于工作台维护、确定性执行、恢复路径、避免破坏离线依赖。

### `repo-review`

用于仓库/PR/重构审查，优先检查正确性、安全、生命周期、数据持久性、接口兼容和不必要复杂度。

### `repo-quality-gate`

用于提交、合并、发布前检查。按照实际 diff 选择格式、类型、单测、集成、构建、迁移和文档门禁。

### `docs-quality`

用于 README、设计文档、注释、API 文档、错误提示和用户可见文本。

### `task-journal`

用于长任务记录目标、约束、决策、证据、失败路径、检查点和下一步。

调用形式：

```text
/repo-review
/repo-quality-gate
/docs-quality
/task-journal
/chatgpt-takeover-ops
```

## 10. MCP 本地文件工具 ✅

当前服务：`localfs`。默认只允许 `/mnt/data`。

模型工具目录可出现：

```text
mcp__localfs__create_directory
mcp__localfs__directory_tree
mcp__localfs__edit_file
mcp__localfs__get_file_info
mcp__localfs__list_allowed_directories
mcp__localfs__list_directory
mcp__localfs__list_directory_with_sizes
mcp__localfs__move_file
mcp__localfs__read_file
mcp__localfs__read_media_file
mcp__localfs__read_multiple_files
mcp__localfs__read_text_file
mcp__localfs__search_files
mcp__localfs__write_file
```

用途：把标准 MCP 文件协议能力纳入 Harness 工具目录。

安全边界：不允许跨出 `MCP_ALLOWED_ROOT`。

## 11. LSP 代码导航 ✅

### TypeScript / JavaScript

覆盖：`.ts .tsx .mts .cts .js .jsx .mjs .cjs`。

### C / C++

覆盖：`.c .h .cc .cpp .cxx .hh .hpp`。

可用于：悬浮信息、定义跳转、引用查询等语义代码导航。

实测 TypeScript 和 `clangd` 均完成初始化、hover、definition；之前验收也包含 references。

## 12. Goal / Plan / TODO ✅/🟡

工具包括：

```text
create_goal
get_goal
update_goal
exit_plan_mode
todo_write
```

状态管理本身可用；如果让 Harness 自己依据这些状态持续推理，需要模型凭据。ChatGPT 接管时可以直接承担规划和推进。

## 13. 子智能体 🟡

框架和工具已保留：

```text
subagent
subagent_fork
list_agents
send_message
interrupt_agent
```

当前限制：Harness 自己没有模型凭据，所以不把它当作“自主多模型并行系统”。ChatGPT 可以替代主智能调度。

## 14. Workflow / Ralph 🟡

```text
workflow
ralph
```

执行编排框架存在；自主多轮判断仍依赖 Harness 模型。

## 15. 沙箱与权限 ✅

关键插件包括：

```text
sandbox-local
sandbox-policy
bash-sandbox
fs-sandbox
permission-presets
user-approval
```

用于限制命令、文件范围、权限提升和高风险操作。

## 16. 大结果与上下文管理 ✅

包括：结果溢出落盘、结果裁剪、上下文压缩、会话引用、文件引用。

用途：大型仓库搜索和长任务时避免把所有原始输出塞进对话上下文。

## 17. 插件运行态查询 ✅

Host 提供只读 `pluginInventory/list`，可以实时查看 Loader 条目、模块名、是否启用和 Fiber 阶段。

```bash
workbench-control.sh inventory
```

当前挂接一个 `chatgpt-takeover` 会话后的深度基线：

```text
总条目 173
激活 143
关闭 30
启用但未激活 0
```

## 18. 原生联网搜索 ⛔

`tool-web` 在接管预设中主动关闭。联网研究由 ChatGPT 完成，再把结果落到工作台。

## 19. Codex / Claude Code 子智能体 ⛔

预留行存在，但当前禁用；基础运行树没有对应 CLI，因此不把它们写进可用能力。

## 20. Harness 网页聊天 🟡

网页、API、插件界面可以使用。若直接在网页聊天框输入提示词并要求 Harness 自己生成回复，仍需独立模型凭据。

工作台的推荐使用方式始终是：

```text
用户 → ChatGPT → 工作台 → 本地项目/工具
```
