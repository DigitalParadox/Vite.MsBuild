---
layout: default
title: Non-.NET Examples
parent: Examples
nav_order: 6
---

# Non-.NET Examples
{: .fs-9 }

Using Vite without ASP.NET Core - static sites, Node.js apps, and standalone frontends.
{: .fs-6 .fw-300 }

{: .note }
While ViteKit.Msbuild is designed for ASP.NET Core integration, Vite itself works great with many other setups. These examples show pure Vite usage.

## Static HTML/JavaScript Website

### Project Structure

```
my-static-site/
├── index.html
├── about.html
├── contact.html
├── package.json
├── vite.config.js
├── src/
│   ├── main.js
│   ├── styles.css
│   └── utils/
└── public/
    ├── favicon.ico
    └── images/
```

### Configuration

**package.json:**

```json
{
  "name": "my-static-site",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  },
  "devDependencies": {
    "vite": "^5.0.0"
  }
}
```

**vite.config.js:**

```javascript
import { defineConfig } from 'vite'
import { resolve } from 'path'

export default defineConfig({
  build: {
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html'),
        about: resolve(__dirname, 'about.html'),
        contact: resolve(__dirname, 'contact.html')
      }
    }
  }
})
```

**index.html:**

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>My Static Site</title>
</head>
<body>
  <header>
    <nav>
      <a href="/">Home</a>
      <a href="/about.html">About</a>
      <a href="/contact.html">Contact</a>
    </nav>
  </header>
  
  <main>
    <h1>Welcome to My Site</h1>
    <p>This is a static website built with Vite.</p>
  </main>
  
  <script type="module" src="/src/main.js"></script>
</body>
</html>
```

**src/main.js:**

```javascript
import './styles.css'

console.log('Site loaded!')

// Add interactivity
document.addEventListener('DOMContentLoaded', () => {
  const navLinks = document.querySelectorAll('nav a')
  const currentPath = window.location.pathname
  
  navLinks.forEach(link => {
    if (link.getAttribute('href') === currentPath) {
      link.classList.add('active')
    }
  })
})
```

### Development & Build

```bash
# Install dependencies
npm install

# Development server
npm run dev
# → http://localhost:5173

# Production build
npm run build
# → Output in dist/

# Preview production build
npm run preview
```

### Deployment

Deploy the `dist/` folder to any static hosting:

```bash
# Netlify
netlify deploy --dir=dist --prod

# Vercel
vercel --prod

# GitHub Pages
npm run build
git subtree push --prefix dist origin gh-pages
```

## Node.js + Express + Vite

### Project Structure

```
my-node-app/
├── server/
│   ├── index.js
│   ├── routes/
│   └── middleware/
├── client/
│   ├── src/
│   │   ├── main.js
│   │   └── App.vue
│   ├── index.html
│   └── vite.config.js
├── package.json
└── .env
```

### Configuration

**package.json:**

```json
{
  "name": "my-node-app",
  "type": "module",
  "scripts": {
    "dev": "concurrently \"npm run dev:server\" \"npm run dev:client\"",
    "dev:server": "nodemon server/index.js",
    "dev:client": "vite",
    "build": "vite build",
    "start": "node server/index.js"
  },
  "dependencies": {
    "express": "^4.18.0",
    "vue": "^3.3.0"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^4.4.0",
    "concurrently": "^8.2.0",
    "nodemon": "^3.0.0",
    "vite": "^5.0.0"
  }
}
```

**server/index.js:**

```javascript
import express from 'express'
import { fileURLToPath } from 'url'
import { dirname, join } from 'path'

const __dirname = dirname(fileURLToPath(import.meta.url))
const app = express()
const PORT = process.env.PORT || 3000
const isDev = process.env.NODE_ENV !== 'production'

app.use(express.json())

// API routes
app.get('/api/users', (req, res) => {
  res.json([
    { id: 1, name: 'Alice' },
    { id: 2, name: 'Bob' }
  ])
})

if (!isDev) {
  // Serve static files from Vite build in production
  app.use(express.static(join(__dirname, '../dist')))
  
  // SPA fallback
  app.get('*', (req, res) => {
    res.sendFile(join(__dirname, '../dist/index.html'))
  })
}

app.listen(PORT, () => {
  console.log(`Server running on http://localhost:${PORT}`)
})
```

**client/vite.config.js:**

```javascript
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:3000',
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: '../dist',
    emptyOutDir: true
  }
})
```

**client/src/App.vue:**

```vue
<template>
  <div>
    <h1>Users</h1>
    <ul>
      <li v-for="user in users" :key="user.id">
        {{ user.name }}
      </li>
    </ul>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const users = ref([])

onMounted(async () => {
  const response = await fetch('/api/users')
  users.value = await response.json()
})
</script>
```

### Development & Build

```bash
# Install dependencies
npm install

# Development (runs both server and Vite)
npm run dev
# → Server: http://localhost:3000
# → Vite dev: http://localhost:5173

# Production build
npm run build
npm start
```

## Vanilla JavaScript SPA

### Project Structure

```
my-spa/
├── index.html
├── package.json
├── vite.config.js
└── src/
    ├── main.js
    ├── router.js
    ├── views/
    │   ├── Home.js
    │   ├── About.js
    │   └── NotFound.js
    └── styles/
        └── main.css
```

### Configuration

**vite.config.js:**

```javascript
import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    target: 'esnext',
    minify: 'terser'
  }
})
```

**src/router.js:**

```javascript
class Router {
  constructor(routes) {
    this.routes = routes
    this.currentRoute = null
    
    window.addEventListener('popstate', () => this.handleRoute())
    
    document.addEventListener('click', (e) => {
      if (e.target.matches('[data-link]')) {
        e.preventDefault()
        this.navigate(e.target.href)
      }
    })
    
    this.handleRoute()
  }
  
  navigate(url) {
    window.history.pushState(null, null, url)
    this.handleRoute()
  }
  
  handleRoute() {
    const path = window.location.pathname
    const route = this.routes.find(r => r.path === path) || this.routes.find(r => r.path === '*')
    
    if (route) {
      this.currentRoute = route
      document.getElementById('app').innerHTML = route.component()
    }
  }
}

export default Router
```

**src/views/Home.js:**

```javascript
export default function Home() {
  return `
    <div class="home">
      <h1>Welcome Home</h1>
      <p>This is a vanilla JavaScript SPA built with Vite.</p>
      <a href="/about" data-link>Go to About</a>
    </div>
  `
}
```

**src/main.js:**

```javascript
import './styles/main.css'
import Router from './router'
import Home from './views/Home'
import About from './views/About'
import NotFound from './views/NotFound'

const routes = [
  { path: '/', component: Home },
  { path: '/about', component: About },
  { path: '*', component: NotFound }
]

new Router(routes)
```

## Static Site Generator (SSG)

### Using VitePress

**Project Structure:**

```
my-docs/
├── .vitepress/
│   ├── config.js
│   └── theme/
├── docs/
│   ├── index.md
│   ├── guide/
│   └── api/
└── package.json
```

**package.json:**

```json
{
  "name": "my-docs",
  "scripts": {
    "docs:dev": "vitepress dev docs",
    "docs:build": "vitepress build docs",
    "docs:preview": "vitepress preview docs"
  },
  "devDependencies": {
    "vitepress": "^1.0.0"
  }
}
```

**.vitepress/config.js:**

```javascript
export default {
  title: 'My Documentation',
  description: 'Documentation site built with VitePress',
  themeConfig: {
    nav: [
      { text: 'Guide', link: '/guide/' },
      { text: 'API', link: '/api/' }
    ],
    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Introduction', link: '/guide/' },
          { text: 'Getting Started', link: '/guide/getting-started' }
        ]
      }
    ]
  }
}
```

## Electron + Vite

### Project Structure

```
my-electron-app/
├── package.json
├── electron/
│   └── main.js
├── src/
│   ├── main.js
│   └── App.vue
└── vite.config.js
```

**package.json:**

```json
{
  "name": "my-electron-app",
  "main": "electron/main.js",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "electron": "electron .",
    "electron:dev": "concurrently \"vite\" \"wait-on http://localhost:5173 && electron .\""
  },
  "dependencies": {
    "vue": "^3.3.0"
  },
  "devDependencies": {
    "@vitejs/plugin-vue": "^4.4.0",
    "concurrently": "^8.2.0",
    "electron": "^28.0.0",
    "vite": "^5.0.0",
    "wait-on": "^7.2.0"
  }
}
```

**electron/main.js:**

```javascript
import { app, BrowserWindow } from 'electron'
import path from 'path'

const isDev = process.env.NODE_ENV !== 'production'

function createWindow() {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false
    }
  })

  if (isDev) {
    win.loadURL('http://localhost:5173')
    win.webContents.openDevTools()
  } else {
    win.loadFile(path.join(__dirname, '../dist/index.html'))
  }
}

app.whenReady().then(createWindow)

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
  }
})
```

## Comparison Table

| Use Case | Vite Setup | Advantages | Considerations |
|---|---|---|---|
| **Static Site** | Standard Vite | Simple, fast, no server | No server-side rendering |
| **Node.js + Express** | Vite with proxy | Full-stack JS, API routes | Requires Node.js hosting |
| **Vanilla JS SPA** | Standard Vite | No framework overhead | Manual routing/state |
| **SSG (VitePress)** | VitePress | SEO-friendly, markdown | Limited interactivity |
| **Electron** | Vite + Electron | Desktop app, native APIs | Larger bundle size |
| **ASP.NET Core** | ViteKit.Msbuild | .NET ecosystem, typed APIs | Requires .NET runtime |

## When to Use ASP.NET Core vs Alternatives

### Choose ASP.NET Core when:
- Building enterprise applications
- Need strong typing with C#
- Require Entity Framework or other .NET libraries
- Team expertise in .NET
- Integration with existing .NET infrastructure

### Choose Node.js when:
- Full JavaScript stack preferred
- Rapid prototyping
- Microservices architecture
- Real-time features (Socket.io)

### Choose Static Site when:
- Content-focused sites
- Blogs or documentation
- Maximum performance
- Minimal hosting costs

## Additional Resources

- [Vite SSG Options](https://github.com/antfu/vite-ssg)
- [VitePress Documentation](https://vitepress.dev/)
- [Electron with Vite](https://github.com/electron-vite/electron-vite-vue)
- [Static Site Hosting Comparison](https://jamstack.org/generators/)
