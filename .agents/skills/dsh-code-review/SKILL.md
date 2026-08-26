---
name: dsh-code-review
description: Use when reviewing a pull request in this repository; prioritizes correctness, lifecycle, security, public contracts, model-visible behavior, and relevant verification against the current single-document architecture.
---

# Review a repository pull request

Verify the live base and exact head, then run `pnpm --silent run change-scope --base <verified-base-ref> --head <verified-head-ref>`. Read the diff plus enough owning code to understand the behavior; the scope report is discovery, not semantic proof.

## Sources of truth

- Root `AGENTS.md` and scoped `AGENTS.md` files: executable repository rules.
- Root `README.md`: architecture, defensive implementation rules, testing policy, public capability map, package index, Web-client constraints, and consolidated historical rationale.
- Public types/JSDoc/schema, current configuration and tests: exact package-level contracts.
- Git history: full historical decision records when the README summary is insufficient.
- `dsh-prose-standard`: semantic review of comments, prompts, diagnostics, visible strings, and technical prose.

## Blocking checks

1. **Behavior matches contract.** Trace both sides of every changed interface, including errors, cancellation, ownership and disposal.
2. **Lifecycle closes.** Registrations, listeners, processes, workers, terminals and background work must have a complete release path; check races before publication and cancellation during awaits.
3. **Security is enforced at the operation.** Schemas, prompts and facades are not enforcement. Follow every denial path to the filesystem, process, network, credential or sandbox operation it controls.
4. **Public facts stay synchronized.** Changes to defaults, config keys, events, status codes, wire data or user/model-visible behavior update owning public types/JSDoc/schema/tests and the root README when the system-level explanation changes.
5. **No speculative API expansion.** A generic service should not gain public surface for one private consumer when a capability closure or local helper suffices.
6. **Reactive/client rules hold.** For `packages/client`, use the scoped `AGENTS.md`; live render data must arrive through framework-owned hooks/stores, and slot ownership must remain explicit.
7. **Tests prove the regression.** Prefer external state, durable events, real Loader/bin/worker/subprocess entry paths and negative controls. A hand-mounted plugin or model self-report is not sufficient evidence for a production path.
8. **Model-visible changes are treated as behavior.** Inspect exact prompts, tool schemas/results, diagnostics and snapshots in every affected mode.
9. **Technical prose is current-state prose.** Remove review narration, stale design-session citations and duplicated manuals; retain only current contracts and durable rationale.
10. **Required evidence exists.** Run the narrow checks that would fail for the changed behavior, then broader build/doctor/cold-start checks when the workbench path is touched.

## Reporting

For each finding state the defect, exact location, impact and evidence. Separate blockers from suggestions. Do not list issues already conclusively enforced by a green gate unless the observed behavior disproves the gate.
