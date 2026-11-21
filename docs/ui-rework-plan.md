# Rustles UI rework: conversation room and unified theme

Status: implementation proposal, 2026-11-21.
Baseline: walkingIssue/ruslesapp main at 93bb8a7c4c6ed16108b648918f16521bb7a1922f.
Scope: the Rustles platform, not Atlas. Source review and route inventory only;
the live application and every individual account interaction have not been tested.
This commit changes documentation only.

## Product direction agreed in conversation

The entire chat viewport is a shared room. Messages occupy irregular positions,
arriving from the sides and bottom, rather than a feed, bulletin board, card grid,
or channel workspace. In each sender's own client, their new message always
enters from the bottom above the composer. Other clients may place it differently.
Send order remains canonical underneath the spatial presentation.

Fresh reactions sustain a message's display lifetime, increase its visual
prominence up to a cap, and draw it inward and upward. Without fresh activity,
old messages gradually leave the current view. The viewport should clear naturally.

Retain marine navy and the golden leaf. Light and dark are modes of one theme.
Account screens can remain conventional and compact; chat carries the distinctive
spatial behavior. Future nearby rooms may blend into the view, but the first
release continues using the existing municipality authorization boundary.

## Proposed behavior to validate in a motion prototype

- Keep lifetime, size, and position as separate values. A large historical reaction
  total must not grant permanent screen occupancy.
- Give every new message a minimum readable dwell period. Fresh eligible reactions
  add bounded time and prominence; diminishing returns prevent indefinite dominance.
- Prototype defaults, not approved production values: 45-second initial dwell,
  8 seconds per eligible reaction, 90-second maximum remaining dwell, 3-minute
  maximum continuous appearance, and a short 2-second exit fade. Compare slower
  settings as well; long messages need additional reading time.
- Start with four to six visible messages on a narrow phone. Keep readable body
  text and large tap targets; overflow waits or moves to Earlier rather than
  shrinking text into illegibility. Tune using realistic arrival bursts.
- Count an actor's eligible reaction once for lifetime credit per message in the
  initial prototype. Emoji toggling and replayed events cannot farm time. Actual
  backend enforcement is required before public rollout.
- Do not make full-opacity text gradually unreadable throughout its lifetime.
  Keep it legible while active; fade briefly at departure. Focused content stays
  readable until the user leaves it.
- Tapping, keyboard focus, or replying holds that conversation still locally.
  A compact focused thread view supplies chronological context and reply targeting.
  Incoming activity queues without moving the user's current target.
- Replies appear near their parent when there is room; a promoted reply retains
  a small parent excerpt or connection. A reply to an absent parent includes a
  context affordance, rather than a dangling line.
- Each message retains its place label when multiple rooms are eventually blended.
  A composer always names its destination. A reply inherits its parent's room.
- Fading is presentation expiry, not deletion. Earlier opens an ordered history
  within the existing retention and authorization rules. Longer/searchable history
  is additional backend work, not something the current catchup API guarantees.
- On return from a hidden tab or reconnect, reconcile a fresh snapshot. Do not
  replay a backlog of entrance animations or revive expired messages by accident.
- Use a stable accessible reading order independent of visual coordinates.
  Offer reduced motion and an ordered reading mode. Screen readers should not
  announce every drift or continually repeat reaction counts.

## Entire user-facing surface inventory

Use the active platform projects below, not the older root Components scaffold.
All paths are repository-relative. Inventory was checked against the main tree;
existence is not proof that a route is currently reachable.

Client base: rustles_platform/rustles_platform.Client/
Server base: rustles_platform/rustles_platform/

| Surface | Existing files / area | Rework |
| --- | --- | --- |
| Application shell | Client Layout/MainLayout.razor, NavMenu.razor, Routes.razor; server Components/App.razor | One compact header/menu, responsive safe areas, accessible disclosure, theme and render-mode rules. |
| Entry and chat | Client Pages/Home.razor, Municipality/Chat.razor, ChatMessageBubble.razor | Remove promotional/diagnostic rail and oversized metadata; introduce room presentation, focused thread and ordered history. |
| Chat states/actions | Chat.razor and Municipality/TrustedTesterSwitch.razor | Location request/denial, queue/retry, catchup, empty, reconnect, send failure, replies, reactions, pinning and moderator controls. Preserve authorization and actionable recovery. |
| Sign-in | Server Components/Account/Pages/Login.razor, LoginWith2fa.razor, LoginWithRecoveryCode.razor, ExternalLogin.razor | Consistent narrow forms, passkey and provider availability, retained destination, explicit errors. |
| Registration and verification | Register.razor, RegisterConfirmation.razor, ConfirmEmail.razor, ConfirmEmailChange.razor, ResendEmailConfirmation.razor | Matching forms and result screens, permanent-username explanation, existing age/policy acknowledgements, clear continuation. |
| Password recovery | ForgotPassword.razor, ForgotPasswordConfirmation.razor, ResetPassword.razor, ResetPasswordConfirmation.razor, InvalidPasswordReset.razor | Shared recovery layout, expiry/retry paths, preserve account-enumeration protections. |
| Access failures | AccessDenied.razor, InvalidUser.razor, Lockout.razor | Plain explanations with appropriate next actions; same theme. |
| Profile and email | Account/Pages/Manage/Index.razor, Email.razor | Distinguish public PermanentUsername from sign-in email; decide whether the template phone field has a product purpose. Do not silently delete stored data. |
| Password settings | Manage/ChangePassword.razor, SetPassword.razor | Shared field, validation and success patterns. |
| Security settings | Manage/TwoFactorAuthentication.razor, EnableAuthenticator.razor, Disable2fa.razor, ResetAuthenticator.razor, GenerateRecoveryCodes.razor, Passkeys.razor, RenamePasskey.razor, ExternalLogins.razor | Group under Security; retain all enrollment, recovery, confirmation and destructive-action flows. |
| Account data | Manage/PersonalData.razor, DeletePersonalData.razor | Separate download from destructive deletion; retain password/antiforgery behavior. |
| Shared account elements | Account/Shared/ManageLayout.razor, ManageNavMenu.razor, StatusMessage.razor, ExternalLoginPicker.razor, PasskeySubmit.razor, ShowRecoveryCodes.razor | Shared compact navigation, notices, controls and accessible feedback. |
| Policies and system pages | Client Pages/Privacy.razor, Terms.razor, NotFound.razor; server Components/Pages/Error.razor and cookie notice in App.razor | Comfortable reading widths, theme-compatible notices/errors, recovery links; no automatic legal-copy rewrite. |
| Remaining templates | Client Pages/Counter.razor, Weather.razor, Auth.razor | Inspect routing and dependencies, then remove unused demos or intentionally retain protected test surfaces. |
| SecretBox | Proposed in PR #1, not implemented | Reuse the shell, account styles and components; preserve its separate private exchange flow. |

## Shared design system

Define semantic tokens for canvas, text, secondary text, accent, borders, focus,
danger and spacing in the platform's shared CSS. Marine dark canvas / warm light
canvas; gold as brand and selection accent. Ensure measured contrast in both modes.
Use one body type family, modest headings, compact form widths, restrained corners.
Replace default Bootstrap appearance consistently without removing Identity behavior.

Components: button, labelled input, field error, status notice, confirmation dialog,
menu, settings row and page header. Put motion duration and reduced-motion behavior
in shared rules. Replace technical UI copy with user actions; keep diagnostics in
developer tooling. Theme preference storage is a proposed change to document.

## Delivery sequence

| Phase | Deliverable | Exit gate |
| --- | --- | --- |
| 0. Inventory and baseline | Exercise every reachable route/state above; capture narrow-phone and desktop baselines, identify unused templates and existing defects. | Every user surface has an owner and keep/rework/remove decision. |
| 1. Motion prototype | Isolated room simulation with own-bottom entry, side arrivals, reaction boost, expiry, replies and focus; adjustable timings and traffic. | User can read, react and reply comfortably on a vertical screen during quiet and burst traffic; motion direction approved. |
| 2. Theme and shell | Shared tokens/components, light/dark, accessible menu, compact navigation and consistent account layout. | Both modes work at narrow widths and enlarged text; no mixed white Bootstrap controls on dark screens. |
| 3. Chat integration | Bind presentation to existing message IDs, timestamps, reactions and reply IDs behind a feature flag. | Reconnect, duplicates, failures and moderation produce correct state; rollback returns to current chat. |
| 4. Account and utility sweep | All registration, login, recovery, security, data, policy, error and empty states; remove verified unused demos. | Full route/state matrix completed with keyboard, touch and theme checks; auth/data behavior preserved. |
| 5. Multi-room readiness and pilot | Explicit room identity/destination contract, performance tuning, usability pilot. | Current municipality behavior remains secure; actual multi-room release waits for backend access/subscription support. |

Start phase 1 before committing to the final chat component architecture.
Phase 2 can establish the form/theme foundations while the motion behavior is tuned.
SecretBox implementation remains a separate feature; its PR does not block this
prototype or the shared theme work.

## Technical integration approach

Keep network/domain state separate from spatial presentation. Proposed new client
components: ConversationRoom, FloatingMessage, FocusedThread, EarlierMessages,
RoomComposer. Proposed services: RoomPresentationState and a layout/motion adapter.
These names are proposals, not existing files.

The server owns IDs, send order, timestamps, membership, moderation and eligible
reaction events. The client owns position, animation, local focus and viewport
capacity. Inspect rustles.contracts and the current reaction payloads before
implementation: aggregate totals alone may not support deduplication, reaction
freshness and lifetime credit. Add versioned event IDs/timestamps or server-computed
eligibility only if needed. Never infer fresh reactions simply by replaying catchup.

Use measured text bounds, collision avoidance and a reserved composer/keyboard zone.
Batch updates and animate transforms/opacity without re-running the entire Blazor
tree every frame. Implement browser interop only after checking the existing build.
Place browser-only setup after interactive rendering; preserve static Identity pages.

Treat reactions and server pinning as different mechanisms. Do not silently replace
existing pinned-message semantics with the room's prominence score. Define their
relationship before live integration.

Account continuation currently needs attention: registration passes ReturnUrl toward
email confirmation, but confirmation offers no corresponding continuation.
Identity.Name can represent an email; do not use it as a public display label.

## Open decisions / implementation backlog

These are plan IDs, not GitHub issue numbers.

| ID | Priority | Unresolved item | Proposed next step |
| --- | --- | --- | --- |
| UI-01 | P0 | Dwell, fade, growth and maximum lifetime | Tune phase-1 controls with reading tests; numbers above are provisional. |
| UI-02 | P0 | High-traffic admission and newcomer fairness | Bounded viewport plus minimum exposure; test bursts and avoid popular-message starvation. |
| UI-03 | P0 | Reaction freshness, repeated votes and self-reactions | Inspect contracts/store; prototype unique-actor credit, no self-credit; verify server enforcement. |
| UI-04 | P0 | Thread behavior when a root fades or reply becomes dominant | Preserve context in focused thread; avoid moving a reading target. |
| UI-05 | P0 | Asynchronous return/history | Specify what a returning user sees and verify pagination/retention; never rely on animations as the only access path. |
| UI-06 | P0 | Motion accessibility and mobile keyboard | Reduced-motion/ordered mode; focus hold; validate phone safe areas and orientation. |
| UI-07 | P1 | Pinning versus reaction prominence | Preserve pins in a small explicit affordance until semantics are settled. |
| UI-08 | P1 | Account templates and continuation gaps | Complete route matrix; fix confirmation return path and public-name rendering. |
| UI-09 | P1 | New multi-room scope and composer destination | Design room labels now; defer blended access until backend authorization exists. |
| UI-10 | P1 | Theme defaults and profile phone field | Follow system theme initially; confirm phone's purpose before removing its UI. |
| UI-11 | P1 | Performance/low-end devices and long content | Measure layout cost, collision handling, memory and frame stability with real phones. |
| UI-12 | P1 | Docs-closeout configuration | Existing PR check failed; earlier logs show missing OPENAI_API_KEY repository secret. Do not insert credentials in source. |

## Validation and rollout

Prototype deterministic scenarios: quiet room, burst arrivals, long message,
reaction surge, repeated reaction, parent expiry, focus while boosting, own-send
failure, reconnect with duplicate events, background/resume, deletion/moderation,
and narrowed viewport with keyboard visible. Verify that faded content remains
reachable only within permitted history and that new users can understand the room.

Integration checks: registration/confirmation/login including 2FA, password reset,
passkey lifecycle, profile/email updates, download and deletion. Check screen-reader
order, reduced motion, keyboard controls, contrast and 200% text zoom. Use service
tests for lifetime/event reconciliation and browser tests for actual interaction;
avoid tests that merely assert CSS class names.

Roll out behind a flag with the ordered reading view available. Compare completion
of read/react/reply tasks and user feedback before expanding. New analytics or
tracking is not authorized by this plan.

## Repository status and documentation closeout

Checked 2026-11-21 before this plan commit: no open GitHub issue tickets in ruslesapp.
Draft PR #1 (SecretBox specification) remains open and unmerged. Its
generate-docs-closeout check remains failed. The previously inspected run failed
because OPENAI_API_KEY was absent. UI backlog above is not yet filed as issue tickets.

Manual diff closeout: this change adds a plan only; no runtime, schema or collection
change. Potential future impacts include theme preference storage, reaction-event
eligibility metadata, history behavior and multi-room subscriptions. Record concrete
changes in docs/data-inventory-and-privacy-impact.md during implementation and
surface policy changes as proposals for human review. No public devlog or release
note is needed for this planning change. No application tests were run for this doc.
