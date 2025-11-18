---
layout: default
title: Home
nav_order: 1
description: "Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects"
permalink: /
---

# Vite.MsBuild
{: .fs-9 }

Framework-agnostic MSBuild integration for Vite in ASP.NET Core projects
{: .fs-6 .fw-300 }

[Get started now](getting-started/){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/DigitalParadox/Vite.MsBuild){: .btn .fs-5 .mb-4 .mb-md-0 }

---

{: .highlight }
> **Seamlessly integrate Vite with ASP.NET Core** - Build Vue, React, Svelte, or any Vite-supported framework directly from `dotnet build`.

[![NuGet Package](https://img.shields.io/nuget/v/Vite.MsBuild)](https://www.nuget.org/packages/Vite.MsBuild)
[![Build Status](https://github.com/DigitalParadox/Vite.MsBuild/workflows/CI/badge.svg)](https://github.com/DigitalParadox/Vite.MsBuild/actions)
[![Tests](https://img.shields.io/badge/tests-192%20passing-brightgreen)](https://github.com/DigitalParadox/Vite.MsBuild/actions)

## Quick start

### Install the package

```bash
dotnet add package Vite.MsBuild
```

### Create your Vite config

```bash
# Vue + TypeScript
npm create vite@latest . -- --template vue-ts

# React + TypeScript  
npm create vite@latest . -- --template react-ts

# Or any other Vite template
```

### Build your project

```bash
dotnet build
```

{: .new }
That's it! Your Vite assets are now built automatically during MSBuild.

---

## Key Features

Zero Configuration
: Auto-detects your project structure and package manager

Incremental Builds
: Only rebuilds when source files actually change  

Parallel Build Safe
: Prevents npm install conflicts in CI/CD

Framework Agnostic
: Vue, React, Svelte, Solid, Preact, vanilla JS/TS

Package Manager Smart
: Auto-detects npm, pnpm, yarn, or bun

Enterprise Ready
: Robust error handling and comprehensive logging

MSBuild Native
: Uses proper MSBuild targets, not hacky scripts

---

## Project Status

| Component | Status |
|:----------|:-------|
| Current Version | 2.0.0 |
| Test Coverage | 192 tests passing (100% success rate) |
| Supported .NET | 6, 7, 8, 9 |
| Node.js Required | 18+ |
| Vite Supported | 4.0+ and 5.0+ |