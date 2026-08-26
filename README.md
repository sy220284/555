# 555 工作台

`555` 是基于 DeepSeek Harness `0.1.1-rc.2` 完整源码改造的本地智能执行工作台。工作台能力直接进入正式源码树，不依赖额外覆盖层，也不强制依赖模型凭据才能启动本地执行底座。

> 架构、运行机制、工具目录、插件配置、234 个工作区包、测试策略、历史决策与维护规则，统一查阅[完整中文技术文档](TECHNICAL.md)。

## 核心能力

```text
555 工作台
├─ 工作区与会话
│  ├─ 会话事件日志、查询、全文索引与分叉
│  ├─ 工作区持久记录、附件与会话引用
│  └─ 标题、统计、投影与反馈
├─ 本地执行
│  ├─ Bash、PowerShell 与持久终端
│  ├─ 文件读写、编辑与搜索
│  ├─ 后台任务与代码运行
│  └─ TypeScript、JavaScript 语言服务
├─ 扩展与编排
│  ├─ 模型上下文协议、智能体客户端协议与开发工具包
│  ├─ Cordis 插件与类型化远程调用
│  ├─ 子智能体、团队和工作流
│  └─ 目标、计划、待办与计划任务
└─ 工程保障
   ├─ 沙箱、权限与安全边界
   ├─ 构建、自检与冷启动验收
   ├─ 测试快照与持久化验证
   └─ 永久离线恢复产物
```

默认预设为 `workbench`。即使没有模型密钥，工作区、文件、命令环境、持久终端、语言服务、会话管理、SQLite 索引、插件装载、构建与自检仍可独立工作。

## 快速启动

推荐环境：

```text
Node.js 22.19.0
pnpm 11.7.0
Linux x64
```

从源码构建并启动：

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

默认访问地址：`http://127.0.0.1:3080`。

`DSH_WORKBENCH_MCP_ROOT` 用于限定本地文件服务允许访问的根目录。C/C++ 语言服务属于可选能力，可通过 `DSH_CLANGD_BIN` 指定 `clangd`；未配置时不会阻断工作台启动。

### 永久离线一键部署

永久 Release 自带匹配版本的 Node.js、pnpm、已安装依赖和生产构建结果。新部署推荐直接执行：

```bash
bash scripts/deploy-workbench-offline.sh
```

脚本会自动下载永久 Release、校验 SHA-256、保留 pnpm 工作区符号链接、验证运行时依赖、生成启动/停止/状态入口并完成 HTTP 冷启动验收；不会修改系统 Node.js/pnpm，也不会重新执行 `pnpm install`。

已从 GitHub Actions 下载离线产物时也可直接部署：

```bash
bash scripts/deploy-workbench-offline.sh \
  --artifact ./workbench-offline-linux-x64.zip \
  --target ~/.local/share/555-workbench
```

升级到同一目标目录会保留 `data/`；启动失败会回滚上一版。新永久产物使用 `tar.gz`，无需宿主机安装 `zstd`；脚本仍兼容历史 `tar.zst` 产物。

## 验收

工作台相关修改至少执行：

```bash
pnpm install --frozen-lockfile
pnpm run build
pnpm run workbench:doctor
pnpm exec vitest run --config vitest.e2e.config.ts apps/cli/tests/web-agent-presets.e2e.ts
```

随后使用空的工作台数据目录进行冷启动：

```bash
export DSH_HOME="$(mktemp -d)"
export DSH_WORKBENCH_MCP_ROOT="$PWD"
pnpm run workbench:web
```

验收应确认：网页返回正常、默认预设为 `workbench`、5 个内置技能可发现、本地文件服务与语言服务已经挂载、SQLite 会话索引能够创建。

`workbench:doctor` 同时验证运行时依赖是否能从命令行应用实际解析，避免出现“依赖声明存在、自检通过，但离线解包后的工作区链接已经损坏”的假阳性。

## 主要源码入口

| 路径 | 作用 |
| --- | --- |
| `apps/cli/config/agent-presets/workbench/` | 默认预设与 5 个内置技能 |
| `apps/cli/config/workbench/` | 本地文件服务与语言服务启动器 |
| `packages/bundle/web-app/cordis.patch.yml` | 网页工作台正式组合 |
| `packages/` | 234 个工作区包 |
| `scripts/workbench-doctor.mjs` | 工作台静态与运行时自检 |
| `scripts/deploy-workbench-offline.sh` | 永久离线一键部署、升级与冷启动验收 |
| `.github/workflows/workbench-ci.yml` | 构建、预设端到端与冷启动验收 |
| `.github/workflows/permanent-toolchain.yml` | 永久离线产物构建与实装验收 |
| `TECHNICAL.md` | 完整中文技术文档，也是系统级说明的单一维护点。
- 运行代码、配置、类型与测试始终是最终事实源。
- 运行时技能、测试快照、测试夹具和法律文件按程序用途独立保留。
- 禁止恢复重复的包级说明、双语镜像或分散决策文档树。

更完整的安装说明、架构解释、能力边界、工具与插件目录、包索引、故障排查及历史决策，见[完整中文技术文档](TECHNICAL.md)。
