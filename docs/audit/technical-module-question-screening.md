---
title: "Technical module and decision record structural screening"
description: "Review of backend/frontend module boundaries, technical questions and implementation evidence"
kind: audit
status: active
---

# Technical modules and questions: structural screening

- Date: 2026-10-27
- Scope: **7 backend modules**, **7 frontend modules**, **12 backend technical questions** and **5 frontend technical questions**.
- Method: inspected each file's section structure and opening content and compared it with the linked business/technical feature families. This screening does not prove the technical soundness of the full implementations or line-by-line contract compatibility.
- Disposition: `screened-structure`, not deletion approval.

## Technical module boundaries

The backend modules describe module/domain ownership, interactions, state and persistence. They do **not** all represent one bounded context each. M06 groups staff workflow across identity, moderation and audit; M07 groups developer tools but explicitly does not own a separate domain data store. Their corresponding frontend modules group user interactions and client dependencies without automatically forming independent deployed units.

**Portable output:** ownership and boundary fields in the base Module and Technical Feature templates. Use modules for discoverability and capability grouping; define runtime/DDD boundaries only where the project deliberately adopts them.

**Not portable:** predecessor module names, database schema assignments, mandatory libraries, exact routes, public DTOs and role policy.

## Technical question records

Several technical question documents are detailed **chosen architecture / wire-contract records**, not genuinely unresolved questions:

- Backend Q15 and Q20 contain a mixture of source-code observations, agreed requirements and still-unverified implementation evidence.
- Backend Q19, Q30, Q31 and Q33 document versioned representations, synchronization and generation contracts; extracting them blindly would insert highly specialized product protocols into a generic repository.
- Frontend Q13 and Q24 describe concrete UI and authentication decisions; Q29, Q32 and Q37 concern a particular SDK delivery and launcher integration.

Other question records preserve accepted decisions alongside deferred details. Both `Qxx` and `technical/questions/` are **locations**, not authority or current approval indicators.

**Portable output:** distinguish chosen target design, verified current behavior, source/protocol contract, unresolved engineering detail and required implementation tests.

## Evidence taxonomy for maintainers

When turning a technical note into an implementation specification, label statements using evidence-supported distinctions:

| Description | What it proves | What it does not prove |
| --- | --- | --- |
| Accepted design | Decision authority chose this behavior | Code exists or passes acceptance |
| Observed source | A particular repository snapshot showed a specific implementation | Current checkout still matches |
| Published contract | Contract text or version exists | Consumers conform or have deployed |
| Passing verification | A stated test/inspection executed against a stated SUT | Different versions, environments or configurations work |
| Open engineering detail | Work or decision remains unspecified | An AI agent may invent a reasonable default |

The [technical feature template](../../catalog/base/docs/templates/technical-feature.md) now includes this evidence distinction to avoid conflating intended behavior with completed work.

## Remaining sources and deletion policy

Business and technical feature/module/question families now have scoped structural screening reports. Original product source and milestone metadata remain preserved. The largest and most complex technical questions have **not** undergone exhaustive semantic or executable-contract audit. Historical files must not be removed until the remaining required value is extracted or rejected, links are checked and the user decides whether any sanitized examples should be retained.
