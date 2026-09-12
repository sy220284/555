# agent.md

AI自动开发执行规则以仓库根目录 [`AGENT.md`](AGENT.md) 为唯一完整版本。本文件只用于兼容要求读取小写 `agent.md` 的代理或工具，不再维护第二套规则。

任何代理开始任务前必须先读取 `AGENT.md`。当前项目处于 `implementation_bootstrap`，还没有达到 `playable`；不得把文档、机器数据、代码契约、占位或灰盒冒充正式完成。

涉及实际实现时还必须读取：

- `docs/development/08_IMPLEMENTATION_BOOTSTRAP.md`
- `docs/development/09_FULL_DESIGN_IMPLEMENTATION_AUDIT.md`
- `docs/specs/SYSTEM_COMPLEXITY_BUDGET.md`
- `docs/specs/IMPLEMENTATION_TEST_GATES.md`
- 当前任务对应专项规格与机器可读 `/Data`。

如果设计、数据和代码互相冲突，先停止扩展并统一三者，再继续编码。
