---
layout: default
title: Multi-SPA Configuration
parent: Enterprise Examples
grand_parent: Examples
nav_order: 1
---

# Enterprise Multi-SPA Configuration
{: .fs-9 }

Complete example showing how to manage multiple Single Page Applications (SPAs) in one ASP.NET Core project.
{: .fs-6 .fw-300 }

## Scenario

Large enterprise application with multiple customer-facing areas:
- **Admin Dashboard** (Vue + TypeScript)
- **Customer Portal** (React + TypeScript) 
- **Partner Portal** (Svelte + TypeScript)
- **Public Website** (Vanilla TypeScript)

Each area has its own:
- ✅ Independent frontend technology stack
- ✅ Separate build configuration
- ✅ Isolated dependencies
- ✅ Different deployment modes

## Project Structure

```
EnterpriseApp/
├── EnterpriseApp.csproj
├── Areas/
│   ├── Admin/
│   │   ├── package.json
│   │   ├── vite.admin.config.ts
│   │   └── src/
│   │       └── admin-app.ts
│   ├── Customer/
│   │   ├── package.json
│   │   ├── vite.customer.config.ts
│   │   └── src/
│   │       └── customer-app.tsx
│   ├── Partner/
│   │   ├── package.json
│   │   ├── vite.partner.config.ts
│   │   └── src/
│   │       └── partner-app.ts
│   └── Public/
│       ├── package.json
│       ├── vite.public.config.ts
│       └── src/
│           └── public-app.ts
└── wwwroot/
    ├── admin/
    ├── customer/
    ├── partner/
    └── public/
```

## Configuration

### EnterpriseApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ViteKit.Msbuild" Version="2.0.0" />
  </ItemGroup>

  <!-- Multi-SPA Configuration -->
  <ItemGroup>
    <!-- Admin Dashboard (Vue + TypeScript) -->
    <ViteConfig Include="Areas/Admin/vite.admin.config.ts">
      <BuildId>admin</BuildId>
      <ProjectRoot>Areas/Admin</ProjectRoot>
      <OutputDir>wwwroot/admin</OutputDir>
      <Mode Condition="'$(Configuration)' == 'Debug'">development</Mode>
      <Mode Condition="'$(Configuration)' == 'Release'">production</Mode>
    </ViteConfig>

    <!-- Customer Portal (React + TypeScript) -->
    <ViteConfig Include="Areas/Customer/vite.customer.config.ts">
      <BuildId>customer</BuildId>
      <ProjectRoot>Areas/Customer</ProjectRoot>
      <OutputDir>wwwroot/customer</OutputDir>
      <Mode>production</Mode>
    </ViteConfig>

    <!-- Partner Portal (Svelte + TypeScript) -->
    <ViteConfig Include="Areas/Partner/vite.partner.config.ts">
      <BuildId>partner</BuildId>
      <ProjectRoot>Areas/Partner</ProjectRoot>
      <OutputDir>wwwroot/partner</OutputDir>
      <Mode>staging</Mode>
    </ViteConfig>

    <!-- Public Website (Vanilla TypeScript) -->
    <ViteConfig Include="Areas/Public/vite.public.config.ts">
      <BuildId>public</BuildId>
      <ProjectRoot>Areas/Public</ProjectRoot>
      <OutputDir>wwwroot/public</OutputDir>
      <Mode Condition="'$(Configuration)' == 'Debug'">development</Mode>
      <Mode Condition="'$(Configuration)' == 'Release'">production</Mode>
    </ViteConfig>
  </ItemGroup>

  <!-- Global Configuration -->
  <PropertyGroup>
    <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
    <EnableViteBuild Condition="'$(SkipFrontendBuild)' == 'true'">false</EnableViteBuild>
  </PropertyGroup>

  <!-- Development-specific settings -->
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <ViteVerbosity>detailed</ViteVerbosity>
  </PropertyGroup>

  <!-- Production-specific settings -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <ViteVerbosity>minimal</ViteVerbosity>
  </PropertyGroup>
</Project>
```

## Individual Area Configurations

### Areas/Admin/package.json (Vue)

```json
{
  "name": "@enterprise/admin-dashboard",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "build": "vue-tsc && vite build --config vite.admin.config.ts",
    "dev": "vite --config vite.admin.config.ts"
  },
  "dependencies": {
    "vue": "^3.3.0",
    "vue-router": "^4.2.0",
    "pinia": "^2.1.0"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^4.4.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0",
    "vue-tsc": "^1.8.0"
  }
}
```

### Areas/Admin/vite.admin.config.ts

```typescript
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: '../../wwwroot/admin',
    manifest: 'admin-manifest.json',
    rollupOptions: {
      input: 'src/admin-app.ts'
    }
  },
  server: {
    port: 5001
  },
  define: {
    __APP_VERSION__: JSON.stringify(process.env.npm_package_version),
    __BUILD_MODE__: JSON.stringify('admin')
  }
})
```

### Areas/Customer/package.json (React)

```json
{
  "name": "@enterprise/customer-portal",
  "private": true,
  "version": "2.1.0",
  "type": "module",
  "scripts": {
    "build": "tsc && vite build --config vite.customer.config.ts",
    "dev": "vite --config vite.customer.config.ts",
    "test": "vitest"
  },
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.8.0",
    "@tanstack/react-query": "^4.24.0"
  },
  "devDependencies": {
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0",
    "@vitejs/plugin-react": "^4.0.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0",
    "vitest": "^0.34.0"
  }
}
```

### Areas/Customer/vite.customer.config.ts

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../../wwwroot/customer',
    manifest: 'customer-manifest.json',
    rollupOptions: {
      input: 'src/customer-app.tsx'
    }
  },
  server: {
    port: 5002
  },
  define: {
    __APP_VERSION__: JSON.stringify(process.env.npm_package_version),
    __BUILD_MODE__: JSON.stringify('customer')
  }
})
```

### Areas/Partner/package.json (Svelte)

```json
{
  "name": "@enterprise/partner-portal",
  "private": true,
  "version": "1.5.0", 
  "type": "module",
  "scripts": {
    "build": "vite build --config vite.partner.config.ts",
    "dev": "vite --config vite.partner.config.ts"
  },
  "dependencies": {
    "svelte-routing": "^2.0.0"
  },
  "devDependencies": {
    "@sveltejs/vite-plugin-svelte": "^2.4.0",
    "svelte": "^4.0.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0"
  }
}
```

### Areas/Partner/vite.partner.config.ts

```typescript
import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

export default defineConfig({
  plugins: [svelte()],
  build: {
    outDir: '../../wwwroot/partner',
    manifest: 'partner-manifest.json',
    rollupOptions: {
      input: 'src/partner-app.ts'
    }
  },
  server: {
    port: 5003
  },
  define: {
    __APP_VERSION__: JSON.stringify(process.env.npm_package_version),
    __BUILD_MODE__: JSON.stringify('partner')
  }
})
```

## ASP.NET Core Integration

### Controllers/AdminController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApp.Controllers;

[Area("Admin")]
[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Dashboard()
    {
        return View();
    }
}
```

### Areas/Admin/Views/Admin/Index.cshtml

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Enterprise Admin Dashboard</title>
    <link rel="stylesheet" href="~/admin/assets/admin-app.css" />
</head>
<body>
    <div id="admin-app"></div>
    <script type="module" src="~/admin/assets/admin-app.js"></script>
</body>
</html>
```

### Controllers/CustomerController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApp.Controllers;

[Area("Customer")]
[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    public IActionResult Portal()
    {
        return View();
    }
}
```

## Build Scenarios

### Development Build
```bash
# Build all SPAs in development mode
dotnet build --configuration Debug

# Or skip frontend builds for C# only changes
dotnet build --configuration Debug -p:SkipFrontendBuild=true
```

### Production Build
```bash
# Build all SPAs in production mode
dotnet build --configuration Release

# Parallel builds for faster CI/CD
dotnet build --configuration Release --parallel
```

### Individual SPA Development
```bash
# Work on just the admin dashboard
cd Areas/Admin
npm run dev

# Work on just the customer portal  
cd Areas/Customer
npm run dev
```

## Expected Build Output

```
🔧 Building 4 Vite configurations in parallel...

[admin] 🔧 Detected package manager: npm from package-lock.json
[admin] 🎯 Building Vite configuration: vite.admin.config.ts (mode: development)
[admin] ✅ Vue admin dashboard build completed

[customer] 🔧 Detected package manager: npm from package-lock.json  
[customer] 🎯 Building Vite configuration: vite.customer.config.ts (mode: production)
[customer] ✅ React customer portal build completed

[partner] 🔧 Detected package manager: npm from package-lock.json
[partner] 🎯 Building Vite configuration: vite.partner.config.ts (mode: staging)
[partner] ✅ Svelte partner portal build completed

[public] 🔧 Detected package manager: npm from package-lock.json
[public] 🎯 Building Vite configuration: vite.public.config.ts (mode: development)
[public] ✅ Public website build completed

🎯 All Vite builds completed successfully in 12.3s
```

## Advanced Features

### Conditional Builds
```xml
<!-- Only build customer portal in production -->
<ViteConfig Include="Areas/Customer/vite.customer.config.ts" 
            Condition="'$(Configuration)' == 'Release'">
  <BuildId>customer</BuildId>
  <OutputDir>wwwroot/customer</OutputDir>
</ViteConfig>
```

### Environment-Specific Configuration
```xml
<!-- Different modes per environment -->
<ViteConfig Include="Areas/Admin/vite.admin.config.ts">
  <Mode Condition="'$(Environment)' == 'Development'">development</Mode>
  <Mode Condition="'$(Environment)' == 'Staging'">staging</Mode>  
  <Mode Condition="'$(Environment)' == 'Production'">production</Mode>
</ViteConfig>
```

### CI/CD Optimization
```bash
# Build only changed areas (requires custom logic)
dotnet build --configuration Release -p:BuildOnlyChanged=true

# Build with specific verbosity for CI logs
dotnet build --configuration Release --verbosity minimal
```

This enterprise setup provides:
- ✅ **Technology flexibility** per business area
- ✅ **Independent versioning** and deployment
- ✅ **Shared infrastructure** (one ASP.NET Core app)
- ✅ **Optimized build times** (parallel builds)
- ✅ **Environment-specific configuration**