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
- **No ordinary push to `main` publishes a package.** A release is intentionally initiated by pushing a new SemVer tag such as `v1.0.2`. The one-time bootstrap for `v1.0.1` has been removed.
- The separate [release workflow](../.github/workflows/release.yml) calls the **full existing CI as a reusable workflow** (Linux, Windows, Chromium, EventBus and package installation), checks that the tag points to a commit reachable from `main`, installs the exact tagged package and only then publishes it to NuGet and creates a GitHub Release. A manual `workflow_dispatch` is also allowed **only when the selected ref is a real valid release tag**; it cannot release directly from a branch.
- The release package version is derived from the tag using `-p:Version`. The default `<Version>` in the .NET project is for local/ordinary CI packaging; it does not restrict later release tags. NuGet versions are immutable: use a new version for any correction, never overwrite a published package.

## NuGet.org Trusted Publishing configuration

Releases `v1.0.1` and `v1.1.0` were successfully published using this account-scoped configuration. Preserve it for future versions:

1. Sign in to [nuget.org](https://www.nuget.org/) as the account that owns `docs-boilerplate`. Do not attempt to reuse the package ID for a different publisher.
2. In the account's **Trusted Publishing** settings, retain the GitHub Actions policy with **Repository Owner** `Spooffit` (the GitHub login, not a display name), **Repository** `docs-boilerplate`, **Workflow File** `release.yml`, and no Environment. Set **Glob Patterns and Packages** to `docs-boilerplate`. The initial package creation required the **Push new packages and package versions** scope; subsequent publishing may use the narrower scope **Push only new package versions**.
3. In the GitHub repository, go to **Settings → Secrets and variables → Actions → Variables**, and add `NUGET_USER` with the **nuget.org username of the account that created the Trusted Publishing policy** (not an email or an assumed GitHub login). For a policy owned by an organization, this may differ from the policy owner. No long-lived NuGet API key needs to be stored. If `NuGet/login@v1` reports HTTP 401 and `No matching trust policy owned by user ...`, check the exact policy creator username and policy owner/repository/workflow before retrying only the failed publishing job.
4. Ensure repository settings permit workflow `GITHUB_TOKEN` to create a Release via `contents: write`. This job alone receives the write permission; the verification job and the regular CI use `contents: read`.
5. With a private GitHub repository, a new policy can initially be active for seven days only; the successful `v1.0.1` publish activated this policy permanently, provided NuGet.org recorded the repository identity as expected.

The [official NuGet Trusted Publishing instructions](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) explain how the GitHub OIDC token becomes a short-lived NuGet API key. The policy must match the actual owner/repository/workflow.

## Subsequent releases

After a successful regular CI run on the desired `main` commit, create a **new, unused** version tag:

```powershell
git checkout main
git pull --ff-only
git tag -a v1.1.1 -m "Release v1.1.1"
git push origin v1.1.1
```

The `v1.0.0` tag remains as an unpublished attempt using the old package ID; **do not move or reuse it**. The first published release under `docs-boilerplate` was [v1.0.1](https://github.com/Spooffit/docs-boilerplate/releases/tag/v1.0.1). The current release is [v1.1.0](https://github.com/Spooffit/docs-boilerplate/releases/tag/v1.1.0), which adds bilingual English/Russian Markdown generation and the expanded technology profile catalog. The next patch version can use `v1.1.1` (or a later unused version). Supported tags follow `vMAJOR.MINOR.PATCH` with an optional prerelease suffix (such as `v0.3.0-rc.1`); prereleases are marked as such in GitHub Releases.

A tag starts **one** workflow that:

1. Executes the complete regular CI suite on the **tagged commit**, including Windows and Chromium; if any job fails, publication is blocked.
2. Validates tag syntax and confirms its commit is in `main`, then checks release-specific package policy.
3. Builds `docs-boilerplate.<version>.nupkg` from the verified tag and installs/tests that exact package.
4. Exchanges GitHub OIDC credentials for a one-hour NuGet.org publishing key and publishes the verified package.
5. Creates the GitHub Release with automatically generated notes and attaches the same `.nupkg` **only after successful NuGet publication**.

If any check or the NuGet authentication fails, the workflow does not create a GitHub Release. An already-published package cannot be republished under the same version; investigate any partial failure instead of retagging existing versions or forcing an overwrite.

## Using the published tool

Once NuGet indexes the renamed package, users can run:

```powershell
dotnet tool install --global docs-boilerplate
docs-boilerplate init
dotnet tool update --global docs-boilerplate
dotnet tool list --global
```

A globally installed tool is independent of the source checkout. The package includes the Markdown catalog and can be used to generate multiple independent documentation repositories.

The local-feed installation route remains available for offline testing: download the `docs-boilerplate-dotnet-tool` artifact from CI or the versioned GitHub Release, then use `--add-source <folder>`. For public releases, verify both a successful NuGet push and the corresponding GitHub Release before claiming availability.

## Boundaries

Publishing publicly makes **the packaged CLI and its bundled Markdown catalog** publicly downloadable, regardless of whether the GitHub source repository stays private. Review package contents before the first release and do not include secrets, internal project material or non-redistributable content. A GitHub Release in a private repository is visible only to authorized collaborators.

The release workflow only tests documented Linux release scenarios in addition to regular multi-platform CI. Browser/Windows checks remain in the ordinary workflow. A GitHub Release describes **this CLI tool**, never the release policy of applications created using the boilerplate.
