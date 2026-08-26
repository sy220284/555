---
name: docs-quality
description: Review and improve technical documentation, comments, prompts, diagnostics, and visible strings for factual accuracy, ownership, structure, terminology, and concise complete prose.
whenToUse: Use when creating or reviewing README files, design docs, API docs, comments, prompts, diagnostics, release notes, or user-visible technical text.
user-invocable: true
---

# Documentation Quality

Treat technical prose as part of product behavior. It must match the code, live in the right place, and state complete propositions without implementation narration or review history.

## 1. Establish authority

1. Identify the owning code, configuration, schema, or behavior before editing derivative documentation.
2. Read repository documentation rules and terminology sources when present.
3. Update the authoritative source before generated, translated, or mirrored copies.

## 2. Required qualities

Every changed passage should be:

- factually correct against current behavior;
- complete enough to act on without hidden assumptions;
- placed at the narrowest durable owner of the information;
- consistent with project terminology and public names;
- concise without deleting important modal, temporal, negative, compatibility, or failure semantics;
- free of stale implementation narration, review conversation, duplicated rationale, and promises the code does not enforce.

## 3. Comments and API prose

Keep comments for non-obvious contracts, invariants, ownership, reasoning, and failure modes. Remove comments that merely restate syntax or narrate tests. Public API descriptions must document meaningful defaults, errors, side effects, lifecycle, and compatibility constraints when those matter to callers.

## 4. Diagnostics and visible strings

Diagnostics should identify the subject, violated rule, and corrective action when non-obvious. Prompts and model-visible text should expose only concepts needed for the task. Treat wording changes as behavioral changes when tests or snapshots depend on them.

## 5. Editing discipline

Classify candidates as keep, add, trim, restructure, restore, or defer. Do not manufacture edits to meet a deletion target. Preserve searchable mechanism names and deliberate emphasis where it carries meaning.

## 6. Validate

Run the repository's relevant documentation, formatting, link, snapshot, or generated-file checks. Inspect the final diff after formatting. Report the scope reviewed, substantive edits, deliberate keeps, and checks actually run.
