# 仓库门禁、任务状态与自动闭环

永久工作流：

```text
.github/workflows/pr-policy.yml
.github/workflows/repository-gates.yml
.github/workflows/controlled-merge.yml
.github/workflows/main-verification.yml
.github/workflows/branch-hygiene.yml
.github/workflows/dependency-update.yml
.github/workflows/release.yml
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
task-governance
quality / quality
security
performance
evidence
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

`evidence` 始终聚合任务治理、风险分类、质量、安全、性能、Node.js 22.19、Windows、macOS、Python SDK 与 Linux Landlock 等常驻门禁；任何常驻子门禁失败，证据门禁必须失败。受控合并和主线复验都绑定同一精确 workflow run 及其最新 run attempt；失败任务重跑后只接受该轮次的精确 job 集合，不得混入旧轮次记录，也不得按检查名称挑选历史结果。

扩展产品验证采用变更影响分类：

```text
产品相关源码
→ 只触发对应端到端 / Web / GUI / 快照等产品专项验证

依赖、工作区、TypeScript 根配置、vendor 或 patches 等全局产品风险
→ 触发完整产品专项验证

仅 AGENTS、agent.md、.github 治理代码或 Workflow
→ governance=true，但不等同于 full product risk
→ 继续执行治理自检与所有常驻门禁，不无条件触发与本次治理变更无关的产品专项验证

仅 .github/task-control/work.json 或 governance.json 状态记账
→ 不作为产品风险或治理逻辑风险驱动项
```

风险分类只决定额外产品专项验证范围，不能跳过常驻类型检查、构建、产品基础测试、跨平台和原生门禁；因此它用于消除误触发，不用于隐藏真实失败。

高权限自动化必须从可信 `main` 执行；候选 PR 不得决定自己的合并资格。合并后只有 `main-verification` 成功，才允许同步永久集成分支；只有同步和最终 Branch Hygiene 均成功，才发布 `delivery-ready=success`。

最终对外声明完成前必须得到：

```bash
node .github/governance/repository-state.mjs assert-delivered
```

只有仓库有效状态为 `DELIVERED` 才属于完整交付。`PR_ACTIVE`、`MAIN_VERIFYING`、`SYNC_PENDING`、`DELIVERY_VERIFYING` 与 `BRANCH_HYGIENE_BLOCKED` 均必须继续推进或修复。

分支卫生自动化仅删除能够证明已完全包含于 `main` 且没有开放 PR 的额外分支；未知工作一律关闭式阻断，禁止为了满足三分支库存而丢弃提交。
