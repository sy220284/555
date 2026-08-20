# DeepSeek Harness 开发代理快速入口

> `AGENTS.md` 是完整权威规则。本文件只保留最短可靠执行路径；出现冲突或缺项立即返回 `AGENTS.md` 与机器状态文件。

## 必读顺序

```text
AGENTS.md
→ .github/governance/repository-policy.json
→ .github/task-control/policy.json
→ 当前永久集成分支对应的 .github/task-control/<lane>.json
→ README.md / package.json / pnpm-workspace.yaml / 相关配置
→ main / work / governance 真实 Ref、开放 PR 与检查状态
→ 任务相关代码、测试和调用链
```

## 两条永久执行分支

```text
产品源码、修复、重构、产品测试、产品配置
→ work

github 治理、AGENTS、Workflow、构建/测试门禁、依赖与发布治理
→ governance
```

仓库长期只允许：

```text
main
work
governance
```

禁止第四分支，禁止直接提交 `main`。

## 接到明确任务后自动推进

用户提出明确的软件开发、修改、修复、重构、测试或发布需求后，代理默认获得完整工程闭环执行权限。

执行目标不是完成某一步，而是持续推进直到 `DELIVERED`。

```text
确认真实基线
→ 若当前 lane 已有未完成闭环，先恢复它
→ 对应 task-control 状态写入 IN_PROGRESS
→ 分析完整影响链
→ 实现
→ 相关测试
→ 代码审计
→ 发现问题继续修复
→ 再验证
→ task-control 状态写入 IMPLEMENTED
→ work/governance → main PR
→ PR Policy + Repository Gates
→ 跟踪 Workflow 与审核反馈
→ 失败则读取真实日志、修复、重跑
→ Controlled Merge
→ Main Verification
→ Integration Branch Synchronization
→ Branch Hygiene
→ delivery-ready
→ repository-state = DELIVERED
→ 最终汇报
```

## PR阶段持续推进

创建 PR 后，代理必须持续跟进：

- GitHub Workflow 检查；
- 构建和测试结果；
- 安全检查；
- Review反馈；
- 合并状态。

以下情况均不是停止理由：

```text
CI失败
测试失败
构建失败
审核要求修改
合并冲突
分支同步问题
```

处理流程：

```text
发现问题
→ 定位根因
→ 修改
→ 验证
→ 更新PR
→ 继续推进
```

## 默认授权范围

明确开发任务默认已经授权：

- 读取仓库、分支、提交、PR、检查结果和日志；
- 修改任务范围内代码；
- 增加或调整必要测试；
- 提交和推送永久集成分支；
- 创建或更新 PR；
- 处理门禁失败；
- 根据审核意见完善实现；
- 完成受控合并和主线验证。

不需要在上述正常工程步骤重复请求许可。

## 任务状态

机器真源：

```text
.github/task-control/policy.json
.github/task-control/work.json
.github/task-control/governance.json
```

静态状态：

```text
IDLE → IN_PROGRESS → IMPLEMENTED
```

`IMPLEMENTED` 只表示候选分支达到进入正式门禁条件，不等于最终完成。

## 唯一最终完成条件

只有：

```text
无开放永久集成 PR
+
main-verification = success
+
delivery-ready = success
+
work / governance 按规则同步
+
无非法第四分支
=
DELIVERED
```

才允许声明任务完成。

## 停止条件

只有以下情况允许请求用户裁决：

- 产品方向变化；
- 删除或永久改变用户数据；
- 核心技术路线变化；
- 新增重大外部服务或付费依赖；
- 破坏公开接口；
- 敏感凭据或权限问题；
- 无法从仓库和上下文推导安全方案。

普通工程问题必须继续自动处理。

## 工程底线

- 先找根因，再改代码；优先修公共机制，禁止堆特判。
- 不能制造第二套状态、能力或持久化真源。
- 公共接口必须检查全部生产和测试调用方。
- 禁止 TODO、空实现、固定成功、删测试、放宽断言、静默吞错和用延时掩盖竞态。
- 测试、构建和最终完成声明都必须有真实运行或真实 GitHub 状态支持。
