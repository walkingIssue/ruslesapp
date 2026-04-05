# Rustles Agent Workflow

This repo uses agents to remove memory-heavy documentation chores from the normal coding loop.

## Core rule

Documentation work should be driven by the git diff, not by what the developer happens to remember at the end of the day.

## When to run docs closeout

Run the docs closeout workflow at meaningful milestones:

- when opening or updating a pull request
- when finishing a ticket or feature slice
- before merging to `main`

Do not treat every local coding session as a documentation checkpoint. That creates noise, churn, and wasted tokens.

## What the agent may draft

- internal architecture documentation updates
- changelog drafts
- privacy and data-inventory proposals
- review summaries and follow-up checklists

## What the agent must not do alone

- publish or finalize privacy-policy text
- publish or finalize terms text
- invent undocumented behavior
- write the final public devlog prose from the code diff

## Public devlog rule

The public devlog should remain human-authored.

The agent may help by generating:

- title ideas
- header suggestions
- reflection prompts
- a lightweight outline

The agent must not draft the final devlog body from the diff or from internal implementation details.

## Privacy rule

Any change that may affect personal data handling must be surfaced as a proposal for human review.

This includes changes to:

- fields or tables
- cookies
- auth claims
- logs
- external vendors or services
- retention behavior
- messaging or notification behavior

The living inventory is [docs/data-inventory-and-privacy-impact.md](./docs/data-inventory-and-privacy-impact.md).

## Changelog rule

The changelog may be drafted from the diff, but it should stay factual, concise, and safe to publish to a public website. Avoid leaking internal architecture details, secrets, or implementation-sensitive notes that do not belong in a public-facing release note.

## Publishing rule

Only reviewed content should land in:

- `docs/changelog`
- `docs/devlog`

Merges to `main` that touch those paths trigger the website publication flow.
