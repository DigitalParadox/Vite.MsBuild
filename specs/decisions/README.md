# Architecture Decision Records (ADR)

This directory stores decisions that shape the evolution of ViteKit.MsBuild.

## Purpose
ADR files document context, decision, and consequences in a lightweight, reviewable format. They provide historical traceability and rationale for non-trivial changes.

## Conventions
- File naming: `YYYY-MM-DD-short-title.md`
- Status values: `Proposed`, `Accepted`, `Deprecated`, `Superseded`
- Each ADR is immutable once *Accepted*; follow-up changes require a new ADR referencing the original.

## Index
(Automatically maintained manually for now; future tooling may generate.)

| Date | Title | Status | Supersedes |
|------|-------|--------|------------|
| 2025-11-23 | Introduce InvocationStrategy Enum | Proposed | |

## Workflow
1. Draft ADR using `_template.md`
2. Open PR referencing specification section(s)
3. Review & discussion
4. Merge when Accepted
5. Update this README index table

## Related
See master redesign spec: `specs/vitekit-architecture-redesign-spec.md`.
