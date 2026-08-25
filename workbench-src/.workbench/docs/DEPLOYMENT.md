# 工作台部署、恢复与升级

## 1. 推荐目录

默认：

```text
/mnt/data/workbench             工作台管理文件/日志/状态
/mnt/data/dsh-direct/dsh        Harness 永久运行树
/mnt/data/dsh-runtime/node      Node.js 22.19.0
~/.dsh                          Harness 用户配置/会话/技能/索引
```

所有路径都可通过安装脚本参数修改。

## 2. 联网完整部署

```bash
git clone https://github.com/sy220284/555.git
cd 555
./bin/install-workbench.sh
```

脚本会：

1. 检查系统工具；
2. 从 `555` Release 下载永久 Harness 和工具链；
3. 使用同一批次 `SHA256SUMS.txt` 校验压缩包；
4. 解压 Harness 生产构建和 Node.js；
5. 备份现有 `~/.dsh` 改造目标文件；
6. 动态定位 MCP filesystem 和 TypeScript 语言服务器；
7. 安装 `chatgpt-takeover` 用户预设；
8. 安装 5 个全局技能；
9. 安装 Host 覆盖：MCP + SQLite 索引；
10. 只把 `chatgpt-takeover` 设成默认预设，不写模型密钥；
11. 修复本地 Profile 需要的离线包链接；
12. 冷启动并执行 `verify`。

## 3. 已有 Harness 时只装改造

```bash
./bin/install-workbench.sh --skip-runtime
```

前提：

- `DSH_ROOT/apps/cli/lib/bin.js` 已存在；
- Node 运行时符合 Harness 要求；
- 运行树内包含冻结依赖。

## 4. 完全离线部署

从 Release 准备：

```text
deepseek-harness-0.1.1-rc.2-ready-linux-x64.tar.zst
dsh-toolchain-linux-x64.tar.zst
```

执行：

```bash
./bin/install-workbench.sh --offline-dir /path/to/assets
```

脚本仍然执行同样的 SHA-256 校验。

## 5. 自定义路径

```bash
./bin/install-workbench.sh \
  --workbench-root /mnt/data/workbench \
  --dsh-root /mnt/data/dsh-direct/dsh \
  --runtime-root /mnt/data/dsh-runtime \
  --dsh-home "$HOME/.dsh" \
  --mcp-root /mnt/data
```

`--mcp-root` 是安全边界，不建议设成 `/`。

## 6. 部署后验收

```bash
/mnt/data/workbench/bin/workbench-control.sh verify
/mnt/data/workbench/bin/workbench-control.sh doctor
```

成功基线至少应看到：

```text
HTTP 200
preset=chatgpt-takeover
mcp=localfs
session-index=durable
lsp=typescript+clangd
skills=5
```

## 7. 回滚覆盖层

```bash
./bin/uninstall-workbench.sh
```

安装脚本会先在：

```text
~/.dsh/backups/workbench-<UTC时间>/
```

保存被替换的用户配置。卸载脚本恢复这些文件，**不会删除基础 Harness 运行树**。

## 8. 环境被平台回收后的恢复

不要依赖 `/mnt/data` 永久存在。恢复顺序：

```text
拉取 555
→ 下载/取得 Release 永久产物
→ install-workbench.sh
→ doctor
→ 拉取业务项目仓库
→ 继续任务
```

工作台自身不应成为业务源码唯一副本。

## 9. Harness 上游升级

当前工作台与 `0.1.1-rc.2` 精确绑定。升级时不要直接替换版本号。正确步骤：

1. 在 `555` 的永久工具产物工作流中更新 Harness 提交、版本、Node/pnpm 要求；
2. GitHub Actions 安装冻结依赖并完成生产构建；
3. 下载新永久产物到测试环境；
4. 重新应用工作台覆盖层；
5. 检查预设是否仍能挂载；
6. 重新测试 MCP/LSP/技能/全文索引/冷启动；
7. 更新 `VERSION`、文档和哈希；
8. 最后替换 Release。

不要把“上游能编译”视作“工作台兼容”。
