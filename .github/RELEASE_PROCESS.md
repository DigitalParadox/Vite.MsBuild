# Release Process

This document describes the end–to–end release workflow for the **ViteKit.Msbuild** NuGet package as implemented in `.github/workflows/release.yml`.

## Overview

We distinguish between two release types:

| Type | Version Pattern | Trigger | GitHub Release | NuGet Publish | Approval |
|------|-----------------|---------|----------------|---------------|----------|
| Prerelease | `X.Y.Z-<suffix>` (contains `-`) | Push to `dev` / `stable` (or tag with suffix) | Published immediately (marked prerelease) | Automatic | None |
| Stable | `vX.Y.Z` tag without suffix | Annotated tag `v*` push | Draft first, finalized after publish | After approval | Production environment |

## Workflow Jobs

1. **`test-matrix`** (ubuntu, windows, macOS)
   - Restore, build (Release), run multi-target unit tests.
   - Publish TRX results via `dorny/test-reporter`.
   - No version stamping here (speed & isolation).

2. **`pack`** (ubuntu)
   - Runs GitVersion to compute semantic version components.
   - Version–stamped build with: `Version`, `PackageVersion`, `AssemblyVersion`, `FileVersion`, `InformationalVersion`.
   - Packs NuGet (`.nupkg`) once (deterministic artifact).
   - Generates **draft GitHub Release** for stable tag (not prerelease).
   - Outputs version data for downstream jobs.

3. **`publish-prerelease`** (ubuntu)
   - Conditional: semVer contains `-`.
   - Downloads packaged artifact.
   - Pushes to GitHub Packages and NuGet.org.
   - Publishes GitHub Release (prerelease=true).

4. **`publish-release`** (ubuntu, environment gated)
   - Conditional: tag starts with `v` AND semVer has no suffix.
   - Environment: `production` (manual approval required).
   - Downloads artifact, pushes to NuGet.org, finalizes previously drafted release.

## Tagging & Versioning

- Tags follow `vX.Y.Z` for stable releases.
- Prerelease versions follow GitVersion configuration (e.g., `1.2.3-beta.1`).
- GitVersion drives all stamped metadata; do **not** manually edit `<Version>` in the project for release builds.

## Required Secrets

| Secret | Purpose |
|--------|---------|
| `NUGET_API_KEY` | Push to NuGet.org |
| `GITHUB_TOKEN` | Implicit token (provided), used for GitHub Release + GitHub Packages |

## Release Notes & Changelog

During the `pack` job we build a categorized changelog using **Conventional Commit** parsing. Commits are grouped under sections only if present:

| Conventional Type | Section Heading |
|-------------------|-----------------|
| `feat`            | Features |
| `fix`             | Bug Fixes |
| `perf`            | Performance |
| `refactor`        | Refactoring |
| `docs`            | Documentation |
| `test`            | Tests |
| `build`           | Build System |
| `ci`              | CI |
| `chore`           | Chores |
| `style`           | Style |
| `revert`          | Reverts |
| (anything else)   | Other |

Format rules:
- Scope (`type(scope):`) and breaking change bang (`!`) are stripped from display text.
- Each entry lists short hash followed by commit description.
- Empty categories are omitted for brevity.
- Header shows the version (tag) for stable releases or generic "Changes" otherwise.

Example fragment:
```markdown
### v1.4.0
#### Features
* `a1b2c3d` Add multi-SPA dependency resolver
#### Bug Fixes
* `d4e5f6g` Correct null reference in ViteConfig parsing
#### Other
* `9f8e7d6` Update README formatting
```

## Environment Gating (Stable Only)

Create a **production** environment in repository settings:
1. Settings → Environments → New environment `production`.
2. Add required reviewers (e.g., maintainers team).
3. Optionally enable deployment history & environment protection rules.

Stable release job (`publish-release`) will pause awaiting approval; once approved it pushes the package and publishes the release.

## Pre-Release Flow (dev / stable branches)

1. Commit & merge to `dev` or `stable`.
2. Workflow runs tests across OSes.
3. `pack` job stamps version (with suffix) & builds artifact.
4. `publish-prerelease` pushes directly (no gating) and publishes prerelease release page.

## Stable Flow (tagged)

1. Create annotated tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z"; git push origin vX.Y.Z`.
2. Workflow runs tests across OSes.
3. `pack` job creates draft release + artifact.
4. Reviewer approves `publish-release` environment.
5. Package is pushed to NuGet.org; draft release finalized.

## Why Draft + Single Pack

- Prevents orphan release pages if publish fails.
- Ensures changelog and metadata are editable before final user visibility.
- Deterministic build reduces supply-chain risk (one hash).

## Rollbacks

- If stable publish fails before finalization: delete draft release, fix issue, retag or reuse existing tag.
- If prerelease publish fails: re-run workflow (the version suffix typically includes an increment; adjust as needed).

## Future Enhancements (Optional)

- SBOM generation (CycloneDX or SPDX) attached to release.
- Vulnerability & license scanning before draft creation.
- Automated Conventional Commits parsing for categorized changelog.
- Package provenance / signing.
- Coverage threshold gating in CI (not in release).

## Manual Validation Checklist

Before approving stable release:
- [ ] Tests green on all OSes
- [ ] Changelog fragment accurate (edit draft if needed)
- [ ] Artifact size & contents as expected (`nuget.exe list -source nupkg` locally)
- [ ] No unexpected dependencies added

## Quick Commands

Tag creation:
```bash
git checkout dev
# merge / fast-forward changes into master prior to tagging
git checkout master
git merge dev
git tag -a v1.2.3 -m "Release v1.2.3"
git push origin v1.2.3
```

Local packaging test:
```powershell
dotnet pack src/ViteKit.MsBuild/ViteKit.MsBuild.csproj -c Release /p:PackageVersion=1.2.3-test -o .\nupkg
```

## Troubleshooting

| Symptom | Cause | Resolution |
|---------|-------|------------|
| Draft release missing | Tag not matched (`v*`) | Ensure tag name starts with `v` and pushed |
| Pre-release not marked prerelease | Version lacks suffix | Verify GitVersion config for branch patterns |
| Publish job skipped | Conditions not satisfied | Check job `if:` expression in workflow run |
| NuGet push fails | Invalid API key / network | Update `NUGET_API_KEY`, retry run |

---
**Maintainers:** Keep this document synchronized with any workflow changes.
