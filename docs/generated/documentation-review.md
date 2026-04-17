# Internal Docs Review

## docs/architecture-rustles-core.md
- Action: `update`
- Reason: Core georesolution responsibilities and batching/state ownership changed substantially, but the architecture docs listed in the index were not updated in this diff.
- Notes: Add a short factual section summarizing the current `ResolutionQue` split: ingress, batching state, flusher/channel handoff, and completion filtering.

## docs/automation-and-publishing.md
- Action: `update`
- Reason: The repo now contains many more internal drafts plus at least one publishable devlog status change.
- Notes: Clarify review expectations for internal drafts, privacy/legal drafts, and when a docs-only session should or should not generate public-content follow-up.

## docs/geodata-raster-pipeline-technical.md
- Action: `update`
- Reason: The new file contains local absolute filesystem paths that are fragile and may leak workstation-specific context.
- Notes: Convert to repo-relative references where possible, or explicitly mark external/local references as non-portable research notes.

## docs/data-inventory-and-privacy-impact.md
- Action: `update`
- Reason: The file was updated, but the changes are policy-direction assertions and should be kept aligned with implementation status.
- Notes: Add a short 'implementation status / assumptions' note if some items remain planned rather than coded.
