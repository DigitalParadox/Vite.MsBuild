---
layout: default
title: Architecture
nav_order: 5
has_children: true
permalink: /architecture/
---

# Architecture
{: .fs-9 }

Technical documentation about how Vite.MsBuild works internally.
{: .fs-6 .fw-300 }

{: .warning }
> This section is for advanced users who want to understand the internal architecture or contribute to the project.

## Core Architecture

### MSBuild Integration
How Vite.MsBuild integrates with the MSBuild system.

- Target execution order
- Property resolution
- Incremental build system
- Task lifecycle

### Task System
The C# task architecture that powers builds.

- Task inheritance hierarchy
- Error handling patterns
- Logging and diagnostics
- Performance optimization

### Command Factory
How Vite commands are constructed and executed.

- Command building patterns
- Package manager abstraction
- Environment variable handling
- Process execution

## Technical Documents

The architecture section contains detailed technical analysis documents that were created during development:

- MSBuild compatibility analysis
- NuGet framework strategy  
- Vite command factory architecture
- Incremental build architecture
- Multi-config convention strategy
- And more...

{: .note }
> These documents provide deep technical insights for developers working on the codebase or implementing similar solutions.

## For Contributors

If you're contributing to Vite.MsBuild, the architecture documentation will help you understand:

- Design decisions and rationale
- Implementation patterns
- Extension points
- Testing strategies

See our [Contributing Guide](https://github.com/DigitalParadox/Vite.MsBuild/blob/main/CONTRIBUTING.md) for more information.