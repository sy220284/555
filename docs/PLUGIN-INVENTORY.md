# 插件运行时完整清单

快照：2026-08-25，使用仓库安装脚本重新部署后，再挂载一个 `chatgpt-takeover` 会话。

- 总条目：**173**
- 激活：**143**
- 关闭：**30**
- 启用但未进入 active：**0**

> `disabled` 不等于故障。Host 层有一批工具故意关闭，由接管预设在会话平面提供对应能力。真正需要关注的是 `enabled=true` 但阶段不是 `active`。

## 激活插件

| 条目 | 模块 | 阶段 |
|---|---|---|
| `include` | `cordis:include` | `active` |
| `include:timer` | `@deepseek-ai/cordis-plugin-timer` | `active` |
| `include:llm` | `@deepseek-ai/dsh-llm` | `active` |
| `include:session` | `@deepseek-ai/dsh-session` | `active` |
| `include:typert` | `@deepseek-ai/dsh-typert-registry` | `active` |
| `include:typert-loader` | `@deepseek-ai/dsh-typert-loader` | `active` |
| `include:typert-gateway` | `@deepseek-ai/dsh-api-gateway` | `active` |
| `include:session-title` | `@deepseek-ai/dsh-session-title` | `active` |
| `include:session-title-llm` | `@deepseek-ai/dsh-session-title-first-prompt-llm` | `active` |
| `include:user-questions` | `@deepseek-ai/dsh-user-questions` | `active` |
| `include:agent` | `@deepseek-ai/dsh-agent` | `active` |
| `include:agent-default-model` | `@deepseek-ai/dsh-agent-default-model` | `active` |
| `include:jobs` | `@deepseek-ai/dsh-jobs-local` | `active` |
| `include:llm-retry` | `@deepseek-ai/dsh-llm-retry` | `active` |
| `include:settings` | `@deepseek-ai/dsh-settings-file` | `active` |
| `include:credentials` | `@deepseek-ai/dsh-credentials-local` | `active` |
| `include:llm-pi-ai` | `@deepseek-ai/dsh-llm-pi-ai` | `active` |
| `include:session-persistence-jsonl` | `@deepseek-ai/dsh-session-persistence-jsonl` | `active` |
| `include:attachment-local` | `@deepseek-ai/dsh-attachment-local` | `active` |
| `include:session-query-sqlite` | `@deepseek-ai/dsh-session-query-sqlite` | `active` |
| `include:session-projection` | `@deepseek-ai/dsh-session-projection` | `active` |
| `include:session-telemetry-otel` | `@deepseek-ai/dsh-session-telemetry-otel` | `active` |
| `include:subprocess` | `@deepseek-ai/dsh-subprocess-local` | `active` |
| `include:sandbox` | `@deepseek-ai/dsh-sandbox-local` | `active` |
| `include:sandbox-policy` | `@deepseek-ai/dsh-sandbox-policy` | `active` |
| `include:bash-sandbox` | `@deepseek-ai/dsh-bash-sandbox` | `active` |
| `include:approval` | `@deepseek-ai/dsh-user-approval` | `active` |
| `include:permission` | `@deepseek-ai/dsh-permission-presets` | `active` |
| `include:shell-env` | `@deepseek-ai/dsh-shell-env` | `active` |
| `include:fs-observation-policy` | `@deepseek-ai/dsh-fs-observation-policy` | `active` |
| `include:skill` | `@deepseek-ai/dsh-skill` | `active` |
| `include:commands` | `@deepseek-ai/dsh-commands` | `active` |
| `include:command-feedback` | `@deepseek-ai/dsh-command-feedback` | `active` |
| `include:goal` | `@deepseek-ai/dsh-goal` | `active` |
| `include:goal-round-driver` | `@deepseek-ai/dsh-goal-round-driver` | `active` |
| `include:command-goal` | `@deepseek-ai/dsh-command-goal` | `active` |
| `include:token-meter` | `@deepseek-ai/dsh-token-meter` | `active` |
| `include:subagent` | `@deepseek-ai/dsh-subagent` | `active` |
| `include:subagent-spawn-in-process` | `@deepseek-ai/dsh-subagent-spawn-in-process` | `active` |
| `include:subagent-fork-in-process` | `@deepseek-ai/dsh-subagent-fork-in-process` | `active` |
| `include:tool-subagent-report` | `@deepseek-ai/dsh-tool-subagent-report` | `active` |
| `include:timeout-policy` | `@deepseek-ai/dsh-tool-call-timeout-policy` | `active` |
| `include:spill-local` | `@deepseek-ai/dsh-spill-local` | `active` |
| `include:spill-policy` | `@deepseek-ai/dsh-spill-policy` | `active` |
| `include:session-checkpoint-policy` | `@deepseek-ai/dsh-session-checkpoint-policy` | `active` |
| `include:repeat-tool-reminder` | `@deepseek-ai/dsh-repeat-tool-reminder` | `active` |
| `include:web` | `@deepseek-ai/dsh-web` | `active` |
| `include:web-search-deepseek` | `@deepseek-ai/dsh-web-search-deepseek` | `active` |
| `include:tools` | `@deepseek-ai/dsh-tools` | `active` |
| `include:system-prompt` | `@deepseek-ai/dsh-system-prompt` | `active` |
| `include:agent-loop` | `@deepseek-ai/dsh-agent-loop` | `active` |
| `include:fs-sandbox` | `@deepseek-ai/dsh-fs-sandbox` | `active` |
| `include:llm-deepseek` | `@deepseek-ai/dsh-llm-deepseek` | `active` |
| `include:code-runtime` | `@deepseek-ai/dsh-code-runtime-worker-thread` | `active` |
| `include:storage` | `@deepseek-ai/dsh-storage` | `active` |
| `include:storage-json` | `@deepseek-ai/dsh-storage-json` | `active` |
| `include:storage-domain` | `@deepseek-ai/dsh-storage-domain` | `active` |
| `include:message-feedback` | `@deepseek-ai/dsh-message-feedback` | `active` |
| `include:session-log-download` | `@deepseek-ai/dsh-session-log-export` | `active` |
| `include:workspace` | `@deepseek-ai/dsh-workspace` | `active` |
| `include:session-projection-cache` | `@deepseek-ai/dsh-session-projection-cache` | `active` |
| `include:session-reference` | `@deepseek-ai/dsh-session-reference` | `active` |
| `include:file-reference-local` | `@deepseek-ai/dsh-file-reference-local` | `active` |
| `include:session-stats` | `@deepseek-ai/dsh-session-stats` | `active` |
| `include:directory-picker` | `@deepseek-ai/dsh-host-directory-picker-auto` | `active` |
| `include:plugin-inventory` | `@deepseek-ai/dsh-host-plugin-inventory` | `active` |
| `include:api-gateway` | `@deepseek-ai/dsh-host-apiproxy` | `active` |
| `include:cordis-host-runner` | `@deepseek-ai/dsh-cordis-host-runner` | `active` |
| `include:web-startup` | `@deepseek-ai/dsh-web-app/startup` | `active` |
| `include:webserver` | `@deepseek-ai/dsh-host-webserver` | `active` |
| `include:web-runtime` | `@deepseek-ai/dsh-web-app` | `active` |
| `include:client-hmr` | `@deepseek-ai/dsh-client-hmr` | `active` |
| `include:modules` | `@deepseek-ai/dsh-client-modules` | `active` |
| `include:connection` | `@deepseek-ai/dsh-client-connection` | `active` |
| `include:api-remotes` | `@deepseek-ai/dsh-api-remotes` | `active` |
| `include:client-runtime` | `@deepseek-ai/dsh-client-runtime` | `active` |
| `include:cordis-client-runner` | `@deepseek-ai/dsh-cordis-client-runner` | `active` |
| `include:ui-theme` | `@deepseek-ai/dsh-client-ui-theme` | `active` |
| `include:locale` | `@deepseek-ai/dsh-client-locale` | `active` |
| `include:ui-layout` | `@deepseek-ai/dsh-client-ui-layout` | `active` |
| `include:ui-renderer` | `@deepseek-ai/dsh-client-ui-renderer` | `active` |
| `include:ui-sidebar` | `@deepseek-ai/dsh-client-ui-sidebar` | `active` |
| `include:ui-settings` | `@deepseek-ai/dsh-client-ui-settings` | `active` |
| `include:ui-settings-general` | `@deepseek-ai/dsh-client-ui-settings-general` | `active` |
| `include:ui-settings-models` | `@deepseek-ai/dsh-client-ui-settings-models` | `active` |
| `include:ui-settings-plugin-inventory` | `@deepseek-ai/dsh-client-ui-settings-plugin-inventory` | `active` |
| `include:ui-conversation` | `@deepseek-ai/dsh-client-ui-conversation` | `active` |
| `include:ui-brand-official` | `@deepseek-ai/dsh-client-ui-brand-official` | `active` |
| `include:ui-attachment` | `@deepseek-ai/dsh-client-ui-attachment` | `active` |
| `include:ui-tool` | `@deepseek-ai/dsh-client-ui-tool` | `active` |
| `include:ui-cordis` | `@deepseek-ai/dsh-client-ui-cordis` | `active` |
| `include:ui-workflow-run` | `@deepseek-ai/dsh-client-ui-workflow-run` | `active` |
| `include:ui-deliverables` | `@deepseek-ai/dsh-client-ui-deliverables` | `active` |
| `include:ui-workspace` | `@deepseek-ai/dsh-client-ui-workspace` | `active` |
| `include:ui-input-trigger` | `@deepseek-ai/dsh-client-ui-input-trigger` | `active` |
| `include:ui-commands` | `@deepseek-ai/dsh-client-ui-commands` | `active` |
| `include:ui-skill` | `@deepseek-ai/dsh-client-ui-skill` | `active` |
| `include:ui-subagent` | `@deepseek-ai/dsh-client-ui-subagent` | `active` |
| `include:ui-reference` | `@deepseek-ai/dsh-client-ui-reference` | `active` |
| `include:ui-jobs` | `@deepseek-ai/dsh-client-ui-jobs` | `active` |
| `include:ui-goal` | `@deepseek-ai/dsh-client-ui-goal` | `active` |
| `include:ui-message-feedback` | `@deepseek-ai/dsh-client-ui-message-feedback` | `active` |
| `include:ui-model-selection` | `@deepseek-ai/dsh-client-ui-model-selection` | `active` |
| `include:ui-permission` | `@deepseek-ai/dsh-client-ui-permission-presets` | `active` |
| `include:ui-agent-preset` | `@deepseek-ai/dsh-client-ui-agent-preset` | `active` |
| `include:ui-settings-plugins` | `@deepseek-ai/dsh-client-ui-settings-plugins` | `active` |
| `include:ui-plan` | `@deepseek-ai/dsh-client-ui-plan` | `active` |
| `include:ui-user-questions` | `@deepseek-ai/dsh-client-ui-user-questions` | `active` |
| `include:ui-trajectory` | `@deepseek-ai/dsh-client-ui-trajectory` | `active` |
| `include:agent-presets` | `@deepseek-ai/dsh-agent-presets` | `active` |
| `include:agent-presets:persona` | `@deepseek-ai/dsh-persona` | `active` |
| `include:agent-presets:agent-instructions` | `@deepseek-ai/dsh-agent-instructions` | `active` |
| `include:agent-presets:tool-bash` | `@deepseek-ai/dsh-tool-bash` | `active` |
| `include:agent-presets:tool-fs` | `@deepseek-ai/dsh-tool-fs` | `active` |
| `include:agent-presets:tool-fs-search` | `@deepseek-ai/dsh-tool-fs-search` | `active` |
| `include:agent-presets:tool-jobs` | `@deepseek-ai/dsh-tool-jobs` | `active` |
| `include:agent-presets:skill-filesystem` | `@deepseek-ai/dsh-skill-filesystem` | `active` |
| `include:agent-presets:tool-skill` | `@deepseek-ai/dsh-tool-skill` | `active` |
| `include:agent-presets:tool-goal` | `@deepseek-ai/dsh-tool-goal` | `active` |
| `include:agent-presets:tool-ask-user` | `@deepseek-ai/dsh-tool-ask-user` | `active` |
| `include:agent-presets:tool-todo` | `@deepseek-ai/dsh-tool-todo` | `active` |
| `include:agent-presets:tool-presentation` | `@deepseek-ai/dsh-agent-tool-presentation` | `active` |
| `include:agent-presets:terminal-service` | `@deepseek-ai/dsh-terminal` | `active` |
| `include:agent-presets:terminal-bash` | `@deepseek-ai/dsh-terminal-bash` | `active` |
| `include:agent-presets:terminal-tools` | `@deepseek-ai/dsh-tool-terminal` | `active` |
| `include:agent-presets:lsp-service` | `@deepseek-ai/dsh-lsp` | `active` |
| `include:agent-presets:lsp-stdio` | `@deepseek-ai/dsh-lsp-stdio` | `active` |
| `include:agent-presets:tool-lsp` | `@deepseek-ai/dsh-tool-lsp` | `active` |
| `include:agent-presets:plan-mode` | `@deepseek-ai/dsh-plan-mode` | `active` |
| `include:agent-presets:compaction-basic` | `@deepseek-ai/dsh-compaction-basic` | `active` |
| `include:agent-presets:command-compact` | `@deepseek-ai/dsh-command-compact` | `active` |
| `include:agent-presets:tool-result-pruner` | `@deepseek-ai/dsh-compaction-tool-result-pruner` | `active` |
| `include:agent-presets:tool-subagent-control` | `@deepseek-ai/dsh-tool-subagent-control` | `active` |
| `include:agent-presets:tool-subagent-list-agents` | `@deepseek-ai/dsh-tool-subagent-control/list-agents` | `active` |
| `include:agent-presets:tool-subagent` | `@deepseek-ai/dsh-tool-subagent` | `active` |
| `include:agent-presets:tool-subagent-fork` | `@deepseek-ai/dsh-tool-subagent` | `active` |
| `include:agent-presets:workflow-worker-thread` | `@deepseek-ai/dsh-workflow-worker-thread` | `active` |
| `include:agent-presets:tool-workflow` | `@deepseek-ai/dsh-tool-workflow` | `active` |
| `include:agent-presets:tool-ralph` | `@deepseek-ai/dsh-tool-ralph` | `active` |
| `include:takeover-mcp-filesystem` | `@deepseek-ai/dsh-mcp-client` | `active` |
| `8edf0ad4` | `@deepseek-ai/dsh-host-directory-picker-browse` | `active` |
| `2287b6ea` | `@deepseek-ai/dsh-client-ui-directory-picker-browse` | `active` |
| `64cab191` | `@deepseek-ai/cordis-plugin-hmr` | `active` |

## 主动关闭/未启用条目

| 条目 | 模块 |
|---|---|
| `include:hmr` | `@deepseek-ai/cordis-plugin-hmr` |
| `include:pwsh-sandbox` | `@deepseek-ai/dsh-pwsh-sandbox` |
| `include:tool-bash` | `@deepseek-ai/dsh-tool-bash` |
| `include:tool-pwsh` | `@deepseek-ai/dsh-tool-pwsh` |
| `include:tool-jobs` | `@deepseek-ai/dsh-tool-jobs` |
| `include:tool-fs` | `@deepseek-ai/dsh-tool-fs` |
| `include:tool-fs-search` | `@deepseek-ai/dsh-tool-fs-search` |
| `include:agent-instructions` | `@deepseek-ai/dsh-agent-instructions` |
| `include:skill-filesystem` | `@deepseek-ai/dsh-skill-filesystem` |
| `include:skill-badge` | `@deepseek-ai/dsh-skill-badge` |
| `include:tool-skill` | `@deepseek-ai/dsh-tool-skill` |
| `include:plan-mode` | `@deepseek-ai/dsh-plan-mode` |
| `include:compaction-basic` | `@deepseek-ai/dsh-compaction-basic` |
| `include:command-compact` | `@deepseek-ai/dsh-command-compact` |
| `include:tool-subagent-control` | `@deepseek-ai/dsh-tool-subagent-control` |
| `include:tool-subagent-list-agents` | `@deepseek-ai/dsh-tool-subagent-control/list-agents` |
| `include:tool-subagent` | `@deepseek-ai/dsh-tool-subagent` |
| `include:tool-subagent-fork` | `@deepseek-ai/dsh-tool-subagent` |
| `include:workflow-worker-thread` | `@deepseek-ai/dsh-workflow-worker-thread` |
| `include:tool-workflow` | `@deepseek-ai/dsh-tool-workflow` |
| `include:tool-result-pruner` | `@deepseek-ai/dsh-compaction-tool-result-pruner` |
| `include:tool-todo` | `@deepseek-ai/dsh-tool-todo` |
| `include:tool-goal` | `@deepseek-ai/dsh-tool-goal` |
| `include:tool-ralph` | `@deepseek-ai/dsh-tool-ralph` |
| `include:tool-str-replace-editor` | `@deepseek-ai/dsh-tool-str-replace-editor` |
| `include:tool-web` | `@deepseek-ai/dsh-tool-web` |
| `include:agent-presets:tool-pwsh` | `@deepseek-ai/dsh-tool-pwsh` |
| `include:agent-presets:tool-web` | `@deepseek-ai/dsh-tool-web` |
| `include:agent-presets:tool-subagent-codex` | `@deepseek-ai/dsh-tool-subagent` |
| `include:agent-presets:tool-subagent-claude-code` | `@deepseek-ai/dsh-tool-subagent` |
