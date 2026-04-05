# Internal Docs Review

## docs/automation-and-publishing.md
- Action: `update`
- Reason: Already updated in the diff to reflect new generated review files, workflow ignore rules, and optional PR token configuration.
- Notes: Human review should confirm the documented behavior matches the workflows and script changes.

## docs/data-inventory-and-privacy-impact.md
- Action: `none`
- Reason: No clear product-data handling change is shown in the diff.
- Notes: There is an automation/security configuration change involving workflow tokens, but this is not enough to claim a product privacy-policy update without human review.

## docs/generated/*
- Action: `create`
- Reason: The script now materializes review artifacts into `docs/generated` during the docs handoff flow.
- Notes: These are generated review files, not canonical long-lived documentation. Confirm repository expectations for generated content.
