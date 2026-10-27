# EF Core persistence

**Applies to:** .NET projects that select Entity Framework Core.

- Preserve domain invariants at application and persistence boundaries.
- Choose tracking intentionally; use projection and `AsNoTracking` for read-only queries when appropriate.
- Do not share DbContext instances between concurrent operations.
- Handle optimistic concurrency and transaction scope explicitly when correctness requires them.
- Test mappings, constraints, migrations and provider-specific SQL against the real target database.
- Keep database-specific migration procedures in project documentation.
