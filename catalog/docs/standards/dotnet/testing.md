---
title: ".NET testing standard"
description: "Technology-scoped engineering standard"
kind: standard
status: active
---

# .NET testing standard

**Applicability:** software projects using .NET. This is a technology baseline, not a requirement to install every named testing tool. Project-specific suites and release gates belong in the project testing strategy at `docs/testing/strategy.md` **if that optional module was selected**; otherwise record them in the project's architecture/README as needed.

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
