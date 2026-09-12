# Unity G1 真实编译与 EditMode 验证

本文件记录 G1 的实际执行路径，不改变 `AGENT.md`、`IMPLEMENTATION_TEST_GATES.md` 和 `10_FORMAL_IMPLEMENTATION_PROGRESS.md` 的上位规则。

## 当前状态

G1 仍为 **PENDING**。

已经具备：

- 项目版本固定为 Unity `6000.3.24f1 (4e7b9b5b6244)`；
- `Tools/project_structure_validator.py` 自动检查精确编辑器版本、冻结 Unity 包版本、禁止 preview/experimental 依赖、嵌入包依赖图、程序集引用、G1 必需程序集、EditMode 测试程序集属性和 30Hz 固定步配置；
- `Tests/test_unity_g1_preflight.py` 对上述前置规则执行自动回归；
- `Tools/run_unity_g1.py` 可在已激活的真实 Unity `6000.3.24f1` 环境执行批处理编译和 EditMode 测试；
- `.github/workflows/unity-g1.yml` 提供自托管 Unity 执行器入口。

尚未具备：

- GitHub 当前没有已确认可用且已激活 Unity `6000.3.24f1` 的自托管执行器运行证据；
- 因而不能宣称 Unity 全程序集编译和 EditMode 已通过；
- 项目生命周期继续保持 `implementation_bootstrap`。

## 执行器契约

自托管执行器需要：

1. GitHub Runner 标签包含 `self-hosted` 与 `unity-6000.3.24f1`；
2. 安装并完成合法激活的 Unity `6000.3.24f1`；
3. 仓库变量 `UNITY_EDITOR_PATH` 指向 Unity Editor 可执行文件；
4. Python 3.12 可用。

满足后手动运行 `unity-g1` 工作流。工作流依次执行：

`project_structure_validator -> Unity batch compile -> EditMode tests`

任何一步失败都保持 G1 未通过，并按 `AGENT.md` 的“失败 -> 最小复现 -> 根因定位 -> 最小修改 -> 回归”闭环修复。

## G1 关闭条件

只有真实 Unity `6000.3.24f1` 执行器给出以下证据后才允许关闭 G1：

- 所有嵌入包解析成功；
- `ModernRA.Core/Rules/Simulation/Combat/Navigation/AI/IntelEW/Robotics/Network/Tests` 编译成功；
- 无 preview/experimental 依赖；
- EditMode 测试全绿；
- `FixedStepSimulationSystemGroup.Timestep == 1/30f`。

静态前置门禁成功不等于 G1 成功。
