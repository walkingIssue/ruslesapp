# Municipality Chat Access

## Purpose

Make it obvious which municipality context the user is in and whether the user can meaningfully join the chat there.

## Main UI States

- Default state:
  Show the user's current municipality context or the need to resolve it.
- Loading state:
  Show that municipality chat access is being prepared.
- Success state:
  Show municipality identity and allow entry into the chat flow.
- Failure state:
  Explain why municipality chat is unavailable right now.

## Validation And Feedback

- Do not present the user as being inside a municipality chat when municipality resolution is unresolved.
- If a connected-count or similar indicator is shown, it should be treated as informative rather than authoritative.
- If location changes enough to change municipality context, the UI should stop implying the old municipality is still current.

## Interaction Notes

- The municipality name should be visually prominent enough that the user knows where they are posting.
- A lightweight active-user or connected-user count is appropriate for MVP if it is cheap to compute.
- If there is no meaningful activity, the UI should still make the municipality chat feel available rather than broken.
- The first entry experience should reinforce what is happening in plain language, for example that the user is now talking to locals in the current municipality.

## Open Questions

- Should the chat access view auto-enter the chat, or should the user click into it explicitly?
- What level of participation count is honest enough for MVP without introducing too much realtime complexity?
