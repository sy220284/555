# 主线必需门禁检查

`main` 的合并保护应要求以下最终检查成功：

```text
repository-gates / merge-gate
```

`merge-gate` 只在仓库范围、Node.js 22.19、Node.js 24、Windows、Python SDK 与 Linux Landlock 原生门禁全部成功后通过。
