# Rustles Operational Data Policy Draft

Status: internal engineering draft. This document is not a public policy and not legal advice. It is meant to turn the current MVP application flow into operational rules for implementation, storage, logging, and retention.

Last updated: 2026-04-12

## 1. Purpose

This document defines the current intended operational handling of personal data for the Rustles MVP.

It should be used to keep:

- code
- storage behavior
- privacy-facing documentation
- operational choices

aligned with each other.

## 2. MVP assumptions this policy is based on

Current MVP assumptions:

- users create an account with email, username, and password
- username is permanent
- registration includes a `16+` self-attestation and privacy acknowledgement step
- those acknowledgement records are stored
- municipality is resolved from live device location
- location data is not persisted after municipality resolution
- chat messages are public while live in the product
- sent messages are not persisted in MVP
- the product uses no cookies in MVP
- users can block and unblock other users

If any of these assumptions change, this document and the public privacy policy draft must be updated.

## 3. Data handling rules by category

| Data category | Persist? | Purpose | Legal/operational note | Target retention direction |
| --- | --- | --- | --- | --- |
| Email address | Yes | Account identity, sign-in, recovery | Account-critical personal data | Active account + limited post-closure need |
| Username | Yes | Public-facing account identity | Permanent by product design | Active account + limited post-closure need |
| Password information | Yes | Authentication | Must never be stored in plaintext | Active account until password change or account closure, subject to auth implementation |
| Age self-attestation | Yes | Enforce registration age gate | Store only the minimum record necessary | Keep while needed to evidence the registration step |
| Privacy-notice acknowledgement | Yes | Evidence registration acknowledgement | Store only the minimum record necessary | Keep while needed to evidence the registration step |
| Block relationships | Yes | User-controlled filtering/safety | Personal to the blocker, not global moderation state | Until user unblocks or account closes, unless legal/security need requires more |
| Live location used for municipality resolution | No | Resolve municipality context | Runtime-only processing in MVP | Discard after resolution step |
| Derived municipality result | Maybe runtime only | Put user in municipality context | Do not persist unless a clear product need appears | Prefer runtime-only in MVP |
| Sent messages | No for MVP persistence | Operate live public chat | Publicly visible in runtime flow but not stored by Rustles in MVP | Do not persist |
| Technical/security logs | Yes, minimal | Security, troubleshooting, abuse investigation | Must avoid unnecessary personal data and must not log message content or precise location | Short limited retention only |

## 4. Required implementation rules

### Account and authentication

- Store password information only in a secure authentication form, not plaintext.
- Username is permanent and must not have a change path in MVP.
- Email and username are core account identity fields.

### Registration acknowledgement

- The `16+` self-attestation must be recorded if account creation depends on it.
- Privacy-notice acceptance must be recorded if account creation depends on it.
- Only the minimum metadata needed for accountability should be stored.

Suggested minimum record:

- user identifier
- acknowledgement type
- timestamp
- policy/version identifier

Only add more than that if there is a concrete compliance or security reason.

### Location handling

- Use location only for live municipality resolution in MVP.
- Do not persist raw coordinates after the resolution step completes.
- Do not create location history.
- Do not add location data to general application logs.

### Messaging

- Treat municipality chat as public runtime content.
- Do not persist sent messages in MVP storage.
- Do not allow routine application logs to capture message content.
- If temporary runtime buffering exists, it must not quietly become de facto message persistence.

### Blocking

- A block must only affect the blocker's own view.
- Block relationships are persisted.
- Unblocking must remove the active block relationship for future filtering.

### Cookies and client storage

- Do not use cookies in MVP.
- If local storage, session storage, or similar client persistence is introduced later, it must be reviewed as part of the privacy update process.

## 5. Logging rules

Allowed direction:

- minimal security and error logs
- timestamps
- service/component names
- request identifiers
- high-level failure categories

Avoid by default:

- raw location coordinates
- message content
- full authentication payloads
- plaintext secrets or tokens
- unnecessary user-identifying details

If an incident requires temporary deeper logging, it should be narrowly scoped, time-limited, and removed afterward.

## 6. Public-chat risk disclosure

The product and privacy notice should consistently disclose:

- chat is public within the relevant municipality context
- other users can read what is posted while it is live
- other users may still save, copy, screenshot, or otherwise persist what is shared, even though Rustles does not persist messages in MVP

Engineering and product copy must stay aligned on this point.

## 7. Data minimisation rules

Before adding a new stored field, table, claim, or external integration, ask:

1. Is this needed for the current MVP flow?
2. Does this need to be persisted, or can it remain runtime-only?
3. Is there a lower-risk derived value we can store instead?
4. Does the privacy notice need to change first?

If the answer to the last question is yes, the privacy docs should be updated before or with the code change.

## 8. Pre-release verification checklist

Before any public release:

1. Verify that the public privacy policy matches actual implementation.
2. Verify whether any hosting or infrastructure layer logs IP addresses or other identifiers by default.
3. Verify whether password recovery adds an email processor/vendor.
4. Verify whether any acknowledgement metadata is actually stored and what exact fields are included.
5. Verify that messages are not silently being persisted by application code, queues, diagnostics, or infrastructure.
6. Verify that location data is not silently being persisted in logs or telemetry.
7. Verify that no cookies or other client-side persistence mechanisms were introduced accidentally.

## 9. Change control rule

Any change to:

- account fields
- acknowledgement flow
- location handling
- message handling
- logging
- cookies/client storage
- vendors/processors

must trigger an update to:

- [data-inventory-and-privacy-impact.md](./data-inventory-and-privacy-impact.md)
- the public privacy policy draft
- the relevant MVP spec files
