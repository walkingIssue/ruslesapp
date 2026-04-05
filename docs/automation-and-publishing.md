# Rustles Automation And Publishing Flow

This workflow exists to make docs, changelog, and privacy review part of shipping work without turning them into a memory burden.

## Goals

- keep internal docs tied to the actual diff
- keep the privacy inventory alive as the code changes
- keep changelog work lightweight
- keep the public devlog human-authored
- keep website publication as a light downstream handoff instead of another manual chore
- avoid recursive docs automation loops

## Workflow shape

### 1. Pull requests get analysis, not auto-commits

The `docs-closeout` workflow runs on pull requests, manual dispatch, and `main` pushes that are not public-docs-only changes.

On pull requests it produces:

- internal documentation review notes
- a changelog draft
- a privacy and data-inventory proposal
- a devlog scaffold with title ideas, headers, and reflection prompts only

That output is uploaded as an artifact for review.

### 2. Main pushes can open a draft docs PR

When code lands on `main` and the change is not only `docs/devlog` or `docs/changelog`, `docs-closeout` also runs on the push.

After a successful `main` run, `open-public-docs-pr` downloads the `docs-closeout` artifact, materializes draft public Markdown files, and opens or updates a PR back to `main`.

That PR contains:

- a draft changelog entry when one was recommended
- a draft devlog starter file with required Astro frontmatter
- generated review files under `docs/generated`

The generated branch is `docs-closeout/public-docs`.

Because `docs-closeout` ignores public-docs-only changes, that generated PR does not trigger a recursive docs loop.

If you need to bootstrap the flow from an already completed `Docs closeout` run, `open-public-docs-pr` also supports manual dispatch with a `run_id`.

### 3. Human review stays in the loop

Before merge:

- rewrite the devlog in your own voice
- review the changelog draft
- review the generated internal and privacy notes in `docs/generated`
- adjust `draft`, `summary`, and any optional frontmatter fields as needed

### 4. Merging public docs triggers the site publish handoff

When reviewed content lands in:

- `docs/changelog`
- `docs/devlog`

the `notify-rustles-web` workflow dispatches the downstream website publish workflow.

Deployment details for the public site are intentionally not duplicated here. This repo only owns the content handoff.

## Loop prevention

The automation avoids stateful tracking by using event and path boundaries:

- `docs-closeout` ignores `docs/generated/**`
- `docs-closeout` ignores `docs/devlog/**` and `docs/changelog/**`
- the draft-docs PR only contains public docs files
- `notify-rustles-web` only reacts to public docs landing on `main`

That keeps the flow linear:

1. code lands on `main`
2. docs PR is opened
3. docs PR is reviewed and merged
4. website publish is dispatched

## Human-review boundaries

### Safe to draft automatically

- internal docs notes
- changelog drafts
- data-inventory proposals
- devlog header suggestions
- draft public-doc starter files
- generated review files under `docs/generated`

### Must remain human-reviewed

- privacy-policy changes
- terms or moderation-policy language
- final devlog prose
- anything public-facing that could expose too much internal implementation detail

## Required repository configuration

Set these GitHub items in this repo before enabling the workflows:

- secret: `OPENAI_API_KEY`
- variable: `OPENAI_MODEL`
  - recommended starting value: `gpt-5-mini`
- variable: `OPENAI_REASONING_EFFORT`
  - recommended starting value: `low`
- secret: `RUSTLES_WEB_REPO_TOKEN`
- secret: `DOCS_PR_TOKEN`
  - optional but recommended if you want to scope PR creation to a dedicated token instead of enabling repo-wide Actions PR creation
- variable: `RUSTLES_WEB_REPOSITORY`
  - format: `owner/repo`
- variable: `RUSTLES_APP_REPOSITORY`
  - if omitted, the workflow uses `github.repository`

## Public content contract

Public entries should live in:

```text
docs/devlog/*.md
docs/changelog/*.md
```

Create them from the templates in `docs/templates` or refine the generated drafts from the docs PR so the website-facing frontmatter stays consistent.

## Suggested working ritual

1. Merge a meaningful code slice to `main`.
2. Review the generated docs PR.
3. Rewrite the devlog in your own voice.
4. Review privacy/data-inventory notes from the artifact if relevant.
5. Merge the docs PR.
6. Let the downstream website publication step handle the public surface.
