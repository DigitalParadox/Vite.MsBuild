---
layout: default
title: Vue + TypeScript
parent: Framework Examples
grand_parent: Examples
nav_order: 1
---

# Vue 3 + TypeScript Example
{: .fs-9 }

Complete example showing Vite.MsBuild with Vue 3 and TypeScript.
{: .fs-6 .fw-300 }

## Project Structure

```
VueWebApp/
├── VueWebApp.csproj
├── package.json
├── vite.config.ts
├── tsconfig.json
├── src/
│   ├── main.ts
│   ├── App.vue
│   └── components/
│       └── HelloWorld.vue
└── wwwroot/
    └── index.html
```

## Files

### VueWebApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Vite.MsBuild" Version="2.0.0" />
  </ItemGroup>

  <!-- Optional: Custom configuration -->
  <PropertyGroup>
    <ViteOutputDir>wwwroot/dist</ViteOutputDir>
    <ViteMode Condition="'$(Configuration)' == 'Debug'">development</ViteMode>
    <ViteMode Condition="'$(Configuration)' == 'Release'">production</ViteMode>
  </PropertyGroup>
</Project>
```

### package.json

```json
{
  "name": "vue-web-app",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "build": "vue-tsc && vite build",
    "dev": "vite",
    "preview": "vite preview"
  },
  "dependencies": {
    "vue": "^3.3.0"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^4.4.0",
    "typescript": "^5.0.0",
    "vite": "^5.0.0",
    "vue-tsc": "^1.8.0"
  }
}
```

### vite.config.ts

```typescript
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: 'wwwroot/dist',
    manifest: true,
    rollupOptions: {
      input: 'src/main.ts'
    }
  },
  server: {
    hmr: {
      clientPort: 443 // For HTTPS development
    }
  }
})
```

### src/main.ts

```typescript
import { createApp } from 'vue'
import './style.css'
import App from './App.vue'

createApp(App).mount('#app')
```

### src/App.vue

```vue
<template>
  <div id="app">
    <img alt="Vue logo" src="/vue.svg" />
    <HelloWorld msg="Vue + ASP.NET Core + Vite" />
  </div>
</template>

<script setup lang="ts">
import HelloWorld from './components/HelloWorld.vue'
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
```

### src/components/HelloWorld.vue

```vue
<template>
  <div class="hello">
    <h1>{{ msg }}</h1>
    <p>
      Build status: <span :class="buildMode">{{ buildMode }}</span>
    </p>
    <button @click="count++" type="button">
      Count is: {{ count }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

defineProps<{
  msg: string
}>()

const count = ref(0)
const buildMode = computed(() => 
  import.meta.env.MODE === 'development' ? 'Development' : 'Production'
)
</script>

<style scoped>
.hello {
  margin: 20px;
}

.Development {
  color: #42b883;
}

.Production {
  color: #35495e;
}

button {
  background-color: #42b883;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  cursor: pointer;
}

button:hover {
  background-color: #369970;
}
</style>
```

## ASP.NET Core Integration

### Controllers/HomeController.cs

```csharp
using Microsoft.AspNetCore.Mvc;

namespace VueWebApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
```

### Views/Home/Index.cshtml

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/vue.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Vue + ASP.NET Core</title>
</head>
<body>
    <div id="app"></div>
    <script type="module" src="~/dist/src/main.js"></script>
</body>
</html>
```

## Usage

```bash
# Install Vue dependencies
npm install

# Development build
dotnet build --configuration Debug

# Production build  
dotnet build --configuration Release

# Run the application
dotnet run
```

## Expected Output

```
🔧 Detected package manager: npm from package-lock.json
📁 Collected 4 source files and 2 config files
🎯 Building Vite configuration: vite.config.ts (mode: development)
✅ Vite build completed successfully
✅ Vue components compiled successfully
```