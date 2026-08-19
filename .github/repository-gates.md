# 仓库门禁

永久工作流：`.github/workflows/repository-gates.yml`

最终合并检查：`repository-gates / merge-gate`

该检查聚合以下门禁：

- `repository-policy`：仓库范围、临时产物、疑似凭据、Node/pnpm 根约束。
- `node-22-floor`：Node.js 22.19 最低支持版本下的冻结安装、类型检查和完整构建。
- `node-24-quality`：Node.js 24 主运行线的冻结安装、类型检查、完整构建和逐文件 100% 覆盖率测试。
- `windows-runtime`：Windows + Node.js 24 的类型检查和产品测试。
- `python-3.10` / `python-3.13`：Python SDK 最低版本与较新版本的语法和测试验证。
- `linux-landlock-native`：Linux 下使用 musl-gcc 编译 C11 Landlock 静态二进制并执行启动器测试。

任何子门禁失败，`merge-gate` 必须失败。门禁工作流只负责验证，不生成或改写正式产品源码。
