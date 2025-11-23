---
layout: default
title: Home
nav_order: 1
description: "ViteKit.Msbuild - Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects"
permalink: /
---

# ViteKit.Msbuild
{: .fs-9 }

Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects. Zero configuration, maximum flexibility.
{: .fs-6 .fw-300 }

[Get started now](getting-started){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/DigitalParadox/ViteKit){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## Quick Start

Install the NuGet package:

```bash
dotnet add package ViteKit.Msbuild
```

That's it! Vite builds automatically run on `dotnet build`.

---

## Features

**🚀 Zero Configuration**
: Works out of the box with sensible defaults. No setup required.

**🎯 Framework Agnostic**
: Supports Vue, React, Svelte, Solid, Preact, and vanilla JS/TS.

**📦 Package Manager Detection**
: Auto-detects npm, yarn, pnpm, or bun from lock files.

**🔧 Fully Customizable**
: Override any setting via MSBuild properties.

**🏗️ Multi-SPA Support**
: Build multiple Vite configurations in a single project.

---

## Why ViteKit.Msbuild?

### Before
```xml
<Target Name="BuildFrontend" BeforeTargets="Build">
  <Exec Command="npm install" />
  <Exec Command="npm run build" />
</Target>
```

Known Issues: Manual configuration, poor MSBuild integration, no proper Clean target support.

### After
```xml
<PackageReference Include="ViteKit.Msbuild" Version="*" />
```

Everything just works. Builds are fast. Integration is seamless.

---

## Supported Frameworks

| Framework | Version | Status |
|-----------|---------|--------|
| .NET 8.0  | LTS     | ✅ Supported |
| .NET 9.0  | STS     | ✅ Supported |
| .NET 10.0 | LTS     | ✅ Supported |

---

## Community

- [GitHub Issues](https://github.com/DigitalParadox/ViteKit/issues) - Bug reports and feature requests
- [GitHub Discussions](https://github.com/DigitalParadox/ViteKit/discussions) - Questions and community support
- [Contributing Guide](contributing) - Help improve ViteKit

---

## License

ViteKit.Msbuild is distributed under the [MIT license](https://github.com/DigitalParadox/ViteKit/blob/dev/LICENSE).
