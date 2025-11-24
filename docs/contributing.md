---
layout: default
title: Contributing
nav_order: 7
---

# Contributing to ViteKit
{: .no_toc }

Help improve ViteKit.Msbuild for everyone!
{: .fs-6 .fw-300 }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Getting Started

### Prerequisites

- **.NET 8.0 SDK or later**
- **Node.js 18+** (for integration tests)
- **Git**
- **Visual Studio 2022** or **VS Code** with C# extension

---

## Development Setup

### 1. Fork and Clone

```bash
git clone https://github.com/YOUR_USERNAME/ViteKit.git
cd ViteKit
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build

```bash
dotnet build
```

### 4. Run Tests

```bash
dotnet test
```

---

## Project Structure

```
ViteKit/
├── src/
│   └── ViteKit.MsBuild/           # C# tasks and orchestration
│       ├── OrchestrateBuildTask.cs
│       ├── ViteConfigurationResolver.cs
│       └── ...
├── build/
│   ├── ViteKit.MsBuild.props      # Property defaults
│   └── ViteKit.MsBuild.targets    # Build targets
├── tests/
│   └── ViteKit.MsBuild.PureUnitTests/  # Unit tests (427+ tests)
├── docs/                          # Documentation (Jekyll)
└── .github/
    └── workflows/                 # CI/CD pipelines
```

---

## Making Changes

### 1. Create a Branch

Use conventional branch names:

```bash
git checkout -b feat/add-feature
git checkout -b fix/bug-description
git checkout -b docs/update-guide
```

### 2. Make Your Changes

Follow these guidelines:

**Code Style**:
- Use C# naming conventions (PascalCase for public members)
- Add XML documentation comments to public APIs
- Keep methods focused and testable

**Commit Messages**:
Use [Conventional Commits](https://www.conventionalcommits.org/):

```bash
git commit -m "feat: add support for custom vite commands"
git commit -m "fix: resolve race condition in parallel builds"
git commit -m "docs: update configuration examples"
```

Types:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `refactor:` - Code refactoring
- `test:` - Adding tests
- `chore:` - Maintenance tasks

---

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~OrchestrateBuildTask"
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
dotnet reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:CoverageReport
```

### Writing Tests

Tests are in `tests/ViteKit.MsBuild.PureUnitTests/`:

```csharp
[Fact]
public void MyTest_Scenario_ExpectedResult()
{
    // Arrange
    var task = new MyTask
    {
        PropertyName = "value"
    };

    // Act
    var result = task.Execute();

    // Assert
    Assert.True(result);
}
```

**Test Conventions**:
- Use descriptive names: `Method_Scenario_ExpectedResult`
- One assertion per test (when possible)
- Use `[Theory]` for parameterized tests
- Mock external dependencies

---

## Documentation

Documentation is in `docs/` using Jekyll (Just the Docs theme).

### Preview Locally

```bash
cd docs
bundle install
bundle exec jekyll serve
```

Visit `http://localhost:4000/ViteKit`

### Writing Docs

- Use clear headings
- Include code examples
- Add table of contents for long pages
- Use callouts for important notes:

```markdown
{: .note }
This is an important note.

{: .warning }
This is a warning.
```

---

## Pull Request Process

### 1. Push Your Branch

```bash
git push origin feat/your-feature
```

### 2. Open Pull Request

Go to [https://github.com/DigitalParadox/ViteKit/pulls](https://github.com/DigitalParadox/ViteKit/pulls)

**PR Title**: Use conventional format
```
feat: add custom vite command support
```

**PR Description**: Include:
- What changed
- Why it changed
- Any breaking changes
- How to test

### 3. CI Checks

Your PR must pass:
- ✅ Build on Ubuntu, Windows, macOS
- ✅ All tests passing
- ✅ Code coverage maintained

### 4. Code Review

Maintainers will review and may request changes. Be responsive and collaborative!

---

## Release Process

(For maintainers)

### Versioning

ViteKit uses [Semantic Versioning](https://semver.org/) with GitVersion:

- **dev** branch → `0.x.0-alpha.N` (auto-published to GitHub Packages)
- **stable** branch → `0.x.0-beta.N` (auto-published to GitHub Packages)
- **master** branch → `0.x.0` (auto-published to NuGet.org)
- **Tags** (`v*`) → Stable release

### Publishing

Automated via GitHub Actions:
1. Merge PR to `dev` → Alpha release
2. Promote to `stable` → Beta release
3. Promote to `master` or tag → Stable release on NuGet.org

---

## Code of Conduct

### Our Standards

- **Be respectful** and inclusive
- **Be constructive** in feedback
- **Focus on** what's best for the community
- **Show empathy** towards others

### Unacceptable Behavior

- Harassment or discrimination
- Trolling or insulting comments
- Public or private harassment
- Publishing others' private information

### Enforcement

Report issues to project maintainers. Violations may result in removal from the project.

---

## Getting Help

- **Questions**: [GitHub Discussions](https://github.com/DigitalParadox/ViteKit/discussions)
- **Bugs**: [GitHub Issues](https://github.com/DigitalParadox/ViteKit/issues)
- **Security**: Email maintainers privately

---

## Recognition

Contributors are recognized in:
- CHANGELOG.md
- Release notes
- Project README

Thank you for contributing! 🎉
