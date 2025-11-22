# ViteKit.Msbuild Package Structure

Complete NuGet package for Vite MSBuild integration.

## Directory Structure

```
packages/ViteKit.Msbuild/
├── build/
│   ├── ViteKit.Msbuild.props          ← Properties, defaults, ItemGroups
│   └── ViteKit.Msbuild.targets        ← All build targets
├── nupkg/                           ← Generated package output (created by build script)
├── README.md                        ← Package documentation
├── ViteKit.Msbuild.nuspec             ← NuGet package manifest
└── build-package.ps1               ← Build script
```

## Building the Package

```powershell
cd packages/ViteKit.Msbuild
.\build-package.ps1 -Version "1.0.0"
```

Output: `nupkg/ViteKit.Msbuild.1.0.0.nupkg`

## Testing Locally

```bash
# In a test project
dotnet add package ViteKit.Msbuild --source D:\tattoomachinegirl-piranha\packages\ViteKit.Msbuild\nupkg
```

## Publishing to NuGet.org

```powershell
nuget push nupkg\ViteKit.Msbuild.1.0.0.nupkg -Source nuget.org -ApiKey YOUR_API_KEY
```

## File Breakdown

### ViteKit.Msbuild.props (166 lines)

**Imported BEFORE user's .csproj**

Contains:
- Property defaults (EnableViteBuild, ViteProjectRoot, etc.)
- Package manager auto-detection
- ViteConfigFile auto-discovery
- Marker file locations
- dotnet watch integration
- Publishing ItemGroup

### ViteKit.Msbuild.targets (~600 lines)

**Imported AFTER user's .csproj**

Contains all targets:
- `ShowViteDiagnostics` - Debug output
- `ResolveViteMode` - Map Configuration to Vite mode
- `ValidateViteSetup` - Config validation + welcome message
- `ValidatePackageManager` - Ensure npm/pnpm/yarn/bun installed
- `CheckNodeRestoreRequired` - Incremental restore check
- `RestoreNodeDependencies` - Run npm install
- `EnsureNodeDependencies` - Safety check
- `ViteBuildAssets` - Main Vite build target
- `_CollectViteInputs` - Gather source files
- `ViteCleanAssets` - Clean output
- `BuildViteOnly` - Standalone build
- `ViteDebugInfo` - Debug mode info
- `CleanViteOutput` - Clean integration

## Version History

### 1.0.0 (TBD)
- Initial release
- Framework-agnostic Vite integration
- Auto-detection of package.json and vite.config
- Incremental build support
- Parallel build safety
- Package manager agnostic (npm, pnpm, yarn, bun)
- Smart validation with helpful errors
- Monorepo support (project-specific or shared config)
- dotnet watch integration (opt-in)

## Features

✅ **Zero configuration** - Works out of the box  
✅ **Smart auto-detection** - Finds package.json and vite.config automatically  
✅ **Incremental builds** - Only rebuilds when files change  
✅ **Parallel build safe** - Prevents npm install conflicts  
✅ **Framework-agnostic** - Vue, React, Svelte, Solid, Preact, vanilla  
✅ **Package manager agnostic** - npm, pnpm, yarn, bun  
✅ **Monorepo ready** - Per-project or shared configuration  
✅ **Production tested** - Used in real ASP.NET Core applications  

## Requirements

- .NET 8.0+ or .NET 9.0+
- Node.js 18+
- Vite 4.0+ or Vite 5.0+
- Package manager: npm, pnpm, yarn, or bun

## License

MIT

## Source

Extracted from: TattooMachineGirl.Web project  
Based on: `Directory.Build.props` Vite integration  
Refactored into: NuGet package format (split .props/.targets)

## Notes

- The package auto-enables when installed (NuGet path detection)
- Can be disabled with `<EnableViteBuild>false</EnableViteBuild>`
- All properties can be overridden in user's .csproj
- Follows Microsoft SDK patterns (marker files in obj/, Inputs/Outputs)
- No custom MSBuild tasks - pure props/targets files
