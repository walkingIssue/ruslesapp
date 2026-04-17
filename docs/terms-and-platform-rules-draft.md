# Rustles Terms and Platform Rules Draft

Status: draft for internal review. This document is based on the current MVP application flow and is not legal advice. It should be reviewed against the final implementation and, if possible, checked by a lawyer before publication.

Last updated: 2026-04-12

## 1. Purpose of this draft

This draft is meant to do three things:

- explain what Rustles is in practical terms
- set user expectations for public municipality chat
- define the first version of the platform rules for MVP

It is intentionally written in plain language. It is not meant to be fake-lawyer theater.

## 2. What Rustles is

Rustles is a location-linked public chat product.

In the current MVP, a user creates an account, confirms age and privacy acknowledgement, is connected to a municipality context based on device location, and can then participate in live public municipality chat.

Rustles is currently intended for users who are at least `16` years old.

## 3. Public-chat warning

Rustles is a public communication space within the relevant municipality chat context.

That means:

- other users in the same municipality chat can read what you post
- other users may copy, screenshot, record, or otherwise keep what you share
- you should not post sensitive, private, or identifying information unless you are prepared for other people to retain it

Even if Rustles does not persist sent messages in the MVP flow, other users may still save what you share outside Rustles.

## 4. No guarantee of other users' behaviour

Rustles can set rules and remove or restrict users where the product allows, but Rustles cannot guarantee that other users will always behave lawfully, safely, or respectfully.

Users should treat public municipality chat as a space involving strangers and act carefully about what they choose to disclose.

## 5. Basic user rules

By using Rustles, users are expected to:

- be respectful and considerate toward other users
- avoid harassing, threatening, or intimidating other people
- avoid sharing private, identifying, or sensitive information about themselves or others
- avoid impersonating other people
- avoid spam, manipulation, or attempts to flood the platform
- avoid posting illegal content or using the service for illegal conduct

## 6. Prohibited conduct

The following should be treated as prohibited conduct in MVP and beyond:

- harassment
- threats
- hate or abuse targeted at people or groups
- doxxing or posting someone else's private information
- stalking or unwanted targeting
- impersonation
- spam or coordinated abuse
- illegal content or illegal activity using the service

## 7. Blocking and future moderation

The MVP is expected to include user blocking so a user can hide messages from another user in their own chat experience.

Blocking is a personal control, not a global moderation decision.

Rustles may later add stronger moderation tools such as reporting, review, removal, and account restrictions. This draft should be updated as those features are added.

## 8. Platform actions Rustles may take

If Rustles reasonably believes that a user is violating the platform rules, Rustles may, to the extent the product supports it:

- warn the user
- limit access
- remove access
- remove or restrict content visibility
- block or disable accounts

The exact enforcement tooling may be limited in the MVP, but the product direction should still be stated clearly.

## 9. Account and identity expectations

- Users are responsible for the information they provide during account creation.
- The chosen username is intended to be permanent in MVP.
- Users should keep their authentication details secure.

## 10. Age rule

Rustles is currently intended for users who are at least `16` years old.

If a user says they are under `16` during the registration acknowledgement step, account creation should not continue.

## 11. Service availability and limitations

Rustles is an early-stage product and may change, break, or become unavailable.

Rustles does not promise uninterrupted availability, permanent access, or permanent message visibility.

Because the MVP does not persist sent messages, users should not rely on the service as a permanent storage system for chat content.

## 12. Risk disclosure

Users should understand:

- this is a public platform
- they may be interacting with strangers
- what they post may be seen by other people in the relevant chat
- other people may retain what is posted even if Rustles does not

This risk disclosure should be reflected both in the registration acknowledgement step and in the published platform rules.

## 13. Liability and legal wording

This draft intentionally does **not** try to invent broad legal magic phrases like "Rustles is not responsible for anything that happens."

The correct final wording on liability, consumer-law limits, disclaimers, complaint handling, and enforcement rights should be reviewed before public launch if possible.

For now, the practical goal is:

- disclose the public-chat risk clearly
- tell users what conduct is prohibited
- reserve the right to restrict abusive use
- avoid pretending the platform can guarantee user safety or perfect moderation

## 14. Related documents

This draft should stay aligned with:

- [privacy-policy-draft.md](./privacy-policy-draft.md)
- [operational-data-policy-draft.md](./operational-data-policy-draft.md)
- [data-inventory-and-privacy-impact.md](./data-inventory-and-privacy-impact.md)
- the MVP spec files in [spec/mvp](../spec/mvp)
