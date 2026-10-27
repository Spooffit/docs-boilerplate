# .NET conventions

**Applies to:** projects using C# and .NET.

- Keep nullable annotations and compiler warnings consistent with the repository build configuration.
- Use `async` for naturally asynchronous I/O; never substitute `Task.Run` for asynchronous I/O.
- Pass cancellation tokens at meaningful I/O boundaries.
- Document public contracts and error behavior; do not expose persistence models as API contracts by default.
- Follow the project's chosen package-version strategy. Central Package Management is optional.
- Do not require CQRS, DDD, or a particular controller framework unless independently adopted.

**Review:** build with warnings inspected; run tests; verify layering and cancellation with code review.
