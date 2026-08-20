# 贡献指南

本仓库采用 `main`、`work`、`governance` 三条永久分支。产品代码从 `work` 提交到 `main`；治理、策略、自动化与仓库级文档从 `governance` 提交到 `main`。不要创建第四条分支，也不要直接提交 `main`。

开始前先阅读 `AGENTS.md`、`.github/repository-gates.md` 和 `.github/required-checks.md`。每条永久通道同一时间只能有一个活动任务；任务必须在对应 `.github/task-control/*.json` 中从 `IN_PROGRESS` 推进到 `IMPLEMENTED`，之后才能进入 PR 门禁。

提交应保持单一目标、说明非目标和回滚路径，并执行与影响面匹配的验证。最低本地验证为：

```bash
pnpm install --frozen-lockfile
pnpm run typecheck
pnpm run build
pnpm run test
node .github/governance/pr-policy.mjs self-test
```

PR 必须使用仓库模板并等待六个永久检查成功。治理信任根变更不会自动合并，需要显式批准。第三方代码、许可证和来源材料必须按现有规则维护；安全漏洞不要公开披露，请使用 GitHub Security Advisory 私下报告。
