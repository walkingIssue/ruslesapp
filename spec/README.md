# Rustles Spec

This directory holds living product behavior specs for Rustles.

The goal is to keep two things side by side:

- `.feature` files for behavior written in Gherkin
- `.interaction.md` files for UI, feedback, validation, and state notes

The split is deliberate.

Use Gherkin for:

- what the user does
- what the system should do
- what outcomes should be observable

Use interaction notes for:

- how the UI communicates success, failure, and loading
- which fields and states matter
- validation expectations
- small implementation-facing UX details that are too specific for Gherkin

## Directory Layout

- `templates/`: Starting points for new feature specs and interaction notes.
- `mvp/`: Current MVP scope and the feature areas we are actively building.

## Writing Rules

- Keep features focused on one product question.
- Keep scenarios concrete and user-observable.
- Avoid implementation details in Gherkin.
- Put UI specifics in the paired interaction note instead of overloading the `.feature` file.
- Prefer updating these files before or during implementation, not after memory has already faded.

## Working Agreement

For a new feature area:

1. Write or update the `.feature` file.
2. Add the paired `.interaction.md` file.
3. Build against the spec.
4. Tighten the spec when the product decision becomes clearer.

These specs are allowed to evolve. They should be honest about current intent, not fake-final documentation.
