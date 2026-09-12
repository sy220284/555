# agent.md

AI 自动开发执行规则以仓库根目录 [`AGENT.md`](AGENT.md) 为唯一完整版本。本文件用于兼容要求读取小写 `agent.md` 的代理或工具。

执行前必须读取：

1. `AGENT.md`
2. `docs/design/00_MASTER_GDD.md`
3. `docs/development/00_ARCHITECTURE.md`
4. `docs/specs/CONTENT_REGISTRY.md`
5. 当前任务对应的执行规格文件
6. `docs/specs/TEST_MATRIX.md`
7. `docs/specs/ACCEPTANCE_CRITERIA.md`

如果当前对象缺少稳定 ID、明确参数、公式/状态机或验收用例，禁止直接编码，先补规格。

不得跳过质量门禁，不得自行改变母设计，不得把占位/可玩状态冒充正式完成。