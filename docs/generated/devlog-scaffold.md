# Devlog Scaffold

Final devlog prose should be written by a human.

## Required Frontmatter Starter

```yaml
---
title: "Improving the docs handoff workflow for review and publishing"
date: "2026-04-05"
slug: "improving-the-docs-handoff-workflow-for-review-and-publishing"
summary: "Replace with a short summary."
draft: true
---
```

Optional fields are intentionally omitted so a human can add them only if needed.

Suggested title: Improving the docs handoff workflow for review and publishing

## Header Suggestions

- What changed in the docs automation flow
- How review artifacts are now surfaced for humans
- Workflow guardrails to prevent noisy reruns
- Follow-up checks before enabling in wider use

## Reflection Prompts

- Why was it useful to materialize review artifacts into the repository instead of leaving them only as workflow artifacts?
- What problem does ignoring `docs/generated` solve in the automation chain?
- When should a dedicated token be used for PR creation, and what safeguards matter most?
- What parts of this workflow still require deliberate human review before publishing?

## Caution Notes

Keep the eventual public devlog high-level. Do not include token names beyond what is already intentionally documented, permission details, internal workflow mechanics, or any sensitive repository/security configuration.
