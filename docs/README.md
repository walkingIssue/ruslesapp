# Rustles Docs

These notes describe the current codebase, not the ideal future version.

- `architecture-overview.md`: Short map of the whole solution.
- `architecture-rustles-application.md`: MAUI host architecture.
- `architecture-rustles-application-shared.md`: Shared Blazor UI architecture.
- `architecture-rustles-application-web.md`: ASP.NET Core host architecture.
- `architecture-rustles-core.md`: Domain and geospatial core architecture.
- `architecture-rustles-infrastructure.md`: Data access and EF Core architecture.
- `automation-and-publishing.md`: Diff-driven docs workflow and public-content handoff.
- `broader-app-capabilities.md`: Product note on Rustles as a spatial runtime and place-bound capability system.
- `data-inventory-and-privacy-impact.md`: Living data inventory and privacy-change checklist.
- `dependency-graph-tooling.md`: Recommended tools and a practical graphing approach.
- `engineering-scorecard.md`: Short scoring pass on code quality and architecture.
- `geodata-raster-pipeline-technical.md`: Verified technical notes on the current GeoPackage to GeoTIFF pipeline.
- `geodata-projection-and-raster-design.md`: Projection, raster math, and the correctness vs compute tradeoff.
- `geospatial-product-stages.md`: Stage 1 municipality chat, stage 2 local rooms, stage 3 broader software context.
- `georesolution-hotcache-batching-design.md`: Technical note explaining the current hotcache, dequeue, and tile-materialization rewrite shape in `rustles.core`.
- `georesolution-responsibility-and-state-model.md`: Responsibility map, data flow, and state automata for the current georesolution rewrite.
- `georesolution-rewrite-implementation-plan.md`: Concrete rewrite plan for ADR 0001 and ADR 0002, including cell and tile key structure.
- `municipality-chat-reactions-pinning-and-replies.md`: Implementation note for persisted municipality chat, reactions, pinned-message hydration, replies, and trusted-tester moderation.
- `operational-data-policy-draft.md`: Internal draft describing current MVP storage, logging, and retention rules.
- `point-in-polygon-strategies.md`: Compares exact vector, raster, hybrid, and spatial-database lookup strategies.
- `privacy-policy-draft.md`: Public-facing privacy policy draft based on the current MVP application flow.
- `terms-and-platform-rules-draft.md`: Draft platform rules, public-chat risk disclosure, and early-stage terms direction.
- `benchmarks/municipality-raster-benchmark-2026-04-11.md`: Structured comparison of the old 4326 raster and the new 5m projected raster.
