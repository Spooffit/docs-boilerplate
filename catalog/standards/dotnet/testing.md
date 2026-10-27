# .NET test strategy

**Applies to:** projects using .NET. Framework and runners are project choices.

- Classify tests by actual system under test (SUT), not by project filename.
- **Unit:** isolated behavior with in-process dependencies; mocks are not proof that infrastructure works.
- **Integration:** test real boundaries such as an EF Core provider against the intended database or a message transport.
- **Architecture:** enforce explicitly adopted dependency rules; do not assert boundaries never agreed upon.
- **Functional/component:** exercise the public interface of one deployable application or component.
- **End-to-end:** verify user-observable behavior through required deployable units.
- Keep test data deterministic, manage clocks/randomness, make cleanup reliable, and isolate mutable shared infrastructure.
- WebApplicationFactory, Testcontainers, Aspire Testing, TUnit and bUnit are optional tools for appropriate scenarios, not mandatory dependencies.
- Avoid arbitrary mandatory code-coverage thresholds; inspect high-risk uncovered behavior.

**Review:** confirm SUT boundary, isolated environment, failure diagnostics and reproducible CI execution.
