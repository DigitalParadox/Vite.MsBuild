---
layout: default
title: Advanced Scenarios
parent: Guides
nav_order: 4
---

# Advanced Scenarios
{: .fs-9 }

Advanced configuration patterns and real-world scenarios for ViteKit.Msbuild.
{: .fs-6 .fw-300 }

## Development vs Production Builds

### Automatic Mode Mapping

ViteKit.Msbuild automatically maps MSBuild configurations to Vite modes:

```bash
# Development mode
dotnet build --configuration Debug
# → vite build --mode development

# Production mode
dotnet build --configuration Release
# → vite build --mode production
```

### Custom Build Configurations

Create custom configurations for staging, testing, or other environments:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Staging'">
  <ViteMode>staging</ViteMode>
  <Optimize>true</Optimize>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Testing'">
  <ViteMode>test</ViteMode>
  <Optimize>false</Optimize>
</PropertyGroup>
```

**Vite configuration:**

```typescript
// vite.config.ts
import { defineConfig } from 'vite'

export default defineConfig(({ mode }) => ({
  build: {
    minify: mode === 'production' || mode === 'staging',
    sourcemap: mode !== 'production'
  },
  define: {
    __APP_VERSION__: JSON.stringify(process.env.npm_package_version),
    __BUILD_MODE__: JSON.stringify(mode)
  }
}))
```

### Environment-Specific Builds

```bash
# Build with custom mode
dotnet build --configuration Release /p:ViteMode=staging

# Build multiple configurations
dotnet build --configuration Debug && dotnet build --configuration Release
```

## Hot Module Replacement (HMR) with dotnet watch

### Setup for Development

Configure Vite dev server to run alongside ASP.NET Core:

**vite.config.ts:**

```typescript
import { defineConfig } from 'vite'

export default defineConfig({
  server: {
    port: 5173,
    strictPort: true,
    hmr: {
      protocol: 'ws',
      host: 'localhost',
      port: 5173
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
})
```

**Development workflow:**

```bash
# Terminal 1: Run Vite dev server
npm run dev

# Terminal 2: Run ASP.NET Core with watch
dotnet watch run
```

### Automatic Dev Server Integration

**Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Proxy to Vite dev server
    app.UseSpa(spa =>
    {
        spa.Options.SourcePath = "ClientApp";
        spa.UseProxyToSpaDevelopmentServer("http://localhost:5173");
    });
}
else
{
    app.UseStaticFiles();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
```

## Conditional Vite Builds

### Skip Vite Build in CI

```xml
<PropertyGroup>
  <!-- Disable Vite build in CI if frontend was pre-built -->
  <EnableViteBuild Condition="'$(CI)' == 'true' AND Exists('wwwroot/manifest.json')">false</EnableViteBuild>
</PropertyGroup>
```

### Build Only on File Changes

```xml
<PropertyGroup>
  <!-- Only build if frontend files changed -->
  <ViteBuildOnlyIfChanged>true</ViteBuildOnlyIfChanged>
</PropertyGroup>
```

### Configuration-Specific Builds

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <!-- Fast builds in debug -->
  <ViteMode>development</ViteMode>
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <!-- Optimized builds in release -->
  <ViteMode>production</ViteMode>
  <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>
</PropertyGroup>
```

## Custom Build Timing

### Build Before C# Compilation (Default)

Assets are available immediately for C# code that references them:

```xml
<PropertyGroup>
  <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>
</PropertyGroup>
```

**Use case:** When C# code needs to read the Vite manifest or verify asset existence.

### Build After C# Compilation

Faster incremental builds when C# changes frequently:

```xml
<PropertyGroup>
  <ViteBuildTiming>AfterCSharp</ViteBuildTiming>
</PropertyGroup>
```

**Use case:** Rapid iteration on backend code without waiting for frontend builds.

## Asset Organization

### Multiple Entry Points

**vite.config.ts:**

```typescript
import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    rollupOptions: {
      input: {
        main: 'src/main.ts',
        admin: 'src/admin.ts',
        public: 'src/public.ts'
      }
    }
  }
})
```

### Shared Components Library

Create a shared component library that multiple projects can use:

```typescript
// packages/ui-library/vite.config.ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    lib: {
      entry: 'src/index.ts',
      name: 'UILibrary',
      fileName: 'ui-library'
    },
    rollupOptions: {
      external: ['vue'],
      output: {
        globals: {
          vue: 'Vue'
        }
      }
    }
  }
})
```

**Consuming project:**

```json
{
  "dependencies": {
    "@myorg/ui-library": "workspace:*"
  }
}
```

## Integration with ASP.NET Core Features

### Static File Configuration

```csharp
// Program.cs
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static assets for 1 year
        if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
        {
            ctx.Context.Response.Headers.Append(
                "Cache-Control", "public,max-age=31536000,immutable");
        }
    }
});
```

### Content Security Policy

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/index.html"))
    {
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline';");
    }
    await next();
});
```

### Manifest-Based Asset Loading

Read Vite's manifest to load assets with cache-busting:

```csharp
public class ViteManifest
{
    public Dictionary<string, ManifestEntry> Entries { get; set; }
    
    public class ManifestEntry
    {
        public string File { get; set; }
        public string[] Css { get; set; }
        public string[] Assets { get; set; }
    }
}

public class ViteHelper
{
    private readonly ViteManifest _manifest;
    
    public ViteHelper(IWebHostEnvironment env)
    {
        var manifestPath = Path.Combine(env.WebRootPath, "manifest.json");
        if (File.Exists(manifestPath))
        {
            var json = File.ReadAllText(manifestPath);
            _manifest = JsonSerializer.Deserialize<ViteManifest>(json);
        }
    }
    
    public string GetAssetPath(string entry)
    {
        return _manifest?.Entries.TryGetValue(entry, out var e) == true
            ? $"/{e.File}"
            : entry;
    }
}
```

**Usage in views:**

```html
@inject ViteHelper Vite

<link rel="stylesheet" href="@Vite.GetAssetPath("src/main.ts")" />
<script type="module" src="@Vite.GetAssetPath("src/main.ts")"></script>
```

## Performance Optimization

### Build Caching

```xml
<PropertyGroup>
  <!-- Enable incremental builds -->
  <ViteIncrementalBuild>true</ViteIncrementalBuild>
</PropertyGroup>
```

### Parallel Builds

```bash
# Build multiple projects in parallel
dotnet build -m:4

# ViteKit.Msbuild handles npm install conflicts automatically
```

### Output Compression

**vite.config.ts:**

```typescript
import { defineConfig } from 'vite'
import viteCompression from 'vite-plugin-compression'

export default defineConfig({
  plugins: [
    viteCompression({
      algorithm: 'gzip',
      ext: '.gz'
    }),
    viteCompression({
      algorithm: 'brotliCompress',
      ext: '.br'
    })
  ]
})
```

### Code Splitting Strategies

```typescript
export default defineConfig({
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'vue-vendor': ['vue', 'vue-router', 'pinia'],
          'ui-components': ['./src/components/Button.vue', './src/components/Input.vue']
        }
      }
    }
  }
})
```

## Docker Integration

### Multi-Stage Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_18.x | bash -
RUN apt-get install -y nodejs

# Copy project files
COPY ["MyApp/MyApp.csproj", "MyApp/"]
COPY ["MyApp/package.json", "MyApp/"]
COPY ["MyApp/package-lock.json", "MyApp/"]

# Restore dependencies
WORKDIR /src/MyApp
RUN dotnet restore
RUN npm ci

# Copy source code
COPY MyApp/ .

# Build application (includes Vite build via ViteKit.Msbuild)
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

### Docker Compose Development

```yaml
version: '3.8'

services:
  webapp:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:80"
    volumes:
      - ./wwwroot:/app/wwwroot
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
  
  vite-dev:
    image: node:18
    working_dir: /app
    volumes:
      - ./:/app
    ports:
      - "5173:5173"
    command: npm run dev
```

## TypeScript Integration

### Shared Types Between C# and TypeScript

Generate TypeScript types from C# models:

```csharp
// Use a tool like TypeGen or NSwag
[ExportTsInterface]
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

**Generated TypeScript:**

```typescript
// src/types/generated.ts
export interface UserDto {
  id: number;
  name: string;
  email: string;
}
```

### Type-Safe API Calls

```typescript
import type { UserDto } from './types/generated'

export async function getUsers(): Promise<UserDto[]> {
  const response = await fetch('/api/users')
  return response.json()
}
```

## Testing Integration

### E2E Testing with Playwright

```typescript
// tests/e2e/app.spec.ts
import { test, expect } from '@playwright/test'

test('homepage loads correctly', async ({ page }) => {
  await page.goto('http://localhost:5000')
  await expect(page.locator('h1')).toContainText('Welcome')
})
```

**Run tests after build:**

```xml
<Target Name="RunE2ETests" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="npx playwright test" />
</Target>
```

### Unit Testing Frontend

```typescript
// src/components/Button.spec.ts
import { mount } from '@vue/test-utils'
import Button from './Button.vue'

describe('Button', () => {
  it('renders correctly', () => {
    const wrapper = mount(Button, {
      props: { label: 'Click me' }
    })
    expect(wrapper.text()).toBe('Click me')
  })
})
```

## Troubleshooting Advanced Scenarios

### Debug Vite Build Process

```bash
# Verbose MSBuild output
dotnet build -v:detailed

# Show Vite diagnostics
dotnet build /p:ViteVerbosity=debug
```

### Custom Error Handling

```xml
<Target Name="HandleViteBuildFailure" AfterTargets="ViteBuildAssets" Condition="'$(ViteBuildFailed)' == 'true'">
  <Warning Text="Vite build failed, but continuing with cached assets" />
  <PropertyGroup>
    <ViteBuildFailed>false</ViteBuildFailed>
  </PropertyGroup>
</Target>
```

### Performance Profiling

```bash
# Profile build time
dotnet build /p:EnableViteBuildTiming=true

# Output shows:
# Vite build completed in 2.3s
# C# compilation completed in 1.5s
# Total build time: 3.8s
```

## Clean Target Integration

ViteKit.Msbuild automatically integrates with MSBuild's `Clean` target by removing its marker files and Vite cache. However, it does NOT remove your Vite build outputs since those locations are configured in your Vite config files.

### Current Behavior

When you run `dotnet clean`, ViteKit.Msbuild automatically:
- ✅ Removes marker files from `obj/` directory
- ✅ Clears incremental build state
- ✅ Allows next build to be a clean build

### Custom Output Cleanup (Optional)

If you want to also remove Vite's build outputs during clean, add a custom target:

```xml
<Target Name="CleanViteOutputs" BeforeTargets="Clean">
  <!-- Remove Vite output directory configured in your vite.config -->
  <RemoveDir Directories="$(ViteOutputDir)" Condition="Exists('$(ViteOutputDir)')" />
</Target>
```

**Note**: This is optional. MSBuild's standard `Clean` already handles ViteKit.Msbuild's internal state.

## Additional Resources

- [Vite Build Optimizations](https://vitejs.dev/guide/build.html)
- [ASP.NET Core Static Files](https://docs.microsoft.com/aspnet/core/fundamentals/static-files)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
