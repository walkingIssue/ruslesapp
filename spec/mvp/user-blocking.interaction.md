# User Blocking

## Purpose

Let users protect their own chat experience without needing full moderation tooling before MVP exists.

## Main UI States

- Default state:
  A user can access a clear block action when another user's identity is visible.
- Success state:
  The blocked user stops appearing in the blocker's message flow.
- Failure state:
  The blocker is told if the block action could not be completed.

## Validation And Feedback

- A block action should be deliberate enough to avoid accidental blocks.
- The blocker should be able to understand that the block only changes their own view.
- Unblock should be available somewhere the user can realistically find again.

## Interaction Notes

- MVP does not need a full moderation dashboard to support blocking.
- The important behavior is that blocked users' messages are not rendered to the blocker.
- The blocked user does not need to be told that they were blocked.
- Blocking should be framed as a personal filtering/safety action, not a global removal action.
- The recovery path for a mistaken block should live in settings.

## Open Questions

- Should MVP support blocking directly from a message, from a user profile surface, or both?
- Do we need to hide historical messages from a newly blocked user immediately, or only prevent future display?
