---
name: repo-quality-gate
description: Select and run the smallest reliable pre-push or pre-merge validation set for the actual outgoing diff, then report exactly what passed, failed, or was not run.
whenToUse: Use before push, force-push, merge, release, handoff, or any claim that a repository change is ready.
user-invocable: true
---

# Repository Quality Gate

Treat readiness as evidence, not a feeling. Validate the actual outgoing change and do not reflexively run the entire repository when narrower checks prove the affected behavior.

## 1. Establish outgoing state

- Confirm repository root and branch/worktree status.
- Verify the comparison base or target branch from current repository state.
- Inspect committed, staged, unstaged, and untracked scope that will matter to the handoff.
- Read repository-specific scripts and contribution instructions before inventing commands.

## 2. Select gates by change type

Choose applicable checks from the project itself:

- formatting / whitespace / generated-file consistency;
- lint;
- type checking;
- focused unit tests for touched modules;
- integration or end-to-end tests when interfaces, processes, networking, storage, or UI behavior changed;
- build/package checks when build graph, exports, assets, manifests, or release files changed;
- migration/schema validation when persistence changed;
- documentation checks when public behavior or docs changed;
- `git diff --check` or repository equivalent for patch hygiene.

Prefer existing project scripts. Do not replace a project-specific gate with a weaker generic command.

## 3. Failure discipline

- Stop claiming readiness when any required gate fails.
- Diagnose whether a failure is introduced by the change, pre-existing, environmental, or unavailable.
- Never silently skip a gate because it is slow or inconvenient.
- If a check cannot run, record the exact reason and what evidence substitutes for it, if any.

## 4. Final repository hygiene

Before reporting ready:

- confirm no unintended generated artifacts, secrets, logs, caches, or temporary files are included;
- confirm intended files are tracked and no required file is left only in the worktree;
- re-read the final diff after automatic formatters or fixes;
- rerun any check invalidated by those fixes.

## 5. Report

Return a compact table or list containing:

- gate;
- exact command or mechanism;
- result;
- relevant scope;
- reason for any skip.

Only say “ready” when every required gate for the actual outgoing change has passed or an explicit limitation is disclosed.
