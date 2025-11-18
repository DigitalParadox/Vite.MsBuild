# Copilot Instructions for Vite.MsBuild

Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects. This is a **NuGet package** that provides MSBuild targets for seamless Vite integration.

## Architecture Overview

This project creates a **NuGet package** with MSBuild integration files, not a consumer application:

- **`build/Vite.MsBuild.props`** - Property defaults, auto-detection logic, imported BEFORE user's .csproj
- **`build/Vite.MsBuild.targets`** - Build targets and validation logic, imported AFTER user's .csproj  
- **`Vite.MsBuild.nuspec`** - NuGet package manifest
- **`build-package.ps1`** - PowerShell build script for creating the .nupkg

### MSBuild Import Order
```
1. Vite.MsBuild.props (sets defaults)
2. User's .csproj (can override properties)
3. Vite.MsBuild.targets (build logic)
```

## Documentation Standards

### Markdown Formatting
**ALL markdown files must conform to GitHub Flavored Markdown (GFM) standards:**

- **Headers**: Use proper hierarchy with hash symbols and single spaces
- **Code blocks**: Use triple backticks with language specification
- **Lists**: Consistent bullet points with proper indentation
- **Tables**: Use pipe syntax with proper alignment
- **Links**: Use reference-style or inline links with descriptive text
- **Badges**: Place on separate lines for better mobile rendering
- **Line breaks**: Use double spaces or empty lines for paragraph breaks

### Content Guidelines
- **Clear structure**: Logical hierarchy with descriptive headings
- **Actionable examples**: Working code snippets that users can copy-paste
- **Visual formatting**: Use emojis (✅, ❌, 🎯) and formatting (`**bold**`, `*italic*`) for clarity
- **Professional tone**: Enterprise-ready documentation with comprehensive examples
- **Update test counts**: Keep test coverage badges current (currently 125 tests)
- **README formatting**: Ensure README.md renders correctly on GitHub without duplicate headers or malformed sections

## Key Technical Patterns

### Property Naming Convention
**ALL properties prefixed with "Vite" for namespace safety:**
- `EnableViteBuild`, `ViteProjectRoot`, `ViteOutputDir`, `ViteMode`, `ViteConfigFile`
- Prevents conflicts with existing MSBuild properties

### Auto-Detection Logic
The package automatically discovers project structure:

```xml
<!-- Check project dir first, then repo root -->
<ViteProjectRoot Condition="'$(ViteProjectRoot)' == '' AND Exists('$(MSBuildProjectDirectory)\package.json')">$(MSBuildProjectDirectory)\</ViteProjectRoot>
<ViteProjectRoot Condition="'$(ViteProjectRoot)' == ''">$(MSBuildThisFileDirectory)</ViteProjectRoot>

<!-- Package manager from lock files -->
<PackageManager Condition="'$(PackageManager)' == '' AND Exists('$(ViteProjectRoot)bun.lockb')">bun</PackageManager>
<PackageManager Condition="'$(PackageManager)' == '' AND Exists('$(ViteProjectRoot)pnpm-lock.yaml')">pnpm</PackageManager>
<!-- ... npm, yarn fallbacks -->
```

### Incremental Build Pattern (Microsoft SDK Standard)
Uses `Inputs`/`Outputs` with marker files in `obj/` directory:

```xml
<Target Name="ViteBuildAssets"
    Inputs="@(ViteInputFiles);$(ViteConfigFile);$(MSBuildProjectFile)"
    Outputs="$(ViteBuildMarker)"
    Condition="'$(EnableViteBuild)' == 'true'">
```

- **Inputs**: All frontend files + config files
- **Outputs**: Marker file in `$(IntermediateOutputPath)` (obj/)
- **Logic**: Only rebuilds if inputs newer than outputs

### Parallel Build Safety
Uses shared marker file for npm install to prevent conflicts:
```xml
<NodeRestoreMarker>$(ViteProjectRoot)obj\Vite.MsBuild.NodeRestore.marker</NodeRestoreMarker>
```

## Development Workflows

### Package Development
```powershell
# Build package
.\build-package.ps1 -Version "1.0.0"

# Test locally in consumer project
dotnet add package Vite.MsBuild --source .\nupkg
```

### Verbosity Mapping
MSBuild verbosity automatically maps to Vite log levels:
- `dotnet build -v:q` → Vite `silent`
- `dotnet build -v:m` → Vite `warn`  
- `dotnet build -v:n` → Vite `info`
- `dotnet build -v:d` → Vite `info` + diagnostics
- `dotnet build -v:diag` → Vite `debug`

### Consumer Usage Patterns

**Zero-config (auto-enables from NuGet):**
```xml
<PackageReference Include="Vite.MsBuild" Version="1.0.0" />
```

**Customization:**
```xml
<PropertyGroup>
  <ViteOutputDir>wwwroot\assets</ViteOutputDir>
  <ViteMode>staging</ViteMode>
  <PackageManager>pnpm</PackageManager>
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>

<ItemGroup>
  <ViteInputFiles Include="wwwroot\custom\**\*.ts" />
  <ViteInputFiles Remove="wwwroot\legacy\**\*" />
</ItemGroup>
```

## Critical Conventions

### Framework Agnostic File Tracking
The `_CollectViteInputs` target includes patterns for ALL supported frameworks:
- Vue: `*.vue`
- React: `*.jsx`, `*.tsx` 
- Svelte: `*.svelte`
- TypeScript: `*.ts`, `*.mts`
- Styles: `*.css`, `*.scss`, `*.less`, etc.

### Asset Organization Convention
Recommend users place imported assets in:
- **JS-imported assets**: `wwwroot/js/assets/`
- **CSS-imported assets**: `wwwroot/css/assets/`

### Error Messages Are User-Facing
All error/warning messages include:
- Clear problem description
- Suggested solutions
- Example configurations
- Links to documentation

## Target Dependencies
Critical target execution order:
```
ShowViteDiagnostics (diagnostic only)
→ ResolveViteMode (maps Configuration to Vite mode)
→ ValidateViteSetup (config validation + welcome message)
→ EnsureNodeDependencies (npm install if needed)
→ ViteBuildAssets OR ViteBuildAssetsAfter (main build - timing configurable)
```

### Build Timing Options
- **`ViteBuildTiming=BeforeCSharp`** (default): Runs before `ResolveStaticWebAssetsInputs` 
- **`ViteBuildTiming=AfterCSharp`**: Runs after `Build` target completes
- Both use shared `_ViteBuildAssetsCore` target for actual build logic

## Monorepo Support Patterns

**Project-specific config:**
```
MyProject/
├── src/WebApp/
│   ├── package.json          ← WebApp-specific
│   ├── vite.config.ts        ← WebApp config
│   └── WebApp.csproj
```

**Shared config:**
```
MyProject/
├── package.json              ← Shared
├── vite.config.ts            ← Shared
└── src/Web/Web.csproj
```

## When Editing Build Logic

- **Properties go in `.props`** - Imported before user .csproj
- **Targets go in `.targets`** - Imported after user .csproj  
- **Always use `Condition` attributes** - Package should be optional
- **Follow Microsoft SDK patterns** - Use `IntermediateOutputPath` for markers
- **Test with verbosity levels** - Ensure proper message importance
- **Validate on both NuGet and Directory.Build.props usage** - Different enable logic

## Testing Checklist

Test matrix for package validation:
- ✅ NuGet install (auto-enables)
- ✅ Package managers: npm, pnpm, yarn, bun
- ✅ Frameworks: Vue, React, Svelte, vanilla
- ✅ Configurations: Debug → development, Release → production
- ✅ Monorepo: project-specific vs shared config
- ✅ Incremental builds: file changes only rebuild when needed
- ✅ Parallel builds: no npm conflicts
- ✅ Clean: `dotnet clean` removes Vite outputs