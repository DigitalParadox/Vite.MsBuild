---
layout: default
title: Process Flow
parent: Architecture
nav_order: 1
---

# ViteKit.Msbuild Process Flow
{: .fs-9 }

Complete execution flow from MSBuild invocation to Vite build completion.
{: .fs-6 .fw-300 }

## High-Level Overview

```mermaid
graph TB
    A[dotnet build] --> B[MSBuild Evaluation]
    B --> C[ViteKit.Msbuild.props Imported]
    C --> D[Project .csproj Evaluated]
    D --> E[ViteKit.Msbuild.targets Imported]
    E --> F{EnableViteBuild?}
    F -->|false| Z[Skip Vite Build]
    F -->|true| G[Execute Build Pipeline]
    G --> H[Vite Assets Built]
    H --> I[Continue C# Build]
    I --> J[Build Complete]
```

## Detailed Build Pipeline

### Phase 1: MSBuild Initialization

```mermaid
graph LR
    A[NuGet Package Loaded] --> B[ViteKit.Msbuild.props]
    B --> C[Set Property Defaults]
    C --> D[Auto-detect Project Root]
    D --> E[User .csproj Evaluated]
    E --> F[ViteKit.Msbuild.targets]
```

**Key Actions:**
- `EnableViteBuild` defaults to `true`
- `ViteProjectRoot` auto-detected from `package.json` location
- `ViteMode` mapped from MSBuild `Configuration`
- Package manager detection deferred to later phase

### Phase 2: Configuration Resolution

```mermaid
graph TB
    A[ViteBuildAssets Target] --> B{Multi-Config?}
    B -->|No| C[Single Config Path]
    B -->|Yes| D[ViteConfigurationResolver]
    
    C --> E[Use Default Config]
    E --> F[Set BuildId=default]
    
    D --> G[Scan for Vite Configs]
    G --> H[Detect Architecture]
    H --> I[Resolve BuildIds]
    I --> J[Resolve Output Dirs]
    J --> K[Validate Configs]
    K --> L[Return ViteConfig Items]
```

**ViteConfigurationResolver Logic:**
1. Collect all `<ViteConfig>` items from user
2. Auto-detect Vite config files if none specified
3. Determine architecture from path patterns:
   - `Areas/*/` → Areas architecture
   - `spa/*/` or `spas/*/` → MultiSPA architecture
   - Root → SPA architecture
4. Generate smart defaults for BuildId and OutputDir
5. Validate for duplicates and conflicts

### Phase 3: Dependency Resolution

```mermaid
graph TB
    A[ViteConfigDependencyResolver] --> B[Parse DependsOn Metadata]
    B --> C[Build Dependency Graph]
    C --> D{Cycles Detected?}
    D -->|Yes| E[Log Error & Fail]
    D -->|No| F[Topological Sort]
    F --> G[Create Dependency Groups]
    G --> H[Return Ordered Configs]
```

**Dependency Graph Example:**
```
Input:
  shared (no deps)
  admin (DependsOn: shared)
  customer (DependsOn: shared)

Output Order:
  1. shared
  2. admin, customer (parallel group)
```

### Phase 4: Mode Resolution

```mermaid
graph TB
    A[ViteModeResolver] --> B{For Each Config}
    B --> C{Config-Specific<br/>Property Set?}
    C -->|Yes| D[Use Property Value]
    C -->|No| E{ItemGroup<br/>Mode Set?}
    E -->|Yes| F[Use ItemGroup Mode]
    E -->|No| G{Global<br/>ViteMode Set?}
    G -->|Yes| H[Use Global Mode]
    G -->|No| I[Use Default Mode]
    
    D --> J[Set EffectiveMode]
    F --> J
    H --> J
    I --> J
    J --> K[Next Config]
```

**Mode Override Hierarchy:**
1. **Config-specific property** (e.g., `$(AdminViteMode)`)
2. **ItemGroup Mode** (e.g., `<Mode>staging</Mode>`)
3. **Global ViteMode** (e.g., `$(ViteMode)`)
4. **Default mode** (development)

### Phase 5: Validation

```mermaid
graph TB
    A[ValidateViteSetup] --> B[Check package.json]
    B --> C{Found?}
    C -->|No| D[Log Error]
    C -->|Yes| E[Validate Vite Config]
    E --> F{Config Valid?}
    F -->|No| G[Log Warning]
    F -->|Yes| H[Check node_modules]
    H --> I[Display Welcome Message]
```

**ValidateViteProjectTask Checks:**
- ✅ `package.json` exists
- ✅ Vite config file exists (or use default)
- ✅ `node_modules/` present (if `RequireNodeModules=true`)
- ✅ Vite dependency installed
- ⚠️ Environment files (`.env`, `.env.production`) - warnings only

### Phase 6: Package Manager Detection & Node Dependencies

```mermaid
graph TB
    A[DetectPackageManager] --> B{PackageManager<br/>Specified?}
    B -->|Yes| C[Use Specified]
    B -->|No| D[Scan Lock Files]
    D --> E{bun.lockb?}
    E -->|Yes| F[Use Bun]
    E -->|No| G{pnpm-lock.yaml?}
    G -->|Yes| H[Use PNPM]
    G -->|No| I{yarn.lock?}
    I -->|Yes| J[Use Yarn]
    I -->|No| K[Use NPM Default]
    
    F --> L[EnsureNodeDependencies]
    H --> L
    J --> L
    K --> L
    C --> L
    
    L --> M{node_modules<br/>Exists?}
    M -->|Yes| N{Lock File<br/>Newer?}
    N -->|No| O[Skip Install]
    N -->|Yes| P[Run Install]
    M -->|No| P
    P --> Q[Update Marker File]
```

**Incremental Install Logic:**
- Uses marker file: `obj/ViteKit.Msbuild.NodeRestore.marker`
- Compares timestamps: marker vs lock file
- Parallel-build safe (shared marker across projects)

### Phase 7: Input File Collection

```mermaid
graph TB
    A[CollectViteInputs] --> B{Cache Exists?}
    B -->|Yes| C[Read from Cache]
    B -->|No| D[CollectViteInputFilesTask]
    
    D --> E[Scan Project Root]
    E --> F[Include Patterns]
    F --> G[**/*.ts, *.tsx, *.js, *.jsx]
    F --> H[**/*.vue, *.svelte]
    F --> I[**/*.css, *.scss, *.sass]
    G --> J[Exclude Patterns]
    H --> J
    I --> J
    J --> K[node_modules/]
    J --> L[.git/, obj/, bin/]
    J --> M[ViteOutputDir/]
    K --> N[Return File List]
    L --> N
    M --> N
    N --> O[Write Cache]
    
    C --> P[ViteInputFiles ItemGroup]
    O --> P
```

**Purpose:** Populate `@(ViteInputFiles)` for incremental build `Inputs`/`Outputs` tracking.

### Phase 8: Build Orchestration

```mermaid
graph TB
    A[ViteBuildAssets or<br/>ViteBuildAssetsAfter] --> B{Inputs Newer<br/>Than Outputs?}
    B -->|No| Z[Skip Build<br/>MSBuild Fast-Path]
    B -->|Yes| C[ExecuteViteBuild]
    C --> D[OrchestrateBuildTask]
    
    D --> E[For Each Config<br/>In Dependency Order]
    E --> F[IsBuildRequired?]
    F -->|No| G[Skip Config]
    F -->|Yes| H[Build Config]
    
    H --> I[Construct Vite Command]
    I --> J[Execute Build]
    J --> K[Update Marker]
    K --> L{More Configs?}
    L -->|Yes| E
    L -->|No| M[All Builds Complete]
    
    G --> L
```

**Two-Tier Incremental Build:**
1. **MSBuild Level:** `Inputs="@(ViteInputFiles)"` / `Outputs="marker"` - Fast skip
2. **C# Task Level:** `IsBuildRequired()` - Handles dependency cascades

### Phase 9: Build Execution Detail

```mermaid
graph TB
    A[OrchestrateBuildTask.BuildConfig] --> B[ViteCommandBuilder]
    B --> C{Determine Command Type}
    
    C --> D{CustomCommand?}
    D -->|Yes| E[CustomCommandBuilder]
    
    C --> F{DirectViteBuild?}
    F -->|Yes| G[DirectToolCommandBuilder]
    
    C --> H{Script Exists?}
    H -->|Yes| I[ScriptBasedCommandBuilder]
    H -->|No| G
    
    E --> J[Build IViteCommand]
    G --> J
    I --> J
    
    J --> K[Add --config]
    K --> L[Add --mode]
    L --> M[Add --outDir]
    M --> N[Add --logLevel]
    N --> O[Set Environment Variables]
    O --> P[Execute Process]
    P --> Q{Exit Code 0?}
    Q -->|Yes| R[Update Marker]
    Q -->|No| S[Log Error]
```

**Command Generation Examples:**

**Script-based (npm):**
```bash
npm run build -- --config vite.admin.config.ts --mode production --outDir wwwroot/admin
```

**Direct tool (npx):**
```bash
npx vite build --config vite.admin.config.ts --mode production --outDir wwwroot/admin
```

### Phase 10: Incremental Build Decision Logic

```mermaid
graph TB
    A[IsBuildRequired] --> B{Marker File<br/>Exists?}
    B -->|No| C[BUILD REQUIRED]
    B -->|Yes| D[Get Marker Timestamp]
    
    D --> E{Any Input File<br/>Newer?}
    E -->|Yes| F[BUILD REQUIRED<br/>Source Changed]
    E -->|No| G{Config File<br/>Newer?}
    
    G -->|Yes| H[BUILD REQUIRED<br/>Config Changed]
    G -->|No| I{Has Dependencies?}
    
    I -->|No| J[SKIP BUILD<br/>Up to Date]
    I -->|Yes| K{Any Dependency<br/>Rebuilt?}
    
    K -->|Yes| L[BUILD REQUIRED<br/>Dependency Changed]
    K -->|No| M{Dependency Output<br/>Newer?}
    
    M -->|Yes| N[BUILD REQUIRED<br/>Dep Output Changed]
    M -->|No| J
```

**Marker Files:**
- Location: `obj/ViteKit.Msbuild.{BuildId}.marker`
- Purpose: Track last successful build time
- Compared against: Input files, config files, dependency markers

### Complete Flow Example: Multi-Config with Dependencies

```mermaid
sequenceDiagram
    participant MSB as MSBuild
    participant VCR as ViteConfigurationResolver
    participant VDR as ViteConfigDependencyResolver
    participant VMR as ViteModeResolver
    participant OBT as OrchestrateBuildTask
    participant VCB as ViteCommandBuilder
    participant Vite as Vite Process

    MSB->>VCR: Resolve configurations
    VCR->>VCR: Detect vite.shared.config.ts
    VCR->>VCR: Detect vite.admin.config.ts (DependsOn: shared)
    VCR->>MSB: Return 2 configs
    
    MSB->>VDR: Resolve dependencies
    VDR->>VDR: Parse dependency graph
    VDR->>VDR: Topological sort
    VDR->>MSB: Return [shared, admin]
    
    MSB->>VMR: Resolve modes
    VMR->>VMR: shared: global mode (production)
    VMR->>VMR: admin: config property (staging)
    VMR->>MSB: Return modes
    
    MSB->>OBT: Execute builds
    
    OBT->>OBT: IsBuildRequired(shared)?
    OBT->>OBT: Yes - input changed
    OBT->>VCB: Build command (shared)
    VCB->>Vite: npm run build --mode production
    Vite-->>OBT: Success
    OBT->>OBT: Update shared marker
    
    OBT->>OBT: IsBuildRequired(admin)?
    OBT->>OBT: Yes - dependency rebuilt
    OBT->>VCB: Build command (admin)
    VCB->>Vite: npm run build --mode staging
    Vite-->>OBT: Success
    OBT->>OBT: Update admin marker
    
    OBT->>MSB: All builds complete
```

## Configuration to Build Data Flow

### Complete Configuration Resolution Pipeline

```mermaid
graph TB
    subgraph "Input Sources"
        A1[User ViteConfig Items]
        A2[File System Scan]
        A3[Package.json]
        A4[MSBuild Properties]
    end
    
    subgraph "Configuration Resolution"
        B1[ViteConfigurationResolver]
        B2[Parse User Items]
        B3[Auto-detect Configs]
        B4[Detect Architecture]
        B5[Generate BuildIds]
        B6[Generate OutputDirs]
        B7[Validate Uniqueness]
    end
    
    subgraph "Dependency Processing"
        C1[Parse DependsOn]
        C2[Build Dependency Graph]
        C3[Detect Cycles]
        C4[Topological Sort]
        C5[Group by Depth]
    end
    
    subgraph "Mode Resolution"
        D1[For Each Config]
        D2[Check Property Override]
        D3[Check ItemGroup Mode]
        D4[Check Global ViteMode]
        D5[Apply Default Mode]
        D6[Set EffectiveMode]
    end
    
    subgraph "Input Collection"
        E1[CollectViteInputFilesTask]
        E2[Scan ViteProjectRoot]
        E3[Apply Include Patterns]
        E4[Apply Exclude Patterns]
        E5[Filter by Extension]
        E6[Generate File List]
    end
    
    subgraph "Build Decision"
        F1[For Each Config]
        F2[Get Marker Path]
        F3[Check Marker Exists]
        F4[Compare Input Timestamps]
        F5[Compare Config Timestamp]
        F6[Check Dependency Markers]
        F7[Check Output Dir Files]
        F8[Determine Build Required]
    end
    
    subgraph "Build Outputs"
        G1[ViteCommand]
        G2[Build Marker File]
        G3[Output Assets]
        G4[Build Status]
    end
    
    A1 --> B2
    A2 --> B3
    A3 --> B4
    A4 --> B5
    
    B2 --> B1
    B3 --> B1
    B1 --> B4
    B4 --> B5
    B5 --> B6
    B6 --> B7
    B7 --> C1
    
    C1 --> C2
    C2 --> C3
    C3 --> C4
    C4 --> C5
    C5 --> D1
    
    D1 --> D2
    D2 --> D3
    D3 --> D4
    D4 --> D5
    D5 --> D6
    D6 --> E1
    
    E1 --> E2
    E2 --> E3
    E3 --> E4
    E4 --> E5
    E5 --> E6
    E6 --> F1
    
    F1 --> F2
    F2 --> F3
    F3 --> F4
    F4 --> F5
    F5 --> F6
    F6 --> F7
    F7 --> F8
    F8 --> G1
    
    G1 --> G2
    G2 --> G3
    G3 --> G4
```

### Detailed Input/Output Data Flow

```mermaid
flowchart TB
    subgraph "Configuration Inputs"
        direction TB
        I1["<b>User ViteConfig Items</b><br/>Include: vite.*.config.ts<br/>Metadata: BuildId, OutputDir, DependsOn, Mode"]
        I2["<b>MSBuild Properties</b><br/>ViteProjectRoot<br/>ViteMode<br/>ViteOutputDir<br/>PackageManager"]
        I3["<b>File System</b><br/>vite.config.ts<br/>package.json<br/>Lock files<br/>Source files"]
    end
    
    subgraph "Phase 1: Configuration Resolution"
        direction TB
        P1A["ViteConfigurationResolver.Execute()"]
        P1B["ResolvedConfigs: ITaskItem[]"]
        P1C["Metadata per Config:<br/>• BuildId (e.g., 'admin')<br/>• ConfigFile (absolute path)<br/>• OutputDir (relative path)<br/>• ProjectRoot<br/>• Architecture (SPA/MultiSPA/Areas)"]
    end
    
    subgraph "Phase 2: Dependency Resolution"
        direction TB
        P2A["ViteConfigDependencyResolver.Execute()"]
        P2B["OrderedConfigurations: ITaskItem[]"]
        P2C["Metadata per Config:<br/>• BuildOrder (integer)<br/>• DependencyGroup (depth)<br/>• DependsOn (comma-separated)<br/>+ All previous metadata"]
    end
    
    subgraph "Phase 3: Mode Resolution"
        direction TB
        P3A["ViteModeResolver.Execute()"]
        P3B["ResolvedConfigs: ITaskItem[]"]
        P3C["Metadata per Config:<br/>• EffectiveMode (resolved)<br/>• ModeSource (property/item/global/default)<br/>+ All previous metadata"]
    end
    
    subgraph "Phase 4: Input File Collection"
        direction TB
        P4A["CollectViteInputFilesTask.Execute()"]
        P4B["ViteInputFiles: ITaskItem[]"]
        P4C["Per File:<br/>• ItemSpec (file path)<br/>• Extension<br/>• RelativePath<br/>• LastModified"]
        P4D["ConfigurationFiles: ITaskItem[]<br/>• Vite configs<br/>• package.json"]
    end
    
    subgraph "Phase 5: Build Decision Logic"
        direction TB
        P5A["OrchestrateBuildTask.Execute()"]
        P5B["For Each Config:<br/>IsBuildRequired()"]
        P5C["Decision Inputs:<br/>• Marker file path<br/>• Marker timestamp<br/>• Input file timestamps<br/>• Config file timestamp<br/>• Dependency markers"]
        P5D["Decision Output:<br/>bool needsBuild"]
    end
    
    subgraph "Phase 6: Command Generation"
        direction TB
        P6A["ViteCommandBuilder.BuildCommand()"]
        P6B["ViteBuildConfiguration"]
        P6C["Configuration Data:<br/>• ProjectRoot<br/>• PackageManager<br/>• ConfigFile<br/>• Mode (EffectiveMode)<br/>• OutputDir<br/>• LogLevel<br/>• Environment vars"]
        P6D["IViteCommand"]
        P6E["Command Properties:<br/>• Executable (npm/pnpm/yarn/bun)<br/>• Command (run build / vite build)<br/>• Arguments (--config, --mode, --outDir)<br/>• WorkingDirectory<br/>• Environment"]
    end
    
    subgraph "Phase 7: Build Execution"
        direction TB
        P7A["Process.Start()"]
        P7B["Vite Build Process"]
        P7C["Standard Output/Error"]
        P7D["Exit Code"]
    end
    
    subgraph "Phase 8: Output Generation"
        direction TB
        P8A["Build Outputs"]
        P8B["Marker File:<br/>obj/ViteKit.Msbuild.{BuildId}.marker<br/>• Timestamp = completion time<br/>• Used for incremental builds"]
        P8C["Asset Files:<br/>{ViteOutputDir}/**/*<br/>• JS bundles<br/>• CSS files<br/>• Assets<br/>• index.html"]
        P8D["Build Status:<br/>• Success/Failure<br/>• Duration<br/>• Logs"]
    end
    
    I1 --> P1A
    I2 --> P1A
    I3 --> P1A
    
    P1A --> P1B
    P1B --> P1C
    P1C --> P2A
    
    P2A --> P2B
    P2B --> P2C
    P2C --> P3A
    
    P3A --> P3B
    P3B --> P3C
    P3C --> P4A
    
    P4A --> P4B
    P4A --> P4D
    P4B --> P4C
    P4C --> P5A
    P4D --> P5A
    P3C --> P5A
    
    P5A --> P5B
    P5B --> P5C
    P5C --> P5D
    
    P5D -->|Build Required| P6A
    P5D -->|Skip Build| P8D
    
    P6A --> P6B
    P6B --> P6C
    P6C --> P6D
    P6D --> P6E
    P6E --> P7A
    
    P7A --> P7B
    P7B --> P7C
    P7C --> P7D
    
    P7D --> P8A
    P8A --> P8B
    P8A --> P8C
    P8A --> P8D
```

### Build Input/Output Determination

```mermaid
graph TB
    subgraph "Determining Build Inputs"
        direction TB
        IN1[ViteProjectRoot Property]
        IN2[Scan File Patterns]
        IN3[Include Patterns:<br/>**/*.ts, *.tsx, *.js, *.jsx<br/>**/*.vue, *.svelte<br/>**/*.css, *.scss, *.less<br/>**/*.sass, *.styl]
        IN4[Exclude Patterns:<br/>node_modules/**<br/>.git/**<br/>obj/**, bin/**<br/>{ViteOutputDir}/**<br/>dist/**]
        IN5[ViteConfigFile]
        IN6[package.json]
        IN7[ViteInputFiles ItemGroup]
        
        IN1 --> IN2
        IN2 --> IN3
        IN3 --> IN4
        IN4 --> IN7
        IN5 --> IN7
        IN6 --> IN7
    end
    
    subgraph "MSBuild Incremental Build"
        direction TB
        MSB1[Target: ViteBuildAssets]
        MSB2["Inputs='@(ViteInputFiles);$(ViteConfigFile);$(MSBuildProjectFile)'"]
        MSB3["Outputs='$(IntermediateOutputPath)ViteBuild.marker'"]
        MSB4{MSBuild Timestamp Check}
        MSB5[Any Input Newer<br/>Than Output?]
        MSB6[Skip Target<br/>Fast Path]
        MSB7[Execute Target]
        
        MSB1 --> MSB2
        MSB2 --> MSB3
        MSB3 --> MSB4
        MSB4 --> MSB5
        MSB5 -->|No| MSB6
        MSB5 -->|Yes| MSB7
    end
    
    subgraph "C# Task Build Decision"
        direction TB
        CS1[OrchestrateBuildTask]
        CS2[For Each Config:<br/>IsBuildRequired]
        CS3[Marker Path:<br/>obj/ViteKit.Msbuild.{BuildId}.marker]
        CS4{Marker Exists?}
        CS5[Return true<br/>MUST BUILD]
        CS6[Get Marker Timestamp]
        CS7[Compare Timestamps]
        
        CS8[Input File Check:<br/>Any ViteInputFile.LastWriteTime<br/>> markerTime?]
        CS9[Config File Check:<br/>ConfigFile.LastWriteTime<br/>> markerTime?]
        CS10[Dependency Check:<br/>Any dependency marker<br/>> markerTime?]
        CS11[Output Check:<br/>Any file in OutputDir<br/>> markerTime?]
        
        CS12{Any Check<br/>Returns True?}
        CS13[Return true<br/>BUILD REQUIRED]
        CS14[Return false<br/>UP TO DATE]
        
        CS1 --> CS2
        CS2 --> CS3
        CS3 --> CS4
        CS4 -->|No| CS5
        CS4 -->|Yes| CS6
        CS6 --> CS7
        CS7 --> CS8
        CS8 --> CS9
        CS9 --> CS10
        CS10 --> CS11
        CS11 --> CS12
        CS12 -->|Yes| CS13
        CS12 -->|No| CS14
    end
    
    subgraph "Determining Build Outputs"
        direction TB
        OUT1[ViteOutputDir Property]
        OUT2[Per-Config OutputDir Metadata]
        OUT3[Default Calculation:<br/>Architecture Detection]
        OUT4[SPA: wwwroot/dist]
        OUT5[MultiSPA: wwwroot/spa/{name}]
        OUT6[Areas: wwwroot/{area}]
        OUT7[User Override]
        OUT8[Final Output Path]
        OUT9[Marker File Path:<br/>obj/ViteKit.Msbuild.{BuildId}.marker]
        OUT10[Asset Files:<br/>{OutputDir}/**/*]
        
        OUT1 --> OUT3
        OUT2 --> OUT7
        OUT3 --> OUT4
        OUT3 --> OUT5
        OUT3 --> OUT6
        OUT4 --> OUT8
        OUT5 --> OUT8
        OUT6 --> OUT8
        OUT7 --> OUT8
        OUT8 --> OUT9
        OUT8 --> OUT10
    end
    
    IN7 --> MSB2
    MSB7 --> CS1
    CS13 --> OUT8
    CS14 --> MSB6
```

### Configuration Metadata Flow

```mermaid
graph LR
    subgraph "Initial State"
        START["<b>ViteConfig Item</b><br/>Include: vite.admin.config.ts<br/>Metadata:<br/>• DependsOn: shared<br/>• Mode: staging"]
    end
    
    subgraph "After ViteConfigurationResolver"
        CONF["<b>Resolved Config</b><br/>+ BuildId: admin<br/>+ ConfigFile: D:\...\vite.admin.config.ts<br/>+ OutputDir: wwwroot/admin<br/>+ ProjectRoot: D:\MyProject<br/>+ Architecture: SPA"]
    end
    
    subgraph "After ViteConfigDependencyResolver"
        DEP["<b>Ordered Config</b><br/>+ BuildOrder: 2<br/>+ DependencyGroup: 1<br/>+ DependsOn: shared<br/>(Previous metadata preserved)"]
    end
    
    subgraph "After ViteModeResolver"
        MODE["<b>Mode-Resolved Config</b><br/>+ EffectiveMode: staging<br/>+ ModeSource: ItemGroup<br/>(All previous metadata preserved)"]
    end
    
    subgraph "Build Time Usage"
        BUILD["<b>Build Execution</b><br/>• Command: npm run build<br/>• Args: --config {ConfigFile}<br/>         --mode {EffectiveMode}<br/>         --outDir {OutputDir}<br/>• WorkingDir: {ProjectRoot}<br/>• Marker: obj/ViteKit.Msbuild.{BuildId}.marker"]
    end
    
    START ==> CONF
    CONF ==> DEP
    DEP ==> MODE
    MODE ==> BUILD
```

### Dependency Cascade Data Flow

```mermaid
graph TB
    subgraph "Config Definitions"
        C1["shared<br/>BuildId: shared<br/>OutputDir: wwwroot/shared<br/>DependsOn: (none)"]
        C2["admin<br/>BuildId: admin<br/>OutputDir: wwwroot/admin<br/>DependsOn: shared"]
        C3["customer<br/>BuildId: customer<br/>OutputDir: wwwroot/customer<br/>DependsOn: shared"]
    end
    
    subgraph "Topological Sort"
        T1[Build Dependency Graph]
        T2[Depth 0: shared]
        T3[Depth 1: admin, customer]
        T4[Build Order:<br/>1. shared<br/>2. admin<br/>3. customer]
    end
    
    subgraph "Build Execution"
        B1["Build shared<br/>Timestamp: 10:30:00"]
        B2["Update shared marker:<br/>obj/ViteKit.Msbuild.shared.marker<br/>LastWriteTime: 10:30:00"]
        
        B3["Check admin IsBuildRequired?"]
        B4["Compare: admin marker (10:25:00)<br/>vs shared marker (10:30:00)"]
        B5["Dependency rebuilt!<br/>BUILD REQUIRED"]
        B6["Build admin<br/>Timestamp: 10:30:05"]
        B7["Update admin marker: 10:30:05"]
        
        B8["Check customer IsBuildRequired?"]
        B9["Compare: customer marker (10:25:00)<br/>vs shared marker (10:30:00)"]
        B10["Dependency rebuilt!<br/>BUILD REQUIRED"]
        B11["Build customer<br/>Timestamp: 10:30:10"]
        B12["Update customer marker: 10:30:10"]
    end
    
    C1 --> T1
    C2 --> T1
    C3 --> T1
    T1 --> T2
    T2 --> T3
    T3 --> T4
    T4 --> B1
    B1 --> B2
    B2 --> B3
    B3 --> B4
    B4 --> B5
    B5 --> B6
    B6 --> B7
    B7 --> B8
    B8 --> B9
    B9 --> B10
    B10 --> B11
    B11 --> B12
```

### Input File Change Detection Flow

```mermaid
graph TB
    subgraph "File System State"
        FS1["Source Files:<br/>src/App.vue: 10:35:00<br/>src/main.ts: 10:20:00<br/>src/styles.css: 10:15:00"]
        FS2["Marker File:<br/>obj/ViteKit.Msbuild.default.marker<br/>LastWriteTime: 10:30:00"]
        FS3["Config File:<br/>vite.config.ts: 10:10:00"]
    end
    
    subgraph "CollectViteInputFilesTask"
        C1[Scan ViteProjectRoot]
        C2[Match Patterns]
        C3["Files Found:<br/>• src/App.vue<br/>• src/main.ts<br/>• src/styles.css"]
        C4[Read Timestamps]
        C5["ViteInputFiles:<br/>App.vue (10:35:00)<br/>main.ts (10:20:00)<br/>styles.css (10:15:00)"]
    end
    
    subgraph "IsBuildRequired Logic"
        I1[Get Marker Timestamp:<br/>10:30:00]
        I2[For Each ViteInputFile]
        I3[Compare Timestamps]
        I4["App.vue: 10:35:00<br/>10:35:00 > 10:30:00?<br/>YES - NEWER!"]
        I5[Return true<br/>BUILD REQUIRED]
    end
    
    subgraph "MSBuild Inputs/Outputs"
        M1["Target Inputs:<br/>@(ViteInputFiles)<br/>$(ViteConfigFile)<br/>$(MSBuildProjectFile)"]
        M2["Target Outputs:<br/>$(IntermediateOutputPath)ViteBuild.marker"]
        M3["MSBuild compares:<br/>Newest Input vs Oldest Output"]
        M4["App.vue (10:35:00)<br/>vs<br/>marker (10:30:00)"]
        M5[Input is newer<br/>TARGET MUST RUN]
    end
    
    FS1 --> C1
    C1 --> C2
    C2 --> C3
    C3 --> C4
    C4 --> C5
    
    C5 --> M1
    FS2 --> M2
    M1 --> M3
    M2 --> M3
    M3 --> M4
    M4 --> M5
    
    M5 --> I1
    C5 --> I2
    FS2 --> I1
    I1 --> I2
    I2 --> I3
    I3 --> I4
    I4 --> I5
```

### Complete Data Transformation Example

```
INITIAL USER INPUT:
==================
<ItemGroup>
  <ViteConfig Include="vite.admin.config.ts">
    <DependsOn>shared</DependsOn>
    <Mode>staging</Mode>
  </ViteConfig>
</ItemGroup>

<PropertyGroup>
  <ViteMode>production</ViteMode>
  <PackageManager>pnpm</PackageManager>
</PropertyGroup>

AFTER CONFIGURATION RESOLUTION:
================================
ITaskItem {
  ItemSpec: "D:\MyProject\vite.admin.config.ts"
  Metadata: {
    BuildId: "admin"
    ConfigFile: "D:\MyProject\vite.admin.config.ts"
    OutputDir: "wwwroot\admin"
    ProjectRoot: "D:\MyProject"
    Architecture: "SPA"
    DependsOn: "shared"
    Mode: "staging"
  }
}

AFTER DEPENDENCY RESOLUTION:
=============================
ITaskItem {
  ItemSpec: "D:\MyProject\vite.admin.config.ts"
  Metadata: {
    ... (previous metadata preserved) ...
    BuildOrder: "2"
    DependencyGroup: "1"
  }
}

AFTER MODE RESOLUTION:
======================
ITaskItem {
  ItemSpec: "D:\MyProject\vite.admin.config.ts"
  Metadata: {
    ... (previous metadata preserved) ...
    EffectiveMode: "staging"
    ModeSource: "ItemGroup"
  }
}

BUILD INPUTS DETERMINED:
========================
ViteInputFiles: [
  D:\MyProject\src\App.vue (LastWrite: 10:35:00)
  D:\MyProject\src\main.ts (LastWrite: 10:20:00)
  D:\MyProject\src\styles.css (LastWrite: 10:15:00)
]

ViteConfigFile: D:\MyProject\vite.admin.config.ts (LastWrite: 10:10:00)
Marker: D:\MyProject\obj\ViteKit.Msbuild.admin.marker (LastWrite: 10:30:00)

BUILD DECISION:
===============
IsBuildRequired(admin):
  - Marker exists: YES (10:30:00)
  - App.vue (10:35:00) > Marker (10:30:00)? YES
  - Decision: BUILD REQUIRED

COMMAND GENERATION:
===================
ViteBuildConfiguration {
  ProjectRoot: "D:\MyProject"
  PackageManager: Pnpm
  ConfigFile: "D:\MyProject\vite.admin.config.ts"
  Mode: "staging"  // from EffectiveMode
  OutputDir: "wwwroot\admin"
  LogLevel: "info"
}

↓

IViteCommand {
  Executable: "pnpm"
  Command: "run build"
  Arguments: "--config D:\MyProject\vite.admin.config.ts --mode staging --outDir wwwroot\admin"
  WorkingDirectory: "D:\MyProject"
  Environment: { NODE_ENV: "staging" }
}

BUILD OUTPUTS GENERATED:
========================
1. Asset Files:
   - D:\MyProject\wwwroot\admin\assets\index-abc123.js
   - D:\MyProject\wwwroot\admin\assets\index-def456.css
   - D:\MyProject\wwwroot\admin\index.html

2. Marker File:
   - D:\MyProject\obj\ViteKit.Msbuild.admin.marker
   - LastWriteTime: 10:35:15 (after successful build)

3. Build Status:
   - Success: true
   - Duration: 3.2s
   - Exit Code: 0
```

## Build Timing Options

### BeforeCSharp (Default)

```mermaid
graph LR
    A[MSBuild Start] --> B[Vite Build]
    B --> C[ResolveStaticWebAssets]
    C --> D[C# Compilation]
    D --> E[Build Complete]
```

**Use when:**
- Frontend assets needed for static web assets
- Assets referenced in C# code
- Standard web application flow

### AfterCSharp

```mermaid
graph LR
    A[MSBuild Start] --> B[C# Compilation]
    B --> C[Build Target]
    C --> D[Vite Build]
    D --> E[Build Complete]
```

**Use when:**
- Vite config reads C# build outputs
- Code generation scenarios
- Frontend depends on backend artifacts

## Diagnostic Output Example

When `ViteShowDiagnostics=true`:

```
[BUILD] ViteKit.Msbuild Diagnostic Information
========================================

Configurations (2):
  [OK] shared → wwwroot/shared (SPA)
  [OK] admin → wwwroot/admin (SPA)

Build Order:
  1. shared (no dependencies)
  2. admin (depends: shared)

Modes:
  [OK] shared: production (global)
  [OK] admin: staging (property override)

Build Plan:
  [BUILD] shared (files changed)
  [SKIP] admin (up to date)
```

## Performance Characteristics

**Fast Path (Nothing Changed):**
```
MSBuild Inputs/Outputs check → Skip in ~1ms
```

**Single Config Build:**
```
Validation (10ms) → 
Detection (5ms) → 
Collection (20ms) → 
Build Check (5ms) → 
Vite Build (2-10s) → 
Marker Update (1ms)
```

**Multi-Config with Dependencies:**
```
Resolution (50ms) → 
Dependency Sort (10ms) → 
Mode Resolution (20ms) → 
Sequential Builds (N × build time) → 
Markers Update (N × 1ms)
```

## Error Handling Flow

```mermaid
graph TB
    A[Error Occurs] --> B{Error Type}
    
    B --> C[Configuration Error]
    C --> D[Log Detailed Message]
    D --> E[Show Fix Suggestion]
    E --> F[Fail Build]
    
    B --> G[Validation Warning]
    G --> H[Log Warning]
    H --> I[Continue Build]
    
    B --> J[Build Failure]
    J --> K[Log Vite Output]
    K --> L[Preserve Marker]
    L --> F
    
    B --> M[Dependency Cycle]
    M --> N[Show Cycle Path]
    N --> F
```

**Error Message Format:**
```
[ERROR] Circular dependency detected: A → B → C → A

To fix:
  - Remove one dependency to break the cycle
  - Example: Remove 'DependsOn' from config C
```

## Clean Build Flow

```mermaid
graph TB
    A[dotnet clean] --> B[Clean Target]
    B --> C[Delete ViteOutputDir]
    C --> D[Delete Marker Files]
    D --> E[Delete Cache Files]
    E --> F[Clean Complete]
    
    G[Next dotnet build] --> H[Full Rebuild]
    H --> I[No Markers Exist]
    I --> J[All Configs Build]
```

**Files Removed:**
- `wwwroot/dist/` (or configured output)
- `obj/ViteKit.Msbuild.*.marker`
- `obj/Vite.InputFiles.cache`
- `obj/NodeRestore.marker` (optional)

## Key Decision Points

### When MSBuild Skips Entire Target
- All files in `@(ViteInputFiles)` older than marker
- Config file unchanged
- `.csproj` unchanged

### When C# Task Skips Individual Configs
- Marker exists and is newer than inputs
- Config file unchanged
- **No dependencies rebuilt** (critical for cascades)
- Output directory not manually modified

### When Builds Always Run
- `dotnet clean` executed
- Any input file modified
- Config file modified
- Dependency rebuilt
- No marker file (first build)

## Integration Points

### With ASP.NET Core
```
Vite Build → Static Web Assets → 
Published as Content → 
Served via StaticFiles middleware
```

### With Hot Reload
```
VS Code → dotnet watch →
MSBuild Incremental → 
Vite Build (if needed) →
Browser Refresh
```

### With CI/CD
```
git clone → dotnet restore →
npm ci (via EnsureNodeDependencies) →
dotnet build (runs Vite) →
dotnet publish → Deploy
```
