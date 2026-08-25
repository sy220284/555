# 555 工作台

`555` 是 **工作台的永久重建仓库**。这里保存 DeepSeek Harness 的离线永久工具产物工作流，以及基于 Harness 改造出的 ChatGPT 接管工作台。

> 工作台 = DeepSeek Harness 本地执行底座 + ChatGPT 接管层。

它不是对 DeepSeek Harness 核心源码的长期分叉。当前改造尽量全部放在用户层预设、配置覆盖、技能和运维脚本中，因此以后升级 Harness 时可以重新应用覆盖层，不需要重新手改核心源码。

## 当前固定基线

| 项目 | 版本 |
|---|---|
| 工作台 | `workbench-1.0.0` |
| DeepSeek Harness | `0.1.1-rc.2` |
| Harness 提交 | `b150a551b8d465e31e418e1b2eaf5e79bbb7d28e` |
| Node.js | `22.19.0` |
| pnpm | `11.7.0` |
| 系统 | Linux x64 |

## 一键部署

联网环境：

```bash
git clone https://github.com/sy220284/555.git
cd 555
./bin/install-workbench.sh
```

如果基础 Harness 和 Node 已经存在，只重新应用改造层：

```bash
./bin/install-workbench.sh --skip-runtime
```

完全离线时，把 Release 中的两个永久产物放到同一目录：

```text
deepseek-harness-0.1.1-rc.2-ready-linux-x64.tar.zst
dsh-toolchain-linux-x64.tar.zst
```

然后：

```bash
./bin/install-workbench.sh --offline-dir /你的/产物目录
```

部署后：

```bash
/mnt/data/workbench/bin/workbench-control.sh doctor
```

## 仓库结构

```text
555/
├─ .github/workflows/              永久工具产物构建
├─ bin/
│  ├─ install-workbench.sh         完整/覆盖层部署
│  ├─ uninstall-workbench.sh       回滚改造层
│  └─ workbench-control.sh         启停、验收、诊断、清单
├─ overlay/
│  ├─ agent-presets/               ChatGPT 接管预设
│  ├─ skills/                      5 个全局技能
│  ├─ profile/                     MCP + 会话索引覆盖模板
│  ├─ settings.yaml                默认接管预设
│  └─ AGENTS.md                    工作台执行约定
├─ docs/
│  ├─ MODIFICATIONS.md             改造定位与全部修改
│  ├─ CAPABILITIES.md              可接管功能明细和用法
│  ├─ DEPLOYMENT.md                部署、恢复、升级
│  ├─ OPERATIONS.md                日常运维
│  ├─ TROUBLESHOOTING.md           故障定位
│  ├─ TEST-REPORT.md               当前验收基线
│  └─ PLUGIN-INVENTORY.md          运行时插件完整清单
└─ VERSION
```

## 重要边界

1. **工作台本地执行不需要模型密钥**；ChatGPT 在当前对话中负责“脑”，Harness 负责“手和工作环境”。
2. 如果直接在 Harness 网页聊天框里要求 Harness 自己生成回答，仍然需要它自己的模型凭据。
3. 默认监听 `127.0.0.1:3080`。Harness 当前 Web 面没有为公网暴露设计完整认证边界，不应改成公网监听。
4. 永久运行树已经包含冻结依赖和生产构建。不要在离线运行树里执行 `pnpm install` / `pnpm exec` 去“修依赖”，这会触发跨机器依赖重建并尝试联网。
5. ChatGPT 沙箱自身可能被平台回收，所以重要状态以 GitHub `555`、Release 永久产物和项目仓库为准。

详细内容从 [docs/MODIFICATIONS.md](docs/MODIFICATIONS.md) 开始。
