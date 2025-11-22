---
layout: default
title: Enterprise Examples
parent: Examples
nav_order: 3
has_children: true
---

# Enterprise Examples
{: .fs-9 }

Production-ready configurations for large-scale applications.
{: .fs-6 .fw-300 }

{: .warning }
> These examples are designed for complex enterprise scenarios. Start with [Basic Examples](../basic/) if you're new to ViteKit.Msbuild.

## Enterprise Scenarios

### Multi-SPA Configuration
Comprehensive example showing how to manage multiple Single Page Applications within one ASP.NET Core project.

**[Multi-SPA Configuration](multi-spa-configuration)**
: Manage Admin Dashboard (Vue), Customer Portal (React), Partner Portal (Svelte), and Public Website with independent technology stacks and build configurations.

## Enterprise Features Demonstrated

### Technology Flexibility
Each business area can use its preferred technology stack:

- **Admin area** - Vue 3 + TypeScript
- **Customer area** - React 18 + TypeScript  
- **Partner area** - Svelte + TypeScript
- **Public area** - Vanilla TypeScript

### Independent Configuration
- Separate package.json per area
- Independent dependency management
- Different build modes per environment
- Isolated development workflows

### Production Optimizations
- Parallel build execution
- Conditional build logic
- Environment-specific configuration
- CI/CD pipeline optimization

{: .note }
> Enterprise examples require more setup time but provide patterns for managing complex, real-world applications.