# DeepSeek Harness 开发代理快速入口

> `AGENTS.md` 是完整权威规则。本文件只保留最短可靠执行路径；出现冲突或缺项立即返回 `AGENTS.md` 与机器状态文件。

## 必读顺序

```text
AGENTS.md
→ .github/governance/repository-policy.json
→ .github/task-control/policy.json
→ 当前永久集成分支对应的 .github/task-control/<lane>.json
→ README.md / package.json / pnpm-workspace.yaml / 相关配置
→ main / work / governance 真实 Ref、开放 PR 与检查状态
→ 任务相关代码、测试和调用链
```

## 两条永久执行分支

```text
产品源码、修复、重构、产品测试、产品配置
→ work

github 治理、AGENTS、Workflow、构建/测试门禁、依赖与发布治理
→ governance
```

仓库长期只允许：

```text
main
work
governance
```

禁止第四分支，禁止直接提交 `main`。

## 接到明确任务后直接推进

```text
确认真实基线
→ 若当前 lane 已有未完成闭环，先恢复它
→ 对应 task-control 状态写入 IN_PROGRESS
→ 分析完整影响链
→ 实现
→ 相关测试
→ 代码审计
→ 发现问题继续修复
→ 再验证
→ task-control 状态写入 IMPLEMENTED
→ work/governance → main PR
→ PR Policy + Repository Gates
→ 失败则读真实日志、修复、重跑
→ Controlled Merge
→ Main Verification
→ Integration Branch Synchronization
→ Branch Hygiene
→ delivery-ready
→ repository-state = DELIVERED
→ 最终汇报
```

明确开发任务默认已经授权正常的提交、推送、PR、门禁处理、受控合并、主线复验与安全收尾，不需要在这些步骤反复询问用户。

只有产品方向、不可逆数据变化、明确破坏公开兼容性、新外部/付费服务、重大生产依赖、核心技术路线变化、敏感凭据或无法证明安全的历史覆盖需要用户裁决。

## 任务状态

机器真源：

```text
.github/task-control/policy.json
.github/task-control/work.json
.github/task-control/governance.json
```

静态状态：

```text
IDLE → IN_PROGRESS → IMPLEMENTED
```

`IMPLEMENTED` 只表示候选分支已经实现、验证并审计，可以进入正式 PR 门禁；它不等于最终完成。

## 唯一最终完成条件

最终向用户说“完成/闭环”前必须得到：

```bash
node .github/governance/repository-state.mjs assert-delivered
```

或从真实 GitHub 数据证明完全等价的：

```text
无开放永久集成 PR
+
main-verification = success
+
delivery-ready = success
+
work / governance 已按安全规则同步
+
无非法第四分支
=
DELIVERED
```

任何 `PR_ACTIVE`、`MAIN_VERIFYING`、`SYNC_PENDING`、`DELIVERY_VERIFYING`、`BRANCH_HYGIENE_BLOCKED` 都不是最终交付。

## 失败处理

```text
404 / 409 / 422 / 权限错误 / CI 失败 / 合并失败 / 分支漂移
≠ 停止理由
```

先核对仓库、路径、Ref、Head、PR、权限、触发条件和真实日志，定位根因后继续。

测试失败：修根因 → 重跑失败项 → 重跑关联验证。

PR 失败：修复 → 更新同一永久集成分支与 PR → 重新门禁。

main 复验失败：任务重新进入进行中 → 来源永久集成分支修复 → 再走完整闭环。

只有确实缺少外部权限、不可推导信息或存在必须由用户裁决的高风险决策时才能停止。

## 工程底线

- 先找根因，再改代码；优先修公共机制，禁止堆特判。
- 不能制造第二套状态、能力或持久化真源。
- 公共接口必须检查全部生产和测试调用方。
- Session / Storage 必须验证“写入 → 读取 → 重建 → 继续运行”。
- Tool / Shell / Terminal / FS / Runtime / Sandbox 必须覆盖权限、路径、超时、取消、资源释放和失败传播。
- Subagent / Workflow 必须有明确所有者、取消与回收路径。
- Web / Client 不得虚构后端能力，界面缓存不得成为业务真源。
- 禁止 TODO、空实现、固定成功、删测试、放宽断言、静默吞错和用延时掩盖竞态。
- 测试、构建和最终完成声明都必须有真实运行或真实 GitHub 状态支持。
