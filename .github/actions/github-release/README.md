# Publish GitHub Release Composite Action

## Overview
Publishes a GitHub release (stable or prerelease) using GitReleaseManager. The action normalizes release notes from either a provided file or inline content, resolves asset globs, and then invokes `gittools/actions/gitrelease-manager` to create or update the release.

## Inputs
| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `tag` | Yes | – | Release tag name (e.g. `v1.2.3`). |
| `name` | No | `''` | Human-friendly release title. Falls back to the tag when omitted. |
| `draft` | No | `'false'` | Set to `'true'` to create the release as a draft. |
| `prerelease` | No | `'false'` | Set to `'true'` to mark the release as a prerelease. |
| `files` | No | `''` | Newline-separated list of asset glob patterns to upload. |
| `token` | No | *(workflow `GITHUB_TOKEN`)* | Token passed to GitReleaseManager. |
| `release_notes_path` | No | `''` | Path to Markdown release notes. Must exist if supplied. |
| `release_notes_content` | No | `''` | Inline Markdown release notes used when `release_notes_path` is absent. |

## Behavior
- When `release_notes_path` is provided, the file is validated and supplied directly to GitReleaseManager.
- When no path is provided, the composite writes `release_notes_content` to `artifacts/release-notes.md`. A default placeholder is emitted if neither source is supplied.
- Asset globs are resolved before publishing, and missing patterns are reported as warnings.

## Example Usage
```yaml
- name: Publish GitHub release
  uses: ./.github/actions/github-release
  with:
    tag: ${{ github.ref_name }}
    name: Release ${{ steps.version.outputs.version }}
    prerelease: 'false'
    files: |
      artifacts/packages/*.nupkg
      artifacts/packages/*.snupkg
    release_notes_path: ${{ steps.notes.outputs.notes_path }}
```

Pair this action with `./.github/actions/generate-release-notes` to streamline changelog generation across both stable and prerelease workflows.
