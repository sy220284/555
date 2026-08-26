---
name: dsh-prose-standard
description: Use to review or edit repository prose: the single technical README, JSDoc, comments, prompts, diagnostics, user-visible strings, functional instructions, and skill text.
---

# Prose standard

Repository prose must state verifiable current facts, complete contracts and durable rationale without leaking the authoring session.

## Scope rules

- Root `README.md` is the only human-maintained technical manual.
- Package-specific precision belongs in public types/JSDoc/schema/tests; do not recreate package READMEs.
- `AGENTS.md`, `CLAUDE.md`, `SKILL.md`, GitHub templates, snapshots/fixtures and legal notices remain separate only because they are functional/test/legal inputs.
- Generated catalogs/snapshots are derivative: edit the owner or scenario first, then regenerate.
- Historical rationale is summarized in the root README; full old records live in Git history.

## Complete-proposition rule

Before deleting or compressing a passage, enumerate every factual proposition it carries. The replacement must preserve every proposition that is still true and useful: obligations, exceptions, failure semantics, ownership, limits, security boundaries, extension conditions and counterexamples.

## Quality bar

Prefer direct current-state prose. Remove:

- review-thread or design-session narration;
- “used to / this PR / this cut / decision 7” style indexical history;
- implementation walkthroughs that add no contract;
- duplicated rationale with a clearer owner;
- filler, hedging and generic praise;
- comments that merely restate syntax or control flow.

Keep and sharpen:

- non-obvious invariants and why they matter;
- exact status/error/wire semantics;
- lifecycle and cleanup obligations;
- security assumptions and fail-closed behavior;
- measured limits and supported-platform facts;
- model-visible text whose exact wording is part of behavior.

## Workflow

1. Establish explicit scope and read the owning code/behavior.
2. Audit read-only first; group analogous issues under one principle.
3. Edit the authoritative source before derivatives.
4. Re-read the final diff for lost negation, modal strength, numbers, exceptions and terminology.
5. Run the surface-specific tests plus `pnpm run doc-sync`, `pnpm run lint`, and `git diff --check` when technical documentation is touched.

For chain-of-thought-style residue, use `dsh-trim-cot-leakage` as the specialized pass.
