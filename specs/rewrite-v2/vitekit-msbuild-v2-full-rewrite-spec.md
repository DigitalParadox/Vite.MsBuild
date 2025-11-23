# ViteKit.MsBuild V2 Full Rewrite Specification

Status: Draft  
Target Release: 2.0.0 (Major)  
Last Updated: 2025-11-23  
Authors: TBD

----------------------------------------------------------------
## 1. Executive Summary
ViteKit.MsBuild V2 proposes a graph‑centric, deterministic, cache‑aware build integration between Vite and MSBuild. It replaces implicit property interplay with explicit strategies, introduces a structured build graph, selective incremental builds via hashing, per‑configuration package manager isolation, structured diagnostics, and a plugin extension model. Goals: faster warm builds, clarity, scalability to multi‑SPA enterprise solutions, and a controlled migration path from V1.

----------------------------------------------------------------
## 2. Design Principles
| Principle | Description | Implication |
|-----------|-------------|------------|
| Determinism | Same inputs → same graph & artifacts | Stable hashing + ordering |
| Incrementality | Only changed nodes (and dependents) rebuild | Hash ledger + dependency propagation |
| Observability | Machine-readable events & perf data | JSON lines + perf summary |
| Explicitness | Clear strategy enum & schema | Eliminates hidden precedence |
| Extensibility | Pluggable phases & env providers | Versioned plugin API |
| Performance | Sub-second warm incremental (single node) | Aggressive caching, minimized IO |
| Security & Integrity | Controlled env + tamper detection | Allowlist, sanitized overrides |
| Minimal Friction | Zero-config single SPA works | Sensible defaults (Script + build) |

Targets:
- Cold multi-config (5 SPAs, ~800 files) < 10s
- Warm single-config (1 file change) < 1.2s
- Warm multi-config (1 node change) < 2.0s
- Peak memory orchestration < 300MB

---
----------------------------------------------------------------
## 3. High-Level Architecture
Layered pipeline:
MSBuild Targets → GraphBuilder → StrategyResolver + PackageManagerResolver → CacheEngine → Executor → ManifestWriter → DiagnosticsEmitter → PluginHost → Report.

Modules:
1. GraphBuilder – Discovers configs, builds typed graph.
2. StrategyResolver – Assigns invocation strategy.
3. PackageManagerResolver – Per-config PM + workspace grouping.
4. CacheEngine – Hash & ledger; selective invalidation.
5. Executor – Phase runner with optional concurrency.
6. DiagnosticsEmitter – Structured events + perf metrics.
7. EnvironmentAggregator – Merges and materializes env layers.
8. PluginHost – Registers phases & callbacks.
9. MigrationAdapter – Legacy → new schema mapping.
10. ManifestWriter – Writes artifact manifests & index.

---
----------------------------------------------------------------
## 4. Data Models
### 4.1 BuildGraph
```json
{
  "version": 1,
  "nodes": [ { "id":"admin" } ],
  "edges": [ { "from": "admin", "to": "customer" } ],
  "generatedAt": "2025-11-23T10:00:00Z",
  "strictMode": true
}
```
### 4.2 ConfigNode
```json
{
  "id": "admin",
  "configPath": "C:/repo/apps/admin/vite.config.ts",
  "strategy": "Script",
  "scriptName": "build:admin",
  "overrideCommand": null,
  "packageManager": "pnpm",
  "workspaceRoot": "C:/repo",
  "outputDir": "wwwroot/admin",
  "mode": "production",
  "dependsOn": [],
  "envLayers": ["global","config","dynamic"],
  "inputs": [{"path":"apps/admin/src/main.ts","hash":"sha256:..."}],
  "configHash": "sha256:...",
  "buildHash": "sha256:...",
  "lastArtifactHash": "sha256:...",
  "flags": { "parallelEligible": true }
}
```
### 4.3 ArtifactManifest
```json
{
  "buildId": "admin",
  "buildHash": "sha256:...",
  "outputs": ["wwwroot/admin/index.js","wwwroot/admin/style.css"],
  "createdAt": "2025-11-23T10:05:00Z",
  "cacheHit": true,
  "durationMs": 1420
}
```
### 4.4 Diagnostics Event
```json
{
  "ts": "2025-11-23T10:11:02.123Z",
  "phase": "Build",
  "node": "admin",
  "event": "CommandStart",
  "command": "pnpm run build:admin",
  "strategy": "Script",
  "pm": "pnpm",
  "severity": "info"
}
```

---
----------------------------------------------------------------
## 5. Configuration Schema
Single source schema file (YAML) → generate docs & IntelliSense.
Key properties:
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ViteInvocationStrategy` | enum | `Script` | Invocation method |
| `ViteScriptName` | string | `build` | Script when strategy=Script |
| `ViteOverrideCommand` | string | `""` | Raw command when strategy=Override |
| `ViteForceDirectFallback` | bool | `true` | Direct fallback on missing script |
| `ViteEnableStructuredDiagnostics` | bool | `false` | Emit JSON events |
| `ViteEnableCache` | bool | `true` | Enable hash-based incremental |
| `ViteCacheDir` | string | `obj/ViteKit.Cache` | Cache storage path |
| `ViteEnv.*` | string | - | Global environment variables |
| `ViteStrictMode` | bool | `false` | Enforce strong validation |
| `ViteParallelism` | int | `1` | Concurrent build workers |

Per-config metadata synonyms (legacy): `BuildId`, `Mode`, `OutputDir`, `DependsOn`, `Env:KEY=VALUE`.

---
----------------------------------------------------------------
## 6. Pipeline Phases
| Phase | Description | Inputs | Outputs | Failure Mode |
|-------|-------------|--------|---------|--------------|
| Detect | Find configs, lockfiles | FS scan | Raw config list | Fatal on root missing |
| Graph | Build dependency graph | Config list | BuildGraph JSON | Fatal on cycle |
| Restore | Install packages intelligently | Graph nodes | node_modules state | Retry + warn |
| Prepare | Hash inputs, merge env | Graph + FS | Hash ledger, env files | Fatal on hash calc I/O error |
| Build | Execute Vite per node | Prepared node | Built assets | Fail fast per node |
| PostProcess | Optional plugin transforms | Assets | Optimized assets | Non-fatal unless critical plugin |
| Emit | Write manifests, copy assets | Asset set | ArtifactManifest JSON | Fatal on write error |
| Report | Summaries, timings | All previous | Build report | Non-fatal warnings |

---
----------------------------------------------------------------
## 7. Incremental Build Strategy
**Hash Composition:**
```
configHash = SHA256(
  normalizedConfigFileContent +
  invocationStrategy + scriptName + overrideCommand + mode + packageManager + envLayerHash
)
inputHash = Merkle(Tree(Hash(each input file content)))
buildHash = SHA256(configHash + inputHash)
```
**Invalidation Rules:**
- If `buildHash` matches previous and all outputs exist → skip build.
- If script changed OR lockfile fingerprint changed → rebuild.
- If dependency node rebuilds → downstream nodes flagged (propagate).

Store ledger: `cache.json` mapping BuildId → { buildHash, lastDurationMs }.

Warm Path Target: < 1.2s single node recompile excluding user plugin overhead.

---
----------------------------------------------------------------
## 8. Command Invocation Model
Decision algorithm (deterministic):
```
if strategy == Override:
  command = overrideCommand
elif strategy == Direct:
  command = <pm-run-vite-direct> (npx/pnpm dlx/yarn dlx/bun x)
elif strategy == Script:
  if script exists:
    command = <pm run script>
  else if forceDirectFallback:
    command = <pm-run-vite-direct>
  else error VK-INV-NOSCRIPT
```
Script existence cached by (package.json path + lastWriteTime) in static dictionary.

---
----------------------------------------------------------------
## 9. Package Manager Strategy
- Per-node lockfile detection: search upward until hitting workspace root or project root.
- Fingerprint: SHA256(lockfileContents).
- Group nodes by identical fingerprint + workspace root → single restore task.
- Support PNPM/Yarn workspaces:
  - Workspace root restore once.
  - Members rely on shared `node_modules`.
- Bun special case: no workspace; treat each root individually.

Error codes (draft):
| Code | Meaning | Action |
|------|---------|--------|
| VK-PM-UNSUPPORTED | Unknown PM | Fallback to npm + warn |
| VK-PM-FPCHANGE | Lockfile changed mid-build | Force rebuild |

---
----------------------------------------------------------------
## 10. Diagnostics & Telemetry
Emit JSON lines log: `obj/ViteKit.Diagnostics.log`.
Optional performance metrics file: `obj/ViteKit.Perf.json` summarizing phase durations & cache stats.
Support verbosity gating:
- `info`: Phase starts/ends
- `detail`: Per-node events
- `trace`: Hash details, command environment

Future extension: emit OpenTelemetry spans (phase as span, node build as child span).

---
----------------------------------------------------------------
## 11. Error Taxonomy & Codes
| Category | Prefix | Example |
|----------|--------|---------|
| Configuration | VK-CFG | Missing config file |
| Invocation | VK-INV | Missing script w/o fallback |
| PackageManager | VK-PM | Unsupported pm value |
| Dependency | VK-DEP | Cycle detected |
| Cache | VK-CAC | Hash ledger write failure |
| Execution | VK-EXE | Vite process exit != 0 |
| Plugin | VK-PLG | Plugin contract violation |
| Internal | VK-INT | Unexpected exception |

Errors include remediation suggestion and doc link.

---
----------------------------------------------------------------
## 12. Plugin & Extension API
Minimal initial surface:
```csharp
public interface IViteKitPlugin {
  string Id { get; }
  void Register(PluginContext ctx);
}

public sealed class PluginContext {
  public BuildGraph Graph { get; }
  public void AddPhase(string name, PhaseDelegate run, PhaseOrder order);
  public void OnNodeBuilt(Action<NodeResult> handler);
}
```
Constraints:
- Plugins cannot mutate resolved dependency edges post Graph phase.
- Phases declare concurrency safety.

---
----------------------------------------------------------------
## 13. Testing & Quality Gates
| Test Type | Tooling | Purpose |
|-----------|---------|---------|
| Graph Snapshot | JSON diff | Ensure deterministic generation |
| Hash Invalidation | Simulation harness | Validate selective rebuild correctness |
| PM Workspace | Integration directory matrix | Confirm grouped restore logic |
| Performance Baselines | Stopwatch & metrics capture | Detect regressions |
| Mutation Testing | Stryker/.NET + scripted | Validate robustness of decision logic |
| Error Surfacing | Table-driven tests | Ensure codes & suggestions appear |
| Plugin API | Mock plugin injection | Contract stability |

SLIs:
- Determinism: identical graph across 5 runs (100%).
- Cache Hit Accuracy: >= 99% for unchanged inputs.
- Median incremental build time: <1.5s.

SLO breach triggers: open issue + mark release candidate invalid.

---
----------------------------------------------------------------
## 14. Migration Plan
Phases:
1. **Compatibility Mode:** New properties optional; legacy fully supported.
2. **Hybrid Mode:** Warnings when legacy used; diagnostics show mapped strategy.
3. **Strict Mode:** `<ViteUseLegacyProperties>false>` errors on legacy usage.
4. **Removal:** Major release eliminates legacy properties.

Rollback: Feature flag to toggle old resolver for critical regressions.

---
----------------------------------------------------------------
## 15. Performance Targets & Benchmarks
Benchmark matrix (example):
| Scenario | Cold | Warm | Parallel (4 workers) |
|----------|------|------|----------------------|
| Single SPA (300 files) | <4s | <0.9s | N/A |
| Multi-SPA (5 x 300 files) | <11s | <2.0s changed single node | <8s cold |
| Workspace restore (pnpm) | <3s | cached: <0.5s | <3s |

Monitoring: store `Perf.json` in CI artifacts; compare against baseline thresholds.

---
----------------------------------------------------------------
## 16. Security & Integrity
- Environment allowlist: only `VITE_*` and explicit `AllowedEnv.*` pass through.
- Hash tamper detection: mismatch between recorded artifact manifest and actual outputs triggers warning.
- Command sanitization: override commands validated against denylist (e.g., destructive shell patterns configurable).

---
----------------------------------------------------------------
## 17. Documentation Generation
`schema/properties.yml` drives:
- README tables
- API reference (auto-regenerated in CI)
- Migration guide
- IntelliSense snippet packages (optional)

Doc validity test: ensure each property in schema appears once in generated docs.

---
----------------------------------------------------------------
## 18. Implementation Roadmap (Detailed)
**Phase 1 (Foundations):**
- Add new properties & adapter mapping
- Implement GraphBuilder (no hashing, sequential build)
- Introduce StrategyResolver
- Basic diagnostics emitter (info level)

**Phase 2 (Incremental & Cache):**
- Input hashing & ledger
- Selective invalidation logic
- ArtifactManifest writing
- Cache accuracy tests

**Phase 3 (Workspaces & Dependencies):**
- Workspace lockfile grouping
- Dependency cycle detection & topological sorting
- Parallel executor prototype (respect order)

**Phase 4 (Plugins & Advanced Diagnostics):**
- PluginHost API
- Structured JSON events with timing spans
- Performance budget CI gate

**Phase 5 (Migration & Hardening):**
- Strict mode enforcement
- Legacy deprecation warnings removal
- Final documentation generation & ADR updates

---
----------------------------------------------------------------
## 19. Risks & Mitigations
| Risk | Impact | Mitigation |
|------|--------|-----------|
| Increased complexity in graph build | Onboarding friction | Keep default single-config path minimal |
| Hash overhead for large repos | Longer cold build | Parallel hashing + lazy env layer hash |
| Concurrency race (parallel builds) | Corrupt outputs | Default sequential; add concurrency safe checks |
| Plugin misuse | Unstable builds | Restricted API surface + validation |
| Workspace variations | Restore inconsistencies | Comprehensive integration test matrix |

---
----------------------------------------------------------------
## 20. Open Questions
1. Should we support optional remote distributed cache integration in 2.x or defer to 3.x?
2. Add watch/dev-server integration into Graph (live phase) or keep separate model?
3. Provide first-party plugins (asset compression, manifest merging)?
4. Support cross-project graph spanning multiple .csproj in solution?

---
----------------------------------------------------------------
## 21. Acceptance Criteria
- Deterministic graph JSON identical across identical inputs (hash stable).
- Single node change triggers only dependent rebuilds.
- Structured diagnostics pass schema validation.
- Cache hit rate > 95% for unchanged builds in test suite.
- Plugin API supports adding a post-build phase in <20 LOC.
- Performance targets met in CI baseline.

---
----------------------------------------------------------------
## 22. ADR Linkage
Initial ADRs to derive from this spec:
- InvocationStrategy Enum (created)
- Hashing & Artifact Ledger
- Plugin Host & Phase Insertion
- Workspace Restore Grouping

---
----------------------------------------------------------------
## 23. Conclusion

----------------------------------------------------------------
## 24. Master Appendix (Consolidated)
Contains: End-to-end scenario, strict mode example, plugin sketch, performance targets, glossary (see below).

### 24.1 End-to-End Scenario (Narrative)
1. Detect finds three configs.
2. Graph builds dependencies; validates uniqueness.
3. Restore groups workspace nodes; skip unchanged fingerprint.
4. Prepare hashes inputs; only shared changed.
5. Build: shared rebuild; dependents rebuild if declaring dependency.
6. PostProcess plugin compresses CSS.
7. Emit manifests and ledger.
8. Report logs cache stats.

### 24.2 Strict Mode Example
```xml
<PropertyGroup>
  <ViteUseLegacyProperties>false</ViteUseLegacyProperties>
  <ViteInvocationStrategy>Override</ViteInvocationStrategy>
  <ViteOverrideCommand>pnpm exec vite build --mode staging --minify</ViteOverrideCommand>
  <ViteForceDirectFallback>false</ViteForceDirectFallback>
</PropertyGroup>
```

### 24.3 Plugin Sketch
```csharp
public class CompressionPlugin : IViteKitPlugin {
  public string Id => "compression";
  public void Register(PluginContext ctx) {
    ctx.AddPhase("Compress", async phase => {
      foreach (var asset in phase.Assets.Where(a => a.Path.EndsWith(".css"))) {
        // compress logic here
      }
    }, PhaseOrder.AfterBuild);
  }
}
```

### 24.4 Glossary
| Term | Definition |
|------|------------|
| Config Node | Single Vite configuration instance |
| BuildGraph | Structured representation of nodes & edges |
| Hash Ledger | Cache file mapping buildId → hash |
| Artifact Manifest | Per-node output summary |
| Workspace Root | Shared root for package manager workspace |
| Strategy | Invocation method (Script/Direct/Override) |
| Env Layer | Precedence tier for environment variables |

### 24.5 Performance KPIs
| KPI | Target |
|-----|--------|
| Cache Hit Rate | ≥95% unchanged builds |
| Warm Single Config | <1.2s |
| Warm Multi (1 change) | <2.0s |
| Cold 5 Configs | <11s |

### 24.6 Error Example
```json
{ "code":"VK-INV-NOSCRIPT", "severity":"error", "node":"admin", "message":"Script 'build:admin' not found.", "suggest":"Add script or set ViteForceDirectFallback=true" }
```

### 24.7 Future Extensions
Remote cache, DevServer strategy, cross-project graph, predictive hashing.

----------------------------------------------------------------
## 25. Final Acceptance Snapshot
All criteria must pass in CI before tagging 2.0.0 release.
This rewrite establishes a robust, extensible foundation designed for enterprise-scale multi-SPA scenarios while preserving ease of adoption for simple projects. Its graph-centric and cache-aware model prepares ViteKit.MsBuild for evolving needs (parallel builds, remote caching, plugin ecosystem) with clear migration semantics and measurable performance outcomes.

Next Action: Implement Phase 1 foundations and author remaining ADRs.
