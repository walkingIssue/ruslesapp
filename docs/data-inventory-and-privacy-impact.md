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
- keep logging minimal
- use only necessary cookies for authentication and session security
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

That signal comes from [UsersDataContext.cs](/C:/Users/Bartek/source/repos/rustles_solution/rustles.Infrastructure/UserDataContext.cs), even though several model types are still placeholders.

## Working inventory

| Data area | Current signal in code | Intended to store? | Privacy note |
| --- | --- | --- | --- |
| User identifier | Yes, `User.UId` | Yes | Needed for accounts and ownership of data. |
| External auth identifiers | Yes, `OAuthId`, `OIDCId` on `User` | Probably | Only store what is needed for sign-in and account linking. |
| Authentication/session cookie | Planned, not implemented yet | Yes | Treat as strictly necessary if used only for auth/session/security. |
| Room membership | Yes, implied by `User` and `Room` models | Probably | Needed if rooms are a core feature. |
| Messages | Yes, implied by `Message` and `Conversation` models | Probably | Message retention and moderation rules will matter. |
| Consents | Yes, implied by `Consent` model | Probably | Useful if consent-driven features are introduced. |
| User location history | Yes, implied by `Position` model | No by default | Current intent is to avoid persisting location history unless absolutely needed. |
| Derived geolocation result | Not clearly modeled yet | Maybe | Municipality or similar derived result is lower risk than raw location history, but still needs review. |
| Operational logs | Implicit in ASP.NET Core | Yes, minimal only | Avoid logging precise location, message content, tokens, or unnecessary identifiers. |
| Device fingerprint / hardware ban data | Not implemented | Avoid if possible | This would increase privacy risk and should not be added casually. |

## Data we should avoid unless we have a strong reason

- raw location history
- precise long-term movement data
- third-party analytics at launch
- advertising identifiers
- device fingerprinting
- excessive message retention
- detailed debug logs in production

## Questions we still need to answer

- Will we store any geolocation data after request completion?
- If yes, do we store raw coordinates, a derived area, or only aggregate metadata?
- How long should messages be retained?
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

## Working rule

If we are unsure whether something affects privacy, add it to this document anyway. False positives are cheap. Missing a real data-handling change is expensive.
