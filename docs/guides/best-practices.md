---
layout: default
title: Best Practices
parent: Guides
nav_order: 5
---

# Best Practices
{: .fs-9 }

Recommended practices for using Vite with ASP.NET Core and Vite.MsBuild.
{: .fs-6 .fw-300 }

## Project Organization

### Recommended Directory Structure

```
MyProject/
├── MyProject.csproj
├── package.json
├── vite.config.ts
├── tsconfig.json
├── Controllers/           ← ASP.NET Core controllers
├── Models/               ← C# models
├── Services/             ← Backend services
├── ClientApp/            ← Frontend source code
│   ├── src/
│   │   ├── main.ts
│   │   ├── App.vue
│   │   ├── components/
│   │   ├── views/
│   │   └── assets/
│   └── public/           ← Static assets (copied as-is)
└── wwwroot/              ← Build output
    └── dist/
```

**Benefits:**
- Clear separation of frontend and backend code
- Easy to navigate and understand
- Follows ASP.NET Core conventions
- Compatible with common frontend frameworks

### Alternative: Separate Client Project

For larger applications, consider separating the frontend:

```
MyApp.sln
├── src/
│   ├── MyApp.Api/        ← ASP.NET Core API
│   │   └── MyApp.Api.csproj
│   └── MyApp.Web/        ← Frontend application
│       ├── MyApp.Web.csproj
│       ├── package.json
│       └── vite.config.ts
```

## Configuration Management

### Use Consistent Naming

```xml
<PropertyGroup>
  <!-- Always prefix with "Vite" -->
  <ViteConfigFile>vite.config.ts</ViteConfigFile>
  <ViteOutputDir>wwwroot/dist</ViteOutputDir>
  <ViteMode>production</ViteMode>
</PropertyGroup>
```

### Environment-Specific Settings

**appsettings.Development.json:**

```json
{
  "Vite": {
    "DevServerUrl": "http://localhost:5173",
    "EnableHMR": true
  }
}
```

**appsettings.Production.json:**

```json
{
  "Vite": {
    "AssetPath": "/dist",
    "EnableHMR": false
  }
}
```

### Centralize Shared Configuration

Use `Directory.Build.props` for multi-project solutions:

```xml
<!-- Directory.Build.props at solution root -->
<Project>
  <PropertyGroup>
    <!-- Shared Vite settings -->
    <PackageManager>pnpm</PackageManager>
    <ViteBuildTiming>BeforeCSharp</ViteBuildTiming>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Vite.MsBuild" Version="2.0.0" />
  </ItemGroup>
</Project>
```

## Development Workflow

### Local Development

**Recommended setup:**

1. Run Vite dev server for HMR
2. Run ASP.NET Core with `dotnet watch`
3. Proxy frontend requests to Vite

```bash
# Terminal 1: Vite dev server
npm run dev

# Terminal 2: Backend with watch
dotnet watch run
```

### Git Ignore Configuration

```gitignore
# Node
node_modules/
npm-debug.log*
yarn-error.log*
.pnpm-store/

# Vite build outputs
dist/
wwwroot/dist/
wwwroot/assets/
*.local

# Keep only one package manager lock file
# (Uncomment the ones you DON'T use)
# package-lock.json
# pnpm-lock.yaml
# yarn.lock
# bun.lockb

# MSBuild markers
obj/
bin/
*.marker

# IDE
.vs/
.vscode/
.idea/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db
```

### Branch Protection

Ensure critical files are committed:

```bash
# Always commit
- package.json
- package-lock.json (or your chosen lock file)
- vite.config.ts
- tsconfig.json

# Never commit
- node_modules/
- dist/
- wwwroot/dist/
```

## Dependency Management

### Lock File Strategy

**Always commit lock files:**

```bash
# Commit your package manager's lock file
git add package-lock.json  # npm
git add pnpm-lock.yaml     # pnpm
git add yarn.lock          # yarn
git add bun.lockb          # bun
```

**Why:** Ensures consistent dependency versions across team and CI/CD.

### Update Dependencies Regularly

```bash
# Check for outdated packages
npm outdated

# Update non-breaking changes
npm update

# Update with breaking changes (carefully)
npm install vue@latest
```

### Pin Critical Versions

```json
{
  "dependencies": {
    "vue": "3.3.4",        // Exact version for stability
    "axios": "^1.4.0"      // Allow patch updates
  },
  "devDependencies": {
    "vite": "~5.0.0"       // Allow minor updates
  }
}
```

## Build Optimization

### Development Builds

Optimize for speed during development:

```typescript
// vite.config.ts
export default defineConfig(({ mode }) => ({
  build: {
    minify: mode === 'production',
    sourcemap: mode !== 'production',
    rollupOptions: {
      output: {
        manualChunks: undefined  // Faster builds
      }
    }
  },
  optimizeDeps: {
    include: ['vue', 'vue-router']  // Pre-bundle dependencies
  }
}))
```

### Production Builds

Optimize for performance in production:

```typescript
export default defineConfig({
  build: {
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true  // Remove console.log
      }
    },
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor': ['vue', 'vue-router', 'pinia'],
          'ui': ['@/components/Button.vue', '@/components/Input.vue']
        }
      }
    },
    chunkSizeWarningLimit: 500
  }
})
```

### Asset Optimization

```typescript
export default defineConfig({
  build: {
    assetsInlineLimit: 4096,  // Inline assets < 4kb
    cssCodeSplit: true,       // Split CSS per chunk
    reportCompressedSize: false  // Faster builds
  }
})
```

## Code Organization

### Component Structure

```
src/
├── components/
│   ├── common/          ← Reusable components
│   │   ├── Button.vue
│   │   ├── Input.vue
│   │   └── Modal.vue
│   ├── layout/          ← Layout components
│   │   ├── Header.vue
│   │   ├── Footer.vue
│   │   └── Sidebar.vue
│   └── features/        ← Feature-specific components
│       ├── user/
│       └── product/
├── views/               ← Page components
├── composables/         ← Composition API logic
├── stores/              ← State management
├── services/            ← API calls
└── utils/               ← Helper functions
```

### Import Aliases

```typescript
// vite.config.ts
import { fileURLToPath } from 'url'
import { defineConfig } from 'vite'

export default defineConfig({
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@components': fileURLToPath(new URL('./src/components', import.meta.url)),
      '@views': fileURLToPath(new URL('./src/views', import.meta.url))
    }
  }
})
```

**Usage:**

```typescript
import Button from '@/components/common/Button.vue'
import { useUserStore } from '@/stores/user'
```

### API Service Layer

Create a consistent API layer:

```typescript
// src/services/api.ts
import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000
})

// Request interceptor
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor
api.interceptors.response.use(
  response => response.data,
  error => {
    console.error('API Error:', error)
    return Promise.reject(error)
  }
)

export default api
```

```typescript
// src/services/users.ts
import api from './api'
import type { User } from '@/types'

export const userService = {
  getAll: () => api.get<User[]>('/users'),
  getById: (id: number) => api.get<User>(`/users/${id}`),
  create: (user: User) => api.post<User>('/users', user),
  update: (id: number, user: User) => api.put<User>(`/users/${id}`, user),
  delete: (id: number) => api.delete(`/users/${id}`)
}
```

## Performance Best Practices

### Lazy Loading

```typescript
// router/index.ts
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: () => import('@/views/Home.vue')  // Lazy loaded
    },
    {
      path: '/admin',
      component: () => import('@/views/Admin.vue')  // Only loads when accessed
    }
  ]
})
```

### Image Optimization

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import viteImagemin from 'vite-plugin-imagemin'

export default defineConfig({
  plugins: [
    viteImagemin({
      gifsicle: { optimizationLevel: 7 },
      optipng: { optimizationLevel: 7 },
      mozjpeg: { quality: 80 },
      webp: { quality: 80 }
    })
  ]
})
```

### Bundle Analysis

```bash
# Install bundle analyzer
npm install --save-dev rollup-plugin-visualizer
```

```typescript
// vite.config.ts
import { visualizer } from 'rollup-plugin-visualizer'

export default defineConfig({
  plugins: [
    visualizer({
      open: true,
      filename: 'dist/stats.html'
    })
  ]
})
```

## Security Best Practices

### Environment Variables

Never commit sensitive data:

```bash
# .env (DO NOT COMMIT)
VITE_API_KEY=secret-key-here
VITE_API_URL=https://api.example.com
```

```bash
# .env.example (COMMIT THIS)
VITE_API_KEY=your-api-key
VITE_API_URL=https://api.example.com
```

**Usage in code:**

```typescript
const apiKey = import.meta.env.VITE_API_KEY
const apiUrl = import.meta.env.VITE_API_URL
```

### Content Security Policy

```html
<!-- Views/Shared/_Layout.cshtml -->
<meta http-equiv="Content-Security-Policy" 
      content="default-src 'self'; 
               script-src 'self' 'unsafe-inline' 'unsafe-eval'; 
               style-src 'self' 'unsafe-inline';">
```

### HTTPS in Development

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import fs from 'fs'

export default defineConfig({
  server: {
    https: {
      key: fs.readFileSync('./certs/localhost-key.pem'),
      cert: fs.readFileSync('./certs/localhost.pem')
    }
  }
})
```

## Testing Best Practices

### Unit Tests

```typescript
// src/components/Button.spec.ts
import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import Button from './Button.vue'

describe('Button', () => {
  it('emits click event', async () => {
    const wrapper = mount(Button)
    await wrapper.trigger('click')
    expect(wrapper.emitted()).toHaveProperty('click')
  })
})
```

### Component Testing

```typescript
// tests/components/UserProfile.spec.ts
import { mount } from '@vue/test-utils'
import UserProfile from '@/components/UserProfile.vue'

describe('UserProfile', () => {
  it('displays user information', () => {
    const user = { name: 'John', email: 'john@example.com' }
    const wrapper = mount(UserProfile, {
      props: { user }
    })
    expect(wrapper.text()).toContain('John')
  })
})
```

### E2E Tests

```typescript
// tests/e2e/login.spec.ts
import { test, expect } from '@playwright/test'

test('user can login', async ({ page }) => {
  await page.goto('http://localhost:5000/login')
  await page.fill('[name="email"]', 'user@example.com')
  await page.fill('[name="password"]', 'password123')
  await page.click('button[type="submit"]')
  await expect(page).toHaveURL('http://localhost:5000/dashboard')
})
```

## CI/CD Best Practices

### GitHub Actions Example

```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          cache: 'npm'
      
      - name: Install dependencies
        run: npm ci
      
      - name: Run tests
        run: npm test
      
      - name: Build
        run: dotnet build --configuration Release
      
      - name: Publish
        run: dotnet publish --configuration Release --output ./publish
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v3
        with:
          name: app
          path: ./publish
```

### Caching Strategies

```yaml
- name: Cache node modules
  uses: actions/cache@v3
  with:
    path: ~/.npm
    key: ${{ runner.os }}-node-${{ hashFiles('**/package-lock.json') }}
    restore-keys: |
      ${{ runner.os }}-node-

- name: Cache NuGet packages
  uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

## Documentation Practices

### README Structure

```markdown
# Project Name

## Prerequisites
- .NET 8.0+
- Node.js 18+
- pnpm 8+

## Getting Started
1. Clone the repository
2. Install dependencies: `pnpm install`
3. Run development: `dotnet watch run`

## Project Structure
[Describe your structure]

## Building for Production
`dotnet publish -c Release`

## Contributing
[Contribution guidelines]
```

### Inline Documentation

```typescript
/**
 * Fetches user data from the API
 * @param userId - The unique identifier of the user
 * @returns Promise containing user data
 * @throws {ApiError} When the user is not found
 */
export async function fetchUser(userId: number): Promise<User> {
  const response = await api.get(`/users/${userId}`)
  return response.data
}
```

## Monitoring and Logging

### Frontend Error Tracking

```typescript
// src/main.ts
import { createApp } from 'vue'
import App from './App.vue'

const app = createApp(App)

app.config.errorHandler = (err, instance, info) => {
  console.error('Vue Error:', err)
  console.error('Component:', instance)
  console.error('Info:', info)
  
  // Send to error tracking service
  // trackError(err, { component: instance, info })
}

app.mount('#app')
```

### Performance Monitoring

```typescript
// Log build performance
if (import.meta.env.DEV) {
  console.log('App started at:', new Date().toISOString())
  
  window.addEventListener('load', () => {
    const perfData = performance.getEntriesByType('navigation')[0]
    console.log('Load time:', perfData.loadEventEnd - perfData.fetchStart, 'ms')
  })
}
```

## Additional Resources

- [Vite Best Practices](https://vitejs.dev/guide/best-practices.html)
- [ASP.NET Core Performance Best Practices](https://docs.microsoft.com/aspnet/core/performance/performance-best-practices)
- [Vue.js Style Guide](https://vuejs.org/style-guide/)
- [React Best Practices](https://react.dev/learn/thinking-in-react)
