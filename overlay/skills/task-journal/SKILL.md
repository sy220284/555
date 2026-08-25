---
name: task-journal
description: Maintain a compact durable task journal that preserves current objective, decisions, evidence, blockers, checkpoints, and recovery instructions across long or interrupted work.
whenToUse: Use for multi-step work, long repository tasks, risky changes, environment rebuilds, or any task likely to be resumed after context loss or instance restart.
user-invocable: true
---

# Task Journal

Keep enough durable state to resume work accurately without turning the journal into a transcript. Record decisions and evidence that will matter later; omit routine narration.

## 1. Journal location

Prefer an existing project status/journal convention. Otherwise create a small task-scoped Markdown or JSON file in the working area. Do not overwrite authoritative project documentation with temporary execution notes.

## 2. Record only durable value

Maintain these fields when relevant:

- objective and explicit constraints;
- current repository/workspace and important paths;
- verified baseline state;
- decisions made and why;
- commands/checks that establish important evidence;
- files changed or artifacts produced;
- unresolved blockers and failed approaches worth avoiding;
- rollback/checkpoint information;
- next deterministic action required to resume;
- final verification status.

Do not copy private reasoning, verbose chat history, raw secrets, or huge command output into the journal.

## 3. Update rules

- Update after a meaningful decision, state transition, failure that changes the path, or successful checkpoint.
- Replace obsolete current-state entries instead of accumulating contradictory status.
- Preserve historical decisions only when reversing them later would be tempting or costly.
- Mark uncertain or unverified claims explicitly.

## 4. Recovery test

Before relying on the journal, ask whether a fresh operator could answer:

1. What are we trying to accomplish?
2. What has definitely been completed?
3. What evidence proves it?
4. What must not be repeated?
5. What is the next safe action?

If any answer is missing, update the journal.

## 5. Closeout

At completion, record final checks and artifact locations. Remove temporary notes that no longer have future value, or archive them if they preserve a durable decision or recovery path.
