# Municipality Messaging

## Purpose

Let municipality chat feel alive enough to be worth opening, without overcomplicating the first implementation.

## Main UI States

- Default state:
  Show recent messages and a clear message entry area.
- Loading state:
  Show that recent messages are being loaded.
- Success state:
  The user can read the timeline and submit a new message.
- Failure state:
  The user is told when messages could not be loaded or sent.
- Empty state:
  If there are no messages yet, the chat should feel new rather than broken.

## Validation And Feedback

- Reject empty messages.
- Reject messages that violate any chosen MVP length or content rules.
- Keep the draft visible if send fails for a transient reason.
- Confirm new messages through the timeline itself rather than a separate success message when possible.

## Interaction Notes

- The user should be able to tell whether a message is currently sending, sent, or failed.
- Realtime updates matter more than rich formatting for MVP.
- The first version does not need elaborate moderation UX, but it should not box the product into an impossible future.

## Open Questions

- How much recent history should be loaded by default?
- Should MVP show message timestamps on every message or only enough context to keep the timeline understandable?
