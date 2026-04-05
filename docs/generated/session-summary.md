# Docs Closeout Summary

The diff extends the docs automation so review artifacts are copied into `docs/generated`, updates workflows to ignore that generated directory to avoid retriggering `docs-closeout`, adds an optional dedicated PR token fallback in the public-docs PR workflow, and updates the internal automation guide to document the new generated review files and token option.

## Human Actions

- Review whether `docs/generated/**` should be committed in docs PRs or remain purely generated output.
- Confirm the optional `DOCS_PR_TOKEN` secret should be added and scoped appropriately if used for PR creation.
- Review generated internal/privacy notes in `docs/generated` before merging any public-docs PR.
- Verify the automation guide remains aligned with actual workflow behavior after these changes.

## Generated Files

- `documentation-review.md`
- `changelog-draft.md`
- `privacy-review.md`
- `devlog-scaffold.md`
- `result.json`
