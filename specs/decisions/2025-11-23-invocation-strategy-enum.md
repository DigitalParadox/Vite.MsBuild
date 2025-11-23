# ADR: Introduce InvocationStrategy Enum

Date: 2025-11-23
Status: Proposed
Supersedes: N/A
Superseded By: N/A

## Context
Current command invocation relies on three loosely coupled MSBuild properties:
- `ViteBuildCommand` (raw override)
- `DirectViteBuild` (boolean flag forcing direct CLI)
- `ViteBuildScript` (script name; empty string means direct build, absent means default `build` script lookup)

This implicit precedence caused ambiguity, undocumented behaviors (empty script = direct), and increased surface for misconfiguration. Multi-configuration scenarios also require consistent strategy classification for each config node.

## Problem Statement
The existing property trio:
1. Intermixes concerns (override vs strategy vs script name)
2. Requires implicit interpretation of empty values
3. Complicates per-config decision logic and testing
4. Limits future extension (e.g., adding a parallel dev-server strategy) without further property sprawl

## Options Considered
1. Keep existing properties; improve documentation
2. Introduce single enum `InvocationStrategy` with supporting fields
3. Replace properties with task-level metadata only (no MSBuild properties)

| Option | Pros | Cons |
|--------|------|------|
| 1 Keep | No migration cost | Retains ambiguity; harder to extend |
| 2 Enum | Clear semantics; extensible; testable | Requires migration layer |
| 3 Metadata-only | Pure graph-driven; minimal MSBuild surface | Less discoverable; harder for simple users |

## Decision
Adopt Option 2: Introduce `ViteInvocationStrategy` enum with values `Script`, `Direct`, `Override`. Supplementary properties:
- `ViteScriptName` (default: `build`) used only when `Script`
- `ViteOverrideCommand` used only when `Override`
- `ViteForceDirectFallback` (bool) controlling behavior when a script is missing (`true` → direct, `false` → error)

## Consequences
### Positive
- Deterministic, explicit command selection
- Simplified precedence elimination (direct mapping to enum)
- Easier unit and snapshot testing
- Reduces user cognitive load once migrated

### Negative / Risks
- Migration overhead (need adapter for legacy properties)
- Potential confusion during transition (dual property sets)

Mitigation: Provide warnings when legacy properties detected; docs map old → new.

## Detailed Design
New properties placed in `.props`:
```xml
<ViteInvocationStrategy Condition="'$(ViteInvocationStrategy)' == ''">Script</ViteInvocationStrategy>
<ViteScriptName Condition="'$(ViteScriptName)' == ''">build</ViteScriptName>
<ViteOverrideCommand Condition="'$(ViteOverrideCommand)' == ''"></ViteOverrideCommand>
<ViteForceDirectFallback Condition="'$(ViteForceDirectFallback)' == ''">true</ViteForceDirectFallback>
```

Legacy mapping logic (adapter task or inline resolver helper):
```
if (ViteBuildCommand != '') strategy=Override; override=ViteBuildCommand;
else if (DirectViteBuild == 'true') strategy=Direct;
else if (ViteBuildScript == '') strategy=Direct; // explicit opt-out
else strategy=Script; scriptName = ViteBuildScript or 'build';
```

Validation rules:
- If `strategy=Override` and `ViteOverrideCommand` empty → error VK-INV-001
- If `strategy=Script` and script not found & `ViteForceDirectFallback=false` → error VK-INV-002 with remediation
- If invalid enum value → error VK-INV-003

Graph node field: `strategy` (string), `scriptName`, `overrideCommand` consolidated.

## Alternatives Rejected
- Metadata-only approach rejected due to discoverability concerns.
- Retaining legacy properties rejected due to long-term maintenance complexity.

## Migration / Rollout Plan
1. Phase 1: Introduce new properties; continue honoring legacy silently.
2. Phase 2: Emit warning when legacy properties used.
3. Phase 3: Enable strict mode flag `<ViteUseLegacyProperties>false</ViteUseLegacyProperties>` to block legacy usage.
4. Phase 4: Remove legacy properties (major version bump).

## Test Strategy Impact
- Add unit tests for mapping scenarios (legacy → enum).
- Snapshot tests: pipeline JSON reflects chosen strategy.
- Negative tests: missing override command, missing script with fallback disabled.

## Open Questions
- Should `ViteInvocationStrategy=DevServer` be reserved for future live mode?
- Should fallback default be `false` (fail fast) instead of `true`?

## Future Follow-Ups
- ADR for incremental hashing integration with strategy-specific cache key components.
- ADR for introducing `DevServer` / watch strategy.

## References
- Redesign specification: `specs/vitekit-architecture-redesign-spec.md` (Sections 6 & 9)
