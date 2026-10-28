---
title: "Accessible interfaces and form state"
description: "Technology-neutral guidance on UI form lifecycle, feedback and accessibility"
kind: standard
status: active
---

# Accessible interfaces and form state

**Applies to:** applications with interactive forms, user actions, validation and changing state. It does not select a frontend framework, component library or design system.

## Form state and user feedback

Distinguish **local input**, **submitted request**, **accepted operation**, **persisted state**, and **visible updated result** where these are separate. Show success only after the operation has reached the level actually promised in the user-facing message.

- Show meaningful loading, success, error and unknown-outcome states. Do not disguise server failure as successful local UI state.
- Prevent accidental repeated submission where it can duplicate side effects, while retaining a recovery path.
- A client timeout does not prove a write was cancelled. Confirm outcome or use a contractually safe idempotency mechanism before retrying.
- Treat changes in form rows as edits until the accepted interaction explicitly requests deletion. Do not infer destructive server actions from removing a local row.
- Keep user-entered values after recoverable validation failures where appropriate. Confirm autosave, cancel and draft semantics based on the specific product requirements.
- Client-side validation helps usability, but must not replace server-side authorization, invariant checks or database constraints.

## Identity, edit intent and lifecycle

Use fields and object identifiers defined by the real operation contract. A client must not invent a server-owned technical identifier or treat locally removing an editable row as authorization to delete durable server data. Different Create, Edit, Save, Apply and Delete operations may require different inputs and confirmation steps; do not infer those operations from one generic form.

Where autosave, cancellation, navigation or multi-screen drafts exist, specify who owns unsaved state and when it is discarded or restored. Distinguish what the user typed from the request sent, a durable server commit and any later publication/application of that state. A "Saved" message must not claim an asynchronous publish or activation has already finished.

For tree or versioned-record UIs, distinguish the *currently selected* item, its identity, hierarchy and its historical version or lifecycle status. Do not make a visual selection indicator imply that this object is current or published. These rules apply only to UIs with those concepts; a simple form needs no invented tree or versioning UI.

## Access and semantics

- Associate each input with an understandable label and any needed instructions.
- Identify invalid fields and errors **in text**, not by color alone. Offer corrective guidance where known.
- Provide keyboard access and a visible, sensible focus indication. Avoid keyboard traps and unexpected context changes.
- Expose the semantics of controls, expanded state, selection and status messages to assistive technologies using appropriate native elements and accessible APIs.
- Maintain meaningful focus when dialogs open and close, validation fails or content changes.
- Use actual contrast, target-size and focus requirements adopted by the project; no template can certify WCAG conformance on its own.

## Accessible hierarchical controls and feedback

When an interface presents hierarchical items, ensure the nesting and selected item can be understood without relying only on indentation, color or icons. Choose native HTML semantics or an accessible tree/list pattern that actually matches keyboard interaction and focus behavior. Selection, expansion and the item's lifecycle/version status are distinct states and need distinguishable labels or descriptions.

Loading, success, warnings and failures must be perceivable as meaningful messages, including for assistive technologies when content changes asynchronously. A visible disabled button alone is not a substitute for a status message or a recovery path. Define keyboard/assistive-technology coverage and the target WCAG level in each project's test plan; automated scans and a few keyboard tests are not certification.

## Verification

Test form submission, duplicate actions, pending/failed writes, focus restoration and text errors. Inspect with keyboard and appropriate assistive technologies. An automated accessibility scanner provides partial evidence, not full certification.

The project determines its browsers, devices and intended conformance target. Reference [WCAG 2.2](https://www.w3.org/TR/WCAG22/), [error identification](https://www.w3.org/WAI/WCAG22/Understanding/error-identification), and [WAI form notifications](https://www.w3.org/WAI/tutorials/forms/notifications/).
