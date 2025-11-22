---
layout: default
title: Examples
parent: Examples  
nav_order: 1
---

# ViteKit.Msbuild Examples
{: .fs-9 }

Comprehensive examples showing different usage scenarios for ViteKit.Msbuild in ASP.NET Core projects.
{: .fs-6 .fw-300 }

{: .note }
This page provides an overview of all available examples. Choose the category that best matches your needs.

## 📁 Example Categories

### 🚀 [Basic Examples](basic/)
Perfect for getting started quickly.

**[Basic Setup](basic/)** - Zero-configuration example with minimal setup
: Single Vite config, auto-detection of package manager, simple TypeScript application

### 🎨 [Framework Examples](frameworks/)
Complete examples for popular frontend frameworks.

**[Vue + TypeScript](frameworks/vue-typescript)** - Vue 3 with full TypeScript support
: Single File Components (SFC), Vue Router integration, development vs production builds

**[React + TypeScript](frameworks/react-typescript)** - React 18 with modern TypeScript
: JSX/TSX support, React Router integration, ESLint configuration

**[Svelte + TypeScript](frameworks/svelte-typescript)** - Svelte with TypeScript
: Svelte preprocessing, component-scoped styles, Svelte routing

### 🏢 [Enterprise Examples](enterprise/)
Production-ready configurations for large applications.

**[Multi-SPA Configuration](enterprise/multi-spa-configuration)** - Multiple Single Page Applications
: Admin Dashboard (Vue), Customer Portal (React), Partner Portal (Svelte), Public Website (Vanilla), independent technology stacks per area

### 🏗️ [Monorepo Examples](monorepo/)
Advanced configurations for monorepo setups.

**[Shared Packages](monorepo/shared-packages)** - Monorepo with shared libraries
: Shared UI component library, shared utility packages, multiple consuming applications, workspace dependencies and linking

- **[React + TypeScript](frameworks/react-typescript.md)** - React 18 with modern TypeScript
  - JSX/TSX support
  - React Router integration
  - ESLint configuration

- **[Svelte + TypeScript](frameworks/svelte-typescript.md)** - Svelte with TypeScript
  - Svelte preprocessing
  - Component-scoped styles
  - Svelte routing

### 🏢 **Enterprise Examples**  
Production-ready configurations for large applications.

- **[Multi-SPA Configuration](enterprise/multi-spa-configuration.md)** - Multiple Single Page Applications
  - Admin Dashboard (Vue)
  - Customer Portal (React)  
  - Partner Portal (Svelte)
  - Public Website (Vanilla)
  - Independent technology stacks per area

### 🏗️ **Monorepo Examples**
Advanced configurations for monorepo setups.

- **[Shared Packages](monorepo/shared-packages.md)** - Monorepo with shared libraries
  - Shared UI component library
  - Shared utility packages
  - Multiple consuming applications
  - Workspace dependencies and linking

## 🎯 **Choose Your Example**

### **New to ViteKit.Msbuild?**
Start with **[Basic Setup](basic/README.md)** to understand the fundamentals.

### **Using a Specific Framework?**
Jump to the relevant framework example:
- Vue developers → **[Vue + TypeScript](frameworks/vue-typescript.md)**
- React developers → **[React + TypeScript](frameworks/react-typescript.md)**  
- Svelte developers → **[Svelte + TypeScript](frameworks/svelte-typescript.md)**

### **Building Enterprise Applications?**
Check out **[Multi-SPA Configuration](enterprise/multi-spa-configuration.md)** for managing multiple frontend applications.

### **Working in a Monorepo?**
See **[Shared Packages](monorepo/shared-packages.md)** for advanced dependency management.

## 📋 **What Each Example Includes**

Every example provides:

✅ **Complete project structure** - See exactly how files are organized  
✅ **Full configuration files** - Copy-paste ready configurations  
✅ **ASP.NET Core integration** - How to integrate with controllers and views  
✅ **Build commands** - Step-by-step build instructions  
✅ **Expected output** - What you should see when it works  

## 🛠️ **Common Patterns**

### **Package Manager Detection**
All examples work with:
- npm (package-lock.json)
- pnpm (pnpm-lock.yaml)  
- Yarn (yarn.lock)
- Bun (bun.lockb)

### **Build Modes**
Examples show different build modes:
- **Development** - Fast builds, HMR support
- **Production** - Optimized builds, minification
- **Staging** - Production-like with debugging enabled

### **Output Directory Patterns**
Common output directory configurations:
- `wwwroot/dist` - Standard ASP.NET Core static files
- `wwwroot/assets` - Framework-specific assets
- `wwwroot/{area}` - Area-specific outputs for multi-SPA

## 🔧 **Customization Guidelines**

All examples can be customized by:

1. **Changing output directories** - Update `ViteOutputDir` property
2. **Adding build steps** - Use `ViteBuildTiming` to control when builds run
3. **Environment-specific configs** - Use MSBuild conditions for different environments
4. **Package manager preference** - Set `PackageManager` property to force specific tool

## 📚 **Additional Resources**

- **[README.md](../README.md)** - Main project documentation
- **[Migration Guide](../README.md#-migration-guide)** - Upgrading from 1.x to 2.x
- **[Troubleshooting](../README.md#-troubleshooting)** - Common issues and solutions

---

*Choose an example above to get started, or refer to the main README for comprehensive documentation.*