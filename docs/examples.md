---
layout: default
title: Examples
nav_order: 4
has_children: true
---

# Examples
{: .no_toc }

Real-world examples for different frameworks and scenarios.
{: .fs-6 .fw-300 }

---

## Quick Examples

### Vue 3
```xml
<!-- MyProject.csproj -->
<ItemGroup>
  <PackageReference Include="ViteKit.Msbuild" Version="*" />
</ItemGroup>
```

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: 'wwwroot',
    emptyOutDir: true
  }
})
```

---

### React
```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'wwwroot',
    manifest: true
  }
})
```

---

### Svelte
```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

export default defineConfig({
  plugins: [svelte()],
  build: {
    outDir: 'wwwroot'
  }
})
```

---

## Framework-Specific Guides

Browse detailed examples for each framework:

- [Vue 3](examples/vue) - Single-file components, Composition API
- [React](examples/react) - JSX, TypeScript, Hot Module Replacement
- [Svelte](examples/svelte) - Reactive components, stores
- [Vanilla TypeScript](examples/vanilla) - No framework, just modern JS/TS

---

## Scenario Examples

Common use cases:

- [Advanced Scenarios](examples/advanced) - Named scripts, custom commands, clean targets
- [Multi-SPA Setup](examples/multi-spa) - Multiple Vite apps in one project
- [Monorepo](examples/monorepo) - Shared configs across projects
- [Custom Build Modes](examples/custom-modes) - Staging, testing environments

---

## All Examples

Visit the child pages for complete, copy-paste ready examples with full explanations.
