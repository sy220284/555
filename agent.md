# agent.md

AI自动开发执行规则以仓库根目录 [`AGENT.md`](AGENT.md) 为唯一完整版本。本文件只用于兼容要求读取小写 `agent.md` 的代理或工具。

执行前必须读取：

1. `AGENT.md`
2. `docs/technical/TECH_STACK.md`
3. `docs/design/00_MASTER_GDD.md`
4. `docs/development/00_ARCHITECTURE.md`
5. `docs/specs/CONTENT_REGISTRY.md`
6. 当前任务对应专项规格
7. `docs/specs/TEST_MATRIX.md`
8. `docs/specs/ACCEPTANCE_CRITERIA.md`

涉及模式、经济、地图、AI战略或胜负逻辑时，还必须读取 `docs/design/03A_FREEDOM_WARFARE.md` 与 `docs/specs/GAME_MODE_RULES.md`。

如果对象缺少稳定ID、明确参数、引用链、公式/状态机或验收用例，禁止直接编码，先补规格。

不得跳过质量门禁，不得自行改变母设计和冻结技术栈，不得在标准规则中加入时间驱动资源/伤害/积分/科技机制，也不得把固定节点链重新引入前线模式。

不得把文档完成、占位、灰盒或可玩状态冒充正式完成。