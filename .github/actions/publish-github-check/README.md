# Publish GitHub Check Composite Action

## Overview
Creates a GitHub Check run with content sourced from Markdown/text files or inline strings. This general-purpose action replaces coverage-specific logic and can surface any relevant build artifacts (test summaries, lint output, release notes, etc.) as a rendered check on pull requests and commits.

## Inputs
| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `check-name` | Yes | – | Name of the GitHub check run. |
| `title` | No | `''` | Title displayed in the check output. Defaults to the check name. |
| `summary-file` | No | `''` | Path (relative to the workspace) to a file whose contents become the check summary. |
| `summary` | No | `Summary not available.` | Fallback summary when no file or content is available. |
| `text-file` | No | `''` | Optional file whose contents populate the check `text` field. |
| `text` | No | `''` | Fallback text content shown when `text-file` is not provided. |
| `conclusion` | No | `"success"` | Conclusion reported when `status` is `completed`. |
| `status` | No | `"completed"` | Check status (`queued`, `in_progress`, or `completed`). |
| `sha` | No | `''` | Commit SHA associated with the check. Defaults to the current workflow SHA. |
| `details-url` | No | `''` | Optional deep link shown on the check details page. |

## Behavior
- Missing `summary-file` values trigger a neutral check conclusion and reuse the provided summary fallback.
- File paths are resolved relative to `GITHUB_WORKSPACE` and warnings are emitted if the file cannot be read.
- When `text-file` or `text` are omitted, the check is created without a `text` field.

## Example Usage
```yaml
- name: Publish coverage summary
  uses: ./.github/actions/publish-github-check
  with:
    check-name: Coverage Summary
    title: Coverage Summary
    summary-file: artifacts/coverage-report/SummaryGithub.md
    summary: Coverage summary not available.
```

You can reuse the same action for other quality signals by pointing `summary-file` or `text-file` at different generated artifacts.
