# 主线必需门禁检查

`main` 的合并保护应要求以下最终检查成功：

```text
pr-policy
task-governance
quality / quality
security
performance
evidence
```

`pr-policy` 必须由可信 `main` 上的治理代码检查 PR 形态、永久 lane 和单 lane 串行约束。

`task-governance` 验证任务状态机、仓库范围与治理内核；`quality / quality` 执行冻结安装、类型检查、构建、测试与覆盖率；`security` 固定全部 Action 提交指纹并阻止新增、升级或 critical 生产依赖安全债务；`performance` 根据变更风险运行性能与压力测试；`evidence` 聚合全部常驻门禁并上传与精确 workflow run 绑定的结构化证据。

受控合并器仅接受与候选 head SHA、workflow run ID、run attempt、check suite ID 及该轮精确 job 集合一致的最新 `repository-gates` 成功记录，并发布 `source-gate-run` 来源状态。失败任务重跑时只验证最新轮次，不得把旧轮次失败 job 与新轮次成功 job 混合判定。主线复验使用同一个 run ID 回查，禁止按检查名称复用历史成功结果。
