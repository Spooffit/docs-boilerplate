---
title: ".NET testing standard"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# .NET testing standard

**Applicability:** software projects using .NET. This is a technology baseline, not a requirement to install every named testing tool. Project-specific suites and verification gates belong in the default project testing strategy scaffold at `docs/testing/strategy.md` when evidenced. Its presence does not imply tests, releases, or any mandatory CI gate already exist.

## 1. Definitions and boundaries

Classify a test by its **system under test (SUT)**, the interaction boundary, and the behavior demonstrated. A test project name or the number of bounded contexts is not a reliable classifier.

| Scope | What it establishes | Typical entry point | Infrastructure |
| --- | --- | --- | --- |
| Unit | Behavior of a small isolated component or business rule | In-process method or handler | In-memory fakes/mocks; no real infrastructure required |
| Architecture | Adopted structural rules and dependency boundaries | Inspect assemblies, projects and source | No Docker or application deployment |
| Integration | Interaction with real technical dependencies | Persistence adapter, message transport, external client | Appropriate real DB/broker/transport when those semantics matter |
| Functional / application | Observable feature of one deployable application | HTTP, gRPC, messaging or other public application boundary | Application host and necessary dependencies |
| Component (UI) | Rendering and interaction of a user-interface component | Component-rendering harness | Usually controlled API doubles, not a full browser |
| End-to-end | Finished user or consumer journey across necessary deployable units | Browser or external API | Required application topology |
| Contract | Provider/consumer compatibility with an agreed interface | Schema or provider/consumer verification | Depends on contract and verification mode |
| Performance / security | Measured non-functional property in defined conditions | Workload, scanner or policy verification | Explicit, controlled environment |

The categories are not ordered proof levels. Integration tests do not substitute for end-to-end business proof, and end-to-end tests do not prove all database constraints.

## 2. Runner versus framework

A **test framework** supplies assertions, fixture APIs and the authoring model. A **test platform** discovers and executes tests. Common frameworks are TUnit, MSTest, NUnit and xUnit.net. VSTest and Microsoft.Testing.Platform (MTP) are runner platforms, not two spellings for the same execution system.

Choose a supported project/solution runner configuration and use it consistently in CI and local development. Do not mix VSTest-only and MTP projects in a single unsupported run configuration.

For .NET 10 SDK or later, native MTP mode is selected through `global.json`:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

This is an **example for projects that have deliberately adopted MTP**, not a file to insert automatically into every .NET project. `dotnet test` argument behavior differs between runner modes and SDK versions; verify scripts against the actual runner. Older SDKs have a different MTP integration path.

References: [Microsoft platform comparison](https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview), [MTP migration](https://learn.microsoft.com/en-us/dotnet/core/testing/migrating-vstest-microsoft-testing-platform).

## 3. Isolation, determinism and lifecycle

- Each test must establish its own preconditions; never depend on previous test order.
- Control time, random generation and asynchronous scheduling where they change expected results.
- Use dedicated logical databases, schemas, queues or tenant IDs when sharing expensive physical resources. Isolation must survive parallel CI jobs.
- Avoid global mutable fixtures unless access is explicitly serialized and validated.
- Define fixture ownership, start and readiness checks, teardown and failure diagnostics.
- Use polling with a bounded deadline for eventual consistency, not arbitrary sleeps. Return meaningful failure diagnostics on timeout.
- Confirm that mocks validate **component logic only**; they do not prove database constraints, SQL, broker routing or transport serialization.

## 4. Real infrastructure and provider fidelity

Test SQL, migrations, query mapping, concurrency, transactions and constraints against the actual database engine when correctness depends on provider-specific behavior. An in-memory provider is not equivalent to PostgreSQL or SQL Server. A narrow mock test can complement, but not replace, provider-level verification.

For ASP.NET Core, `WebApplicationFactory` is a useful application-level host for exercising the request pipeline. It does not by itself prove reverse-proxy, real networking or cross-service deployment behavior. Use a real browser or deployed topology when that is the SUT.

For container-based dependencies, use Testcontainers or an equivalent controlled fixture when appropriate. Prefer dynamically assigned resource names and ports, pinned images and automatic cleanup. See the optional Testcontainers profile.

## Architecture-test boundaries

Architecture tests check **adopted, enforceable dependency rules** in assemblies, namespaces and project references without starting databases, brokers or containers. If a project has explicitly chosen a layered or bounded-context style, test the agreed allowed dependency directions and the separation of public contracts from private infrastructure.

Do not invent Domain/Application/Infrastructure assemblies to satisfy a boilerplate test, and do not freeze a historical architectural rule after the project's approved structure has changed. A shared architecture-testing helper can evaluate rules, but actual assembly names and accepted constraints belong to the specific project. The mere presence of a test library without effective assertions is not architectural coverage.

## End-to-end journeys and optional Aspire testing

End-to-end testing demonstrates the complete **externally observable journey through the actual dependencies required for that scenario**, which may involve one deployable application or several. Run the necessary path, not every service in the solution by default. Prefer black-box inputs and results to assertions on private handlers, repository calls or mock invocation counts.

When the application actually uses an Aspire AppHost, its `Aspire.Hosting.Testing` infrastructure can orchestrate the existing application graph through `DistributedApplicationTestingBuilder`. Select the **optional `aspire-testing` profile** for that tooling; do not duplicate a maintained AppHost graph with an unrelated manually assembled Testcontainers topology merely for tests. Aspire is not a required dependency of ordinary .NET testing or a replacement for `WebApplicationFactory` when the intended SUT is a single ASP.NET Core application.

For eventual outcomes, use a bounded deadline, observable condition, meaningful timeout diagnostics and cancellation. E2E jobs need their own fixture lifecycle and isolated state; resource-heavy parallelism must be bounded based on real CI capacity. A mocked internal sequence does not establish real distributed behavior.

## Unit, integration and functional test depth

These three boundaries can use similar code and still **prove different claims**. Do not classify them only by whether a test happens to use HTTP or a mock.

### Unit tests

Use an isolated component or a small set of interacting rules as the SUT. Domain value objects, business policies, validators and application handlers with controlled substitutes can qualify. Avoid real databases, brokers, containers and other external infrastructure in this category.

Tests should establish their own input and expectations, run independently and ordinarily tolerate parallel execution. Assert outputs, state changes and errors that express the component's contract. An assertion that a mocked persistence adapter was called does **not** establish correct SQL, transaction behavior or durable writes.

Avoid a universal naming mandate imported from another repository. If the team adopts `MethodUnderTest_Scenario_ExpectedBehavior`, use it consistently for the adopted suite. For tests of a workflow rather than one method, an operation or rule name is more informative than an implementation-private method name.

### Integration tests

Use the actual provider where correctness depends on its behavior. PostgreSQL constraints, EF Core/Dapper SQL, broker serialization, routing/topology, object-store metadata and network failure handling are not validated by substituting an in-memory implementation.

When the actual SUT is a message broker integration, verify the chosen serialization, routing keys, exchange/queue bindings and delivery outcome against the real broker. Test retry and error/dead-letter behavior when they are contractual. A mocked consumer alone does not establish real transport delivery.

Integration tests may deliberately observe technical internals: inspect SQL rows, schema metadata, queues, exchanges, delivery headers or adapter state when these **are the integration boundary under test**. This is not permission to expose those internals through a public API. Start only the dependencies needed by the adapter: testing one repository does not require the full distributed application graph.

Where a disposable dependency is required, select an appropriate provisioner such as the independently chosen Testcontainers profile, then isolate resource names and mutable data. The particular container library, image, broker or ORM is not implied by .NET alone. Failed mandatory dependency startup is an actual failed verification, not a silent `Skipped` success.

### Functional tests

Exercise the real external interface of one application and verify a consumer-observable outcome. For an ASP.NET Core API, a project may use `WebApplicationFactory` to run that application with realistic dependencies; the factory itself does not implement PostgreSQL, broker or storage services.

Do not replace a public HTTP request with a direct handler invocation and still claim request binding, filters, authorization and serialization were tested. Prefer observable outcomes over assertions on internal handler invocation counts. Limited database inspection can be a deliberate diagnostic or necessary assertion when no suitable public observation exists; document that exception rather than turning functional tests into adapter tests.

Prepare and clean up test data independently from integration and end-to-end suites. Wait for async outcomes using bounded, meaningful conditions rather than fixed sleeps. A single application can contain several modules or bounded contexts without automatically making its functional test a distributed E2E test.

Treat the chosen `WebApplicationFactory` host as one application's in-process public boundary, not proof of reverse-proxy routing or live multi-service deployment. For asynchronous functional outcomes, assert an observable condition with a bounded deadline, cancellation and meaningful timeout diagnostics instead of relying on an arbitrary delay.

## Test environment ownership and bounded parallelism

For suites using real infrastructure, identify a resource owner **per test run and isolation scope**. Logical isolation can be sufficient and faster than starting a separate physical container for every method, provided parallel writers cannot share mutable state or reset it underneath each other.

Integration, Functional and E2E suites must be runnable independently: none may require another suite's setup, seeded data, database, broker virtual host or cleanup step. Prefer resources provisioned by the test harness rather than a manually prepared long-lived external server. Document unavoidable external prerequisites separately from resource provisioning; a missing required container runtime or disposable dependency is a failed verification, not a passing skip.

- Database tests may use a separate database per fixture/parallel worker or a dedicated instance where behavior requires it. A reset between **sequential** tests does not protect a database against concurrent test writers or reset operations.
- Broker tests may use isolated virtual hosts or resource namespaces and unique queues/bindings per parallel scope. A dedicated broker is appropriate if the SUT changes node-level configuration, plugins, global policies or tests restart/recovery of the broker itself.
- Share heavyweight resources only within an explicitly owned run and guarantee cleanup of exactly what that run created. Suite A must not depend on suite B's preexisting database, queue, container or setup.
- Provisioning follows `Create → Start → Wait for readiness → Configure → Use → Cleanup → Dispose`; also handle failures during setup or tests. Use dynamically assigned ports/names when the selected tooling supports them.

Tune expensive suite concurrency according to real runner capacity, not magic global limits. Prefer the selected test framework's supported parallelism controls over introducing an unrelated scheduler. Unit and architecture suites must not be throttled merely because integration or E2E workloads are resource-heavy. When suites share one CI host, verify **aggregate** concurrency as well as limits per suite.

## Shared testing infrastructure and safe migration

Share only **repeated technical plumbing**, such as resource provisioning, polling/assertion helpers, connection settings and teardown logic. Create a shared testing library when multiple tests genuinely benefit; don't prebuild a speculative hierarchy of database/broker/Aspire fixtures. Keep feature-specific data builders, users, authorization policy fixtures and domain expectations near the tests that own them.

For eventually consistent outcomes, poll the **observable outcome**, with a deadline, cancellation, a meaningful interval and timeout diagnostics. Waiting an arbitrary duration does not assert that work completed.

Before migrating an existing suite, inventory its real SUT, test platform, ignored/skipped tests, environment variables, current fixtures, inherited project settings and CI invocation. Move or split tests according to the actual verification boundary, not just filenames. Preserve existing coverage unless a test is shown to be obsolete; do not silently turn a dependency startup failure into a passed skip. Running and migrating test infrastructure does not automatically authorize production business-logic, API, schema or topology changes.

Run every suite independently and then together, including parallel scenarios and negative setup/cleanup paths where appropriate. State the actual runner commands and observed results; do not present a migrated fixture or planned test list as executed proof.

## 5. Test selection and CI execution

- Keep quick unit/architecture checks independently runnable from infrastructure-heavy suites.
- Fail required suites when the environment cannot start, instead of hiding failures as unconditional skip results.
- Document which tests need a container runtime, credentials for disposable external systems or other explicit prerequisites.
- Supply a reproducible command and publish runner-compatible test results, logs and diagnostic artifacts.
- Isolate CI resource names so concurrent pipelines cannot interfere.
- Distinguish the outcome of a test command from the quality of the behavior it measures.

## 6. Naming and assertions

Name tests so their purpose and expected outcome are visible. Avoid baking architectural assumptions into naming conventions for unrelated suites. Assert on externally observable behavior appropriate to the SUT, including errors and boundary cases, not only internal invocation counts.

## 7. Coverage and risk

Coverage can reveal unexercised code but cannot establish assertion quality or production readiness. Do not impose arbitrary universal thresholds. Prioritize business invariants, data loss risks, authorization, concurrency, integration failure, backward compatibility and essential journeys.

## Review checklist

A review should be able to answer: what is the SUT, which behavior is evidenced, which components are real versus substituted, what makes data isolated, what happens on failure, where CI reports results and what remains unverified.
