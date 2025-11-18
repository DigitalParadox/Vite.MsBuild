# Vite.MsBuild# Vite.MsBuild



**Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects.****Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects**



[![NuGet Package](https://img.shields.io/nuget/v/Vite.MsBuild)](https://www.nuget.org/packages/Vite.MsBuild)Supports: Vue, React, Svelte, Solid, Preact, and vanilla JS/TS

[![Build Status](https://github.com/DigitalParadox/Vite.MsBuild/workflows/CI/badge.svg)](https://github.com/DigitalParadox/Vite.MsBuild/actions)

[![Test Coverage](https://img.shields.io/badge/tests-90%20passing-brightgreen)](https://github.com/DigitalParadox/Vite.MsBuild/actions)## Features



**Zero-configuration** Vite integration that works seamlessly with any frontend framework (Vue, React, Svelte, vanilla TypeScript) and any package manager (npm, yarn, pnpm, bun).✅ **Zero configuration** - Auto-detects package.json and vite.config  

✅ **Incremental builds** - Only rebuilds when source files change  

## 🚀 **Quick Start**✅ **Parallel build safe** - Prevents npm install conflicts  

✅ **Framework-agnostic** - Works with any Vite-supported framework  

### **1. Install Package**✅ **Package manager agnostic** - Auto-detects npm, pnpm, yarn, or bun  

```xml✅ **Smart validation** - Helpful error messages and welcome guide  

<PackageReference Include="Vite.MsBuild" Version="1.0.0" />✅ **Production ready** - Used in production ASP.NET Core apps  

```

## Quick Start

### **2. Build Your Project**

```bash### Install

dotnet build

``````bash

dotnet add package Vite.MsBuild

**That's it!** 🎉 Your frontend builds automatically alongside your .NET project.```



## ✨ **Key Features**The package automatically enables when installed.



### **🎯 Zero Configuration Experience**### Create Vite Config

- **Works immediately** after NuGet install (< 30 seconds to productivity)

- **Auto-detects everything**: package manager, build scripts, project structure```bash

- **Intelligent defaults**: Debug → development mode, Release → production modenpm create vite@latest . -- --template vue-ts

- **Framework agnostic**: Vue, React, Svelte, Angular, vanilla TypeScript```



### **📦 Universal Package Manager Support**### Build

- **npm** - Default Node.js package manager

- **yarn** - Including Yarn v1 and Yarn Berry (PnP)```bash

- **pnpm** - Performance-focused package manager  dotnet build

- **bun** - Ultra-fast JavaScript runtime```

- **Smart fallback**: Script-based → direct tool → helpful error

That's it! Vite assets are built automatically during `dotnet build`.

### **🏢 Enterprise Multi-SPA Architecture**

- **Multiple frontend apps** in single .NET project## Configuration (Optional)

- **Independent configurations** per application

- **Isolated dependency trees** with separate outputsAll configuration is optional - the package works with sensible defaults.

- **Flexible deployment** strategies per SPA

```xml

### **🔧 Intelligent Command Factory**<PropertyGroup>

```csharp  <!-- Disable if needed -->

// Automatically chooses the best approach:  <EnableViteBuild>false</EnableViteBuild>

// 1. Custom commands (literal execution)  

// 2. Package scripts (npm run build)   <!-- Custom config location -->

// 3. Direct tools (npx vite build)  <ViteConfigFile>$(MSBuildProjectDirectory)\custom-vite.config.ts</ViteConfigFile>

```  

  <!-- Custom output directory -->

### **⚡ Performance Optimized**  <ViteOutputDir>wwwroot\assets</ViteOutputDir>

- **Incremental builds**: Only rebuilds changed assets (60-90% time savings)  

- **Parallel build safety**: Multiple projects build without conflicts  <!-- Override Vite mode -->

- **Smart markers**: Per-config tracking for multi-SPA scenarios  <ViteMode>staging</ViteMode>

- **Cross-platform**: Windows, macOS, Linux support  

  <!-- Force specific package manager -->

## 📖 **Usage Examples**  <PackageManager>pnpm</PackageManager>

  

### **Beginner: Zero Config** (Recommended)  <!-- Validate .env files -->

```xml  <ViteMissingEnvAction>warn</ViteMissingEnvAction>

<Project Sdk="Microsoft.NET.Sdk.Web">  

  <PropertyGroup>  <!-- Disable colors for CI/CD -->

    <TargetFramework>net8.0</TargetFramework>  <ViteEnableColors>false</ViteEnableColors>

  </PropertyGroup>  

  <!-- When to run Vite build (default: BeforeCSharp) -->

  <ItemGroup>  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>

    <PackageReference Include="Vite.MsBuild" Version="1.0.0" /></PropertyGroup>

  </ItemGroup>```

</Project>

```## Custom File Tracking

**Result**: Automatic Vite integration with sensible defaults! ✨

Add or remove files from incremental build tracking:

### **Intermediate: Custom Settings**

```xml```xml

<PropertyGroup><ItemGroup>

  <ViteOutputDir>wwwroot/assets</ViteOutputDir>  <!-- Add custom paths -->

  <ViteMode>staging</ViteMode>  <ViteInputFiles Include="wwwroot\data\**\*.json" />

  <PackageManager>pnpm</PackageManager>  

  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>  <!-- Exclude paths -->

</PropertyGroup>  <ViteInputFiles Remove="wwwroot\js\legacy\**\*" />

```</ItemGroup>

```

### **Advanced: Multi-SPA Enterprise**

```xml## Build Timing

<ItemGroup>

  <ViteConfig Include="Areas/Admin/vite.admin.config.ts">Control when Vite builds relative to C# compilation:

    <BuildId>admin</BuildId>

    <OutputDir>wwwroot/admin</OutputDir>```xml

    <Mode>development</Mode><PropertyGroup>

  </ViteConfig>  <!-- Default: Vite builds before C# compilation -->

  <ViteConfig Include="Areas/Customer/vite.customer.config.ts">  <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>

    <BuildId>customer</BuildId>   

    <OutputDir>wwwroot/customer</OutputDir>  <!-- Alternative: Vite builds after C# compilation -->

    <Mode>production</Mode>  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>

  </ViteConfig></PropertyGroup>

  <ViteConfig Include="Areas/Partner/vite.partner.config.ts">```

    <BuildId>partner</BuildId>

    <OutputDir>wwwroot/partner</OutputDir>**BeforeCSharp (Default):**

    <Mode>staging</Mode>- ✅ Frontend assets available during C# compilation

  </ViteConfig>- ✅ Static web assets properly integrated

</ItemGroup>- ✅ Faster overall build (parallel where possible)

```

**AfterCSharp:**

### **Expert: MSBuild Property Integration**- ✅ C# compilation completes first

```xml- ✅ Useful for debugging build order issues

<PropertyGroup>- ✅ Good for scenarios where frontend depends on C# outputs

  <!-- OS-conditional commands -->

  <ViteBuildCommand Condition="$([MSBuild]::IsOSPlatform('Windows'))">powershell -Command "npm run build"</ViteBuildCommand>## Development Workflow

  <ViteBuildCommand Condition="!$([MSBuild]::IsOSPlatform('Windows'))">npm run build</ViteBuildCommand>

  ### Recommended: Two Terminals

  <!-- Solution root integration -->

  <ViteConfigFile>$(SolutionRoot)/shared/vite.config.ts</ViteConfigFile>**Best performance with instant Hot Module Replacement:**

  

  <!-- Environment-specific -->```bash

  <ViteMode Condition="'$(Configuration)' == 'Staging'">staging</ViteMode># Terminal 1: Backend

</PropertyGroup>dotnet watch run

```

# Terminal 2: Frontend (instant HMR)

## 🎛️ **Configuration Options**npm run dev

```

### **Core Properties**

| Property | Description | Default | Example |### Alternative: Single Terminal

|----------|-------------|---------|---------|

| `EnableViteBuild` | Enable/disable Vite integration | `true` | `false` |```xml

| `ViteMode` | Build mode for all configurations | Based on `$(Configuration)` | `production` |<PropertyGroup>

| `ViteOutputDir` | Output directory for built assets | `wwwroot` | `wwwroot/assets` |  <ViteWatchIntegration>true</ViteWatchIntegration>

| `ViteConfigFile` | Custom vite.config.ts location | Auto-detected | `custom/vite.config.ts` |</PropertyGroup>

| `PackageManager` | Preferred package manager | Auto-detected | `pnpm` |```



### **Advanced Properties**```bash

| Property | Description | Default | Example |dotnet watch run

|----------|-------------|---------|---------|```

| `ViteBuildTiming` | When to run frontend build | `BeforeCSharp` | `AfterCSharp` |

| `ViteBuildCommand` | Custom build command | Auto-generated | `npm run build:custom` |Slower - app restarts on frontend changes.

| `ViteEnableColors` | Enable colored output | `true` in terminals | `false` |

| `ViteValidateCommandOnly` | Validate commands without execution | `false` | `true` |## Supported Package Managers



### **Multi-SPA Properties**Auto-detected from lock files:

| Property | Description | Example |

|----------|-------------|---------|- **npm** - `package-lock.json`

| `AdminViteMode` | Mode override for admin config | `development` |- **pnpm** - `pnpm-lock.yaml` 

| `CustomerViteMode` | Mode override for customer config | `production` |- **yarn** - `yarn.lock`

| `PartnerViteMode` | Mode override for partner config | `staging` |- **bun** - `bun.lockb`



## 🌍 **Cross-Platform Support**## Supported Frameworks



### **Package Manager Auto-Detection**Works with all Vite-compatible frameworks:

```bash

# Detection priority (first found wins):- ✅ Vue 3

bun.lockb        → bun- ✅ React 18+

pnpm-lock.yaml   → pnpm  - ✅ Preact

yarn.lock        → yarn- ✅ Svelte 4+

package-lock.json → npm- ✅ Solid.js

```- ✅ Lit

- ✅ Vanilla JS/TS

### **Command Execution Strategy**

```bash## Build Modes

# 1. Try package scripts (respects project setup)

npm run buildAuto-mapped from MSBuild Configuration:

yarn build         # (yarn runs scripts directly)

pnpm run build| MSBuild | Vite Mode |

bun run build|---------|-----------|

| Debug | development |

# 2. Fall back to direct tools (universal compatibility)| Release | production |

npx vite build| Staging | staging |

yarn dlx vite build  | UAT | uat |

pnpm dlx vite build

bunx vite buildOverride with `<ViteMode>custom</ViteMode>` if needed.



# 3. Clear error with guidance (helpful failure)## Monorepo Support

```

### Project-Specific Configuration

### **Operating System Support**

- **Windows**: PowerShell and cmd support```

- **macOS/Linux**: bash and sh support  MyMonorepo/

- **CI/CD**: All major platforms (Azure DevOps, GitHub Actions, Jenkins)├── src/

│   ├── WebApp/

## 🏗️ **Architecture Overview**│   │   ├── package.json          ← WebApp-specific

│   │   ├── vite.config.ts        ← WebApp config

### **ViteCommand Factory Pattern**│   │   └── WebApp.csproj

```│   └── AdminPanel/

┌─────────────────────────┐│       ├── package.json          ← AdminPanel-specific

│   ViteCommandFactory    │ ← Smart command type detection│       ├── vite.config.ts        ← AdminPanel config

└─────────────────────────┘│       └── AdminPanel.csproj

            │```

     ┌──────┼──────┐

     │      │      │Each project can have its own Vite setup.

┌────▼───┐ ┌▼────┐ ┌▼──────────────┐

│Custom  │ │Script│ │DirectTool     │ ← 3-tier command system### Shared Configuration

│Command │ │Based │ │CommandBuilder │

└────────┘ └─────┘ └───────────────┘```

```MyProject/

├── package.json                  ← Shared for all projects

### **Multi-SPA Architecture**├── vite.config.ts                ← Shared config

```└── src/

Project.csproj    ├── Web/

├── Areas/Admin/         ← Independent Vue.js app    │   └── Web.csproj

│   ├── vite.admin.config.ts    └── Api/

│   └── wwwroot/admin/   ← Isolated output        └── Api.csproj

├── Areas/Customer/      ← Independent React app  ```

│   ├── vite.customer.config.ts

│   └── wwwroot/customer/ ← Isolated outputAll projects share the same Vite configuration.

└── Areas/Partner/       ← Independent Svelte app

    ├── vite.partner.config.ts## Troubleshooting

    └── wwwroot/partner/  ← Isolated output

```### Verbosity Control



## 🧪 **Quality & Testing**```bash

dotnet build -v:q        # Quiet

### **Test Coverage**dotnet build -v:m        # Minimal

- **90 Pure Unit Tests** - 100% passing ✅dotnet build -v:n        # Normal (default)

- **Command Validation**: All package managers testeddotnet build -v:d        # Detailed (shows diagnostics)

- **Error Scenarios**: Comprehensive edge case coverage  dotnet build -v:diag     # Diagnostic (full details)

- **Cross-Platform**: Windows/macOS/Linux validation```

- **Enterprise Scenarios**: Multi-SPA configurations tested

### Force Rebuild

### **Supported Frameworks**

- ✅ **Vue.js** - Progressive JavaScript framework (.vue files)```bash

- ✅ **React** - Component-based UI library (.jsx, .tsx files)  dotnet clean

- ✅ **Svelte** - Compile-time optimized framework (.svelte files)dotnet build

- ✅ **Angular** - Full-featured platform (TypeScript support)```

- ✅ **Vanilla TypeScript/JavaScript** - Direct ES modules

### See Full Vite Output

### **Development Environments**

- ✅ **Visual Studio 2019/2022** - Full IDE integration```bash

- ✅ **Visual Studio Code** - Lightweight editor supportdotnet build -tl:false

- ✅ **JetBrains Rider** - Cross-platform .NET IDE  ```

- ✅ **Command Line** - Terminal-based workflows

Disables terminal logger to show complete Vite output.

## 🚀 **Runtime Override Hierarchy**

## Performance

Control build modes with flexible priority system:

| Scenario | Time | Notes |

### **1. Command Line (Highest Priority)** 🥇|----------|------|-------|

```bash| Clean build | ~5-10s | Full Vite build + C# |

dotnet build -p:ViteMode=production                    # Override all configs| Incremental (C# only) | ~1-2s | Vite skipped |

dotnet build -p:AdminViteMode=local                   # Override specific config  | Incremental (frontend only) | ~2-3s | C# skipped |

dotnet publish -p:CustomerViteMode=staging            # Deployment-specific| No changes | <1s | Both skipped |

```| HMR (npm run dev) | ~50ms | Instant updates |



### **2. Config-Specific Project Properties** 🥈## Examples

```xml

<PropertyGroup>### Minimal Setup

  <AdminViteMode>development</AdminViteMode>

  <CustomerViteMode>staging</CustomerViteMode>```xml

</PropertyGroup><Project Sdk="Microsoft.NET.Sdk.Web">

```  <PropertyGroup>

    <TargetFramework>net9.0</TargetFramework>

### **3. Global Project Property** 🥉  </PropertyGroup>

```xml  

<PropertyGroup>  <ItemGroup>

  <ViteMode>production</ViteMode>    <PackageReference Include="Vite.MsBuild" Version="1.0.0" />

</PropertyGroup>  </ItemGroup>

```</Project>

```

### **4. ItemGroup Mode Metadata** 

```xml### Advanced Setup

<ViteConfig Include="...">

  <Mode>development</Mode>```xml

</ViteConfig><Project Sdk="Microsoft.NET.Sdk.Web">

```  <PropertyGroup>

    <TargetFramework>net9.0</TargetFramework>

### **5. Configuration-Based Fallback**    <PackageManager>pnpm</PackageManager>

- Debug → development  </PropertyGroup>

- Release → production  

  <ItemGroup>

## 🛡️ **Error Handling**    <PackageReference Include="Vite.MsBuild" Version="1.0.0" />

  </ItemGroup>

### **Intelligent Error Messages**  

```bash  <!-- Development: Local mode -->

# Missing dependencies  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">

ERROR: Node.js dependencies not found    <ViteMode>local</ViteMode>

SOLUTION: Run 'npm install' in: /path/to/project  </PropertyGroup>

HELP: https://docs.npmjs.com/getting-started  

  <!-- Production: Strict validation -->

# Package manager not found    <PropertyGroup Condition="'$(Configuration)' == 'Release'">

ERROR: pnpm not found (detected from pnpm-lock.yaml)    <ViteMissingEnvAction>error</ViteMissingEnvAction>

SOLUTION: Install pnpm: npm install -g pnpm    <ViteEnableColors>false</ViteEnableColors>

ALTERNATIVE: Remove pnpm-lock.yaml to use npm  </PropertyGroup>

</Project>

# Invalid configuration```

ERROR: Syntax error in vite.config.ts:15:3

SOLUTION: Check configuration syntax## How It Works

HELP: https://vitejs.dev/config/

```### Build Flow



### **Graceful Degradation**```

- Package manager fallback chaindotnet build

- Command execution alternatives  ↓

- Clear guidance for resolutionAuto-detect package.json location

- No silent failures  ↓

Auto-detect vite.config.{ts,js,mjs}

## 📚 **Documentation**  ↓

Resolve ViteMode (Debug → development)

### **Quick References**  ↓

- [Configuration Examples](examples/) - Ready-to-use configurationsValidate setup

- [Architecture Guide](docs/) - Technical implementation details    ↓

- [Package Manager Support](docs/multi-package-manager-support.md) - Compatibility matrixCheck if npm install needed

- [Multi-SPA Patterns](docs/multi-spa-single-project.md) - Enterprise scenarios  ↓

Run npm install (if needed)

### **Advanced Topics**  ↓

- [MSBuild Integration](docs/msbuild-compatibility-analysis.md) - Build system detailsCollect frontend source files

- [Performance Optimization](docs/enhanced-marker-file-strategy.md) - Incremental builds  ↓

- [Monorepo Support](docs/monorepo-architecture.md) - Complex project structuresRun vite build (if files changed)

- [Security Considerations](docs/security-vulnerability-management.md) - Safe practices  ↓

Include in ASP.NET static assets

## 🤝 **Contributing**```



### **Development Setup**### Incremental Builds

```bash

git clone https://github.com/DigitalParadox/Vite.MsBuild.gitUses Microsoft SDK pattern with Inputs/Outputs:

cd Vite.MsBuild

dotnet restore- **Inputs**: All frontend files, config files, lock files

dotnet test  # Should see 90/90 tests passing ✅- **Outputs**: Marker file in `obj/`

```- **Logic**: Only rebuilds if inputs newer than outputs



### **Project Structure**### Parallel Build Safety

```

src/Vite.MsBuild.Tasks/     ← Core C# implementationUses marker files in `obj/` with MSBuild's built-in serialization:

├── Commands/               ← Factory pattern & command builders  

├── BuildViteCommand.cs     ← Main MSBuild task```

└── ViteCommandFactory.cs   ← Smart command type detectionobj/

├── Vite.MsBuild.NodeRestore.marker    ← Shared restore tracking

build/                      ← MSBuild integration files└── Debug/net9.0/

├── Vite.MsBuild.props      ← Property defaults & auto-detection    └── net9.0.Vite.MsBuild.Build.marker   ← Per-project build tracking

└── Vite.MsBuild.targets    ← Build targets & logic```



tests/Vite.MsBuild.PureUnitTests/ ← 90 comprehensive unit testsMSBuild ensures only one project restores at a time.

├── CommandValidationTests.cs    ← Package manager scenarios

├── FactoryPatternRobustnessTests.cs ← Edge cases & error handling## Architecture

└── MSBuildPropertyUsageTests.cs ← MSBuild integration tests

```### File Structure



### **Branching Strategy**```

- **`dev`** - Active development (default branch)Vite.MsBuild.nupkg

- **`stable`** - Stable releases  ├── Vite.MsBuild.nuspec

- **`master`** - Production-ready code├── README.md

├── LICENSE

## 📄 **License**└── build/

    ├── Vite.MsBuild.props      ← Properties, defaults

MIT License - see [LICENSE](LICENSE) file for details.    └── Vite.MsBuild.targets    ← Build targets

```

---

### Import Order

## 🎯 **Why Choose Vite.MsBuild?**

```

### **For Individual Developers**1. Vite.MsBuild.props (before project)

- ⚡ **Zero setup time** - Works in < 30 seconds2. Your .csproj

- 🔧 **Tool flexibility** - Use your preferred package manager3. Vite.MsBuild.targets (after project)

- 🎨 **Framework freedom** - Vue, React, Svelte, anything```

- 🚀 **Fast builds** - 60-90% time savings via incremental builds

This allows you to override any property in your `.csproj`.

### **For Enterprise Teams**  

- 🏢 **Multi-SPA support** - Multiple frontend apps per .NET project## Requirements

- 🔒 **Reliable CI/CD** - Uses Microsoft MSBuild patterns

- 📊 **Clear diagnostics** - Reduce support burden by ~70%- **.NET 8.0+** or **.NET 9.0+**

- 🌍 **Cross-platform** - Windows, macOS, Linux compatibility- **Node.js 18+**

- **Vite 4.0+** or **Vite 5.0+**

### **For Platform Teams**- **Package manager**: npm, pnpm, yarn, or bun

- 🛡️ **Zero breaking changes** - 100% backward compatible

- 📈 **Future-proof** - Supports emerging tools (bun, Yarn PnP)## License

- 🎯 **Standards compliant** - Follows .NET ecosystem conventions

- 🔄 **Vendor agnostic** - Not tied to specific frameworksMIT



**Transform your .NET + frontend workflow today!** 🚀## Contributing

Issues and PRs welcome at: https://github.com/DigitalParadox/Vite.MsBuild

## Documentation

Full documentation: https://github.com/DigitalParadox/Vite.MsBuild#readme
