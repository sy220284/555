# 前期改造文件去向

前期在 `/mnt/data/harness-takeover` 中产生的文件已经重新评估，不原样堆进仓库，而是按长期价值归档：

| 前期文件 | 处理 | 现在的位置 |
|---|---|---|
| `AGENTS.md` | 保留并改名义为“工作台执行约定” | `overlay/AGENTS.md` |
| `README.md` | 保留内容并扩展为完整仓库文档 | 根 `README.md` + `docs/` |
| `harness-control.sh` | 保留成熟逻辑，改造成可配置/可迁移版本 | `bin/workbench-control.sh` |
| `TAKEOVER_STATUS.json` | 不原样同步 | 静态事实进入 `MANIFEST.json`，运行态由安装脚本生成 `WORKBENCH_STATE.json` |
| `harness-control.sh.bak-readiness` | 不同步 | 属于修复过程备份，信息已固化进 `MODIFICATIONS.md` 与现有控制脚本 |

原因：仓库应该保存**可重建规则、稳定配置和验收方法**，不保存旧 PID、旧时间戳、一次性备份这类会误导后续部署的运行态残留。
