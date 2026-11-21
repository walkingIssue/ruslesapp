# SecretBox frontend integration review

Date: 2026-11-21. Repository: walkingIssue/ruslesapp.
Reviewed main revision: 93bb8a7c4c6ed16108b648918f16521bb7a1922f.
Method: static source review of the platform navigation, layout, routing, chat UI,
account flow, host registration, and related persistence/export hooks.
The application was not run; visual behavior, browser compatibility, and unpublished
local changes are not verified. Findings below distinguish observed code from risks
to a proposed integration.

## Recommendation

Add **SecretBox** immediately after **Chat** in the existing platform navigation.
Use the existing Rustles account and a dedicated /secrets feature area. The same
entry should appear in the mobile burger menu and desktop navigation. Do not require
Atlas, municipality membership, or GPS to exchange secrets.

The relevant application is rustles_platform/rustles_platform plus its .Client
project. The root Components directory and rustles.application projects contain
other UI scaffolds; adding a link there would not modify this platform's menu.

## Findings that affect the first implementation

| Priority | Evidence at the reviewed revision | Consequence and proposed action |
| --- | --- | --- |
| P1 | [Register](../rustles_platform/rustles_platform/Components/Account/Pages/Register.razor#L124) puts returnUrl in its confirmation link; [ConfirmEmail](../rustles_platform/rustles_platform/Components/Account/Pages/ConfirmEmail.razor#L16) reads only UserId and Code and displays a result. [RegisterConfirmation](../rustles_platform/rustles_platform/Components/Account/Pages/RegisterConfirmation.razor) receives ReturnUrl but offers no continuation. | A new invitee has no explicit route back from confirmation to the box. Preserve a validated local destination through registration, confirmation, sign-in, and two-factor paths. Add a visible continuation action; do not silently redeem an invitation on GET. |
| P1 | [App](../rustles_platform/rustles_platform/Components/App.razor#L48) disables prerendering only for municipality/chat; [client Program](../rustles_platform/rustles_platform.Client/Program.cs) registers browser-side services separately. | A new /secrets page that injects a client-only API or recorder could fail during default prerendering. Explicitly use the existing non-prerendered WebAssembly mode for this feature initially; keep account pages' static rendering intact. This is an integration risk, not an observed SecretBox failure. |
| P1 | [Home](../rustles_platform/rustles_platform.Client/Pages/Home.razor) redirects to chat; [Chat](../rustles_platform/rustles_platform.Client/Pages/Municipality/Chat.razor#L290) starts geolocation on first render. | An invitation must return directly to /secrets, not detour through the home/chat bootstrap. Denied GPS must not prevent access to SecretBox. Existing chat behavior can remain. |
| P2 | [MainLayout](../rustles_platform/rustles_platform.Client/Layout/MainLayout.razor#L4) special-cases the chat URL; its [CSS](../rustles_platform/rustles_platform.Client/Layout/MainLayout.razor.css) changes the same menu from a sidebar to a floating burger. | A new page otherwise gets a different shell. Make an explicit, small layout decision for Chat and SecretBox; reserve space for navigation and avoid spreading route-string checks across feature components. Do not copy the entire chat layout into SecretBox. |
| P2 | [NavMenu](../rustles_platform/rustles_platform.Client/Layout/NavMenu.razor#L11) uses a styled checkbox and a container-wide onclick with document.querySelector. It has no explicit button expanded state or focus-return behavior. | Replace this interaction with a component-owned disclosure button, aria-expanded/aria-controls, Escape handling, and focus return. Preserve keyboard access, active links, and logout's antiforgery form. Validate the result with a browser rather than assuming CSS alone solves it. |
| P2 | [NavMenu](../rustles_platform/rustles_platform.Client/Layout/NavMenu.razor#L35) displays Identity.Name; [registration](../rustles_platform/rustles_platform/Components/Account/Pages/Register.razor#L105) sets the Identity username to email while storing PermanentUsername separately. [UserDisplayName](../rustles_platform/rustles_platform/Services/UserDisplayName.cs) also has email/username fallbacks. | Do not reuse Identity.Name or that fallback chain for public clips, invitation previews, or peer labels. Use an explicitly safe permanent display name; default to a neutral label for legacy accounts. Bind all authorization to immutable account IDs, never display names. |
| P2 | [Chat](../rustles_platform/rustles_platform.Client/Pages/Municipality/Chat.razor) combines markup, connection state, moderation, composition, and diagnostics in one large file. Its copy mentions queueing, SignalR, and backend wiring. Its [mobile CSS](../rustles_platform/rustles_platform.Client/Pages/Municipality/Chat.razor.css#L822) stacks the explanatory rail rather than removing it. | Reuse small visual conventions, not the chat page or its orchestration. SecretBox needs one primary action per state and plain feedback. Verify that the actual action stays easy to reach on a small screen. |
| P2 | [Chat sending](../rustles_platform/rustles_platform.Client/Pages/Municipality/Chat.razor#L576) clears its draft after a service call, with a separate chat-error callback elsewhere. | Do not carry that interaction into irreversible sealing. Clear a secret draft only after a persisted seal receipt; reconcile an ambiguous timeout using an idempotency key. This is a design precaution, not a claim that existing chat messages are lost. |

## Existing foundations to reuse

- [Routes](../rustles_platform/rustles_platform.Client/Routes.razor): authorized routes and focus-on-navigation.
- [Login](../rustles_platform/rustles_platform/Components/Account/Pages/Login.razor): existing account and ReturnUrl flow.
- [Host Program](../rustles_platform/rustles_platform/Program.cs): Identity cookies, confirmed accounts, authorization, antiforgery middleware, dependency injection.
- [UsersDataContext](../rustles.Infrastructure/UserDataContext.cs): Identity-backed persistence.
- [Identity endpoints](../rustles_platform/rustles_platform/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs): personal-data export and examples of explicit antiforgery validation.
- Existing scoped CSS and Bootstrap assets; a new design framework is unnecessary for this slice.

## Documentation drift

The [architecture overview](architecture-overview.md) describes the earlier five-project
skeleton and says hosts are not wired to infrastructure. The reviewed platform host
does reference and configure infrastructure and Identity. Use the code-backed paths
above for this feature. The inventory also contains older placeholder-model claims;
the SecretBox proposal must not be presented as existing data handling.

## Boundaries

This is a focused integration review, not a complete frontend, security, or accessibility
audit. No runtime defects were reproduced and no UI changes are part of this commit.
The next work is described in the [integration plan](secretbox-integration-plan.md).
