# Rustles Automation And Publishing Flow

This workflow exists to make docs, changelog, and privacy review part of shipping work without turning them into a memory burden.

## Goals

- keep internal docs tied to the actual diff
- keep the privacy inventory alive as the code changes
- keep changelog work lightweight
- keep the public devlog human-authored
- keep website publication as a light downstream handoff instead of another manual chore

## Workflow shape

### 1. Work happens normally

During a feature or ticket, focus on the code. Do not stop at every local session just to document partial work.

### 2. Open or update a pull request

The `docs-closeout` workflow runs on pull requests and manual dispatch.

It reads the diff and produces:

- internal documentation review notes
- a changelog draft
- a privacy and data-inventory proposal
- a devlog scaffold with title ideas, headers, and reflection prompts only

### 3. Human review

Before merge:

- review the changelog draft
- review any privacy/data-inventory proposal
- write the final devlog prose in your own words if a devlog entry is needed

### 4. Merge reviewed docs to `main`

When reviewed content lands in:

- `docs/changelog`
- `docs/devlog`

the downstream website publishing workflow may be triggered.

Deployment details for the public site are intentionally not duplicated here. This repo only owns the content handoff.

## Human-review boundaries

### Safe to draft automatically

- internal docs notes
- changelog drafts
- data-inventory proposals
- devlog header suggestions

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

Create them from the templates in `docs/templates` so the website-facing frontmatter stays consistent without duplicating that contract in multiple places.

## Suggested working ritual

1. Finish a ticket or meaningful feature slice.
2. Open or update the PR.
3. Review the generated docs-closeout artifact.
4. Write or refine the public devlog entry in your own voice if one is needed.
5. Merge the reviewed docs.
6. Let the downstream website publication step handle the public surface.
