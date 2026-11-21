# SecretBox interaction specification

Status: proposed behavior, not shipped. Date: 2026-11-21.
Pairs with [secret-exchange.feature](secret-exchange.feature).
See the [integration plan](../../docs/secretbox-integration-plan.md) for code locations,
technical boundaries, and decisions still open.

## Navigation and authentication

An authenticated user opens the existing Rustles burger menu and selects **SecretBox**
directly below **Chat**. The destination is /secrets. Desktop navigation exposes the
same item and order. The feature title is SecretBox, with Rustles still visible.

A direct invitation opens an explanation: "You have been invited to exchange secrets.
Both people seal a message before either can read." Sign in and Create a Rustles account
reuse the existing account pages. Preserve a validated local return destination
through email confirmation and login; never accept an external return destination.
After successful authentication, return to the invitation, not the municipality chat.

The landing page does not join automatically. The user chooses Request to join.
The creator sees the joining account's safe display name and chooses Confirm person
or Reject. Confirming identity is required before either secret becomes available.
Rejected or rotated invitations cannot later join using the old token.

## Screens and states

| State | Visible content | Primary action and recovery |
| --- | --- | --- |
| Empty dashboard | Short explanation and no boxes | New secret exchange. |
| Loading | Compact progress placeholder | Retry on failure; never display an empty list as if loading had succeeded. |
| Composer | Plain-text input, character count, fairness rule | Seal my secret; draft remains editable until server receipt. |
| Seal pending | Disabled submit and progress | Reconcile an ambiguous timeout; keep the draft until success is known. |
| Waiting for person | Sealed receipt, copy invitation link, expiry | Copy invite; rotate or cancel. |
| Peer awaiting confirmation | Joining account's display name, no secret text | Confirm person or Reject. |
| Waiting for contribution | Who has sealed, expiry, no preview | Wait or cancel while the box is Waiting. |
| Ready, manual opening | "Both secrets are sealed" | Read privately or Record and reveal. |
| Ready, automatic opening selected | Previously chosen opening preference | Open only in a foreground active session; otherwise await an explicit action on return. |
| Private reveal | Recipient's message and opened state | Close; recording later is a replay. |
| Camera preparation | Camera preview, recording controls, message still hidden | Start recording, then Reveal; denied access offers private reading. |
| Recording | Clear recording indicator, elapsed time, secret only after reveal | Stop; release camera/microphone tracks on finish or navigation. |
| Export preview | Selected clip, editable captions, visible included elements | Download or Share after required participant approval. |
| Cancelled, expired, deleted | Clear reason with no remaining content preview | Start a new exchange. |
| Offline/session expired | Explain failed action without pretending it succeeded | Reconnect/sign in and re-fetch authoritative state. |
| Not a participant | Generic unavailable page | Return to SecretBox; disclose no participants or content. |

Use one main heading and one primary action per stage. Keep developer concepts such
as SignalR, queueing, encryption keys, and storage internals out of ordinary UI copy.
Explain the actual privacy promise in plain language: sealed until both contribute;
private to participants in the app; server-managed encryption is not E2EE.

## Compose, seal, and recovery

- Proposed maximum: 2,000 characters; reject whitespace-only messages and show an
  inline error before sending. Enforce the same rule on the server.
- Label the action **Seal my secret**, not Send. Explain that normal editing ends
  after sealing. There is no ordinary chat transcript or free-form reply composer.
- A seal receipt identifies the accepted operation. Repeated clicks/retries must not
  produce a different contribution or overwrite the first one.
- On failure, keep the draft in memory while the page stays open. Warn before leaving
  with an unsent draft or recording; do not promise recovery after a reload.
- Copy-link success is shown only after clipboard success; provide selectable text
  as a fallback. Invite previews and notifications never include secret text.
- Counts and waiting states come from the server. A client-side timer cannot unlock a box.

## Opening and recording

Default to manual opening so someone can prepare their reaction. Optional "Open when
ready" is a text-only preference, selected knowingly; it must not fetch/reveal while
the tab is backgrounded. No camera permission is requested until Record and reveal
is chosen. Camera refusal does not block the underlying exchange.

For a recorded reveal, the recorder must be running before the server open request.
If the request fails, keep the message closed and offer retry. If the response succeeds
but recording fails, do not claim the first reaction was saved. Subsequent openings
are labelled as already opened; another recording is a replay.

Proposed clip cap: 60 seconds with an explicit Stop control. Stop and release tracks
when the user exits. Mobile backgrounding/interruption must reconcile message state
independently of whether the video survived.

## Sharing and permissions

Recording, saving, including the peer's material, and publishing are separate choices.
Default exports contain only the current user's selected reaction and generic Rustles
branding. A recipient reading a secret is not the author's permission to publish it.

If another person's message, display name, voice, or reaction is included, obtain
their approval of the exact export version. Preview visual text, audio, and captions:
a reaction-only layout can still disclose a secret spoken aloud. Exclude disallowed
material or allow a redacted version when approval is absent. Do not suggest that
the platform can prevent an external screenshot or recall an already exported copy.

An ordinary clip ends with a generic "Try SecretBox on Rustles" destination. Never
include a private invitation or box-access URL. Export generates a real downloadable
file; captions must be present in that file, not just overlaid in the web preview.
Offer the device share flow where supported and download otherwise. Cancellation
returns to the preview without a "posted" or "shared successfully" claim.

## Accessibility and mobile acceptance

- Navigation toggle is a labelled button with expanded state, keyboard operation,
  Escape handling, and focus return; active feature is identifiable.
- Status changes have appropriate live announcements; sealed/ready states do not
  depend only on color. Focus moves predictably to a new screen's heading.
- Composer, reveal controls, and video controls are usable at narrow mobile widths,
  with the on-screen keyboard, landscape orientation, and enlarged text.
- A camera, microphone, geolocation permission, or persistent real-time connection is not a prerequisite
  to reading an eligible secret privately.
- No secret is present in hidden DOM, page metadata, or fetched status data before reveal.

## Deliberately unresolved

The name, exact limits, retention defaults, automatic-opening option, and confirmation
step are working product choices. Group behavior, paired video montages, and automatic
social posting are outside the first implementation. No whole-site redesign is required.
