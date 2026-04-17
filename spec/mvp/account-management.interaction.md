# Settings

## Purpose

Give the user a minimal but trustworthy place to view account state, manage credentials, and manage blocked users.

## Main UI States

- Default state:
  Show the settings actions that matter for MVP.
- Loading state:
  Show when account data or password-change requests are in progress.
- Success state:
  Confirm that the requested account change completed.
- Failure state:
  Explain why an account action failed and how the user can recover.

## Validation And Feedback

- Password changes should require enough information to avoid accidental lockout.
- Invalid account-management actions should fail clearly, not silently.
- If the username is displayed, it should also be clear that it is permanent and cannot be edited.
- Blocked-user management should be understandable enough that a user can undo a block without guessing where the control lives.

## Interaction Notes

- This screen can stay intentionally small for MVP.
- The goal is not a polished profile system. The goal is to avoid trapping the user once the account exists.
- Email and username are the main profile details that matter for MVP.
- Username should be presented as fixed account identity, not as something the user can casually rename.
- Settings is the right home for blocked-user management in MVP.
- A blocked-users list is enough for MVP as long as each blocked user can be unblocked from that surface.

## Open Questions

- Does MVP need editable display information beyond password management and blocked-user management?
