---
layout: default
title: Getting Started
nav_order: 2
has_children: true
permalink: /getting-started/
---

# Getting Started
{: .fs-9 }

Get up and running with Vite.MsBuild in your ASP.NET Core project quickly.
{: .fs-6 .fw-300 }

## Prerequisites

{: .note }
Before you begin, make sure you have:
- **.NET 6.0+** (supports .NET 7, 8, 9)
- **Node.js 18+** 
- **ASP.NET Core project** (Web, MVC, API, Blazor)

## Quick Start Guide

Follow these steps to get Vite.MsBuild working in your project:

1. **[Installation](installation)** - Install and configure the package
2. **[Quick Setup](quick-setup)** - Get your first build working
3. **[First Build](first-build)** - Understand the build process

## TL;DR - Quick Start

```bash
# 1. Install the package
dotnet add package Vite.MsBuild

# 2. Initialize your frontend
npm create vite@latest . -- --template react-ts

# 3. Build your project
dotnet build
```

{: .highlight }
> That's it! Your Vite assets are now built automatically during MSBuild.

## What's Next?

After completing the getting started guide:

- **[Guides](../guides/)** - Learn specific scenarios and configurations
- **[Examples](../examples/)** - Copy-paste ready examples for your framework
- **[Architecture](../architecture/)** - Understand how Vite.MsBuild works internally