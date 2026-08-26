---
name: dsh-find-simplifications
description: Use to find code or architecture that can be deleted, merged, narrowed, or made more direct without weakening current behavior, contracts, security, or verification.
---

# Find simplifications

The target is lower complexity at equal or better observable behavior. Do not optimize for file count, age, or novelty.

## Read first

Read root `AGENTS.md`, the relevant root `README.md` architecture/subsystem sections, owning source/types/tests, and scoped `AGENTS.md`. Use the README historical decision index plus Git history when a strange boundary may be deliberate.

## Candidate tests

A strong candidate usually has one or more of these properties:

- two abstractions carry the same authority or state;
- a public option has one real value or no production consumer;
- a service method exists for one internal caller that could receive a private capability instead;
- a compatibility branch protects no supported format/platform/version;
- state is copied instead of derived from the authoritative owner;
- a wrapper merely renames another stable interface;
- a lifecycle state or background mechanism can be replaced by the framework's existing primitive;
- tests preserve implementation shape rather than externally required behavior.

Reject a simplification when it would weaken a security boundary, durability/wire compatibility, cleanup guarantee, cross-platform behavior, or a currently supported extension point.

## Evidence

Trace current consumers, configuration, persisted formats, public exports and negative tests. For deletion candidates, search for dynamic loading, string-based references and subprocess/worker entry points that static imports miss.

## Recording the outcome

- Tiny local work: use a precise TODO/FIXME only when it has a concrete owner/condition.
- Actionable larger work: record it in the issue/PR or implementation plan.
- A system-level decision that becomes current: update the owning section of root `README.md` and, when useful, its historical decision index.
- Do not recreate `.agents/notes/` or another proposal-document tree.

Validate the resulting change with the narrow owning tests and the repository checks selected by `dsh-pre-push-checks`.
