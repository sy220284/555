# 555 工作台

`555` 是 **工作台的永久源码与重建仓库**。这里同时保存：

1. **改造后的完整源码树**：`workbench-src/`；
2. 工作台接管层源码：预设、技能、MCP/LSP、脚本和文档；
3. DeepSeek Harness 离线永久运行产物及构建工作流；
4. 一键部署、回滚、验收和故障定位工具。

> 工作台 = DeepSeek Harness 完整源码/执行底座 + ChatGPT 接管层。

当前改造采用非侵入式设计：尽量不直接改上游核心文件，而把差异集中到 `workbench-src/.workbench/`。这样 `workbench-src/` 仍然是一份**完整可审计、可继续开发的源码**，同时所有改造位置都能精确定位。

## 完整源码入口

```text
workbench-src/
├─ apps/                       Harness 应用完整源码
├─ packages/                   Harness 包与插件完整源码
├─ python/                     Python SDK 等源码
├─ native/                     原生组件源码
├─ vendor/                     仓库内供应代码
├─ website/                    文档站/网站源码
├─ scripts/                    构建脚本
├─ package.json
├─ pnpm-lock.yaml
│
├─ .workbench/                 工作台全部改造源码
│  ├─ bin/                     安装、卸载、控制脚本
│  ├─ overlay/                 接管预设、技能、MCP/LSP 配置
│  ├─ docs/                    改造、能力、部署、排错文档
│  └─ github-workflows/        工作台相关构建/发布工作流
│
├─ WORKBENCH_SOURCE.md         完整源码说明
└─ WORKBENCH_SOURCE_MANIFEST.json
```

`workbench-src/` 固定对应 DeepSeek Harness 提交 `b150a551b8d465e31e418e1b2eaf5e79bbb7d28e`，工作台配置、技能、脚本或文档变化后，会由 GitHub Actions 自动重建完整源码树，避免版本漂移。

> `node_modules`、缓存和本机构建垃圾不属于源码，因此不提交 Git；可运行冻结依赖与生产构建保存在 Release 永久产物中。

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

完全离线时，把 Release 中的永久产物放到同一目录：

```text
deepseek-harness-0.1.1-rc.2-ready-linux-x64.tar.zst
dsh-toolchain-linux-x64.tar.zst
SHA256SUMS.txt
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
├─ workbench-src/                  改造后的完整源码树
├─ .github/workflows/
│  ├─ permanent-toolchain.yml      永久运行产物构建
│  ├─ workbench-overlay-release.yml
│  └─ sync-full-workbench-source.yml  完整源码自动同步
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
├─ MANIFEST.json
└─ VERSION
```

## 重要边界

1. **工作台本地执行不需要模型密钥**；ChatGPT 在当前对话中负责推理，Harness 负责本地执行与工作环境。
2. 如果直接在 Harness 网页聊天框里要求 Harness 自己生成回答，仍然需要它自己的模型凭据。
3. 默认监听 `127.0.0.1:3080`，不应直接暴露公网。
4. 永久运行树已经包含冻结依赖和生产构建。不要在离线运行树里执行 `pnpm install` / `pnpm exec` 去“修依赖”。
5. ChatGPT 沙箱自身可能被平台回收，所以重要状态以 GitHub `555`、完整源码树和 Release 永久产物为准。

详细内容从 [workbench-src/WORKBENCH_SOURCE.md](workbench-src/WORKBENCH_SOURCE.md) 和 [docs/MODIFICATIONS.md](docs/MODIFICATIONS.md) 开始。
