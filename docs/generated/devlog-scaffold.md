# Devlog Scaffold

Final devlog prose should be written by a human.

## Required Frontmatter Starter

```yaml
---
title: "Documenting the geospatial pipeline and tightening MVP privacy assumptions"
date: "2026-04-17"
slug: "documenting-the-geospatial-pipeline-and-tightening-mvp-privacy-assumptions"
summary: "Replace with a short summary."
draft: true
---
```

Optional fields are intentionally omitted so a human can add them only if needed.

Suggested title: Documenting the geospatial pipeline and tightening MVP privacy assumptions

## Header Suggestions

- What we clarified in the geospatial docs
- How the resolution pipeline responsibilities are being separated
- What changed in the MVP specs and privacy direction
- What still needs verification before release

## Reflection Prompts

- Which of the new documents describe current verified behavior versus intended direction?
- What did the new tests and refactor make easier to reason about in the resolution pipeline?
- Which privacy assumptions still need implementation proof before they can be treated as settled?
- What follow-up docs should condense this large internal write-up into a smaller maintainable architecture summary?

## Caution Notes

If a public devlog is written, keep it high-level. Avoid local filesystem paths, internal-only implementation seams, and detailed privacy/legal claims that have not yet been verified in production configuration. The eventual published file will still need Astro frontmatter fields: `title`, `date`, `slug`, `summary`, and `draft`.
