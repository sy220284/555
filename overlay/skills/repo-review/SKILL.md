---
name: repo-review
description: Review repository changes for correctness, security, lifecycle, interface contracts, unnecessary complexity, and regression risk; combine semantic code review with simplification analysis.
whenToUse: Use when reviewing a branch, pull request, patch, refactor, or substantial code change before approval or merge.
user-invocable: true
---

# Repository Review

Review the real outgoing change and enough surrounding code to understand its contract. Prefer a small number of substantiated findings over a long list of stylistic nits.

## 1. Establish scope

1. Confirm repository root, branch/worktree state, and requested comparison base.
2. Inspect the complete diff against the verified base, including staged, unstaged, and untracked files when relevant.
3. Read applicable repository instructions (`AGENTS.md`, contribution rules, architecture notes, package-level guidance) before judging conventions.
4. Trace changed public interfaces to both producers and consumers; do not review isolated lines without their lifecycle.

## 2. Review priorities

Evaluate in this order:

- correctness and broken required behavior;
- security, permissions, trust boundaries, and bypass paths;
- lifecycle, async races, cancellation, cleanup, ownership, and disposal;
- data durability, cache invalidation, event ordering, and state reconstruction;
- interface compatibility, defaults, error semantics, and caller expectations;
- tests that prove the intended regression rather than mirror implementation;
- documentation and visible strings that must change with behavior;
- unnecessary abstractions, duplicate state, speculative generality, and compatibility paths without current consumers.

## 3. Simplification pass

For every new abstraction, state variable, helper, option, wrapper, and compatibility branch, ask:

- Which current requirement or consumer needs it?
- Can existing code express the same contract more directly?
- Is state duplicated or derivable?
- Is a generic public API being added for one internal caller?
- Can branches, wrappers, or translations be deleted without losing behavior?
- Does the change make future maintenance easier or merely move complexity?

Do not simplify at the expense of explicit contracts, testability, safety, or performance evidence.

## 4. Evidence

Run the smallest relevant static checks and tests that cover the touched behavior. For risky changes, add or inspect a negative control that would fail for the intended regression. Verify real entry paths where practical instead of only unit-level helpers.

## 5. Report

For each finding state:

- severity: blocker / important / suggestion;
- precise location;
- concrete defect or risk;
- impact;
- supporting evidence;
- minimal corrective direction.

Separate confirmed defects from optional cleanup. If no material issue is found, state what scope and evidence were actually reviewed.
