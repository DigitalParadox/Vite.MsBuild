# Vite.MsBuild

**Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects**

Supports: Vue, React, Svelte, Solid, Preact, and vanilla JS/TS

## Features

✅ **Zero configuration** - Auto-detects package.json and vite.config  
✅ **Incremental builds** - Only rebuilds when source files change  
✅ **Parallel build safe** - Prevents npm install conflicts  
✅ **Framework-agnostic** - Works with any Vite-supported framework  
✅ **Package manager agnostic** - Auto-detects npm, pnpm, yarn, or bun  
✅ **Smart validation** - Helpful error messages and welcome guide  
✅ **Production ready** - Used in production ASP.NET Core apps  

## Quick Start

### Install

```bash
dotnet add package Vite.MsBuild
```

The package automatically enables when installed.

### Create Vite Config

```bash
npm create vite@latest . -- --template vue-ts
```

### Build

```bash
dotnet build
```

That's it! Vite assets are built automatically during `dotnet build`.

## Configuration (Optional)

All configuration is optional - the package works with sensible defaults.

```xml
<PropertyGroup>
  <!-- Disable if needed -->
  <EnableViteBuild>false</EnableViteBuild>
  
  <!-- Custom config location -->
  <ViteConfigFile>$(MSBuildProjectDirectory)\custom-vite.config.ts</ViteConfigFile>
  
  <!-- Custom output directory -->
  <ViteOutputDir>wwwroot\assets</ViteOutputDir>
  
  <!-- Override Vite mode -->
  <ViteMode>staging</ViteMode>
  
  <!-- Force specific package manager -->
  <PackageManager>pnpm</PackageManager>
  
  <!-- Validate .env files -->
  <ViteMissingEnvAction>warn</ViteMissingEnvAction>
  
  <!-- Disable colors for CI/CD -->
  <ViteEnableColors>false</ViteEnableColors>
  
  <!-- When to run Vite build (default: BeforeCSharp) -->
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

## Custom File Tracking

Add or remove files from incremental build tracking:

```xml
<ItemGroup>
  <!-- Add custom paths -->
  <ViteInputFiles Include="wwwroot\data\**\*.json" />
  
  <!-- Exclude paths -->
  <ViteInputFiles Remove="wwwroot\js\legacy\**\*" />
</ItemGroup>
```

## Build Timing

Control when Vite builds relative to C# compilation:

```xml
<PropertyGroup>
  <!-- Default: Vite builds before C# compilation -->
  <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>
  
  <!-- Alternative: Vite builds after C# compilation -->
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

**BeforeCSharp (Default):**
- ✅ Frontend assets available during C# compilation
- ✅ Static web assets properly integrated
- ✅ Faster overall build (parallel where possible)

**AfterCSharp:**
- ✅ C# compilation completes first
- ✅ Useful for debugging build order issues
- ✅ Good for scenarios where frontend depends on C# outputs

## Development Workflow

### Recommended: Two Terminals

**Best performance with instant Hot Module Replacement:**

```bash
# Terminal 1: Backend
dotnet watch run

# Terminal 2: Frontend (instant HMR)
npm run dev
```

### Alternative: Single Terminal

```xml
<PropertyGroup>
  <ViteWatchIntegration>true</ViteWatchIntegration>
</PropertyGroup>
```

```bash
dotnet watch run
```

Slower - app restarts on frontend changes.

## Supported Package Managers

Auto-detected from lock files:

- **npm** - `package-lock.json`
- **pnpm** - `pnpm-lock.yaml` 
- **yarn** - `yarn.lock`
- **bun** - `bun.lockb`

## Supported Frameworks

Works with all Vite-compatible frameworks:

- ✅ Vue 3
- ✅ React 18+
- ✅ Preact
- ✅ Svelte 4+
- ✅ Solid.js
- ✅ Lit
- ✅ Vanilla JS/TS

## Build Modes

Auto-mapped from MSBuild Configuration:

| MSBuild | Vite Mode |
|---------|-----------|
| Debug | development |
| Release | production |
| Staging | staging |
| UAT | uat |

Override with `<ViteMode>custom</ViteMode>` if needed.

## Monorepo Support

### Project-Specific Configuration

```
MyMonorepo/
├── src/
│   ├── WebApp/
│   │   ├── package.json          ← WebApp-specific
│   │   ├── vite.config.ts        ← WebApp config
│   │   └── WebApp.csproj
│   └── AdminPanel/
│       ├── package.json          ← AdminPanel-specific
│       ├── vite.config.ts        ← AdminPanel config
│       └── AdminPanel.csproj
```

Each project can have its own Vite setup.

### Shared Configuration

```
MyProject/
├── package.json                  ← Shared for all projects
├── vite.config.ts                ← Shared config
└── src/
    ├── Web/
    │   └── Web.csproj
    └── Api/
        └── Api.csproj
```

All projects share the same Vite configuration.

## Troubleshooting

### Verbosity Control

```bash
dotnet build -v:q        # Quiet
dotnet build -v:m        # Minimal
dotnet build -v:n        # Normal (default)
dotnet build -v:d        # Detailed (shows diagnostics)
dotnet build -v:diag     # Diagnostic (full details)
```

### Force Rebuild

```bash
dotnet clean
dotnet build
```

### See Full Vite Output

```bash
dotnet build -tl:false
```

Disables terminal logger to show complete Vite output.

## Performance

| Scenario | Time | Notes |
|----------|------|-------|
| Clean build | ~5-10s | Full Vite build + C# |
| Incremental (C# only) | ~1-2s | Vite skipped |
| Incremental (frontend only) | ~2-3s | C# skipped |
| No changes | <1s | Both skipped |
| HMR (npm run dev) | ~50ms | Instant updates |

## Examples

### Minimal Setup

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Vite.MsBuild" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Advanced Setup

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <PackageManager>pnpm</PackageManager>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Vite.MsBuild" Version="1.0.0" />
  </ItemGroup>
  
  <!-- Development: Local mode -->
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <ViteMode>local</ViteMode>
  </PropertyGroup>
  
  <!-- Production: Strict validation -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <ViteMissingEnvAction>error</ViteMissingEnvAction>
    <ViteEnableColors>false</ViteEnableColors>
  </PropertyGroup>
</Project>
```

## How It Works

### Build Flow

```
dotnet build
  ↓
Auto-detect package.json location
  ↓
Auto-detect vite.config.{ts,js,mjs}
  ↓
Resolve ViteMode (Debug → development)
  ↓
Validate setup
  ↓
Check if npm install needed
  ↓
Run npm install (if needed)
  ↓
Collect frontend source files
  ↓
Run vite build (if files changed)
  ↓
Include in ASP.NET static assets
```

### Incremental Builds

Uses Microsoft SDK pattern with Inputs/Outputs:

- **Inputs**: All frontend files, config files, lock files
- **Outputs**: Marker file in `obj/`
- **Logic**: Only rebuilds if inputs newer than outputs

### Parallel Build Safety

Uses marker files in `obj/` with MSBuild's built-in serialization:

```
obj/
├── Vite.MsBuild.NodeRestore.marker    ← Shared restore tracking
└── Debug/net9.0/
    └── net9.0.Vite.MsBuild.Build.marker   ← Per-project build tracking
```

MSBuild ensures only one project restores at a time.

## Architecture

### File Structure

```
Vite.MsBuild.nupkg
├── Vite.MsBuild.nuspec
├── README.md
├── LICENSE
└── build/
    ├── Vite.MsBuild.props      ← Properties, defaults
    └── Vite.MsBuild.targets    ← Build targets
```

### Import Order

```
1. Vite.MsBuild.props (before project)
2. Your .csproj
3. Vite.MsBuild.targets (after project)
```

This allows you to override any property in your `.csproj`.

## Requirements

- **.NET 8.0+** or **.NET 9.0+**
- **Node.js 18+**
- **Vite 4.0+** or **Vite 5.0+**
- **Package manager**: npm, pnpm, yarn, or bun

## License

MIT

## Contributing

Issues and PRs welcome at: https://github.com/DigitalParadox/Vite.MsBuild

## Documentation

Full documentation: https://github.com/DigitalParadox/Vite.MsBuild#readme
