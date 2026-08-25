# 工作台改造定位与修改清单

## 1. 改造原则

工作台没有长期修改 DeepSeek Harness 上游核心源码，主要使用三层覆盖：

```text
上游 Harness 生产构建（保持原样）
        ↓
用户级 Host 覆盖（cordis.patch.yml）
        ↓
用户级接管预设 + 全局技能
        ↓
工作台控制/部署脚本
```

这样做的目的：

- 上游升级后可以重新应用改造；
- 不把一次性绝对路径、密钥和沙箱状态写进上游源码；
- 出问题可以只回滚覆盖层；
- `555` 能作为唯一重建入口。

## 2. 基础运行时

当前固定：

- Harness `0.1.1-rc.2`
- 提交 `b150a551b8d465e31e418e1b2eaf5e79bbb7d28e`
- Node.js `22.19.0`
- pnpm `11.7.0`
- 生产依赖通过 `pnpm install --frozen-lockfile` 在 GitHub Actions 中安装
- 生产构建通过 `pnpm run build` 完成

永久产物由 `.github/workflows/permanent-toolchain.yml` 生成并发布到 Release。

## 3. 启动方式改造

运行态不通过 `pnpm dsh web` 启动，直接调用已经构建好的 CLI：

```bash
/path/to/node apps/cli/lib/bin.js web --no-open
```

原因：永久产物中的 `node_modules` 来自另一台 Linux 构建机。pnpm 在换机器后可能主动检查并重建依赖布局，从而在离线沙箱中尝试访问软件源。直接运行生产构建可以避免这个无意义的重建。

## 4. 启动就绪判定

原始运维脚本早期只检查首页 `HTTP 200`，实测发现 Web 静态页可能先可用，而核心 API 仍短暂返回 `404`。

工作台控制脚本现在要求同时满足：

1. `GET /` 返回 `200`；
2. `host.describe` RPC 返回 `result.ok=true`。

只有两项都通过才报告“启动完成”。

## 5. 接管预设 `chatgpt-takeover`

位置：

```text
~/.dsh/.agent-presets/chatgpt-takeover/
```

它基于 Harness 标准编码预设的成熟结构，但属于用户预设，不修改官方 `standard/code/minimal/cordis`。

新增/强化：

- 文件读写、搜索；
- Bash；
- 后台任务；
- 持久终端；
- 技能发现和加载；
- TypeScript/JavaScript 与 C/C++ LSP；
- Goal / Plan / TODO；
- Code Mode，呈现方式 `both`；
- 子智能体/Workflow/Ralph 框架保留；
- 原生 Web Search 工具关闭。

## 6. Host 覆盖

`cordis.patch.yml` 只增加两项跨会话能力：

### 6.1 SQLite 会话全文索引

```text
~/.dsh/storages/session-search.sqlite
```

用于长任务的会话搜索与重启后恢复。

### 6.2 MCP 本地文件服务

- 服务名：`localfs`
- 传输：stdio
- 默认允许根目录：`/mnt/data`
- 超时：60 秒
- 自动重连：开启

权限边界经过实际测试：允许读取 `/mnt/data`，拒绝 `/etc/hosts`。

## 7. LSP

当前工作台启用：

- TypeScript / TSX / JavaScript / JSX / MJS / CJS；
- C / C++；
- TypeScript 语言服务器从 Harness 冻结依赖中定位；
- C/C++ 使用系统 `clangd`。

安装脚本不写死包管理器内部哈希路径，会在新运行树中动态寻找 TypeScript 与 MCP 服务端实际位置。

## 8. 全局技能

共 5 个：

1. `chatgpt-takeover-ops`：工作台运维与确定性执行规范；
2. `repo-review`：代码审查 + 简化分析；
3. `repo-quality-gate`：提交/合并/发布前质量门禁；
4. `docs-quality`：技术文档和用户可见文本质量；
5. `task-journal`：长任务可恢复状态记录。

技能做成流程层，不做插件，因为它们改变“如何做事”，不需要新增常驻服务。

## 9. 模型配置收口

工作台最终 `settings.yaml` 只固定：

```yaml
agent-presets:
  default: chatgpt-takeover
```

不保存 OpenAI/DeepSeek API 密钥，也不要求工作台本地管理绑定任何模型。Harness 自身仍有上游默认模型信息，这是上游 Host 的默认值，接管模式不会调用它完成本地管理。

## 10. 主动关闭/不启用

- Harness 原生 `web_search/web_fetch`：由 ChatGPT 的联网能力替代；
- PowerShell：Linux 不需要；
- Codex 子智能体：当前运行树没有独立 Codex CLI；
- Claude Code 子智能体：当前运行树没有对应 CLI；
- 公网监听：安全原因不启用。

## 11. 未改动内容

- Harness 官方预设；
- Harness 核心源码；
- 生产构建文件；
- Harness 自带凭据系统；
- Harness 原生插件框架。

因此后续排障第一定位顺序应该是：

```text
555 覆盖层 → ~/.dsh 用户配置 → 永久运行树 → 上游 Harness
```
