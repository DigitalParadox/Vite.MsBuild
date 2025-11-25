# Publish Test Results Composite Action

## Overview
Centralizes post-test reporting by uploading `.trx` files, emitting GitHub checks, and optionally handling coverage artifacts plus a coverage summary check. Use this action after running the `build-and-test` composite to keep workflows concise.

## Inputs
| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `test-results-path` | Yes | – | Workspace-relative glob or directory containing `.trx` files. |
| `test-check-name` | No | `Unit Test Results` | GitHub check name for the unit test summary (matrix info appended automatically when provided). |
| `retention-days` | No | `7` | Retention period for uploaded artifacts. |
| `fail-on-test-report-error` | No | `false` | When `true`, fail the workflow if `dorny/test-reporter@v1` encounters an error. |
| `upload-coverage` | No | `true` | Enables coverage artifact upload and summary publishing. |
| `coverage-artifact-path` | No | `''` | Directory (workspace-relative) containing coverage artifacts to upload. |
| `coverage-summary-file` | No | `''` | Path to the Markdown coverage summary (e.g., `artifacts/coverage-report/SummaryGithub.md`). |
| `coverage-check-name` | No | `Coverage Summary` | Name of the coverage GitHub check. |
| `matrix-os` | No | `''` | Optional string appended to artifact names/check titles, typically the matrix OS. |
| `skip-test-report` | No | `false` | When `true`, skip creating the test report check run (useful when aggregating results in a separate job). |

## Outputs
| Name | Description |
| --- | --- |
| `test-results-artifact` | Name of the uploaded test results artifact. |

## Example Usage
```yaml
- name: Publish test results
  uses: ./.github/actions/publish-test-results
  with:
    test-results-path: "${{ steps.build.outputs.test-results-relative }}/**/*.trx"
    matrix-os: ${{ matrix.os }}
    retention-days: 7
    fail-on-test-report-error: ${{ env.FAIL_ON_TEST_REPORT_ERROR }}
    upload-coverage: ${{ matrix.os == 'ubuntu-latest' }}
    coverage-artifact-path: ${{ steps.build.outputs.coverage-report-relative }}
    coverage-summary-file: ${{ steps.build.outputs.coverage-report-relative }}/SummaryGithub.md
```

Set `upload-coverage` to `false` (or omit the coverage inputs) when no coverage is produced on the current runner.
