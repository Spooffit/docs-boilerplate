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

## Access and semantics

- Associate each input with an understandable label and any needed instructions.
- Identify invalid fields and errors **in text**, not by color alone. Offer corrective guidance where known.
- Provide keyboard access and a visible, sensible focus indication. Avoid keyboard traps and unexpected context changes.
- Expose the semantics of controls, expanded state, selection and status messages to assistive technologies using appropriate native elements and accessible APIs.
- Maintain meaningful focus when dialogs open and close, validation fails or content changes.
- Use actual contrast, target-size and focus requirements adopted by the project; no template can certify WCAG conformance on its own.

## Verification

Test form submission, duplicate actions, pending/failed writes, focus restoration and text errors. Inspect with keyboard and appropriate assistive technologies. An automated accessibility scanner provides partial evidence, not full certification.

The project determines its browsers, devices and intended conformance target. Reference [WCAG 2.2](https://www.w3.org/TR/WCAG22/), [error identification](https://www.w3.org/WAI/WCAG22/Understanding/error-identification), and [WAI form notifications](https://www.w3.org/WAI/tutorials/forms/notifications/).
