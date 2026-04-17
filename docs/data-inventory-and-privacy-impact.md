# Rustles Data Inventory and Privacy Impact Notes

This is a working engineering document, not legal advice. The goal is to keep the codebase and the future privacy policy aligned as the product evolves.

## Why this exists

Rustles is still early enough that data handling can be shaped deliberately instead of patched over later.

We should use this document to track:

- what data the codebase already suggests we will store
- what data we actually intend to store
- what data we want to avoid storing
- what changed in a coding session that may affect the privacy policy

## Current privacy direction

These are the operating assumptions for now:

- do not persist user location history unless there is a strong product reason
- do not persist location data at all in the MVP flow
- do not persist sent messages in the MVP flow
- keep logging minimal
- do not use cookies in MVP
- avoid analytics and third-party trackers at launch
- keep the first release's data model as small as possible

## Current code signals

The current infrastructure layer suggests these storage areas:

- `Users`
- `Conversations`
- `Messages`
- `Positions`
- `Rooms`
- `Consents`

That signal comes from [UsersDataContext.cs](../rustles.Infrastructure/UserDataContext.cs), even though several model types are still placeholders.

## Working inventory

| Data area | Current signal in code | Intended to store? | Privacy note |
| --- | --- | --- | --- |
| User identifier | Yes, `User.UId` | Yes | Needed for accounts and ownership of data. |
| Email address | Yes, implied by user account/auth flows | Yes | Used for account identity and recovery. |
| Username | Yes, implied by MVP account spec | Yes | Chosen at registration and intended to be permanent. |
| Password information | Yes, implied by auth flows | Yes | Store only what is needed for authentication, ideally as hashed credentials rather than plaintext. |
| Age self-attestation | Implied by MVP registration acknowledgement step | Yes | Current MVP direction assumes the 16+ registration gate result must be recorded if it is required before account creation. |
| Privacy notice acknowledgement | Implied by MVP registration acknowledgement step | Yes | Current MVP direction assumes acceptance must be recorded if it gates account creation. |
| External auth identifiers | Yes, `OAuthId`, `OIDCId` on `User` | Probably | Only store what is needed for sign-in and account linking. |
| Authentication/session cookie | Previously discussed, not part of current MVP privacy direction | No | Current MVP direction is to avoid cookies entirely. |
| User block relationships | Implied by MVP blocking feature | Yes | A block list is a user-to-user safety relationship and should be described truthfully in privacy-facing docs if implemented. |
| Room membership | Yes, implied by `User` and `Room` models | Probably | Needed if rooms are a core feature. |
| Messages | Yes, implied by `Message` and `Conversation` models | No for MVP persistence | Messages may exist in runtime flow, but current MVP direction is not to persist sent messages. |
| Consents | Yes, implied by `Consent` model | Probably | Useful if consent-driven features are introduced. |
| User location history | Yes, implied by `Position` model | No | Current MVP direction is not to persist location data. |
| Derived geolocation result | Not clearly modeled yet | Maybe | Municipality resolution may exist at runtime without persistence. |
| Operational logs | Implicit in ASP.NET Core | Yes, minimal only | Avoid logging precise location, message content, tokens, or unnecessary identifiers. |
| Device fingerprint / hardware ban data | Not implemented | Avoid if possible | This would increase privacy risk and should not be added casually. |

## Data we should avoid unless we have a strong reason

- raw location history
- any persisted location coordinates in MVP
- precise long-term movement data
- third-party analytics at launch
- advertising identifiers
- device fingerprinting
- persisted message history in MVP
- detailed debug logs in production

## Questions we still need to answer

- Do we need to persist any derived municipality result, or can that stay runtime-only for MVP?
- When do messages need to become durable enough to justify persistence?
- What is the minimum age-gate and privacy-notice acknowledgement metadata we need to persist?
- Do we need soft-delete, hard-delete, or both?
- Will any vendor outside our own hosting stack process user data?
- Do we need account deletion and data export in the first public release?

## Session review checklist

At the end of each coding session, review these questions:

1. Did we add a new field, table, cookie, claim, log entry, or external service?
2. Did we start storing any new personal data?
3. Did we increase the precision, retention, or visibility of existing data?
4. Did we add any new auth, analytics, messaging, or notification behavior?
5. Did we create anything that the privacy policy, cookie notice, or terms will need to mention?
6. Did we create anything that makes account deletion, consent, or moderation harder?

## Session change log

Use this table as a running log.

| Date | Change | Data impact | Needs privacy-policy update? | Notes |
| --- | --- | --- | --- | --- |
| 2026-04-05 | Initial inventory document created | No product-data change yet | No | Baseline engineering/privacy tracking document. |
| 2026-04-12 | MVP privacy direction tightened | Yes | Yes | Current MVP direction stores email, username, and password information, does not persist location data or sent messages, and does not use cookies. |
| 2026-04-12 | Registration acknowledgement persistence assumed | Yes | Yes | Current MVP direction assumes age self-attestation and privacy-notice acceptance are recorded when account creation is completed. |

## Working rule

If we are unsure whether something affects privacy, add it to this document anyway. False positives are cheap. Missing a real data-handling change is expensive.
