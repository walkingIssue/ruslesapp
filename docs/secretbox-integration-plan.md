# SecretBox: specification and starting integration plan

Status: proposal, not implemented. Date: 2026-11-21.
Repository: walkingIssue/ruslesapp; reviewed main: 93bb8a7c4c6ed16108b648918f16521bb7a1922f.

## Intended outcome

A person signs in with a normal Rustles account, opens **SecretBox** from the burger
menu, and exchanges a sealed secret with one other account. Each contributes before
either can read the other's message. They can reveal privately or record their own
reaction and prepare a branded vertical clip to share.

This implements the secret-sharing idea inside the existing platform. Atlas, maps,
and location capture are independent future work. No second account system is needed.

Read the [frontend review](secretbox-frontend-review.md), the
[interaction specification](../spec/secretbox/secret-exchange.interaction.md), and
[behavior scenarios](../spec/secretbox/secret-exchange.feature) together.

## Confirmed direction and working assumptions

| Item | Basis |
| --- | --- |
| Existing Rustles account; burger-menu entry; specification before implementation | Requested in this conversation. |
| Asynchronous secret exchange, encryption, optional reaction video and social sharing | Original SecretBox concept. |
| Two participants for the first version | Proposed scope; group exchanges follow later. |
| Both seal before either receives the other's content | Proposed concrete interpretation of a fair exchange. |
| Opening manually by default; optional automatic opening when ready | Proposed interpretation of the earlier timing choice. Availability and opening are separate. |
| SecretBox as the visible feature name, /secrets as the route | Working names, easy to change. |
| Sender confirms the joining account before availability | Proposed protection against a forwarded invitation enrolling an unintended person. |
| 2,000 characters per secret; one secret per participant; 60-second recording cap | Proposed starting limits, to validate in the pilot. |

## Entry point and page structure

Place SecretBox directly below Chat in the authenticated menu. Keep Privacy, Terms,
Account, and Logout. Preserve the same feature order across screen sizes. A count
badge, if added, means boxes ready for this user to open, never message contents.

| Route | Access | Purpose |
| --- | --- | --- |
| /secrets | Signed in | Your boxes: action needed, waiting, opened; New secret exchange button. |
| /secrets/new | Signed in | Explain the exchange, create a box, compose and seal a message. |
| /secrets/join/{token} | Generic invitation landing before sign-in; claiming requires sign-in | Explain the invitation, reuse login/register, then explicitly request to join. |
| /secrets/{boxId:guid} | Participant only | Confirm peer, compose, waiting, ready, private reveal or recording, and export. |

An invitation landing must not expose secret text, email, or a box transcript.
The ordinary /secrets route requires an account; the generic invitation explanation
helps a new user understand why they are being asked to register.

Use a compact shell with a visible Rustles identity, feature title, menu, and a
single main content column. Reuse the platform's menu component. Extract only the
layout support needed for Chat and SecretBox; a site-wide visual redesign is outside
this slice.

## Exchange behavior

1. A creates a box and seals a message. A receives a persisted receipt and an invitation link.
2. B opens the link, signs in or registers and confirms email, then explicitly joins.
3. A sees B's safe display name and confirms that account. B may draft and seal while
   awaiting confirmation; no message is available before confirmation.
4. Once both messages are sealed and the peer is confirmed, the server atomically
   marks the box Ready. Neither participant has to be online at the same time.
5. Each participant chooses when to open. Manual opening offers Read privately or
   Record and reveal. Recording starts only after an explicit action and browser permission.
6. A preview lets the recorder trim, correct captions, choose permitted content, and
   download or open the device's share flow. Nothing posts automatically.

The box states are Waiting, Ready, Cancelled, and Expired. Peer confirmation and each
participant's sealed/opened timestamps are separate fields, not a proliferation of
box states. Drafts remain in component memory until sealing; do not silently persist
secret drafts in localStorage.

A participant may cancel a Waiting box. Cancelling, expiry, or losing a peer removes
the ability to reveal. Once Ready, normal withdrawal cannot take back the contribution
while preserving access to the other person's secret. Explicit content/account
deletion is a separate privacy operation and can remove future access; explain this
without promising that already-read or exported copies can be recalled.

Automatic opening is opt-in per participant, only in an active foreground session
when Ready. It never opens a camera and never reveals in notifications. When the
session is absent or backgrounded, show Ready on return and require an open action.
Changing to recording mode requires manual opening; a later replay is labelled a
replay, not a first reaction.

## Implementation attachment map

Paths are relative to the repository. New paths below are proposals.

| Existing location | Planned change |
| --- | --- |
| rustles_platform/rustles_platform.Client/Layout/NavMenu.razor and its CSS | Add SecretBox link; replace checkbox/global-click menu behavior with accessible component state. |
| rustles_platform/rustles_platform.Client/Layout/MainLayout.razor and its CSS | Give the feature a compact shell while keeping navigation consistent with Chat. |
| rustles_platform/rustles_platform.Client/Routes.razor | Reuse authorized routing; new pages declare their routes and authorization requirements. |
| rustles_platform/rustles_platform/Components/App.razor | Explicit non-prerendered WebAssembly mode for /secrets; preserve static account rendering. |
| rustles_platform/rustles_platform/Components/Account/Pages/Login.razor, Register.razor, ConfirmEmail.razor, RegisterConfirmation.razor | Preserve and validate the local invitation destination through account creation and confirmation. Verify 2FA and external-login paths rather than assuming they work. |
| rustles_platform/rustles_platform.Client/Program.cs | Register a feature API client; browser recorder is lazy and not initialized on page load. |
| rustles_platform/rustles_platform/Program.cs | Register feature services and map authenticated SecretBox endpoints. No geolocation or municipality-hub dependency. |
| rustles.Infrastructure/UserDataContext.cs and Model/ | Add exchange entities and mappings through an EF migration; reuse User.Id. |
| rustles_platform/rustles_platform/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs | Extend personal-data export without revealing a peer's still-sealed content. |
| rustles_platform/rustles_platform/Components/Account/Pages/Manage/DeletePersonalData.razor | Coordinate content, media, and key cleanup with account deletion, beyond database cascades. |

Proposed new frontend files under rustles_platform/rustles_platform.Client:
Pages/Secrets/Index.razor, Create.razor, Join.razor, Box.razor, plus small
SecretComposer, ExchangeStatus, RevealPanel, and ReactionRecorder components.
Put API/state logic under Services/Secrets and recording interop under src/.
Check the existing TypeScript build/output convention before adding a module.

Proposed server files under rustles_platform/rustles_platform:
Endpoints/SecretBoxEndpoints.cs, Services/Secrets/SecretBoxService.cs, and
Services/Secrets/ISecretProtector.cs. Add explicit request/response records in
rustles.contracts/Secrets. Do not extend municipality chat messages or broadcast
secrets through municipality groups.

## Minimal server design

Use same-origin authenticated requests. Derive the acting user from the server's
Identity principal; every box read and mutation checks membership and state.
UI authorization alone is insufficient.

| Proposed operation under /api/secrets | Responsibility |
| --- | --- |
| POST /boxes | Create a box for the current account; return ID and state. |
| GET /boxes and GET /boxes/{id} | Return the user's boxes or participant-safe status; no peer secret body. |
| POST /boxes/{id}/seal | Validate, encrypt, and persist exactly one immutable contribution for this actor. |
| POST /boxes/{id}/invite and POST /invites/{token}/claim | Issue/rotate an invitation and atomically claim a provisional peer slot. |
| POST /boxes/{id}/confirm-peer | Creator confirms that account; recompute Ready atomically. |
| POST /boxes/{id}/open | If Ready, return the peer message to its recipient and record opening idempotently. |
| POST /boxes/{id}/cancel | Atomically cancel a Waiting exchange and invalidate its invitation. |

The first internal UI slice can use bounded status polling with backoff, paused when
hidden. Status responses and any later real-time notifications contain no secret
bodies. A dedicated real-time channel is optional later, never the municipality hub.

Persist a SecretExchange (creator, state, peer confirmation, creation/expiry,
concurrency token), two SecretParticipants (account, role, sealed/opened timestamps,
opening preference), encrypted SecretMessages (ciphertext, nonce/tag, key reference,
author), and hashed, expiring SecretInvites. Enforce unique participant/message
constraints and reject self-join and third participants.

Use a transaction plus database constraints/concurrency checks for sealing,
confirmation, cancellation, claim, and expiry. Network retries must use idempotency
keys scoped to actor and operation; reject reuse with a different payload.
Do not show Sealed until storage succeeds. Recover ambiguous timeouts by fetching
the receipt/state, rather than allowing a second contribution.

## Encryption and access boundaries

Proposed first-version model: TLS in transit and authenticated application-level
encryption at rest, using a maintained cryptographic library and managed keys stored
separately from the database. Bind encryption to box, author, and recipient context.
The server enforces the release rule and can decrypt: **this is not end-to-end encryption**.
Do not put keys in the WebAssembly bundle or invent a custom encryption protocol.

Before implementation, record key storage, rotation, backup recovery, and deletion
behavior in a short design decision. If server-blind E2EE is required, revisit recipient
key exchange and multi-device recovery before building the message store.

- Validate antiforgery tokens explicitly for cookie-authenticated mutations; middleware
  registration alone is not the acceptance test.
- Use high-entropy single-use invitation tokens, store only hashes, reject expired or
  revoked claims, and scrub token paths from logs/referrers.
- Private content responses use no-store caching; no preview HTML, page metadata,
  analytics events, error logs, or public object URLs contain messages or invite tokens.
- Validate message length server-side and render text as text, not raw HTML.
- Apply per-account/action limits and invitation abuse limits; do not inherit the
  general fallback limiter without reviewing the feature's operations.
- Provide decline, cancellation, and a way to prevent repeated invitations from a
  blocked account. Existing municipality bans are not a global user-block mechanism;
  this requires explicit feature/backend work before a public pilot.
- Existing TrustedTester status does not imply permission to read private secrets.

## Recording and export slice

Start with the current user's own reaction. Record before requesting the reveal, place
a marker when the secret is actually shown, then retain a short aftermath. If camera
permission or recording fails before opening, the message stays closed and the user
can choose Read privately. If opening succeeded but capture failed, retain access
and explain that the first reaction was not captured.

Use HTTPS, runtime MediaRecorder MIME detection, and a tested encoding path. Capture
format detection does not guarantee an Instagram/TikTok-ready file. Spike actual
iOS Safari and Android Chrome recording, orientation, audio, interruptions, and memory
use. For a consistent 9:16 MP4 output, evaluate an isolated server render worker that
accepts only the user-approved clip/assets, uses private temporary storage, and
enforces duration/size/job limits. Do not assume browser capture alone burns captions.

Generate editable captions from the selected audio with an explicitly chosen processing
path. Check captions as well as visual overlays for a spoken/read-out secret.
The proposed initial export is reaction-only with a small Rustles/SecretBox label and
a generic /secrets entry link. Including the other person's words, name, voice, or
reaction requires their approval of the exact export version. Edits invalidate that
approval. A failed/declined approval still permits a version containing only the
recorder's own material.

Never put the private invite token or box URL into the exported video or its public
caption. Test sharing with navigator.canShare for the actual file, offer download
when unavailable, and treat share-sheet cancellation as cancellation. Do not claim
an export was posted to a social platform merely because a share action returned.

A shared media/export service could later support Atlas capture. The first exchange
must not depend on Atlas or on a completed cross-platform media library.

## Delivery slices and acceptance gates

| Slice | Work | Ready when |
| --- | --- | --- |
| 1. Navigation and account continuity | Menu entry, compact routes and fake UI states, explicit render mode, invitation continuation. | Existing and new users reach the intended feature, including email confirmation; GPS denied makes no difference; keyboard/mobile navigation works. |
| 2. Private text exchange | Storage, encryption, invitation/peer confirmation, both-sealed release, cancellation/expiry, API authorization. | Two different accounts complete an asynchronous exchange; a third account cannot read it; race/retry tests preserve the rule. |
| 3. Reaction and reusable clip | Capture, preview, editable captions, permitted export composition, download/share fallback. | At least one real iOS and Android device produce a usable clip; private reading remains available; no automatic publication or unauthorized message inclusion. |
| 4. Small public pilot readiness | Feature flag, limits/block handling, export/delete/cleanup, policy-owner review, tested recovery. | Product/data choices are resolved and the minimum text-plus-own-reaction flow is reliable. |

Groups, paired remote-reaction montages, live calls, social OAuth/autoposting, searchable
stranger matching, a new account system, and a whole-site redesign are later work.
The first releasable goal includes own-reaction export, even though the text exchange
is built first.

Test the meaningful boundaries: confirmation return paths; anonymous and third-user
access; sealed-content absence from network/status/export before Ready; concurrent
seals versus cancellation; duplicate retries; token reuse; denied camera; interrupted
capture; share cancellation; keyboard/screen-reader use; account deletion and cleanup.
Use the existing rustles_platform.tests project for service/API coverage and add
component/browser coverage suited to the new UI. Gherkin files here are specifications,
not claims of executable passing tests.

## Data proposal and open decisions

See the [inventory proposal](data-inventory-and-privacy-impact.md#secretbox-proposal-2026-11-21).
No data handling changes in this documentation commit. Before launch, the owner must
review disclosures and retention; the agent has not finalized privacy or terms text.

Working retention defaults for discussion: unready invitations/boxes expire after
7 days, Ready content after 30 days, temporary rendering assets within 24 hours.
Expiry must delete content/assets via a cleanup job; merely hiding a row is insufficient.
Backup retention and deletion propagation still need a concrete policy.

Resolve before implementing the relevant slice:
- Confirm the working name, 1:1 scope, peer-confirmation step, limits, and opening modes.
- Accept server-managed encryption or require E2EE.
- Select caption/render processing and hosting, including where audio is processed.
- Confirm deletion/export semantics, retention/backup policy, and review the existing
  16+ registration rule for this feature without silently changing it.
- Decide whether coarse aggregate pilot counters are needed. Do not add third-party
  tracking or call a share-sheet action a confirmed post.

## Technical references

Consulted 2026-11-21:
- [Blazor client services and prerendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-10.0).
- [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).
- [getUserMedia](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia).
- [MediaRecorder MIME detection](https://developer.mozilla.org/en-US/docs/Web/API/MediaRecorder/isTypeSupported_static).
- [File-share feature detection](https://developer.mozilla.org/en-US/docs/Web/API/Navigator/canShare).
