# Build and Test Composite Action

## Overview
Runs the full .NET build pipeline for this repository: restores the solution, builds the configured configuration, executes unit tests with code coverage enabled, and generates HTML/Markdown coverage summaries. All artifacts are written under the workspace `artifacts/` directory so they can be uploaded or consumed by later steps.

## Inputs
| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `solution` | No | `ViteKit.MsBuild.sln` | Path to the solution file that should be restored and built. |
| `test-project` | No | `tests/ViteKit.MsBuild.PureUnitTests/ViteKit.MsBuild.PureUnitTests.csproj` | Specific test project to execute for coverage reporting. |
| `configuration` | No | `Release` | Build configuration supplied to both `dotnet build` and `dotnet test`. |

## Outputs
| Name | Description |
| --- | --- |
| `test-results` | Absolute path to the directory containing generated `.trx` test result files. |
| `test-results-relative` | Workspace-relative path to the `.trx` results directory (useful for globbing in GitHub Actions). |
| `coverage-directory` | Absolute path to Cobertura coverage output produced by Coverlet. |
| `coverage-directory-relative` | Workspace-relative path to the Cobertura coverage directory. |
| `coverage-report` | Absolute path to the directory containing the ReportGenerator HTML and `SummaryGithub.md` coverage summary. |
| `coverage-report-relative` | Workspace-relative path to the coverage summary directory. |

## Example Usage
```yaml
- name: Build and Test
  uses: ./.github/actions/build-and-test
  with:
    solution: ViteKit.MsBuild.sln
    test-project: tests/ViteKit.MsBuild.PureUnitTests/ViteKit.MsBuild.PureUnitTests.csproj
    configuration: Release
```

The generated outputs can be fed into test reporters, coverage uploads, or the generic GitHub check action included in this repository.
