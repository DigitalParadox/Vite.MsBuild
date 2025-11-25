# Generate Release Notes Composite Action

## Overview
Aggregates conventional commits into a Markdown changelog and writes the result to disk for reuse across packaging and release steps. The action wraps `TriPSs/conventional-changelog-action@v5`, adds optional custom notes, and guarantees both the file path and the rendered Markdown are available as outputs.

## Inputs
| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `token` | No | *(workflow `GITHUB_TOKEN`)* | Token used by the changelog generator to query commits. |
| `extra_notes` | No | `''` | Additional Markdown appended after the generated changelog. |
| `output_directory` | No | `artifacts` | Workspace-relative directory where the release notes file is written. |
| `file_name` | No | `release-notes.md` | File name (no path) for the generated release notes. |

## Outputs
| Name | Description |
| --- | --- |
| `notes_path` | Absolute path to the generated Markdown release notes file. |
| `notes_content` | Markdown content rendered by the changelog step (including extras). |

## Example Usage
```yaml
- name: Generate release notes
  id: notes
  uses: ./.github/actions/generate-release-notes
  with:
    extra_notes: |
      ## Deployment
      - Rolled out to staging on {{ env.RELEASE_DATE }}
```

The `notes_path` output can be uploaded as an artifact or passed to packaging steps, while `notes_content` is ideal for embedding directly in NuGet metadata or release descriptions.
