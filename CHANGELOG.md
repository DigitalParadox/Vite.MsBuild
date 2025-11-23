# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Cross-platform CI/CD pipeline with Ubuntu, Windows, and macOS support
- GitVersion integration for automatic semantic versioning
- Code coverage collection and reporting with Coverlet
- NuGet package artifacts in CI workflow
- Local tool manifest with GitVersion and ReportGenerator
- Multi-framework testing (net8.0, net9.0, net10.0)

### Changed
- Consolidated CI workflows into single multi-platform workflow
- Improved test execution with proper coverage collection
- Updated versioning strategy: dev → alpha, stable → beta, master → production

### Removed
- Removed legacy v1.0.0 tag
- Cleaned up obsolete architecture detection logic

## [0.1.0] - TBD

### Added
- Initial release of ViteKit.Msbuild
- Framework-agnostic MSBuild integration for Vite
- Support for Vue, React, Svelte, Solid, Preact, and vanilla JS/TS
- Multi-SPA configuration support
- Automatic package manager detection (npm, yarn, pnpm, bun)
- Incremental build support with intelligent caching
- Comprehensive test suite (427+ tests)
- C#-first orchestration architecture with MSBuild tasks

[Unreleased]: https://github.com/DigitalParadox/ViteKit/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/DigitalParadox/ViteKit/releases/tag/v0.1.0
