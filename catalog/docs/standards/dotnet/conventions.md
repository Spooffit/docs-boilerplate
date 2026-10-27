---
title: ".NET conventions"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# .NET conventions

**Applies to:** projects using C# and .NET.

- Keep nullable annotations and compiler warnings consistent with the repository build configuration.
- Use `async` for naturally asynchronous I/O; never substitute `Task.Run` for asynchronous I/O.
- Pass cancellation tokens at meaningful I/O boundaries.
- Document public contracts and error behavior; do not expose persistence models as API contracts by default.
- Follow the project's chosen package-version strategy. Central Package Management is optional.
- Do not require CQRS, DDD, or a particular controller framework unless independently adopted.

**Review:** build with warnings inspected; run tests; verify layering and cancellation with code review.

## Formatting changed C# source

After creating or modifying C# files, run the repository's adopted C# formatter before marking the change finished. Where the project uses the .NET SDK formatter, a focused invocation is:

```sh
dotnet format --include path/to/changed-file.cs
```

Run it against the actual solution/project context and verify that it succeeds. Inspect the diff, since formatting should not unintentionally rewrite unrelated files or change intended code behavior. If the formatter is unavailable or cannot be run, report that limitation instead of claiming formatting succeeded. Honor an existing repository-specific `.editorconfig` and formatter configuration rather than imposing one universal whitespace style.

`dotnet format` is an SDK tool and its exact options or working directory may depend on the installed version and solution layout; validate the invocation in the target environment. Do not silently claim that CI/build success means formatting was checked.

Reference: [dotnet format CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format).
