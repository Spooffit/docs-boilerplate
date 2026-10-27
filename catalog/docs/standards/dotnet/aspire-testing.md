---
title: "Aspire AppHost distributed application testing"
description: "Optional test tooling for actual Aspire AppHost graphs, resource readiness and end-to-end evidence"
kind: standard
status: active
---

# Aspire AppHost distributed application testing

**Applicability:** only .NET solutions with a real Aspire AppHost and tests that need that distributed application's running processes and dependencies. This is a separate opt-in profile. It requires the `dotnet` profile but does **not** automatically install Aspire, Testcontainers, TUnit or any test-framework package.

## Select the correct SUT

Use `Aspire.Hosting.Testing` and `DistributedApplicationTestingBuilder` to test the selected AppHost graph as a running distributed application. This tooling starts application components as separate processes, not a set of in-process fakes. It is suitable for cross-service journeys and actual dependency integration, not for cheap isolated domain unit tests.

If the required SUT is just one ASP.NET Core application, `WebApplicationFactory` may be a better and less expensive test boundary. A single public API call that crosses a real worker/broker may be end-to-end, while an AppHost test targeting one technical adapter is still about integration. Classify by **verified behavior and boundaries**, not the orchestration library.

## Reuse the AppHost model

- Reference the **actual AppHost project** from the test project and use supported testing-package versions for the solution's Aspire SDK.
- Launch through the AppHost's maintained graph. Avoid copying resource names, connection strings, health checks or container definitions into a second divergent test topology.
- Use configured application settings/environment parameters to specialize disposable test resources where permitted; do not modify production dependency direction merely to accommodate tests.
- Aspire testing commonly randomizes proxied host ports for isolation; use service discovery and supplied endpoint/resource access rather than hardcoding `localhost` or fixed ports.
- Confirm all required resources and services belong to the tested scenario; do not start unrelated deployable units just to satisfy an arbitrary completeness slogan.

## Readiness and lifetime

A process entering `Running` is not proof that it is ready to serve requests. Where the AppHost exposes health checks, use its documented resource-health readiness mechanisms; otherwise define a bounded, observable readiness probe. Every wait needs a timeout, cancellation and useful failure diagnostics.

The AppHost and dependencies are expensive. Reuse a test fixture at the appropriate safe scope, but isolate mutable database/broker state across tests and concurrent CI jobs. Dispose the builder/application and ephemeral resources on failure as well as success. Do not share a running mutable state across unrelated suites without verified isolation.

## Assertions and failure testing

Exercise the user's or consumer's real entry point and assert the final observable result. For an asynchronous scenario, poll a real status/resource rather than sleeping for a fixed duration or checking whether an internal handler was invoked.

Verify at least a representative startup-failure path, resource-not-ready timeout, broker/database unavailable behavior when relevant, and cleanup. Keep actual logs, traces, resource states and failing requests available in CI diagnostics without leaking credentials or sensitive payloads.

## Runner, package and compatibility evidence

Choose the test framework independently: the AppHost testing package does not imply TUnit, xUnit, NUnit or MSTest. Confirm SDK/package compatibility, required container/runtime permissions, test result reporting and process exit codes using the project's real CI.

This guidance is not evidence that any particular AppHost/test code compiles or a distributed scenario passes. Generated documentation must be filled with actual resource identifiers, tests and executed results.

References: [Aspire testing overview](https://learn.microsoft.com/en-us/dotnet/aspire/testing/overview), [resource readiness](https://learn.microsoft.com/en-us/dotnet/aspire/testing/accessing-resources), [AppHost test lifecycle](https://learn.microsoft.com/en-us/dotnet/aspire/testing/manage-app-host), [general .NET testing](testing.md).
