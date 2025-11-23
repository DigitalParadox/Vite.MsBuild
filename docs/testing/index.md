---
layout: default
title: Testing
nav_order: 6
has_children: true
permalink: /testing/
---

# Testing
{: .fs-9 }

Comprehensive information about the test suite and testing strategies.
{: .fs-6 .fw-300 }

## Test Coverage Status

{: .highlight }
> **427 tests passing** with 100% success rate across all scenarios.

## Testing Strategy

### Unit Tests
Pure unit tests for individual components.

- Task testing with mock MSBuild engines
- Command factory testing
- Package manager detection
- File system operations

### Integration Tests  
End-to-end testing with real projects.

- Complete build workflows
- Multiple package managers
- Framework combinations
- CI/CD scenarios

### Test Organization

The test suite is organized into logical groups:

- **Core Tasks** - ValidateViteProjectTask, DetectPackageManagerTask, CollectViteInputFilesTask
- **Build Integration** - MSBuild target execution, incremental builds
- **Framework Support** - Vue, React, Svelte file detection
- **Package Managers** - npm, pnpm, yarn, bun detection and commands  
- **Error Scenarios** - Validation, conflict resolution, helpful messages

## Test Documentation

This section contains:

- **Test case documentation** - Comprehensive scenarios and acceptance criteria
- **Coverage analysis** - What's tested and what gaps exist
- **Test strategy** - How different components are tested
- **Integration testing** - Real-world scenario validation

{: .note }
> The testing documentation helps contributors understand the test philosophy and add new tests effectively.

## Quality Metrics

| Metric | Status |
|:-------|:-------|
| Unit Tests | 427 passing |
| Success Rate | 100% |
| Core Task Coverage | Complete |
| Package Manager Coverage | All 4 supported |
| Framework Coverage | Vue, React, Svelte |
| Error Scenario Coverage | Comprehensive |

## For Contributors

When adding new features:

1. **Add unit tests** for new tasks or components
2. **Add integration tests** for new workflows  
3. **Update test documentation** for new scenarios
4. **Ensure 100% pass rate** before submitting PRs

See the test files in the repository for examples and patterns.