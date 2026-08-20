# 主线必需门禁检查

`main` 的合并保护应要求以下最终检查成功：

```text
pr-policy
repository-gates / merge-gate
```

`pr-policy` 必须由可信 `main` 上的治理代码检查 PR 形态、永久 lane 和单 lane 串行约束。

`repository-gates / merge-gate` 只有在仓库策略、风险分类、Node.js 22.19、Node.js 24、扩展产品验证、Windows、macOS、Python SDK 与 Linux Landlock 原生门禁全部成功后通过。
