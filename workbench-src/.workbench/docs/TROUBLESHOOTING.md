# 工作台故障定位

## 1. 首页能打开，但 API 404

说明静态 Web 已启动，Host API 还没完全就绪。

不要只用首页判断。执行：

```bash
workbench-control.sh restart
workbench-control.sh verify
```

控制脚本已经把 `host.describe` 纳入就绪判断。

## 2. `Cannot find package @deepseek-ai/dsh-tool-terminal` 或 LSP 包

用户预设依赖的几个包不在 Web Profile 默认解析闭包里。执行：

```bash
workbench-control.sh repair-links
workbench-control.sh restart
```

不要运行 `pnpm install`。

## 3. `pnpm` 尝试联网/重建 `node_modules`

立即停止。永久运行树已经构建完成。

工作台启动必须使用：

```text
NODE apps/cli/lib/bin.js web --no-open
```

不要用 `pnpm exec` 去“验证一下”。跨机器后 pnpm 可能认为依赖布局需要重建。

如果运行树已经被破坏，重新从 `555` Release 的永久产物恢复。

## 4. MCP 不启动

检查：

```bash
workbench-control.sh doctor
```

再确认模板渲染后的 `cordis.patch.yml` 中 MCP 服务端路径仍存在。安装脚本会动态定位它，因此手工搬目录后应重新执行安装脚本覆盖层。

## 5. MCP 访问被拒绝

如果目标路径不在 `MCP_ALLOWED_ROOT`，拒绝是预期行为。默认只允许 `/mnt/data`。

需要扩大范围时，重新部署并明确指定：

```bash
./bin/install-workbench.sh --skip-runtime --mcp-root /明确需要的目录
```

不要直接设成 `/`。

## 6. TypeScript LSP 不工作

检查冻结依赖中是否仍存在：

```bash
find "$DSH_ROOT/node_modules/.pnpm" -path '*/typescript-language-server/lib/cli.mjs' -print -quit
```

安装脚本通过动态寻找该文件生成最终预设。

## 7. C/C++ LSP 不工作

检查：

```bash
command -v clangd
clangd --version
```

如果系统没有 `clangd`，可以用 `--allow-partial` 先部署工作台，但 C/C++ 语义导航不计入完整验收。

## 8. 技能看不到

检查：

```bash
find ~/.dsh/skills -maxdepth 2 -name SKILL.md -print
```

应至少有 5 个全局技能。然后确认会话使用 `chatgpt-takeover` 预设。

## 9. 插件显示 disabled

先区分“主动关闭”和“故障”。运行：

```bash
workbench-control.sh inventory
```

当前设计本来就关闭一批 Host 层模型工具、PowerShell、HMR 等条目，接管会话挂载后会在会话平面启用自己的文件/Shell/技能/终端/LSP 工具。

真正异常是：`enabled=true` 但 `fiberPhase` 不是 `active`。完整基线要求这种条目为 0。

## 10. Harness 网页聊天报模型凭据错误

这是已知边界，不代表工作台坏了。工作台本地执行由 ChatGPT 接管，不依赖 Harness 模型。

如果要让 Harness 网页自身自主回复，另行配置模型凭据。

## 11. 本地整个环境消失

这是沙箱生命周期问题。不要修旧目录，按：

```text
555 → Release 永久产物 → install-workbench.sh → doctor
```

重新建立。

## 12. SHA-256 与另一批同版本产物不一致

先确认 `SHA256SUMS.txt` 是否与这两个压缩包来自**同一次工作流/同一个 Release 批次**。

相同源码和依赖目录经过不同次 `tar + zstd` 重新压缩，压缩元数据可能让最终字节哈希不同。不能拿 A 批次的哈希去验证 B 批次。

正确做法：

1. 三个文件保持同批：两个 `.tar.zst` + `SHA256SUMS.txt`；
2. 按同批清单校验；
3. 解压后再检查 Harness `package.json` 版本和 `OFFLINE_BUILD_INFO.txt` 中的提交。
