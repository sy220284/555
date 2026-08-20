# 仓库门禁、任务状态与自动闭环

永久工作流：

```text
.github/workflows/pr-policy.yml
.github/workflows/repository-gates.yml
.github/workflows/controlled-merge.yml
.github/workflows/main-verification.yml
.github/workflows/branch-hygiene.yml
```

任务状态机器真源：

```text
.github/task-control/policy.json
.github/task-control/work.json
.github/task-control/governance.json
```

最终合并检查：

```text
pr-policy
repository-gates / merge-gate
```

候选分支在进入可信 PR Policy 前必须先完成实现、验证和审计，并把对应永久集成分支任务状态标记为：

```text
IMPLEMENTED / IMPLEMENTED
```

`IMPLEMENTED` 仅表示候选分支达到合并门禁资格，不表示最终交付。

自动闭环固定为：

```text
work/governance 任务状态 IN_PROGRESS
→ 实现、验证、审计、修复、再验证
→ 任务状态 IMPLEMENTED
→ work/governance → main PR
→ 可信主线 PR Policy
→ Repository Gates
→ Controlled Merge
→ Main Verification
→ Integration Branch Synchronization
→ Branch Hygiene
→ delivery-ready
→ repository-state = DELIVERED
```

`repository-gates / merge-gate` 聚合仓库范围、变更风险、Node.js 22.19、Node.js 24、扩展产品验证、Windows、macOS、Python SDK 与 Linux Landlock 原生门禁。任何子门禁失败，最终门禁必须失败。

高权限自动化必须从可信 `main` 执行；候选 PR 不得决定自己的合并资格。合并后只有 `main-verification` 成功，才允许同步永久集成分支；只有同步和最终 Branch Hygiene 均成功，才发布 `delivery-ready=success`。

最终对外声明完成前必须得到：

```bash
node .github/governance/repository-state.mjs assert-delivered
```

只有仓库有效状态为 `DELIVERED` 才属于完整交付。`PR_ACTIVE`、`MAIN_VERIFYING`、`SYNC_PENDING`、`DELIVERY_VERIFYING` 与 `BRANCH_HYGIENE_BLOCKED` 均必须继续推进或修复。

分支卫生自动化仅删除能够证明已完全包含于 `main` 且没有开放 PR 的额外分支；未知工作一律关闭式阻断，禁止为了满足三分支库存而丢弃提交。
