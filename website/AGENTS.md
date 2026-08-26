# AGENTS.md — Documentation Website

本文件补充根目录 `AGENTS.md`，适用于 `website/`。

文档站只有一个规范内容源：仓库根 `README.md`。`website/` 只负责把它投影为静态站点与原始 Markdown 版本，不拥有第二份技术正文。

- 禁止在 `website/` 下新增复制版技术 Markdown；`website/.generated/`、`.cache/`、`.dist/` 都是可丢弃产物。
- 修改技术内容直接改根 `README.md`；修改站点路由、主题或投影逻辑才改 `website/` 与 `scripts/project-doc-site.ts`。
- 仓库不再维护英文镜像、`docs/` 源树或多层文档侧边栏。
- 文档站变更至少运行 `pnpm run docs:build`，并确认根 README 仍是唯一规范源。

仓库级文档治理规则见根 `AGENTS.md`；站点投影实现以 `website/docs.ts`、`website/.vitepress/config.ts` 与 `scripts/project-doc-site.ts` 为准。
