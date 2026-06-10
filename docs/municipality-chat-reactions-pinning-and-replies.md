# Municipality Chat Reactions, Pinning, Replies, and Moderation

Status: implementation note for the platform MVP.

Last updated: 2026-06-05

## Summary

Municipality chat messages are now ID-based and persisted in the application database.

The message ID is the stable anchor for:

- emoji reactions
- reaction count updates
- pinned-message scoring
- replies that visually link back to another message
- moderation actions
- personal data export and account deletion behavior

## Stored model

`MunicipalityChatMessage` stores:

- message ID
- municipality name
- sender user ID
- display name at send time
- message body
- sent timestamp
- optional reply target message ID

`MunicipalityMessageReaction` stores one reaction event per user action:

- reaction ID
- message ID
- reacting user ID
- emoji string
- reaction timestamp

Repeated use of the same emoji creates multiple reaction events. The client receives grouped counts by emoji.

`MunicipalityUserBan` stores municipality-scoped moderation bans:

- ban ID
- municipality name
- banned user ID
- moderator user ID
- optional reason
- creation timestamp
- optional expiration timestamp

Message deletion is soft deletion. Deleted messages keep their database row, deletion timestamp, moderator ID, and optional reason, but are excluded from catchup, pinned-message scoring, and new reactions.

## Catchup window

The server returns the latest 20 persisted messages for the municipality catchup window, ordered chronologically for display.

Hydration also includes the current pinned messages in the same `ConnectMunipAck` response. Clients should not poll the pinned-message API during initial hydration. After hydration, pinned-message updates continue through the SignalR `PinnedMessagesUpdated` client method.

The client can only hydrate municipality chat once location access has resolved a municipality or a trusted-tester municipality override has produced an equivalent connection acknowledgement. The moderation controls live inside the hydrated chat state, so they are not shown before the client has a room context.

## Pinned-message scoring

Pinned messages are recalculated after each reaction. A message becomes eligible once it has at least two reactions.

The current score is reaction velocity:

```text
reaction count / seconds between first and latest reaction
```

The two messages with the highest velocity are sent to clients as pinned messages. Ties are resolved by latest reaction time.

This intentionally favors fast interaction curves. A smaller number of reactions in a much shorter time can outrank a larger but slower reaction pattern.

## Reply links

Replies carry the target message ID plus a display-name and preview snapshot. Clients render this as a visible link back to the referenced message context.

If the referenced message is later deleted as part of account deletion or moderation, the database nulls the reply pointer so deletion is not blocked and clients do not render stale reply links.

## Moderation

Trusted testers can moderate from municipality chat without changing the normal user requirement for sending messages. Normal users can send messages unless they are banned in the current municipality.

Current moderation actions:

- delete a message
- ban the author of a message in that municipality

Message deletion broadcasts `MessageDeleted` to connected clients and refreshes pinned messages for the room. Bans are enforced server-side before message send and emoji reaction handling.

The trusted-tester room override and moderation controls are separate capabilities. The role exposes extra UI and API access, but the server still treats ordinary authenticated users as eligible chat participants unless a municipality ban applies.
