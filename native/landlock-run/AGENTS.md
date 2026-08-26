# AGENTS.md — Landlock Run

本文件补充根目录 `AGENTS.md`，适用于 `native/landlock-run/`。

## 不可破坏的安全契约

- 本项目只负责 Linux Landlock **约束机制**，授权策略由上层消费者决定。
- 公开命令协议固定为 `landlock-run [--ro <path>]... [--rw <path>]... -- <argv>...` 与 `landlock-run --probe`。
- 启动器级错误统一退出 `125`，并输出 `landlock-run: ` 前缀；未成功 `exec` 前绝不能运行目标命令。
- `probe()` 是唯一可用性判据，结果为 `full` / `partial` / `unusable`；不能以文件存在或内核版本代替真实规则集探测。
- 运行时入口与二进制不得读取环境变量来决定安全策略；`NALR_*` 只允许构建/测试编排用途。
- 入口包与平台包共同版本化。Linux x64/arm64 平台包使用静态 musl 二进制；新增架构必须同时拥有原生持续集成构建机和真实约束证明，不使用交叉编译冒充支持。
- 平台包打包必须保留 `bin/landlock-run` 可执行位；当前发布链因此使用 `npm pack` 打平台包、`pnpm pack` 打入口包。禁止绕过现有 `prepack` 与安装后字节/真实约束校验。
- `package.json` 的 `os`/`cpu`、`prebuilds.json` 与 `scripts/github-matrix.mjs` 必须保持一致；缺失/不匹配平台要失败关闭。

完整架构、命令协议、支持矩阵、打包和发布细节已收敛到仓库根 `README.md` 的 Linux Landlock 与自动化章节。原 `native/landlock-run/docs/` 与包 README 不再维护；实现事实以 C 源码、入口 API、元数据和测试为准。
