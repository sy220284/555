# 发布策略

发布只能从已达到 `DELIVERED` 的 `main` 触发 `.github/workflows/release.yml`。标签必须为 `v<package.json version>`，且目标必须是触发时的精确 `main` SHA。

发布工作流会重新执行冻结安装、类型检查、构建和测试，再创建 GitHub Release 与自动生成的发行说明。发布失败不得通过移动已有标签或绕过主线复验修正；先在 `work` 或 `governance` 完成新任务并重新进入交付闭环。

当前根包为私有工作区，自动化只创建 GitHub Release，不发布 npm 或 Python 包。若将来增加注册表发布，必须单独设计受信身份、来源证明、最小权限和可回滚策略。
