# Account Authentication

## Purpose

Help a user enter the product without confusion and recover from common authentication failures.

## Main UI States

- Default state:
  Show a clear path to create an account and sign in.
  Also explain what the product is, what municipality chat means, and why the user is being asked to create an account.
- Loading state:
  Disable the active submit action while the request is in flight.
- Registration acknowledgement state:
  After valid registration details are entered, show the age and privacy acknowledgement step before the account is created.
- Success state:
  Move the user out of the auth screen and into the authenticated app flow.
- Failure state:
  Keep the user on the current auth screen and explain the failure.

## Validation And Feedback

- Validate that required fields are present before submission.
- Validate that the email address is in a valid format.
- Validate that the username satisfies the chosen username rules.
- Registration should block obviously invalid passwords according to the chosen password rules.
- On failed registration or sign-in, the user should get a plain-language explanation.
- Preserve the email field after a failed attempt.
- Preserve the username field after a failed registration attempt.
- Do not expose whether a password-recovery target exists beyond what the product intentionally allows.
- If the user rejects the registration acknowledgement step or says they are under 16, do not create the account.

## Interaction Notes

- Registration and sign-in can be separate screens or a clear two-mode auth surface.
- Password recovery should be reachable from the sign-in screen.
- The flow does not need to decide the exact reset mechanism yet, but the spec assumes the user gets a real path to regain access.
- If the implementation begins with email-based recovery, the UI should still stay generic enough that the product wording can evolve later.
- The auth surface should not assume the product idea is self-evident.
- A short plain-language explainer should tell the user that Rustles connects them to local municipality chat based on where they are.
- That explainer should make account creation feel purposeful rather than like generic signup friction.
- Registration should clearly communicate that the chosen username is permanent and cannot be changed later.
- The final pre-account step should separate concerns clearly:
  - age eligibility
  - privacy/public-chat disclosure
- Rejecting that step should send the user back out of account creation rather than half-creating an account.
- If acceptance of that step is required for account creation, the system should record the minimum metadata needed to prove that the user completed it.

## Open Questions

- What password policy is strong enough for MVP without being annoying?
- Will password recovery be email-link based, code based, or temporarily simplified for early internal testing?
