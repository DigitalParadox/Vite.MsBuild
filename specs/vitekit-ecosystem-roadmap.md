# ViteKit Ecosystem Roadmap

**Status:** Planning  
**Version:** Internal Specification  
**Last Updated:** November 18, 2025

---

## 🎯 Overview

This document outlines the planned evolution of the ViteKit.Msbuild ecosystem into a comprehensive ViteKit toolkit for .NET developers. The vision is to provide seamless Vite integration across the entire .NET stack with best-in-class developer experience.

---

## 📦 Package Architecture

### Current State (v1.0)

```
ViteKit.Msbuild
├── MSBuild targets and tasks
├── Command factories
└── Build-time asset compilation
```

### Target State (v2.0+)

```
ViteKit.Msbuild (Build Integration)
├── MSBuild targets & tasks
├── Command factories
└── Build-time asset compilation

ViteKit.Sdk (Runtime Core)
├── IViteManifest interface
├── IViteDevServer interface
├── ViteManifestService
├── ViteDevServerDetector
└── ViteOptions configuration

ViteKit.Middleware (ASP.NET Core Proxy)
├── ViteDevServerMiddleware
├── ViteProxyHandler
└── HMR WebSocket proxying

ViteKit.TagHelpers (Razor Integration)
├── <vite-module> tag helper
├── <vite-assets> tag helper
└── <vite-preload> tag helper

ViteKit (Meta-package)
└── References: Sdk + Middleware + TagHelpers
```

---

## 🚀 Release Roadmap

### v1.0 - Core Foundation (Current)

**Status:** ✅ Complete

- MSBuild integration
- Build automation
- Package manager detection
- Framework-agnostic asset compilation

---

### v1.1 - Enhanced Features

**Status:** 🔄 Planning

#### 1. Vite 6.0 Support

**Priority:** High  
**Complexity:** Medium

**Features:**
- Environment API support (client, ssr, edge)
- Multiple manifest generation
- Enhanced SSR capabilities
- New preview mode support

**Implementation:**
```csharp
// ViteKit.Sdk/Services/ViteEnvironmentService.cs
public class ViteEnvironmentService
{
    public async Task<ViteEnvironment> GetEnvironmentAsync(string name)
    {
        // Support for "client", "ssr", "edge" environments
        var manifestPath = Path.Combine(_options.OutputDir, $"manifest-{name}.json");
        return await LoadEnvironmentManifest(manifestPath);
    }
}
```

**Tag Helper Usage:**
```html
<vite-environment name="client" entry="main.ts" />
<vite-environment name="ssr" entry="server.ts" />
```

---

#### 2. Import Maps Support

**Priority:** Medium  
**Complexity:** Medium

**Features:**
- Native ES module imports
- Better code splitting
- CDN fallbacks
- Scope-based imports

**Implementation:**
```csharp
// ViteKit.TagHelpers/ViteImportMapTagHelper.cs
[HtmlTargetElement("vite-importmap")]
public class ViteImportMapTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var importMap = _manifestService.GenerateImportMap();
        output.TagName = "script";
        output.Attributes.Add("type", "importmap");
        output.Content.SetHtmlContent(JsonSerializer.Serialize(importMap));
    }
}
```

**Generated Output:**
```html
<script type="importmap">
{
  "imports": {
    "vue": "/dist/assets/vue.esm-XxYyZz.js",
    "lodash": "https://cdn.jsdelivr.net/npm/lodash@4.17.21/+esm"
  },
  "scopes": {
    "/admin/": {
      "vue": "/dist/assets/vue-admin.esm-AaBbCc.js"
    }
  }
}
</script>
```

---

#### 3. CSS Code Splitting

**Priority:** High  
**Complexity:** High

**Features:**
- Route-based CSS loading
- Critical CSS extraction
- Automatic preloading
- Lazy CSS loading

**Implementation:**
```csharp
// ViteKit.TagHelpers/ViteCssChunkTagHelper.cs
[HtmlTargetElement("vite-css-chunks")]
public class ViteCssChunkTagHelper : TagHelper
{
    [HtmlAttributeName("preload")]
    public bool Preload { get; set; } = true;
    
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Critical CSS inline
        if (entry.CriticalCss != null)
        {
            output.Content.AppendHtml($"<style>{entry.CriticalCss}</style>");
        }
        
        // Non-critical CSS with preload
        foreach (var css in entry.Css)
        {
            output.Content.AppendHtml($@"
                <link rel=""preload"" href=""/dist/{css}"" as=""style"" 
                      onload=""this.onload=null;this.rel='stylesheet'"" />
            ");
        }
        
        // Route-specific CSS prefetch
        foreach (var import in entry.DynamicImports)
        {
            var importEntry = _manifestService.GetEntry(import);
            if (importEntry?.Css != null)
            {
                foreach (var css in importEntry.Css)
                {
                    output.Content.AppendHtml($@"
                        <link rel=""prefetch"" href=""/dist/{css}"" as=""style"" />
                    ");
                }
            }
        }
    }
}
```

**Usage:**
```html
<vite-css-chunks entry="wwwroot/js/main.ts" preload="true" />
```

---

#### 4. SSR/SSG Helpers

**Priority:** High  
**Complexity:** Very High

**Features:**
- Server-side rendering
- Static site generation
- Pre-rendering support
- Hydration helpers

**Implementation:**
```csharp
// ViteKit.Sdk/Services/ViteSsrService.cs
public class ViteSsrService
{
    public async Task<string> RenderAsync(string entry, object props)
    {
        if (_env.IsDevelopment() && _devServer.IsRunning())
        {
            // Dev: Use Vite SSR endpoint
            return await RenderDevSsr(entry, props);
        }
        
        // Production: Use built SSR entry
        return await RenderProductionSsr(entry, props);
    }
    
    private async Task<string> RenderDevSsr(string entry, object props)
    {
        using var client = new HttpClient();
        var response = await client.PostAsJsonAsync(
            $"{_options.ServerUrl}/__vite_ssr",
            new { entry, props }
        );
        return await response.Content.ReadAsStringAsync();
    }
}
```

**Tag Helper:**
```csharp
// ViteKit.TagHelpers/ViteSsrTagHelper.cs
[HtmlTargetElement("vite-ssr")]
public class ViteSsrTagHelper : TagHelper
{
    [HtmlAttributeName("entry")]
    public string Entry { get; set; } = "";
    
    [HtmlAttributeName("props")]
    public object? Props { get; set; }
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Render on server
        var html = await _ssrService.RenderAsync(Entry, Props ?? new { });
        output.Content.SetHtmlContent(html);
        
        // Hydration script
        output.PostContent.AppendHtml($@"
            <script type=""module"">
                import {{ hydrate }} from '/{Entry}';
                hydrate(document.querySelector('[data-ssr-root]'), {JsonSerializer.Serialize(Props)});
            </script>
        ");
    }
}
```

**Usage:**
```html
<vite-ssr entry="components/ProductList.tsx" 
          props='@Json.Serialize(new { products = Model.Products })' />
```

**Dependencies:**
- Node.js runtime for SSR execution
- Vite SSR plugin
- V8 isolation or similar sandboxing

---

## 🔮 v2.0 - Advanced Features

**Status:** 🔮 Future

### 1. Blazor Integration

**Priority:** High  
**Complexity:** High

**Package:** `ViteKit.Blazor`

**Features:**
- Vite integration with Blazor WebAssembly
- JS Interop with HMR support
- Shared component architecture
- Optimize Blazor assets with Vite

**Implementation:**
```csharp
// ViteKit.Blazor/ViteBlazorExtensions.cs
public static class ViteBlazorExtensions
{
    public static IServiceCollection AddViteBlazor(this IServiceCollection services)
    {
        services.AddViteKit();
        services.AddScoped<ViteBlazorJsInterop>();
        return services;
    }
}

// ViteKit.Blazor/ViteBlazorJsInterop.cs
public class ViteBlazorJsInterop
{
    private readonly IJSRuntime _js;
    
    public async Task<IJSObjectReference> ImportModuleAsync(string path)
    {
        if (_devServer.IsRunning())
        {
            // Development: Import from dev server with HMR
            return await _js.InvokeAsync<IJSObjectReference>(
                "import", 
                $"http://localhost:5173/{path}"
            );
        }
        
        // Production: Import built module
        var entry = _manifest.GetEntry(path);
        return await _js.InvokeAsync<IJSObjectReference>(
            "import",
            $"/_content/MyApp/dist/{entry.File}"
        );
    }
    
    [JSInvokable]
    public async Task OnHmrUpdate(string path)
    {
        // Re-import module when HMR updates
        await ImportModuleAsync(path);
    }
}
```

**Usage in Blazor:**
```csharp
@inject ViteBlazorJsInterop ViteJs

@code {
    private IJSObjectReference? _module;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await ViteJs.ImportModuleAsync("wwwroot/js/chart.ts");
            await _module.InvokeVoidAsync("renderChart", ChartData);
        }
    }
}
```

**Benefits:**
- Use modern frontend tooling with Blazor
- Share JS libraries between Blazor and traditional SPAs
- Better tree-shaking and optimization
- HMR for JS components in Blazor apps

---

### 2. .NET Aspire Hosting

**Priority:** Medium  
**Complexity:** Medium

**Package:** `ViteKit.Aspire`

**Features:**
- Orchestrate Vite dev server with Aspire
- Automatic service discovery
- Health checks and monitoring
- Integrated logging

**Implementation:**
```csharp
// ViteKit.Aspire/ViteResourceExtensions.cs
public static class ViteResourceExtensions
{
    public static IResourceBuilder<ViteResource> AddVite(
        this IDistributedApplicationBuilder builder,
        string name,
        string projectPath)
    {
        var resource = new ViteResource(name, projectPath);
        
        return builder.AddResource(resource)
            .WithHttpEndpoint(port: 5173, name: "vite-dev")
            .WithHealthCheck("vite", () => CheckViteHealth(resource))
            .WithCommand("npm", "run dev", workingDirectory: projectPath);
    }
}

// ViteKit.Aspire/ViteResource.cs
public class ViteResource : Resource, IResourceWithServiceDiscovery
{
    public string ProjectPath { get; }
    
    public ViteResource(string name, string projectPath) 
        : base(name)
    {
        ProjectPath = projectPath;
    }
}
```

**Usage in AppHost:**
```csharp
// MyApp.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Add Vite dev server as a resource
var vite = builder.AddVite("frontend", "../MyApp.Web")
    .WithEnvironment("VITE_API_URL", "http://localhost:5001");

// ASP.NET app references Vite
var api = builder.AddProject<Projects.MyApp_Api>("api")
    .WithReference(vite);

builder.Build().Run();
```

**Benefits:**
- Single F5 starts entire stack
- Unified logging across all services
- Service discovery for API URLs
- Health monitoring in Aspire dashboard
- Automatic restart on crash

---

### 3. Multi-SPA Support

**Priority:** Medium  
**Complexity:** High

**Features:**
- Multiple SPAs in one application
- Independent HMR per SPA
- Shared dependency optimization
- Separate build outputs

**Implementation:**
```csharp
// ViteKit.Sdk/Services/ViteMultiSpaService.cs
public class ViteMultiSpaService
{
    private readonly Dictionary<string, ViteManifestService> _manifests = new();
    
    public void RegisterSpa(string name, ViteSpaOptions options)
    {
        _manifests[name] = new ViteManifestService(options);
    }
    
    public ViteManifestService GetSpa(string name) => _manifests[name];
}
```

**Configuration:**
```csharp
builder.Services.AddViteKit(options =>
{
    options.RegisterSpa("public", spa =>
    {
        spa.EntryPoint = "wwwroot/public/main.ts";
        spa.OutputDir = "wwwroot/dist/public";
        spa.DevServerPort = 5173;
    });
    
    options.RegisterSpa("admin", spa =>
    {
        spa.EntryPoint = "wwwroot/admin/main.ts";
        spa.OutputDir = "wwwroot/dist/admin";
        spa.DevServerPort = 5174;
    });
});
```

**MSBuild Integration:**
```xml
<ItemGroup>
  <ViteSpa Include="public">
    <EntryPoint>wwwroot/public/main.ts</EntryPoint>
    <DevServerPort>5173</DevServerPort>
  </ViteSpa>
  <ViteSpa Include="admin">
    <EntryPoint>wwwroot/admin/main.ts</EntryPoint>
    <DevServerPort>5174</DevServerPort>
  </ViteSpa>
</ItemGroup>
```

**Usage:**
```html
<!-- Public site -->
<vite-module spa="public" entry="main.ts" />

<!-- Admin panel -->
<vite-module spa="admin" entry="main.ts" />
```

**Use Cases:**
- Public website + admin dashboard
- Customer portal + employee portal
- Mobile app + desktop app
- Different frameworks per SPA

---

### 4. Module Federation

**Priority:** Low  
**Complexity:** Very High

**Package:** `ViteKit.Federation`

**Features:**
- Share code between micro-frontends
- Dynamic remote loading
- Independent deployment
- Runtime integration

**Implementation:**
```csharp
// ViteKit.Federation/ViteFederationService.cs
public class ViteFederationService
{
    public async Task<ViteRemoteModule> LoadRemoteAsync(string remoteName, string url)
    {
        // Load remote module manifest
        var manifest = await LoadRemoteManifest(url);
        
        // Register remote in import map
        _importMapService.AddRemote(remoteName, manifest);
        
        return new ViteRemoteModule(remoteName, url, manifest);
    }
}

// ViteKit.TagHelpers/ViteFederationTagHelper.cs
[HtmlTargetElement("vite-federation")]
public class ViteFederationTagHelper : TagHelper
{
    [HtmlAttributeName("remotes")]
    public string Remotes { get; set; } = "";
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var remotes = ParseRemotes(Remotes); // "dashboard@http://localhost:3001"
        
        foreach (var (name, url) in remotes)
        {
            var remote = await _federationService.LoadRemoteAsync(name, url);
            
            output.Content.AppendHtml($@"
                <script type=""module"">
                    import('{url}/remoteEntry.js').then(module => {{
                        window['{name}'] = module;
                    }});
                </script>
            ");
        }
    }
}
```

**vite.config.ts:**
```typescript
import { defineConfig } from 'vite'
import federation from '@originjs/vite-plugin-federation'

export default defineConfig({
  plugins: [
    federation({
      name: 'host-app',
      remotes: {
        dashboard: 'http://localhost:3001/remoteEntry.js',
        analytics: 'http://localhost:3002/remoteEntry.js'
      },
      shared: ['vue', 'react']
    })
  ]
})
```

**Usage:**
```html
<vite-federation remotes="dashboard@http://localhost:3001,analytics@http://localhost:3002" />

<div id="app">
  <script type="module">
    const Dashboard = await window.dashboard.Dashboard;
    // Render federated dashboard component
  </script>
</div>
```

**Use Cases:**
- Micro-frontend architecture
- Team autonomy (deploy independently)
- Different release cycles per feature
- Share common libraries at runtime

---

## 📊 Feature Comparison Matrix

| Feature | Version | Priority | Complexity | Dependencies | Impact |
|---------|---------|----------|------------|--------------|--------|
| Vite 6.0 Support | v1.1 | High | Medium | Vite 6.0+ | High |
| Import Maps | v1.1 | Medium | Medium | Modern browsers | Medium |
| CSS Code Splitting | v1.1 | High | High | Build analysis | High |
| SSR/SSG Helpers | v1.1 | High | Very High | Node.js | High |
| Blazor Integration | v2.0 | High | High | Blazor WASM | High |
| .NET Aspire | v2.0 | Medium | Medium | .NET Aspire 9.0+ | Medium |
| Multi-SPA | v2.0 | Medium | High | Process mgmt | Medium |
| Module Federation | v2.0 | Low | Very High | @originjs plugin | Medium |

---

## 🎯 Success Criteria

### v1.1 Goals

- ✅ Support all Vite 6.0 environment APIs
- ✅ Generate import maps from manifest
- ✅ Automatic critical CSS extraction
- ✅ SSR rendering in both dev and production
- ✅ 90%+ test coverage for new features
- ✅ Comprehensive documentation with examples

### v2.0 Goals

- ✅ Blazor WASM apps can use Vite with HMR
- ✅ Aspire orchestration works seamlessly
- ✅ Multi-SPA support with independent HMR
- ✅ Module federation with remote loading
- ✅ Production-ready performance
- ✅ Enterprise support and documentation

---

## 🔗 Related Documents

- [ViteKit Architecture](./vitekit-architecture.md) (planned)
- [HMR Integration Guide](./hmr-integration.md) (planned)
- [Performance Optimization](./performance.md) (planned)
- [Migration Guide](./migration-guide.md) (planned)

---

## 📝 Notes

### Technical Considerations

**Browser Compatibility:**
- Import maps require modern browsers (Chrome 89+, Firefox 108+, Safari 16.4+)
- Fallback strategies needed for older browsers
- Consider polyfills for enterprise scenarios

**Performance:**
- CSS code splitting must not increase initial load time
- Lazy loading should be opt-in
- Monitor Core Web Vitals impact

**Developer Experience:**
- All features should "just work" with minimal configuration
- Error messages must be clear and actionable
- Documentation must include working examples

**Testing:**
- Unit tests for all services
- Integration tests for middleware
- E2E tests for tag helpers
- Performance benchmarks

### Open Questions

1. **SSR Runtime:** Node.js, V8 isolates, or .NET-based JS engine?
2. **Blazor:** Should we support Blazor Server or only WASM?
3. **Aspire:** Minimum .NET Aspire version requirement?
4. **Federation:** Support webpack federation protocol or Vite-specific?

---

**Document Status:** Living document - will be updated as features are implemented

**Last Review:** November 18, 2025  
**Next Review:** Q1 2026
