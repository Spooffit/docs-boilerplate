# Dapper persistence

**Applies to:** .NET projects that select Dapper.

- Always parameterize user-controlled values; validate non-parameterizable identifier choices against explicit allowlists.
- Manage connection and transaction lifecycle explicitly.
- Review SQL query plans and indexes for important queries.
- Integration-test queries, mappings and transaction behavior against the real database engine.
