# Privacy Notice

## Purpose

Tell the user, in plain language, what MVP Rustles stores and what it deliberately does not store.

## Main UI States

- Default state:
  The privacy notice is readable and easy to find.
- Registration acknowledgement state:
  The privacy notice is available before account creation is finalized.
- Success state:
  The user can quickly understand the storage model without reading legalese.

## Validation And Feedback

- The notice should be written truthfully and match the actual MVP implementation.
- If the implementation changes, the notice must be updated with it.

## Interaction Notes

- The notice should explicitly state that Rustles stores the user's email address, chosen username, and password information for authentication.
- The notice should explicitly state that location data is not persisted.
- The notice should explicitly state that sent messages are not persisted.
- The notice should explicitly state that Rustles only uses strictly necessary authentication cookies.
- The notice should explicitly state that Rustles connects the user to a live chat reflecting the municipality of the device.
- The notice should explicitly state that messages are publicly displayed to other users in that chat.
- The notice should explicitly state that other users may still save, copy, or persist messages outside Rustles even if Rustles itself does not persist them.
- The tone should be plain-language and human-readable, not legal theater.
- If password handling is implemented as hashed credentials, the final notice should describe that truthfully instead of implying plaintext storage.
- The acknowledgement step can include a short plain-language warning about strangers, public conversation, and being careful about what is shared.
- The acknowledgement step should not pretend that plain-language caution text is a full Terms of Service if no real Terms exist yet.
- If acceptance is required before account creation, the implementation should record the minimum acknowledgement metadata needed for that requirement.

## Open Questions

- Where is the best MVP location for the privacy notice: auth flow, settings, footer, or some combination of those?
