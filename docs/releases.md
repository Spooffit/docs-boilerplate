---
title: "Releasing the Docs Boilerplate CLI"
description: "SemVer tags, verified GitHub Releases and keyless NuGet.org publication"
kind: guide
status: active
---

# Releasing the Docs Boilerplate CLI

**This is distribution of the reusable documentation generator, not a release process imposed on generated projects.** The project-specific Markdown catalog must not inherit package publishing, release version policies or delivery milestones.

## Branch policy

- `main` remains the integration branch and can be developed on directly. Feature branches and pull requests are optional, recommended for larger changes.
- Every push to `main` runs the regular [generator CI](../.github/workflows/docs-generator.yml), including build, smoke tests, browser checks and a local .NET tool installation.
- **No ordinary push to `main` publishes a package.** A release is intentionally initiated by pushing a SemVer tag such as `v1.0.0`. A one-time, commit-message-guarded automation is available only to bootstrap the very first release when no tag-write integration is connected.
- The separate [release workflow](../.github/workflows/release.yml) calls the **full existing CI as a reusable workflow** (Linux, Windows, Chromium, EventBus and package installation), checks that the tag points to a commit reachable from `main`, installs the exact tagged package and only then publishes it to NuGet and creates a GitHub Release. A manual `workflow_dispatch` is also allowed **only when the selected ref is a real valid release tag**; it cannot release directly from a branch.
- The release package version is derived from the tag using `-p:Version`. The default `<Version>` in the .NET project is for local/ordinary CI packaging; it does not restrict later release tags. NuGet versions are immutable: use a new version for any correction, never overwrite a published package.

## One-time NuGet.org configuration

The first public release **cannot be published** until the publisher completes these account-scoped settings:

1. Sign in to [nuget.org](https://www.nuget.org/) as the account that will own `DocsBoilerplate.Cli`. Confirm that the package ID is available or already belongs to that account. If another owner has taken it, choose and update the package ID everywhere **before releasing**.
2. Go to the account's **Trusted Publishing** settings and create a GitHub Actions policy. Set repository owner `Spooffit`, repository `docs-boilerplate`, workflow filename **`release.yml`** (just the filename), and leave the optional environment empty. Scope it to publication of `DocsBoilerplate.Cli`, including new packages and new versions.
3. In the GitHub repository, go to **Settings → Secrets and variables → Actions → Variables**, and add `NUGET_USER` with the **nuget.org account username** (not email). No long-lived NuGet API key needs to be stored.
4. Ensure repository settings permit workflow `GITHUB_TOKEN` to create a Release via `contents: write`. This job alone receives the write permission; the verification job and the regular CI use `contents: read`.
5. For a private GitHub repository, an unactivated Trusted Publishing policy may only remain temporarily active. Complete the initial publication promptly after creating it, or reactivate it in nuget.org before the first tag.

The [official NuGet Trusted Publishing instructions](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) explain how the GitHub OIDC token becomes a short-lived NuGet API key. The policy must match the actual owner/repository/workflow.

## First release and subsequent releases

**Do not create the tag until NuGet.org Trusted Publishing is ready.** After a successful regular CI run on the desired `main` commit:

```powershell
git checkout main
git pull --ff-only
git tag -a v0.2.0 -m "Release v0.2.0"
git push origin v0.2.0
```

Use `v0.2.1` for a later patch, `v0.3.0` for new features and `v1.0.0` when you deliberately decide the first stable major release is ready. Supported tags follow `vMAJOR.MINOR.PATCH` with an optional prerelease suffix (such as `v0.3.0-rc.1`); prereleases are marked as such in GitHub Releases.

A tag starts **one** workflow that:

1. Executes the complete regular CI suite on the **tagged commit**, including Windows and Chromium; if any job fails, publication is blocked.
2. Validates tag syntax and confirms its commit is in `main`, then checks release-specific package policy.
3. Builds `DocsBoilerplate.Cli.<version>.nupkg` from the verified tag and installs/tests that exact package.
4. Exchanges GitHub OIDC credentials for a one-hour NuGet.org publishing key and publishes the verified package.
5. Creates the GitHub Release with automatically generated notes and attaches the same `.nupkg` **only after successful NuGet publication**.

If any check or the NuGet authentication fails, the workflow does not create a GitHub Release. An already-published package cannot be republished under the same version; investigate any partial failure instead of retagging existing versions or forcing an overwrite.

## Using the published tool

Once NuGet indexes the first version, users can run:

```powershell
dotnet tool install --global DocsBoilerplate.Cli
docs-boilerplate init
dotnet tool update --global DocsBoilerplate.Cli
dotnet tool list --global
```

A globally installed tool is independent of the source checkout. The package includes the Markdown catalog and can be used to generate multiple independent documentation repositories.

Until the first successful publish, the previous installation route remains available: download the `docs-boilerplate-dotnet-tool` artifact from the normal CI workflow, extract its `.nupkg` into a folder and install through `--add-source <folder>`. A successful regular CI or successful local `dotnet pack` is **not** evidence that the package is available on nuget.org.

## Boundaries

Publishing publicly makes **the packaged CLI and its bundled Markdown catalog** publicly downloadable, regardless of whether the GitHub source repository stays private. Review package contents before the first release and do not include secrets, internal project material or non-redistributable content. A GitHub Release in a private repository is visible only to authorized collaborators.

The release workflow only tests documented Linux release scenarios in addition to regular multi-platform CI. Browser/Windows checks remain in the ordinary workflow. A GitHub Release describes **this CLI tool**, never the release policy of applications created using the boilerplate.
