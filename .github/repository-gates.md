# 仓库门禁与自动闭环

永久工作流：

```text
.github/workflows/pr-policy.yml
.github/workflows/repository-gates.yml
.github/workflows/controlled-merge.yml
.github/workflows/main-verification.yml
.github/workflows/branch-hygiene.yml
```

最终合并检查：

```text
pr-policy
repository-gates / merge-gate
```

自动闭环固定为：

```text
work/governance → main PR
→ 可信主线 PR Policy
→ Repository Gates
→ Controlled Merge
→ Main Verification
→ Integration Branch Synchronization
→ Branch Hygiene
```

`repository-gates / merge-gate` 聚合仓库范围、变更风险、Node.js 22.19、Node.js 24、扩展产品验证、Windows、macOS、Python SDK 与 Linux Landlock 原生门禁。任何子门禁失败，最终门禁必须失败。

高权限自动化必须从可信 `main` 执行；候选 PR 不得决定自己的合并资格。合并后只有 `main-verification` 成功，才允许同步永久 lane。分支卫生自动化仅删除能够证明已完全包含于 `main` 且没有开放 PR 的额外分支；未知工作一律失败关闭。
