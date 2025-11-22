---
layout: default
title: Build Decision Matrix
parent: Architecture
nav_order: 2
---

# Build Decision Matrix
{: .fs-9 }

Decision trees and scenario matrices for ViteKit.Msbuild build execution.
{: .fs-6 .fw-300 }

## Primary Decision Tree

```mermaid
graph TB
    Start[dotnet build] --> Enable{EnableViteBuild<br/>= true?}
    Enable -->|No| Skip[Skip All Vite Processing]
    Enable -->|Yes| PKG[package.json<br/>exists?]
    
    PKG -->|No| Error1[ERROR: No package.json]
    PKG -->|Yes| Multi{User Defined<br/>ViteConfig Items?}
    
    Multi -->|No| Single[Single Config Mode]
    Multi -->|Yes| MultiMode[Multi-Config Mode]
    
    Single --> AutoDetect{Vite config<br/>file exists?}
    AutoDetect -->|No| UseDefault[Use Default Config]
    AutoDetect -->|Yes| UseFound[Use Found Config]
    
    UseDefault --> Validate1[Validate Setup]
    UseFound --> Validate1
    MultiMode --> Resolve[Resolve Configurations]
    
    Resolve --> DepCheck{Has DependsOn<br/>Metadata?}
    DepCheck -->|No| Validate2[Validate Setup]
    DepCheck -->|Yes| DepSort[Topological Sort]
    
    DepSort --> Cycle{Circular<br/>Dependency?}
    Cycle -->|Yes| Error2[ERROR: Circular Deps]
    Cycle -->|No| Validate2
    
    Validate1 --> PM[Detect Package Manager]
    Validate2 --> PM
    
    PM --> Install{node_modules<br/>up to date?}
    Install -->|No| NPMInstall[Run npm/pnpm/yarn/bun install]
    Install -->|Yes| Collect[Collect Input Files]
    NPMInstall --> Collect
    
    Collect --> MSBCheck{MSBuild Inputs<br/>Newer Than<br/>Marker?}
    MSBCheck -->|No| FastSkip[SKIP: Fast Path]
    MSBCheck -->|Yes| CSCheck[Run C# Task]
    
    CSCheck --> ConfigLoop[For Each Config]
    ConfigLoop --> IsBuild{IsBuildRequired?}
    
    IsBuild -->|No| NextConfig[Next Config]
    IsBuild -->|Yes| BuildVite[Execute Vite Build]
    
    BuildVite --> Success{Exit Code<br/>= 0?}
    Success -->|No| Error3[ERROR: Build Failed]
    Success -->|Yes| UpdateMarker[Update Marker File]
    
    UpdateMarker --> NextConfig
    NextConfig --> More{More<br/>Configs?}
    More -->|Yes| ConfigLoop
    More -->|No| Complete[Build Complete]
```

## Incremental Build Decision Matrix

| Condition | Input Files Changed | Config Changed | Dependency Rebuilt | Output Manually Modified | Decision |
|-----------|---------------------|----------------|-------------------|-------------------------|----------|
| First Build (no marker) | N/A | N/A | N/A | N/A | **BUILD** |
| All unchanged | ❌ No | ❌ No | ❌ No | ❌ No | **SKIP** |
| Source file modified | ✅ Yes | ❌ No | ❌ No | ❌ No | **BUILD** |
| Config modified | ❌ No | ✅ Yes | ❌ No | ❌ No | **BUILD** |
| Dependency rebuilt | ❌ No | ❌ No | ✅ Yes | ❌ No | **BUILD** (cascade) |
| Output modified | ❌ No | ❌ No | ❌ No | ✅ Yes | **BUILD** (detected) |
| Multiple changes | ✅ Yes | ✅ Yes | ❌ No | ❌ No | **BUILD** |
| After `dotnet clean` | ❌ No | ❌ No | ❌ No | ❌ No | **BUILD** (no marker) |

## Command Type Decision Tree

```mermaid
graph TB
    Start[Determine Command Type] --> Custom{CustomCommand<br/>Property Set?}
    Custom -->|Yes| CustomCmd[CustomCommandBuilder]
    Custom -->|No| Direct{DirectViteBuild<br/>= true?}
    
    Direct -->|Yes| DirectCmd[DirectToolCommandBuilder]
    Direct -->|No| EmptyScript{BuildScript<br/>= empty string?}
    
    EmptyScript -->|Yes| DirectCmd
    EmptyScript -->|No| CheckScript[Check package.json]
    
    CheckScript --> ScriptExists{Script<br/>Exists?}
    ScriptExists -->|Yes| ScriptCmd[ScriptBasedCommandBuilder]
    ScriptExists -->|No| DirectCmd
    
    CustomCmd --> Output1[Custom: user-provided command]
    DirectCmd --> Output2[Direct: npx vite build]
    ScriptCmd --> Output3[Script: npm run build]
```

**Command Type Examples:**

| Configuration | Package.json Scripts | Result | Command |
|---------------|---------------------|--------|---------|
| Default | `"build": "vite build"` | Script-based | `npm run build` |
| `<DirectViteBuild>true</DirectViteBuild>` | Any | Direct tool | `npx vite build` |
| `<BuildScript></BuildScript>` | Any | Direct tool | `npx vite build` |
| `<CustomCommand>vite build --ssr</CustomCommand>` | Any | Custom | `vite build --ssr` |
| Default | No scripts | Direct tool | `npx vite build` |

## Mode Resolution Decision Tree

```mermaid
graph TB
    Start[Resolve Mode for Config] --> Prop{Config-Specific<br/>Property?<br/>AdminViteMode}
    
    Prop -->|Set| PropValue[Use Property Value]
    Prop -->|Not Set| ItemMeta{ItemGroup<br/>Mode Metadata?<br/>ViteConfig/Mode}
    
    ItemMeta -->|Set| ItemValue[Use ItemGroup Value]
    ItemMeta -->|Not Set| Global{Global<br/>ViteMode<br/>Property?}
    
    Global -->|Set| GlobalValue[Use Global Value]
    Global -->|Not Set| Default[Use Default:<br/>development]
    
    PropValue --> Done[EffectiveMode Set]
    ItemValue --> Done
    GlobalValue --> Done
    Default --> Done
```

**Mode Resolution Examples:**

| Global ViteMode | ItemGroup Mode | Config Property | Result | Priority |
|-----------------|----------------|-----------------|--------|----------|
| production | - | - | production | 3 (global) |
| production | staging | - | staging | 2 (ItemGroup) |
| production | staging | qa | qa | 1 (property - highest) |
| production | - | development | development | 1 (property) |
| - | - | - | development | 4 (default) |
| production | - | - | production | 3 (global) |

## Package Manager Detection Decision Tree

```mermaid
graph TB
    Start[Detect Package Manager] --> Explicit{PackageManager<br/>Property Set?}
    
    Explicit -->|Yes| UseExplicit[Use Specified Manager]
    Explicit -->|No| BunLock{bun.lockb<br/>exists?}
    
    BunLock -->|Yes| UseBun[Use Bun]
    BunLock -->|No| PnpmLock{pnpm-lock.yaml<br/>exists?}
    
    PnpmLock -->|Yes| UsePnpm[Use PNPM]
    PnpmLock -->|No| YarnLock{yarn.lock<br/>exists?}
    
    YarnLock -->|Yes| UseYarn[Use Yarn]
    YarnLock -->|No| NpmLock{package-lock.json<br/>exists?}
    
    NpmLock -->|Yes| UseNpm[Use NPM]
    NpmLock -->|No| DefaultNpm[Default to NPM]
    
    UseExplicit --> Result[Package Manager Set]
    UseBun --> Result
    UsePnpm --> Result
    UseYarn --> Result
    UseNpm --> Result
    DefaultNpm --> Result
```

## Common Build Scenarios

### Scenario 1: First Build (Clean State)

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSB as MSBuild
    participant Vite as ViteKit.Msbuild
    participant NPM as Package Manager
    participant V as Vite CLI

    Dev->>MSB: dotnet build
    Note over MSB: EnableViteBuild=true
    
    MSB->>Vite: ValidateViteSetup
    Vite->>Vite: ✅ package.json found
    Vite->>Vite: ✅ vite.config.ts found
    Vite->>Vite: ⚠️ node_modules missing
    
    MSB->>Vite: DetectPackageManager
    Vite->>Vite: Found pnpm-lock.yaml
    Vite-->>MSB: PackageManager=pnpm
    
    MSB->>Vite: EnsureNodeDependencies
    Vite->>NPM: pnpm install
    NPM-->>Vite: Dependencies installed
    Vite->>Vite: Create marker file
    
    MSB->>Vite: CollectViteInputs
    Vite->>Vite: Scan *.ts, *.vue, *.css
    Vite-->>MSB: 47 files collected
    
    MSB->>Vite: ViteBuildAssets
    Note over MSB: No marker = BUILD
    
    Vite->>Vite: IsBuildRequired?
    Note over Vite: No marker = true
    
    Vite->>V: pnpm run build
    V-->>Vite: Build success
    
    Vite->>Vite: Create obj/ViteKit.Msbuild.default.marker
    Vite-->>MSB: Build complete
    
    MSB->>Dev: Build succeeded
```

**Key Points:**
- No markers exist (first build)
- npm install runs (no node_modules)
- All files collected
- Build always executes
- Markers created for next build

**Duration:** ~30-60s (includes npm install)

---

### Scenario 2: No Changes (Incremental Skip)

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSB as MSBuild
    participant Vite as ViteKit.Msbuild

    Dev->>MSB: dotnet build
    Note over MSB: Second build, no changes
    
    MSB->>Vite: ValidateViteSetup
    Vite->>Vite: ✅ All checks pass
    
    MSB->>Vite: DetectPackageManager
    Vite->>Vite: Cached: pnpm
    
    MSB->>Vite: EnsureNodeDependencies
    Vite->>Vite: Marker up to date
    Note over Vite: SKIP: npm install
    
    MSB->>Vite: CollectViteInputs
    Vite->>Vite: Read from cache
    Vite-->>MSB: 47 files (cached)
    
    MSB->>Vite: ViteBuildAssets
    Note over MSB: Check Inputs/Outputs
    Note over MSB: All files older than marker
    Note over MSB: SKIP TARGET (Fast Path)
    
    MSB->>Dev: Build succeeded (1.2s)
```

**Key Points:**
- MSBuild Inputs/Outputs triggers fast skip
- C# task never executes
- No file scanning, no Vite execution
- Extremely fast (~1s)

**Duration:** ~1-2s

---

### Scenario 3: Single File Change

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSB as MSBuild
    participant Vite as ViteKit.Msbuild
    participant V as Vite CLI

    Dev->>Dev: Edit src/App.vue
    Dev->>MSB: dotnet build
    
    MSB->>Vite: ValidateViteSetup
    Vite->>Vite: ✅ All checks pass
    
    MSB->>Vite: EnsureNodeDependencies
    Note over Vite: SKIP: Up to date
    
    MSB->>Vite: CollectViteInputs
    Vite->>Vite: Read from cache
    
    MSB->>Vite: ViteBuildAssets
    Note over MSB: App.vue newer than marker
    Note over MSB: RUN TARGET
    
    Vite->>Vite: OrchestrateBuildTask
    Vite->>Vite: IsBuildRequired(default)?
    Note over Vite: src/App.vue timestamp check
    Vite->>Vite: 2025-01-15 10:30 > marker
    Note over Vite: BUILD REQUIRED
    
    Vite->>V: pnpm run build
    V-->>Vite: Build success (3.2s)
    
    Vite->>Vite: Update marker timestamp
    Vite-->>MSB: Build complete
    
    MSB->>Dev: Build succeeded (5.1s)
```

**Key Points:**
- MSBuild detects file change via Inputs/Outputs
- C# task confirms via IsBuildRequired()
- Only changed config rebuilds
- Marker updated with new timestamp

**Duration:** ~3-5s (Vite build time)

---

### Scenario 4: Multi-Config with Dependencies

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSB as MSBuild
    participant Vite as ViteKit.Msbuild
    participant V as Vite CLI

    Note over Dev: Project has shared + admin configs
    Note over Dev: admin DependsOn shared
    
    Dev->>Dev: Edit shared/Button.vue
    Dev->>MSB: dotnet build
    
    MSB->>Vite: ViteConfigurationResolver
    Vite->>Vite: Found vite.shared.config.ts
    Vite->>Vite: Found vite.admin.config.ts
    Vite-->>MSB: 2 configs
    
    MSB->>Vite: ViteConfigDependencyResolver
    Vite->>Vite: Parse DependsOn: admin→shared
    Vite->>Vite: Topological sort
    Vite-->>MSB: Order: [shared, admin]
    
    MSB->>Vite: ViteModeResolver
    Vite->>Vite: shared: production
    Vite->>Vite: admin: production
    
    MSB->>Vite: ViteBuildAssets
    Note over MSB: Button.vue in shared/ changed
    
    Vite->>Vite: IsBuildRequired(shared)?
    Note over Vite: shared/Button.vue changed
    Vite->>Vite: ✅ BUILD REQUIRED
    
    Vite->>V: pnpm run build --config vite.shared.config.ts
    V-->>Vite: Build success
    Vite->>Vite: Update shared marker (10:45:30)
    
    Vite->>Vite: IsBuildRequired(admin)?
    Note over Vite: Check dependencies
    Vite->>Vite: shared marker: 10:45:30
    Vite->>Vite: admin marker: 10:30:00
    Note over Vite: Dependency rebuilt!
    Vite->>Vite: ✅ BUILD REQUIRED (cascade)
    
    Vite->>V: pnpm run build --config vite.admin.config.ts
    V-->>Vite: Build success
    Vite->>Vite: Update admin marker (10:45:35)
    
    Vite-->>MSB: 2 builds completed
    MSB->>Dev: Build succeeded
```

**Key Points:**
- Dependency resolution orders builds
- shared builds first (source changed)
- admin builds second (dependency cascade)
- Both markers updated
- Next build with no changes: both skip

**Duration:** ~6-12s (2× Vite build time)

---

### Scenario 5: Config File Change

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSB as MSBuild
    participant Vite as ViteKit.Msbuild
    participant V as Vite CLI

    Dev->>Dev: Edit vite.config.ts
    Note over Dev: Changed build.outDir
    
    Dev->>MSB: dotnet build
    
    MSB->>Vite: ViteBuildAssets
    Note over MSB: vite.config.ts newer than marker
    
    Vite->>Vite: IsBuildRequired(default)?
    Vite->>Vite: Check config file timestamp
    Vite->>Vite: vite.config.ts: 11:05
    Vite->>Vite: marker: 10:45
    Note over Vite: Config changed!
    
    Vite->>V: pnpm run build
    V-->>Vite: Build with new outDir
    
    Vite->>Vite: Update marker
    Vite-->>MSB: Build complete
```

**Key Points:**
- Config changes always trigger rebuild
- Affects all configs using that file
- New output directory used
- Critical for config experimentation

**Duration:** ~3-5s

---

### Scenario 6: Parallel Build (Multiple Projects)

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSB as MSBuild
    participant P1 as Project1.csproj
    participant P2 as Project2.csproj
    participant PM as Package Manager

    Dev->>MSB: dotnet build -m
    Note over MSB: Parallel build mode
    
    par Project 1
        MSB->>P1: Build
        P1->>P1: EnsureNodeDependencies
        P1->>PM: Check shared marker
        Note over PM: obj/ViteKit.Msbuild.NodeRestore.marker
        PM-->>P1: Need install
        P1->>PM: npm install
        PM->>PM: Update shared marker
        PM-->>P1: Done
        P1->>P1: Build Vite assets
    and Project 2
        MSB->>P2: Build
        P2->>P2: EnsureNodeDependencies
        P2->>PM: Check shared marker
        Note over PM: Marker being written by P1
        P2->>P2: Wait for marker file
        PM-->>P2: Marker updated
        Note over P2: SKIP: Already installed
        P2->>P2: Build Vite assets
    end
    
    MSB->>Dev: Build succeeded
```

**Key Points:**
- Shared marker file prevents duplicate installs
- First project installs, others skip
- Parallel-build safe via marker file locking
- Significant time savings in CI/CD

**Duration:** ~30s (single install, parallel builds)

---

### Scenario 7: dotnet clean → dotnet build

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant MSB as MSBuild
    participant Vite as ViteKit.Msbuild
    participant FS as File System
    participant V as Vite CLI

    Dev->>MSB: dotnet clean
    
    MSB->>Vite: Clean Target
    Vite->>FS: Delete wwwroot/dist/
    Vite->>FS: Delete obj/*.marker
    Vite->>FS: Delete obj/Vite.InputFiles.cache
    Vite-->>MSB: Clean complete
    
    Note over Dev: All markers removed
    
    Dev->>MSB: dotnet build
    
    MSB->>Vite: ViteBuildAssets
    Note over MSB: No markers = always run
    
    Vite->>Vite: IsBuildRequired(default)?
    Vite->>Vite: No marker file found
    Note over Vite: ALWAYS BUILD
    
    Vite->>V: pnpm run build
    V-->>Vite: Full build (no cache)
    
    Vite->>Vite: Create new markers
    Vite-->>MSB: Build complete
```

**Key Points:**
- Clean removes all markers and outputs
- Next build is always full rebuild
- No incremental checks (no markers to compare)
- Ensures clean state for troubleshooting

**Duration:** ~30-60s (full rebuild)

---

### Scenario 8: CI/CD Build (First Time)

```mermaid
sequenceDiagram
    participant CI as CI Server
    participant Git as Git Clone
    participant MSB as MSBuild
    participant Vite as ViteKit.Msbuild
    participant NPM as npm ci
    participant V as Vite CLI

    CI->>Git: git clone
    Git-->>CI: Fresh repository
    Note over CI: No node_modules, no markers
    
    CI->>MSB: dotnet restore
    MSB-->>CI: NuGet packages restored
    Note over CI: ViteKit.Msbuild package installed
    
    CI->>MSB: dotnet build
    
    MSB->>Vite: DetectPackageManager
    Vite->>Vite: Found package-lock.json
    Vite-->>MSB: Use npm
    
    MSB->>Vite: EnsureNodeDependencies
    Vite->>NPM: npm ci
    Note over NPM: CI mode: exact versions
    NPM-->>Vite: Installed from lock file
    
    MSB->>Vite: CollectViteInputs
    Vite->>Vite: Scan all source files
    Vite-->>MSB: Files collected
    
    MSB->>Vite: ViteBuildAssets
    Note over MSB: No markers (first build)
    
    Vite->>V: npm run build
    V-->>Vite: Production build
    
    Vite->>Vite: Create markers
    Vite-->>MSB: Build complete
    
    CI->>MSB: dotnet publish
    MSB-->>CI: Artifacts ready
```

**Key Points:**
- Always full build (no markers in repo)
- Uses `npm ci` for reproducible builds
- Lock file ensures version consistency
- Markers not committed to git
- Build artifacts ready for deployment

**Duration:** ~60-120s (includes npm ci)

---

## Decision Matrix: When Does Each Config Build?

| Scenario | Config A (Standalone) | Config B (DependsOn: A) | Config C (DependsOn: B) |
|----------|----------------------|-------------------------|-------------------------|
| First build | ✅ BUILD | ✅ BUILD | ✅ BUILD |
| A source changed | ✅ BUILD | ✅ BUILD (cascade) | ✅ BUILD (cascade) |
| B source changed | ⏭️ SKIP | ✅ BUILD | ✅ BUILD (cascade) |
| C source changed | ⏭️ SKIP | ⏭️ SKIP | ✅ BUILD |
| A config changed | ✅ BUILD | ⏭️ SKIP | ⏭️ SKIP |
| After clean | ✅ BUILD | ✅ BUILD | ✅ BUILD |
| No changes | ⏭️ SKIP | ⏭️ SKIP | ⏭️ SKIP |

## Build Timing Decision Matrix

| ViteBuildTiming | Target Hook | Runs Before | Use Case |
|-----------------|-------------|-------------|----------|
| `BeforeCSharp` (default) | `BeforeTargets="ResolveStaticWebAssetsInputs"` | C# compilation | Standard web apps |
| `AfterCSharp` | `AfterTargets="Build"` | After C# build completes | Codegen scenarios |
| Not set | Same as `BeforeCSharp` | C# compilation | Default behavior |

## Error Scenarios

### Circular Dependency Detected

```
[ERROR] Circular dependency detected in Vite configurations:
  admin → shared → ui → admin

To fix this issue:
  1. Remove one of the dependencies to break the cycle
  2. Restructure your build dependencies
  3. Example: Remove DependsOn from 'ui' config

Current dependency chain: admin → shared → ui → admin
                                              └─────┘
                                              Cycle here
```

### Missing Dependency

```
[ERROR] Configuration 'admin' depends on 'shared', but 'shared' configuration not found

Available configurations:
  - admin
  - customer

To fix this issue:
  1. Add ViteConfig for 'shared'
  2. Or remove DependsOn from 'admin'
```

### Duplicate BuildId

```
[ERROR] Duplicate BuildId 'admin' found in multiple configurations:
  - vite.admin.config.ts (BuildId: admin)
  - Areas/Admin/vite.config.ts (BuildId: admin)

To fix this issue:
  1. Explicitly set unique BuildIds for each config
  2. Example:
     <ViteConfig Include="vite.admin.config.ts">
       <BuildId>admin-spa</BuildId>
     </ViteConfig>
```

## Performance Decision Matrix

| Build Type | MSBuild Check | C# Task Check | npm Install | Vite Build | Total Time |
|------------|---------------|---------------|-------------|------------|------------|
| First build | Skip | Skip | ✅ Run | ✅ Run | ~30-60s |
| No changes | ✅ Skip | N/A | Skip | Skip | ~1-2s |
| Source changed | Run | ✅ Skip others | Skip | ✅ Run changed | ~3-5s |
| Config changed | Run | ✅ Rebuild all | Skip | ✅ Run all | ~N×3-5s |
| After clean | Run | ✅ Run all | Skip | ✅ Run all | ~N×3-5s |
| Lock file changed | Run | Run | ✅ Run | ✅ Run | ~30-60s |

**Legend:**
- ✅ = Action taken
- ⏭️ = Skipped (optimization)
- N/A = Not reached (fast path)
- N = Number of configs

## Build Input Calculation Decision Tree

```mermaid
graph TB
    Start[Calculate Build Inputs] --> Step1{ViteInputFiles<br/>Already Populated?}
    
    Step1 -->|Yes| UseExisting[Use Existing Items]
    Step1 -->|No| CheckCache{Input Cache<br/>Exists?}
    
    CheckCache -->|Yes| ValidCache{Cache Valid?<br/>package.json/config<br/>unchanged?}
    ValidCache -->|Yes| ReadCache[Read from Cache]
    ValidCache -->|No| Scan
    
    CheckCache -->|No| Scan[Scan Project Root]
    
    Scan --> AdvTask{ViteUseAdvancedTasks<br/>= true?}
    
    AdvTask -->|Yes| CSTask[CollectViteInputFilesTask]
    AdvTask -->|No| MSBGlob[MSBuild Glob Patterns]
    
    CSTask --> IncludePatterns
    MSBGlob --> FallbackPatterns
    
    subgraph "C# Task Scanning (Advanced)"
        IncludePatterns[Include Patterns:<br/>**/*.ts, *.tsx, *.mts<br/>**/*.js, *.jsx, *.mjs<br/>**/*.vue, *.svelte<br/>**/*.css, *.scss, *.sass<br/>**/*.less, *.styl]
        
        IncludePatterns --> ExcludePatterns[Exclude Patterns:<br/>node_modules/**<br/>.git/**<br/>obj/**, bin/**<br/>dist/**<br/>{ViteOutputDir}/**]
        
        ExcludePatterns --> FilterExt[Filter by Extension:<br/>Check IsValidSourceFile()]
        
        FilterExt --> AddConfig[Add Config Files:<br/>$(ViteConfigFile)<br/>package.json]
        
        AddConfig --> WriteCache[Write to Cache]
    end
    
    subgraph "MSBuild Glob (Fallback)"
        FallbackPatterns[Simple Patterns:<br/>wwwroot/**/*.ts<br/>wwwroot/**/*.js<br/>wwwroot/**/*.vue<br/>wwwroot/**/*.css]
        
        FallbackPatterns --> AddConfigFB[Add Config Files]
        AddConfigFB --> NoCache[No Cache Written]
    end
    
    ReadCache --> Output
    WriteCache --> Output
    NoCache --> Output
    UseExisting --> Output
    
    Output[ViteInputFiles ItemGroup]
```

**Key Decision Points:**

| Condition | Path | Result |
|-----------|------|--------|
| `@(ViteInputFiles)` already set by user | Use Existing | User-defined files used |
| Cache exists & up to date | Read Cache | Fast path (~1ms) |
| Cache stale or missing | Scan Filesystem | C# task scans (~20ms) |
| `ViteUseAdvancedTasks=false` | MSBuild Glob | Fallback patterns (~50ms) |

## Build Output Calculation Decision Tree

```mermaid
graph TB
    Start[Calculate Output Directory] --> Multi{Multi-Config<br/>Build?}
    
    Multi -->|No| SingleConfig[Single Config Mode]
    Multi -->|Yes| PerConfig[Per-Config Calculation]
    
    SingleConfig --> GlobalProp{ViteOutputDir<br/>Property Set?}
    GlobalProp -->|Yes| UseGlobal[Use Global Property]
    GlobalProp -->|No| DefaultOut[Use Default: wwwroot/dist]
    
    PerConfig --> ConfigMeta{OutputDir<br/>Metadata Set?}
    ConfigMeta -->|Yes| UseMeta[Use Metadata Value]
    ConfigMeta -->|No| DetectArch[Detect Architecture]
    
    DetectArch --> CheckPath{Config File Path?}
    
    CheckPath --> Areas{Contains<br/>Areas/?}
    Areas -->|Yes| AreasArch[Architecture: Areas]
    
    CheckPath --> MultiSPA{Contains<br/>spa/ or spas/?}
    MultiSPA -->|Yes| MultiSPAArch[Architecture: MultiSPA]
    
    CheckPath --> Root{In Project<br/>Root?}
    Root -->|Yes| SPAArch[Architecture: SPA]
    
    subgraph "Areas Architecture"
        AreasArch --> ExtractArea[Extract Area Name<br/>from Path]
        ExtractArea --> AreasOut[OutputDir:<br/>wwwroot/{areaName}]
    end
    
    subgraph "MultiSPA Architecture"
        MultiSPAArch --> ExtractSPA[Extract SPA Name<br/>from Path]
        ExtractSPA --> MultiOut[OutputDir:<br/>wwwroot/spa/{spaName}]
    end
    
    subgraph "SPA Architecture"
        SPAArch --> BuildId{BuildId Set?}
        BuildId -->|default| SPADefault[OutputDir:<br/>wwwroot/dist]
        BuildId -->|custom| SPACustom[OutputDir:<br/>wwwroot/{buildId}]
    end
    
    UseGlobal --> Final[Final Output Path]
    DefaultOut --> Final
    UseMeta --> Final
    AreasOut --> Final
    MultiOut --> Final
    SPADefault --> Final
    SPACustom --> Final
    
    Final --> Validate{Validate<br/>Uniqueness}
    Validate -->|Conflict| Error[ERROR: Duplicate OutputDir]
    Validate -->|Unique| Success[Output Path Confirmed]
```

**Output Directory Examples:**

| Configuration | Path Pattern | Architecture | BuildId | Result OutputDir |
|---------------|--------------|--------------|---------|------------------|
| `vite.config.ts` | Root | SPA | default | `wwwroot/dist` |
| `vite.admin.config.ts` | Root | SPA | admin | `wwwroot/admin` |
| `Areas/Admin/vite.config.ts` | Areas/ | Areas | admin | `wwwroot/admin` |
| `spa/customer/vite.config.ts` | spa/ | MultiSPA | customer | `wwwroot/spa/customer` |
| User metadata | Any | Any | Any | User-specified value |
| `<ViteOutputDir>custom</ViteOutputDir>` | Single config | SPA | default | `custom` |

## Marker File Calculation Decision Tree

```mermaid
graph TB
    Start[Calculate Marker Path] --> Multi{Multi-Config?}
    
    Multi -->|No| Single[Single Config: default]
    Multi -->|Yes| EachConfig[For Each Config]
    
    Single --> BuildIdDef[BuildId = default]
    EachConfig --> GetBuildId[Get BuildId from Metadata]
    
    BuildIdDef --> BasePath
    GetBuildId --> BasePath[IntermediateOutputPath]
    
    BasePath --> Construct[Construct Marker Path:<br/>$(IntermediateOutputPath)<br/>ViteKit.Msbuild.{BuildId}.marker]
    
    Construct --> Example1[Example:<br/>obj/Debug/net8.0/<br/>ViteKit.Msbuild.default.marker]
    
    Construct --> Example2[Example:<br/>obj/Debug/net8.0/<br/>ViteKit.Msbuild.admin.marker]
    
    Example1 --> Usage
    Example2 --> Usage
    
    subgraph "Marker Usage"
        Usage[Marker File Purposes]
        Usage --> Use1[1. Incremental Build<br/>Compare timestamps]
        Usage --> Use2[2. Dependency Tracking<br/>Check dep markers]
        Usage --> Use3[3. MSBuild Outputs<br/>Target skip logic]
        Usage --> Use4[4. Build Status<br/>Existence check]
    end
```

**Marker File Paths by Configuration:**

| Scenario | BuildId | Marker Path |
|----------|---------|-------------|
| Single config, default | `default` | `obj/Debug/net8.0/ViteKit.Msbuild.default.marker` |
| Multi-config: admin | `admin` | `obj/Debug/net8.0/ViteKit.Msbuild.admin.marker` |
| Multi-config: shared | `shared` | `obj/Debug/net8.0/ViteKit.Msbuild.shared.marker` |
| Release build | `default` | `obj/Release/net8.0/ViteKit.Msbuild.default.marker` |

## Incremental Build Input/Output Decision Tree

```mermaid
graph TB
    Start[Incremental Build Check] --> MSBLevel[MSBuild Target Level]
    
    MSBLevel --> MSBInputs["Inputs:<br/>@(ViteInputFiles)<br/>$(ViteConfigFile)<br/>$(MSBuildProjectFile)"]
    
    MSBLevel --> MSBOutputs["Outputs:<br/>$(IntermediateOutputPath)ViteBuild.marker"]
    
    MSBInputs --> MSBCompare{MSBuild Compares:<br/>Any Input Newer<br/>Than Output?}
    
    MSBCompare -->|No| FastSkip[SKIP TARGET<br/>Fast Path ~1ms]
    MSBCompare -->|Yes| RunTarget[RUN TARGET]
    
    RunTarget --> CSLevel[C# Task Level]
    
    CSLevel --> ForEachConfig[For Each Config]
    
    ForEachConfig --> GetMarker[Get Marker Path:<br/>obj/ViteKit.Msbuild.{BuildId}.marker]
    
    GetMarker --> MarkerExists{Marker<br/>Exists?}
    
    MarkerExists -->|No| BuildReq1[BUILD REQUIRED<br/>First build]
    MarkerExists -->|Yes| GetTimestamp[Get Marker Timestamp]
    
    GetTimestamp --> CheckInputs[Check Input Files]
    
    CheckInputs --> InputLoop{For Each<br/>ViteInputFile}
    
    InputLoop --> InputTime{File.LastWriteTime<br/>> markerTime?}
    InputTime -->|Yes| BuildReq2[BUILD REQUIRED<br/>Source changed]
    InputTime -->|No| NextInput[Next File]
    
    NextInput --> MoreInputs{More<br/>Files?}
    MoreInputs -->|Yes| InputLoop
    MoreInputs -->|No| CheckConfig
    
    CheckConfig --> ConfigTime{Config.LastWriteTime<br/>> markerTime?}
    ConfigTime -->|Yes| BuildReq3[BUILD REQUIRED<br/>Config changed]
    ConfigTime -->|No| CheckDeps
    
    CheckDeps --> HasDeps{Has<br/>Dependencies?}
    HasDeps -->|No| UpToDate[UP TO DATE<br/>Skip build]
    HasDeps -->|Yes| DepLoop
    
    DepLoop --> ForEachDep{For Each<br/>Dependency}
    
    ForEachDep --> GetDepMarker[Get Dependency<br/>Marker Path]
    GetDepMarker --> DepMarkerTime{DepMarker.LastWriteTime<br/>> markerTime?}
    
    DepMarkerTime -->|Yes| BuildReq4[BUILD REQUIRED<br/>Dependency rebuilt]
    DepMarkerTime -->|No| CheckDepOutput
    
    CheckDepOutput --> DepOutputTime{Any file in<br/>DepOutputDir<br/>> markerTime?}
    
    DepOutputTime -->|Yes| BuildReq5[BUILD REQUIRED<br/>Dep output changed]
    DepOutputTime -->|No| NextDep[Next Dependency]
    
    NextDep --> MoreDeps{More<br/>Dependencies?}
    MoreDeps -->|Yes| ForEachDep
    MoreDeps -->|No| UpToDate
    
    BuildReq1 --> ExecuteBuild
    BuildReq2 --> ExecuteBuild
    BuildReq3 --> ExecuteBuild
    BuildReq4 --> ExecuteBuild
    BuildReq5 --> ExecuteBuild
    
    ExecuteBuild[Execute Vite Build]
    ExecuteBuild --> UpdateMarker[Update Marker<br/>File.SetLastWriteTime(Now)]
```

## Input File Collection Algorithm

```mermaid
graph TB
    Start[CollectViteInputFilesTask] --> Init[Initialize Lists:<br/>- sourceFiles<br/>- configFiles]
    
    Init --> GetRoot[ViteProjectRoot Path]
    GetRoot --> Scan[Enumerate Files<br/>SearchOption.AllDirectories]
    
    Scan --> ForEach{For Each File}
    
    ForEach --> GetRel[Get Relative Path]
    GetRel --> CheckExclude{Matches Exclude?<br/>node_modules/<br/>.git/<br/>obj/, bin/<br/>{OutputDir}/}
    
    CheckExclude -->|Yes| Skip[Skip File]
    CheckExclude -->|No| CheckExt[Get Extension]
    
    CheckExt --> IsConfig{Is Config File?<br/>vite.config.*<br/>package.json}
    
    IsConfig -->|Yes| AddConfig[Add to ConfigFiles]
    IsConfig -->|No| IsSource{Is Source File?<br/>.ts, .tsx, .js<br/>.vue, .svelte<br/>.css, .scss}
    
    IsSource -->|Yes| AddSource[Add to SourceFiles<br/>With Metadata:<br/>- Extension<br/>- RelativePath<br/>- LastModified]
    IsSource -->|No| Skip
    
    Skip --> More{More Files?}
    AddConfig --> More
    AddSource --> More
    
    More -->|Yes| ForEach
    More -->|No| Combine[Combine Lists]
    
    Combine --> Output[Output:<br/>ViteInputFiles<br/>ConfigurationFiles]
```

**File Inclusion Logic:**

```
Decision Flow for File: src/components/Button.vue
=================================================
1. Path: D:\MyProject\src\components\Button.vue
2. Relative: src\components\Button.vue
3. Exclude check:
   - Contains "node_modules"? NO
   - Contains ".git"? NO
   - Contains "obj" or "bin"? NO
   - Contains "wwwroot\dist"? NO
   → Not excluded ✅
4. Extension: .vue
5. IsSourceFile(".vue")? YES ✅
6. Result: INCLUDED in ViteInputFiles
7. Metadata:
   - Extension: .vue
   - RelativePath: src\components\Button.vue
   - LastModified: 2025-01-15 10:35:00

Decision Flow for File: node_modules/vite/dist/node/cli.js
==========================================================
1. Path: D:\MyProject\node_modules\vite\dist\node\cli.js
2. Relative: node_modules\vite\dist\node\cli.js
3. Exclude check:
   - Contains "node_modules"? YES
   → Excluded ❌
4. Result: SKIPPED

Decision Flow for File: vite.config.ts
======================================
1. Path: D:\MyProject\vite.config.ts
2. Relative: vite.config.ts
3. Exclude check: PASS
4. Name check:
   - Matches "vite.config.*"? YES ✅
5. Result: INCLUDED in ConfigurationFiles
```

## Output Directory Uniqueness Validation

```mermaid
graph TB
    Start[Validate Output Directories] --> Collect[Collect All OutputDirs<br/>from Resolved Configs]
    
    Collect --> Group[Group by OutputDir Value]
    
    Group --> Check{For Each Group}
    
    Check --> Count{Count > 1?}
    
    Count -->|No| NextGroup[Next Group]
    Count -->|Yes| Conflict[Duplicate Detected!]
    
    Conflict --> GetConfigs[Get Conflicting BuildIds]
    GetConfigs --> Error[ERROR: Duplicate OutputDir]
    
    Error --> LogError["Log Error Message:<br/>[ERROR] Multiple configs target<br/>same OutputDir: {path}<br/>Conflicts: {buildId1}, {buildId2}"]
    
    LogError --> Suggest["Suggest Fix:<br/>Set unique OutputDir metadata<br/>for each config"]
    
    NextGroup --> MoreGroups{More Groups?}
    MoreGroups -->|Yes| Check
    MoreGroups -->|No| Success[Validation Passed]
```

**Validation Example:**

```xml
<!-- BAD: Will fail validation -->
<ItemGroup>
  <ViteConfig Include="vite.admin.config.ts">
    <BuildId>admin</BuildId>
    <OutputDir>wwwroot/dist</OutputDir>
  </ViteConfig>
  <ViteConfig Include="vite.customer.config.ts">
    <BuildId>customer</BuildId>
    <OutputDir>wwwroot/dist</OutputDir>  <!-- CONFLICT! -->
  </ViteConfig>
</ItemGroup>

Error:
[ERROR] Multiple configurations target the same OutputDir: 'wwwroot/dist'
  Conflicting BuildIds: admin, customer

Fix:
  1. Set unique OutputDir for each config
  2. Example:
     <OutputDir>wwwroot/admin</OutputDir>
     <OutputDir>wwwroot/customer</OutputDir>

<!-- GOOD: Unique output directories -->
<ItemGroup>
  <ViteConfig Include="vite.admin.config.ts">
    <BuildId>admin</BuildId>
    <OutputDir>wwwroot/admin</OutputDir>
  </ViteConfig>
  <ViteConfig Include="vite.customer.config.ts">
    <BuildId>customer</BuildId>
    <OutputDir>wwwroot/customer</OutputDir>
  </ViteConfig>
</ItemGroup>
```

## Cache Strategy Decision Matrix

| Cache Type | Location | Invalidated By | Purpose |
|------------|----------|----------------|---------|
| Input Files Cache | `obj/Vite.InputFiles.cache` | Config/package.json change | Fast file collection |
| Build Marker | `obj/ViteKit.Msbuild.{BuildId}.marker` | Successful build | Incremental build tracking |
| Node Restore Marker | `obj/ViteKit.Msbuild.NodeRestore.marker` | Lock file change | Prevent duplicate installs |
| MSBuild Incremental | Built-in | Inputs/Outputs | Fast target skip |

## Optimization Decision Points

### Use Fast Path When:
- ✅ No source files changed
- ✅ No config files changed
- ✅ No dependencies rebuilt
- ✅ Marker files exist and are newer

### Run Full Build When:
- ❌ First build (no markers)
- ❌ After `dotnet clean`
- ❌ Lock file changed (forces npm install)
- ❌ CI/CD environment (no cached markers)

### Run Partial Build When:
- 🔄 Some configs need rebuild
- 🔄 Dependency cascade triggered
- 🔄 Specific config changed
