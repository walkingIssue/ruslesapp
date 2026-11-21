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
- persist municipality chat messages and emoji reaction events for the chat experience
- keep logging minimal
- use only strictly necessary authentication cookies
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
| Authentication/session cookie | Yes, ASP.NET Core Identity auth flow | Yes | Strictly necessary for sign-in and account protection. No advertising or analytics cookies in the current build. |
| User block relationships | Implied by MVP blocking feature | Yes | A block list is a user-to-user safety relationship and should be described truthfully in privacy-facing docs if implemented. |
| Room membership | Yes, implied by `User` and `Room` models | Probably | Needed if rooms are a core feature. |
| Messages | Yes, `MunicipalityChatMessage` | Yes | Stored for chat catchup, reply links, pinned-message scoring, account export, and account deletion. |
| Emoji reactions | Yes, `MunicipalityMessageReaction` | Yes | Stored as one reaction event per user action, then aggregated for display. |
| Municipality moderation bans | Yes, `MunicipalityUserBan` | Yes | Stored when a trusted tester bans a user from a municipality chat. Scope bans tightly to the municipality and avoid device fingerprinting. |
| Message deletion metadata | Yes, `MunicipalityChatMessage` soft-delete fields | Yes | Stores deletion timestamp, moderator ID, and optional reason while excluding deleted messages from chat catchup, reactions, and pinned-message scoring. |
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
- detailed debug logs in production

## Questions we still need to answer

- Do we need to persist any derived municipality result, or can that stay runtime-only for MVP?
- What is the minimum age-gate and privacy-notice acknowledgement metadata we need to persist?
- Do moderation bans need expiry defaults, appeal/review tooling, or an audit export before public launch?
- Should message deletion eventually hard-delete rows after a retention period, or remain soft-delete only for moderation/accountability?
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
| 2026-04-12 | MVP privacy direction tightened | Yes | Yes | Superseded by the 2026-06-05 message persistence and authentication-cookie update. |
| 2026-04-12 | Registration acknowledgement persistence assumed | Yes | Yes | Current MVP direction assumes age self-attestation and privacy-notice acceptance are recorded when account creation is completed. |
| 2026-06-05 | Persisted municipality messages, emoji reactions, pinned-message scoring, replies, auth-cookie notice, data export/delete support | Yes | Yes | Privacy and terms pages were added, but public legal text still requires human review before production publication. |
| 2026-06-05 | Added trusted-tester message deletion and municipality-scoped author bans | Yes | Yes | Moderation introduces ban records and message deletion metadata. Public and internal docs must describe this without implying device fingerprinting or global bans. |
| 2026-11-21 | SecretBox specification and frontend integration proposal | No runtime change; future data handling proposed | Before feature launch, subject to human review | See the SecretBox proposal below; no public policy or terms finalized. |

## SecretBox proposal (2026-11-21)

**Proposal for human review; not implemented.** This documentation change introduces
no new collection, table, cookie, vendor, or retention behavior. It records the
potential impact of the [SecretBox plan](secretbox-integration-plan.md). Existing
policy/terms pages have not been rewritten or finalized.

| Proposed data | Purpose and access | Review needed before implementation/launch |
| --- | --- | --- |
| Exchange membership, peer confirmation, sealed/opened timestamps | Enforce a two-person exchange using existing account IDs | Minimize status visibility; no public participant directory or municipality association. |
| Encrypted secret messages and key references | Hold contributions until both seal; deliver only to the intended recipient | Decide server-managed encryption versus E2EE, key storage/rotation, backup recovery, and deletion. |
| Hashed expiring invitation tokens | Permit an explicit join request and creator confirmation | Single use, token rotation, log/referrer redaction, and expiry. |
| Optional camera/audio recording | Capture the current user's reaction after permission | No background capture; private preview; explicit upload if server rendering is selected. |
| Temporary media, transcripts, captions, and export jobs | Produce a reviewed vertical clip | Choose processing location/vendor, access controls, retention, cleanup, and consent for another participant's material. |
| Export-version approvals | Record permission to include another person's message, identity, or recording | Bind approval to exact content; edits invalidate it; no promise to recall an exported file. |
| Blocking and minimal abuse records | Prevent repeated unwanted invitations | Current municipality bans do not implement this; define a narrow access/retention model. |
| Optional aggregate pilot counts | Evaluate completion/export usage | Existing launch direction excludes third-party trackers; no raw secrets, audio, or invite tokens in telemetry. |

Working defaults proposed for review: expire unready boxes/invitations after 7 days,
Ready content after 30 days, and temporary rendering assets within 24 hours.
Define backup retention and cleanup behavior before making retention promises.
Extend account export/delete to cover this feature while excluding a peer's secret
before it is available; deletion must include non-database media and key material.

Existing registration records a 16+ self-attestation. Preserve that requirement in
the specification while the product owner reviews feature eligibility and disclosures.
Do not infer that account acceptance authorizes recording, third-party processing,
or public sharing. The current public-chat description does not describe this private
exchange; updated legal/product copy remains a separate human-reviewed launch task.

## Working rule

If we are unsure whether something affects privacy, add it to this document anyway. False positives are cheap. Missing a real data-handling change is expensive.

