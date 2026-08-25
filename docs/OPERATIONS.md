# 工作台日常运维

## 1. 基本命令

默认控制脚本：

```bash
/mnt/data/workbench/bin/workbench-control.sh
```

### 状态

```bash
workbench-control.sh status
```

### 启停

```bash
workbench-control.sh start
workbench-control.sh stop
workbench-control.sh restart
```

### 快速验收

```bash
workbench-control.sh verify
```

### 深度诊断

```bash
workbench-control.sh doctor
```

### 插件/版本清单

```bash
workbench-control.sh inventory
```

### 修复本地包链接

```bash
workbench-control.sh repair-links
```

## 2. 直接调用 Harness API

普通 API：

```bash
workbench-control.sh rpc host.describe '{}'
workbench-control.sh rpc agentPreset.list '{}'
workbench-control.sh rpc session.list '{}'
```

Typert 直连 Remote：

```bash
workbench-control.sh remote-rpc 'pluginInventory/list' '{}'
```

## 3. 任务开始前

建议固定流程：

```text
verify
→ 确认项目目录/仓库状态
→ 读取项目规则
→ 选择适用技能
→ 执行任务
→ 质量门禁
→ 最终复查
```

大型任务先调用 `task-journal`。

## 4. 仓库任务推荐技能

- 审查：`/repo-review`
- 准备提交/合并/发布：`/repo-quality-gate`
- 文档：`/docs-quality`
- 长任务：`/task-journal`
- 工作台维护：`/chatgpt-takeover-ops`

## 5. 日志

默认：

```text
/mnt/data/workbench/workbench.log
```

看最后 100 行：

```bash
tail -100 /mnt/data/workbench/workbench.log
```

## 6. SQLite 会话索引

默认：

```text
~/.dsh/storages/session-search.sqlite
```

它是会话检索缓存/索引，不是业务数据库。业务数据不要写进去。

## 7. 安全规则

- 不把 Web 服务改成公网监听；
- 不把 MCP 根目录改为 `/`；
- 不把 API 密钥写进 `555`；
- 不在永久运行树里执行依赖重建命令；
- 不修改 Harness 官方预设来实现工作台功能；
- 重要业务结果必须同步到项目仓库或正式文件。
