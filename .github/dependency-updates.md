# 依赖更新

仓库只允许 `main`、`work`、`governance` 三条永久分支，因此不启用会创建 `dependabot/*` 临时分支的默认 Dependabot 模式。

`Dependency Update` 每周在 `governance` 通道生成递归依赖漂移报告。手工选择 `update` 模式时，它只在现有版本约束内更新锁文件，执行类型检查、构建与测试，并导出可审计补丁。维护者审阅补丁后在 `governance` 任务中应用，再按正常六门禁流程交付。

安全门禁独立于更新节奏：新增 advisory、严重度升级或 critical 结果会立即阻断；已修复的基线记录应随依赖更新删除。
