# 工作台当前验收报告

验收日期：2026-08-25

## 固定版本

```text
工作台：workbench-1.0.0
DeepSeek Harness：0.1.1-rc.2
Harness commit：b150a551b8d465e31e418e1b2eaf5e79bbb7d28e
Node.js：v22.19.0
pnpm：11.7.0
```

## 当前冷启动复验

执行：

```bash
workbench-control.sh restart
workbench-control.sh verify
```

结果：

```text
HTTP 200                            通过
host.describe API                  通过
chatgpt-takeover 默认预设           通过
MCP localfs                         通过
SQLite 会话全文索引                 通过
TypeScript 语言服务器               通过
clangd                              通过
5 个全局技能文件                    通过
```

## 插件深度审计

空载 Host：

```text
总 Loader 条目：140
激活：114
关闭：26
启用但未激活：0
```

创建并挂载一个 `chatgpt-takeover` 会话后：

```text
总 Loader 条目：173
激活：143
关闭：30
启用但未激活：0
```

这说明接管预设的会话层插件成功挂载，并且没有启用后卡在非 active 阶段的插件。

## 已完成的真实功能测试

在此前同一部署基线上已经完成：

- MCP 读取 `/mnt/data` 成功；
- MCP 读取 `/etc/hosts` 被拒绝；
- 持久终端跨调用保持目录和环境状态；
- TypeScript LSP：初始化、hover、definition、references；
- C/C++ `clangd`：初始化、hover、definition、references；
- 会话全文索引跨冷启动仍能检索测试令牌；
- 5 个全局技能均通过实际斜杠调用进入会话；
- 启动脚本修复为“首页 + 核心 API”双就绪条件。

## 已知边界

唯一重要边界：Harness 网页聊天的自主模型回复仍需要独立模型凭据；这不影响 ChatGPT 接管模式使用本地文件、终端、技能、MCP、LSP、会话和工作区。

## 仓库安装器从零离线重建测试

使用同一套 `555` 永久产物，在完全独立目录执行：

```text
新 WORKBENCH_ROOT
新 DSH_ROOT
新 RUNTIME_ROOT
新 DSH_HOME
```

没有复用当前工作台用户配置。结果：

```text
Harness 解压                         通过
Node.js 工具链解压                   通过
同批 SHA256SUMS 校验                 通过
chatgpt-takeover 预设安装            通过
5 个技能安装                         通过
MCP localfs                          通过
TypeScript + clangd LSP              通过
SQLite 会话索引                      通过
核心 API 双就绪                      通过
挂接会话插件：173/143/30/0           通过
停止测试实例并恢复正式工作台          通过
```

测试中曾发现：两次 GitHub Actions 对相同目录重新压缩时，压缩元数据会导致 `.tar.zst` 字节哈希不同。因此安装器不再把某一次压缩结果的 SHA-256 写死为全局常量，而是**必须使用产物同批次的 `SHA256SUMS.txt` 校验**。这项问题已经在从零复测前修复。
