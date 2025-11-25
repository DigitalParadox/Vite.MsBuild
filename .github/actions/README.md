# GitHub Actions Toolkit

This directory hosts reusable composite actions that power the ViteKit.Msbuild workflows. Each action focuses on a specific step in the package lifecycle—from building and testing to publishing releases. Use these actions in repository workflows or copy them into downstream projects that need the same automation patterns.

## Available Actions

| Action | Description |
| --- | --- |
| [`build-and-test`](build-and-test/README.md) | Restores the solution, builds it, runs unit tests with coverage, and emits coverage summaries. |
| [`generate-release-notes`](generate-release-notes/README.md) | Produces conventional-commit release notes and persists them to a Markdown file. |
| [`github-release`](github-release/README.md) | Publishes GitHub releases via GitReleaseManager, wiring in generated notes and asset globs. |
| [`pack-and-push-nuget`](pack-and-push-nuget/README.md) | Packs the NuGet package, embeds release notes metadata, and optionally pushes to NuGet.org and GitHub Packages. |
| [`publish-github-check`](publish-github-check/README.md) | Creates generic GitHub checks from file or inline content (used for coverage summaries and more). |
| [`publish-test-results`](publish-test-results/README.md) | Uploads unit test results, publishes the test check, and optionally collects coverage artifacts and summaries. |

## Usage Tips

- All actions assume they run within the repository workspace and create artifacts under the `artifacts/` directory by default.
- Paths exposed as outputs often include both absolute and workspace-relative variants; the latter simplify globbing on Windows runners.
- Most actions accept optional flags to toggle behavior (for example, coverage uploads or failing on reporter errors). See each action’s README for details.
- When authoring new workflows, prefer these first-party actions over third-party alternatives. If a scenario is not covered, consult with the team before introducing custom scripting or external actions.

## Versioning

Changes to these actions follow the repository’s standard contribution process. Update individual READMEs and workflows when inputs or outputs change to keep documentation synchronized.
