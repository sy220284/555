---
name: dsh-trim-cot-leakage
description: Use when prose contains leaked authoring-session reasoning, dead design labels, PR/review narration, control-flow walkthroughs, or planning residue instead of current repository facts.
---

# Trim authoring-session leakage

Ask one question of every suspect passage: **could a reader at current HEAD verify every reference and claim without the private work session, PR discussion, or an uncommitted draft?** If not, restate the surviving facts from the repository's current vantage and remove the session transcript around them.

## Common leakage

- dead labels such as `(decision 7)`, `(audit C2)`, `design §4.7`, temporary phase codes;
- “this PR adds”, “a later PR”, “previous commit”, “this cut”, “now” used as change narration;
- reviewer-addressed defenses and round/version commentary;
- obvious control-flow walkthroughs and test narration;
- hedges such as “probably fine for now” with no explicit bound or TODO;
- untranslated scratch-language fragments in otherwise stable prose.

## Legitimate history/provenance

Keep resolvable issue references, standards citations, measured bounds, suppression reasons, current lifecycle terms such as “old connection drains before new connection accepts”, and counterfactual regression pins such as “without X, Y occurs”. Historical design rationale that still matters belongs in the root `README.md` summary; detailed old records are retrieved from Git history, not copied back into a parallel note tree.

## Workflow

1. Scope the audit; exclude `vendor/` and recorded test snapshots/fixtures unless the task explicitly targets them.
2. Run the recall patterns in `references/recall-batteries.md`, then read dense prose manually because patterns do not define the problem.
3. Apply the complete-proposition rule from `dsh-prose-standard` before deleting any sentence.
4. Fix the owner first: source JSDoc/prompt/config before generated artifacts; model-visible strings require their owning snapshot/test update.
5. Re-run the recall pass and relevant tests; for technical-document changes also run `pnpm run doc-sync`, `pnpm run lint`, and `git diff --check`.
