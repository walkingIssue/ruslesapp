# Privacy And Data Inventory Review

Needs review: `true`

Privacy review is required. This diff adds or revises multiple privacy-facing and operational-policy documents asserting that the MVP will store account data, username, age/privacy acknowledgements, and block relationships, while not persisting location data, sent messages, or cookies. The diff also adds MVP specs around authentication, blocking, privacy notice, and municipality chat. These appear to be documentation and product-direction changes more than clearly implemented data-handling changes, so they should be treated as review-required assumptions until verified against code and hosting behavior.

## Proposed Session Log Entry

2026-04-17 | Documentation and MVP-spec expansion around privacy/account/location handling | Review required | Added draft privacy policy, operational data policy, and MVP specs stating current intended MVP direction: store core account data plus registration acknowledgements and block relationships; do not persist location data, sent messages, or cookies. Treat as policy-direction documentation pending implementation verification.

## Suggested Inventory Changes

### Registration acknowledgement data
- Change: Docs now explicitly assume storage of age self-attestation and privacy-notice acknowledgement metadata during account creation.
- Risk: `medium`
- Policy note: Needs review against actual registration implementation and minimum-metadata retention decisions.

### Block relationships
- Change: Specs and privacy docs now treat user blocking as a persisted safety relationship.
- Risk: `medium`
- Policy note: If implemented, public privacy-facing documents should mention that block records are stored and used for filtering/safety behavior.

### Location data
- Change: Docs tighten MVP direction to runtime-only municipality resolution with no persisted location data.
- Risk: `high`
- Policy note: Verify application logs, telemetry, infrastructure defaults, and any persistence layer do not retain coordinates or location history.

### Messages
- Change: Docs now state sent messages are not persisted in MVP even though live municipality chat exists.
- Risk: `high`
- Policy note: Verify runtime buffers, diagnostics, queues, and infrastructure do not effectively create message retention.

### Cookies and client storage
- Change: Docs now state no cookies in MVP.
- Risk: `medium`
- Policy note: Verify auth/session approach and check that no cookies or equivalent client persistence are introduced by framework defaults.

### Operational logs
- Change: New policy drafts emphasize minimal logging and avoiding message content, precise location, tokens, and unnecessary identifiers.
- Risk: `medium`
- Policy note: Needs review against actual host/runtime logging configuration, especially default ASP.NET Core and infrastructure behavior.
