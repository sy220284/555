---
name: chatgpt-takeover-ops
description: Operate this Harness instance as a local execution workbench controlled from the current ChatGPT conversation; verify state before and after changes, prefer deterministic local execution, and keep recoverable checkpoints.
whenToUse: Use for repository work, builds, tests, environment inspection, local service management, and Harness workspace operations in takeover mode.
user-invocable: true
---

# ChatGPT Takeover Operations

1. Treat `/mnt/data` as the local project/artifact area and `$DSH_HOME` as Harness configuration state.
2. Inspect actual state before modifying it; never report deployment or tests as complete without verification.
3. Prefer bounded one-shot shell commands; use persistent terminal sessions for interactive or stateful work.
4. Use `run_code` when several tool calls can be safely composed into one deterministic operation.
5. Use LSP where a configured language server supports the target file; fall back to search/static inspection otherwise.
6. External web research and model reasoning are supplied by the controlling ChatGPT conversation; do not require Harness-native model or search credentials for local work.
7. Preserve rollback points for profile/preset configuration changes and record operational state in the workspace when useful.
