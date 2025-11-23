# ViteKit.MsBuild Architecture Redesign Specification

Status: Draft  
Target Version: 2.x (post current stable)  
Authors: TBD  
Last Updated: 2025-11-23

---
## 1. Overview
ViteKit.MsBuild currently delivers MSBuild integration for Vite by offloading most logic to C# tasks (configuration resolution + orchestration + command construction). While effective, several architectural limitations constrain extensibility, performance insight, and deterministic incremental builds.

This spec defines a future architecture focused on: deterministic build graphs, structured diagnostics, explicit invocation strategies, artifact caching, per-config package manager handling, and a formal migration path.

---
## 2. Goals
- Deterministic, inspectable build pipeline (graph-first design)
- Precise incremental rebuilds via content hashing of inputs & configuration
- Extensible multi-configuration dependency graph (topological ordering, cycle detection)
- Unified command invocation model with explicit strategies
- Per-configuration package manager restoration with deduplication
- Structured, machine-readable diagnostics & artifact manifests
- Environment variable layering & consistent injection
- Improved developer ergonomics (clear precedence rules, reduced implicit behavior)
- Backwards compatibility with graceful deprecation of legacy properties

---
## 3. Non-Goals
- Implementing full Vite dev-server integration (out-of-scope for build pipeline redesign)
- Replacing Vite’s own bundling or manifest mechanisms
- Providing a generic task runner competing with npm scripts
- Domain-specific asset fingerprinting beyond Vite outputs (future consideration)

---
## 4. Current Architecture Summary
| Concern | Current Implementation | Issue |
|---------|-----------------------|-------|
| Config Resolution | `ViteConfigurationResolver` yields `ITaskItem` with metadata | Metadata is untyped; coupling to string keys |
| Command Invocation | Precedence: `ViteBuildCommand` > `DirectViteBuild` > empty `ViteBuildScript` > script-based > fallback | Multiple overlapping properties create ambiguity |
| Multi-Config | Metadata (`BuildId`, `DependsOn`, optional flags) | No strongly-typed graph; limited validation |
| Incremental Build | Marker file concept exists, unused for fine-grain invalidation | Always rebuild; timestamp-only model |
| Package Manager | Global + optional per-config metadata | No lockfile fingerprint or workspace awareness |
| Diagnostics | Ad-hoc string logging with severity importance | Hard to parse programmatically; no timeline/events |
| Environment | Inline environment per task via semicolon encoding | No layered precedence; poor discoverability |
| Testing | Unit tests for command builder & tasks | No snapshot of entire pipeline or performance baselines |

---
## 5. Proposed Architecture (Graph-Centric)
Introduce a strongly typed `BuildGraph`:
```csharp
class BuildGraph {
  List<ConfigNode> Nodes { get; }
  IReadOnlyList<BuildPhase> Phases { get; }
}

class ConfigNode {
  string Id;                // BuildId
  string ConfigPath;        // Absolute path or virtual marker
  string? OutputDir;        // Optional override
  PackageManagerId PackageManager; // npm|pnpm|yarn|bun
  InvocationStrategy Strategy;     // Script | Direct | Override
  string? ScriptName;       // Used when Strategy=Script
  string? OverrideCommand;  // Used when Strategy=Override
  List<string> DependsOn;   // BuildId references
  EnvLayer Environment;     // Merged environment variables
}

enum InvocationStrategy { Script, Direct, Override }
```
Phases (logical, not all must exist):
1. Detect → Resolve config set & lockfiles
2. Restore → Package manager installations (deduped)
3. Prepare → Env generation, manifest pre-check
4. Build → Execute Vite commands per node
5. Optimize (optional) → Post processing tasks
6. Emit → Final artifact publishing / manifest write
7. Report → Structured summary (timings, cache hits)

`GenerateBuildGraphTask` creates the graph; subsequent tasks consume its serialized output (`obj/ViteKit.Graph.json`).

---
## 6. Command Strategy Redesign
Replace existing property triad with:
| New Property | Type | Description |
|--------------|------|-------------|
| `ViteInvocationStrategy` | string enum | `Script` | `Direct` | `Override` |
| `ViteScriptName` | string | Name of script when strategy=Script (default: `build`) |
| `ViteOverrideCommand` | string | Raw command when strategy=Override |
| `ViteForceDirectFallback` | bool | If script missing, fail or force direct |

Decision flow (single pass, explicit):
1. If `Strategy=Override` and `OverrideCommand` set → use override
2. If `Strategy=Direct` → direct `vite build`
3. If `Strategy=Script` → script existence check → run or fallback per `ViteForceDirectFallback`
4. If script missing & `ViteForceDirectFallback=false` → emit error with remediation

Caching script existence: maintain static dictionary keyed by `package.json` path + last write time.

---
## 7. Package Manager Handling Improvements
- Per-node detection via scanning nearest lockfile (if multiple present).
- Lockfile fingerprint: SHA256 of file contents stored in `obj/ViteKit.Lock.<pm>.hash`.
- Restore skip: if fingerprint unchanged & `node_modules` present.
- Workspaces:
  - Detect `pnpm-workspace.yaml`, Yarn workspaces via `package.json` `workspaces` field.
  - Consolidate restore tasks at workspace root.

---
## 8. Multi-Config Dependency Graph
Formal dependency resolution:
- Build adjacency list from `DependsOn` lists.
- Detect cycles (Kahn’s algorithm); emit categorized error.
- Topological ordering stable (alphabetical tie breaker for determinism).
- Allow future features: parallel execution scheduling & resource constraints.

Artifact interface:
```json
{
  "configs": [
    { "id": "admin", "outputDir": "wwwroot/admin", "hash": "..." }
  ],
  "version": 2,
  "generatedAt": "2025-11-23T10:20:11Z",
  "cacheHits": 3,
  "cacheMisses": 1
}
```

---
## 9. Incremental Build & Caching
Hash key composition per config:
```
Hash = SHA256(
  ConfigFileContent +
  EffectiveMode +
  InvocationStrategy + ScriptName/OverrideCommand +
  PackageManager +
  InputFilesContentHashes +
  EnvironmentLayerHash
)
```
Stored in `obj/ViteKit.Cache/<BuildId>.hash`. Rebuild only if hash changed or outputs missing.
Input file enumeration uses existing collection task; extend to compute content hash (stream + incremental buffer).

Partial invalidation: rebuild impacted node + downstream dependents only.

---
## 10. Environment Layering
Layers (lowest → highest precedence):
1. Global MSBuild properties prefixed `ViteEnv.` (`ViteEnv.API_BASE=https://...`)
2. Per-config metadata `Env:KEY=VALUE`
3. Dynamic runtime injection (timestamp, commit SHA)
4. Override env file (.env.production etc.) if present

Merged into `obj/ViteKit.Env.<BuildId>.json` and optionally written to `.env.generated` if requested.

---
## 11. Structured Diagnostics & Logging
Introduce JSON Lines log (opt-in): `obj/ViteKit.Diagnostics.log`:
```json
{"ts":"2025-11-23T10:10:00Z","phase":"Restore","config":"admin","event":"CacheHit"}
{"ts":"...","phase":"Build","config":"customer","event":"Started","command":"npm run build:prod"}
```
Log levels mapping:
| Level | Purpose |
|-------|---------|
| `error` | Build-stopping issues |
| `warn` | Non-fatal misconfig or fallback |
| `info` | High-level progress |
| `detail` | Per-task expansions |
| `trace` | Debug instrumentation |

Activation: `<ViteEnableStructuredDiagnostics>true</ViteEnableStructuredDiagnostics>` or MSBuild verbosity diagnostic.

---
## 12. Error Classification & Suggestions
Error object shape:
```json
{ "type":"Configuration", "code":"VK001", "message":"Duplicate BuildId 'admin'", "suggest":"Rename one BuildId" }
```
Categories: `Configuration`, `Dependency`, `Environment`, `Execution`, `Internal`.
Suggestion engine: pattern matching on common messages (e.g., missing script → propose adding script or switching strategy).

---
## 13. Testing Strategy Enhancements
| Test Type | Purpose |
|-----------|---------|
| Graph Snapshot Tests | Validate deterministic graph generation |
| Hash Invalidation Tests | Ensure selective rebuild correctness |
| Performance Baselines | Measure cold vs warm builds |
| Mutation Tests | Integrity of command decision logic |
| Workspace Restore Tests | Multi-package manager scenarios |
| Log Contract Tests | Structured diagnostics schema stability |

CI gating: fail if performance regression > threshold (e.g. +15% build time). 

---
## 14. Documentation Model & Generation
Create `schema/properties.yml`:
```yaml
properties:
  ViteInvocationStrategy:
    type: enum
    values: [Script, Direct, Override]
    default: Script
    description: Controls how Vite is invoked.
```
Generate:
- README property tables
- API reference sections
- Example decision tree (user selects strategy → required properties)

---
## 15. Migration & Backwards Compatibility
Phase-in approach:
1. Legacy Mode (default): old properties honored; new ones optional.
2. Hybrid Mode: warnings on legacy property usage; map internally to new model.
3. Strict Mode: legacy properties disabled; enforced via `<ViteUseLegacyProperties>false</ViteUseLegacyProperties>`.
4. Deprecation: after N versions, remove legacy properties entirely.

Mapping examples:
| Legacy | New Equivalent |
|--------|----------------|
| `ViteBuildCommand` | `ViteInvocationStrategy=Override` + `ViteOverrideCommand` |
| `DirectViteBuild=true` | `ViteInvocationStrategy=Direct` |
| `ViteBuildScript=xyz` | `ViteInvocationStrategy=Script` + `ViteScriptName=xyz` |
| empty `ViteBuildScript` | `ViteInvocationStrategy=Direct` (explicit) |

---
## 16. Phased Roadmap
| Phase | Focus | Deliverables |
|-------|-------|-------------|
| 1 Foundations | Strategy enum, BuildGraph skeleton, structured logging stub | New properties, graph JSON, basic logs |
| 2 Incremental | Hashing, artifact manifest, selective rebuild | Cache layer, hash tests |
| 3 Advanced | Workspace support, env layering, dependency ordering | Per-node PM, env files, ordering tests |
| 4 Optimization | Parallel builds (optional), performance tuning | Timing metrics, parallel executor |
| 5 Migration | Legacy property deprecation | Adapter warnings, strict mode |

---
## 17. Risks & Mitigations
| Risk | Impact | Mitigation |
|------|--------|-----------|
| Increased complexity | Harder onboarding | Progressive activation; defaults remain simple |
| Hashing overhead | Longer initial build | Cache file content hashes; parallel hashing |
| Parallel build ordering errors | Asset race conditions | Keep sequential until dependency verification passes |
| Workspace restore variance | Different PM behaviors | Extensive integration tests per PM |
| Backward compatibility confusion | Adoption friction | Clear deprecation timeline & warnings |

---
## 18. Open Questions
1. Should parallel build execution be opt-in per configuration (`<ParallelEligible>true</ParallelEligible>`)?
2. Should we generate cumulative source map index for multi-config builds?
3. Provide plugin API for third-party post-processing (e.g., image optimization)?
4. Optional remote cache integration (artifact server)? Future roadmap item.
5. Security considerations for environment injection—need allowlist?

---
## 19. Implementation Sequencing (Detailed)
**Phase 1 Tasks:**
- Add new properties & legacy mapping
- Implement `GenerateBuildGraphTask` (no hashing yet)
- Update targets to consume graph
- Introduce diagnostics writer (info level only)
- Minimal tests (graph generation for single/multi config)

**Phase 2 Tasks:**
- Content hash service (parallel I/O)
- Artifact manifest & selective invalidation logic
- Structured diagnostics enriched with timings

**Phase 3 Tasks:**
- Workspace root detection & consolidated restore tasks
- Enhanced dependency ordering & cycle tests
- Environment layering module

**Phase 4 Tasks:**
- Parallel executor prototype (respect dependency constraints)
- Performance benchmark integration in CI

**Phase 5 Tasks:**
- Warning set for legacy property usage
- Strict mode gating & doc updates

---
## 20. Acceptance Criteria
- Deterministic graph JSON identical across identical inputs.
- Selective rebuild: modifying one input file only rebuilds its config + dependents.
- Structured diagnostics consumable by external tooling (JSON schema validated).
- Clear error classification & actionable suggestions in common failure cases.
- Backwards compatibility mode passes existing test suite.
- Documentation auto-generated from property schema.

---
## 21. Appendix: Example Graph JSON (Phase 2)
```json
{
  "version": 2,
  "nodes": [
    {
      "id": "admin",
      "configPath": "C:/repo/src/admin/vite.config.ts",
      "outputDir": "wwwroot/admin",
      "packageManager": "pnpm",
      "strategy": "Script",
      "scriptName": "build:admin",
      "dependsOn": [],
      "hash": "f6b2...",
      "env": {"VITE_VERSION": "1.4.2"}
    },
    {
      "id": "customer",
      "configPath": "C:/repo/src/customer/vite.config.ts",
      "outputDir": "wwwroot/customer",
      "packageManager": "npm",
      "strategy": "Direct",
      "dependsOn": ["admin"],
      "hash": "9a8c...",
      "env": {"VITE_REGION": "us"}
    }
  ],
  "phases": ["Detect","Restore","Prepare","Build","Emit","Report"],
  "generatedAt": "2025-11-23T10:30:00Z"
}
```

---
## 22. Decision Log Template (Future)
Maintain `specs/decisions/` with ADR-style records:
```
# ADR: Introduce InvocationStrategy Enum
Date: 2025-12-05
Context: Simplify overlapping properties.
Decision: Add enum property; map legacy properties.
Consequences: Clearer code paths; migration complexity.
```

---
## 23. Conclusion
This redesign establishes a scalable foundation for ViteKit.MsBuild 2.x, enabling precise incremental builds, richer diagnostics, clearer configuration semantics, and future parallelization.

Next steps: implement Phase 1 tasks, draft property schema, begin adapter layer.
