# Modular monolith

**Applies to:** projects that explicitly select a modular-monolith architecture.

- Modules share a deployment boundary but retain explicit ownership and interfaces.
- Avoid direct use of another module's private persistence or domain internals.
- Record legal dependency directions and module contracts in project architecture documentation.
- Do not confuse bounded contexts, modules, and independently deployed services.
