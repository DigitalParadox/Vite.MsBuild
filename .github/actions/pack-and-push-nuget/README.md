# Pack and Push NuGet Package Composite Action

## Overview
Packages the specified project, embeds release notes metadata, and optionally publishes the resulting `.nupkg` files to NuGet.org and/or GitHub Packages. The action mirrors the repository’s packaging rules and surfaces the output directory so downstream steps can upload artifacts or publish releases.

## Inputs
| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `project-path` | No | `src/ViteKit.MsBuild/ViteKit.MsBuild.csproj` | Project file to pack. |
| `configuration` | No | `Release` | Build configuration passed to `dotnet pack`. |
| `version` | Yes | – | Version assigned to the generated package. |
| `output-directory` | No | `artifacts/packages` | Workspace-relative folder for packed artifacts. |
| `push-to-nuget` | No | `'false'` | When `'true'`, push packages to NuGet.org. Requires `nuget-api-key`. |
| `nuget-api-key` | No | `''` | API key used when pushing to NuGet.org. |
| `push-to-github` | No | `'false'` | When `'true'`, push packages to GitHub Packages. |
| `github-source` | No | `https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json` | GitHub Packages source URL. |
| `github-token` | No | *(workflow `GITHUB_TOKEN`)* | Token used when pushing to GitHub Packages. |
| `release-notes` | No | `''` | Inline Markdown release notes embedded as `PackageReleaseNotes`. |
| `release-notes-file` | No | `''` | Path to a Markdown file embedded as both `PackageReleaseNotes` and `PackageReleaseNotesFile`. |

## Outputs
| Name | Description |
| --- | --- |
| `package-directory` | Absolute path to the directory containing generated packages. |
| `package-version` | Effective package version (mirrors the `version` input). |

## Notes
- Inline `release-notes` take precedence; when omitted, `release-notes-file` content is used as the metadata value.
- Setting `release-notes-file` also wires the file into the package so consumers can access the Markdown within the `.nupkg` payload.
- Push steps are skipped automatically unless the respective `push-to-*` flag is `'true'`.

## Example Usage
```yaml
- name: Pack and push NuGet package
  uses: ./.github/actions/pack-and-push-nuget
  with:
    project-path: src/ViteKit.MsBuild/ViteKit.MsBuild.csproj
    version: ${{ steps.version.outputs.version }}
    release-notes: ${{ steps.notes.outputs.notes_content }}
    release-notes-file: ${{ steps.notes.outputs.notes_path }}
    push-to-nuget: 'true'
    nuget-api-key: ${{ secrets.NUGET_API_KEY }}
    push-to-github: 'true'
    github-token: ${{ secrets.GITHUB_TOKEN }}
```
