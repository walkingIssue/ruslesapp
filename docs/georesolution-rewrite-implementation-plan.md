# Georesolution Rewrite Implementation Plan

Status: internal implementation plan. This document translates ADR 0001 and ADR 0002 into a concrete refactor direction for `rustles.core`. It is intentionally practical and implementation-shaped, but it does not commit the codebase to exact method signatures yet.

Last updated: 2026-04-12

## 1. Why this rewrite exists

The current resolution pipeline is too tightly coupled to the older request-response model and does not yet reflect the two accepted architectural decisions:

- [ADR 0001](../adr/0001-async-geolocation-delivery.md): short-lived HTTP requests, async processing, and SignalR delivery
- [ADR 0002](../adr/0002-spatial-batching-and-cache-for-georesolution.md): exact-cell dedupe, tile bucketing, bounded workers, and completion fan-out

The goal is not to throw away every existing class. The goal is to preserve the useful shape where possible while changing the state model and scheduling behavior to match the ADRs.

## 2. Rewrite goals

### Goal A: Keep HTTP requests short

The web/API layer should:

1. validate input
2. transpose and bounds-check coordinates
3. assign a `requestId`
4. enqueue work
5. return immediately

The client should receive completion later through SignalR, not by holding the original HTTP request open.

### Goal B: Deduplicate work spatially

The core resolution path should:

- dedupe exact in-flight requests by `cellKey`
- group pending work by `tileKey`
- run bounded readers over ready tile buckets
- fan out one resolved result to all attached request IDs

### Goal C: Keep request succession cheap

When the same user submits a newer position request, the older one should become stale cheaply.

The invalidation path should not eagerly walk and mutate every shared collection. It should stay lazy and cheap, and stale work should be removed when a tile is actually claimed for execution.

## 3. Non-goals for this pass

This rewrite does not need to solve all future work at once.

Not part of the first pass:

- exact vector fallback near borders
- custom in-memory decompressed tile cache owned by Rustles
- perfect progress reporting
- durable message persistence
- permanent request-result history

## 4. Recommended responsibility split

## `PositionResolveQue`

Keep this as the submission boundary.

It should own:

- input acceptance from the application layer
- assigning `requestId`
- associating the request with `userId`
- calling transpose/bounds validation
- returning accepted/rejected status to the caller
- forwarding normalized requests into the batching layer

It should not own:

- tile selection logic
- raster reads
- SignalR push logic

## `Batcher`

Keep this as the orchestration layer for spatial batching.

It should own:

- deriving `cellKey`
- deriving `tileKey`
- checking exact-result cache
- checking in-flight dedupe by `cellKey`
- adding new work to the correct tile bucket
- request succession handling at the metadata level
- deciding when a tile is ready for execution

It should not own:

- direct GDAL tile reads
- SignalR push
- client-facing result delivery

## `Batched`

This should become a concurrent state store, not the place where executable work is created.

It should own:

- `latestByUser`
- `inFlightByCell`
- `pendingByTile`
- `dequed` or invalidation markers
- optional exact-result cache

It should not own:

- worker execution
- SignalR fan-out
- timer-based readiness decisions

Think of this less as "the batch" and more as the shared resolution state store.

## `IBatchFlusher`

Do not turn this into the SignalR/public completion surface.

Instead, use it as the readiness promotion boundary.

Its job should be:

- check pending buckets for age/threshold readiness
- promote ready tile work into the ready-work dispatch mechanism

It should answer the question:

- "which tile buckets are ready to run now?"

It should not answer:

- "which clients should be pushed now?"

## `BulkDispatcher`

Keep this as the bounded worker runner.

It should:

- consume ready tile work
- claim a tile bucket atomically
- apply lazy invalidation cleanup
- resolve the tile through the raster layer
- emit completed results to the completion/publication stage

## Completion stage

Whether this stays named `CompletionSink` or gets renamed, the responsibility still exists.

It should:

- correlate resolved cells back to attached `requestId`s
- filter out stale requests
- update exact-result cache if enabled
- hand results to the SignalR push layer
- remove completed entries from in-flight state

This stage is not removed by channels. Channels can replace the transport between stages, but not the completion responsibility itself.

## 5. The key model

These keys should be kept separate because they solve different problems.

### `requestId`

- correlates one client submission to one eventual completion
- should be an explicit generated ID
- should not be derived from `GetHashCode()`

### `userId`

- identifies the user who submitted the request
- used for "latest request wins" succession logic

### `cellKey`

- identifies one exact raster cell
- used for exact in-flight dedupe and result cache hits

Recommended shape:

- `rasterId`
- `resolution`
- `cellX`
- `cellY`

### `tileKey`

- identifies one raster-aligned spatial bucket
- used for batching and worker scheduling

Recommended shape:

- `rasterId`
- `resolution`
- `tileX`
- `tileY`

## 6. How to derive `cellKey` and `tileKey`

This is the important part of the rewrite.

The raster does **not** need to physically create new tiles for Rustles. The file already has a layout. Rustles should overlay a deterministic logical tile grid on top of raster-space coordinates.

### Step 1: transpose to raster coordinates

Given a client request:

- validate coordinate range
- transpose lon/lat into raster-space cell coordinates
- produce `(cellX, cellY)`

If the transposed position is outside the raster bounds, reject the request early.

### Step 2: build `cellKey`

Use the exact raster cell:

```text
cellKey = (rasterId, resolution, cellX, cellY)
```

That gives:

- exact dedupe
- exact result cache lookup
- exact fan-out target when many requests ask for the same cell

### Step 3: build `tileKey`

Choose a fixed tile size in cells, then derive:

```text
tileX = floor(cellX / tileWidthCells)
tileY = floor(cellY / tileHeightCells)
tileKey = (rasterId, resolution, tileX, tileY)
```

That is reproducible, deterministic, and independent of the individual request object.

## 7. Recommended tile size rule

Start by aligning Rustles tile buckets to the actual GeoTIFF block layout.

The current projected raster:

- [Municipality_5m_3006.tif](../rustles.core/Assets/Municipality_5m_3006.tif)

currently reports:

- raster size: `135195 x 311959`
- pixel size: `5m`
- block size: `256 x 256`

That means one raster block covers:

- `256 * 5m = 1280m`

So the first practical tile definition should be:

- `tileWidthCells = 256`
- `tileHeightCells = 256`

Why this is the right first step:

- it aligns with the file's actual internal layout
- it maximizes the chance that one logical tile maps cleanly to one GDAL block read
- it avoids inventing a second arbitrary tiling system before benchmarking proves a need

If later profiling shows a different logical tile size performs better, that can be changed behind the key factory.

## 8. Should tile-key derivation be its own dependency?

Probably yes, but keep it small.

A dedicated dependency makes sense if you want:

- testable deterministic key generation
- one place to encode raster version, resolution, and tile-size rules
- the option to change tile-size strategy later without contaminating the batcher

Suggested shape:

- `ISpatialKeyFactory`
  - `CellKey CreateCellKey(...)`
  - `TileKey CreateTileKey(...)`

This should be a tiny pure dependency, not a service with side effects.

If the implementation stays extremely small, this could also be a static helper. But if the rewrite is meant to last, a small injectable key factory is a cleaner seam.

## 9. Lazy dequeue / invalidation semantics

Keeping the cheap dequeue path is reasonable.

The intended semantics should be:

- disconnect, supersession, or cancellation should be cheap
- actual cleanup should happen when a tile is claimed for execution

That means `dequed` should become an invalidation log, not immediate structural removal from all shared state.

Recommended invalidation markers:

- `requestId`
- `userId`
- invalidation reason

Do not depend on object-reference identity as the long-term invalidation mechanism.

At tile-claim time:

1. inspect the tile bucket
2. remove requests invalidated by supersession or cancellation
3. if the tile empties out, skip execution

This preserves the cheap semantics you want without requiring eager mutation across all buckets.

## 10. Channels and batching

Channels should not replace batching.

They should be used only after batching has already decided a tile is ready.

Correct mental model:

- dictionaries form batches
- channels dispatch ready batches

That suggests two useful channels:

### Ready-work channel

- contains `tileKey` or a claimed tile-work object
- consumed by bounded worker runners

### Completed-work channel

- contains resolved tile or cell results
- consumed by the completion/publication layer

## 11. Caching strategy for the first rewrite

Use two layers only:

### App-level exact-result cache

- `cellKey -> municipality result`
- owned by Rustles
- bounded in size

### GDAL block cache

- let GDAL keep decompressed raster blocks hot
- do not build a custom decompressed tile cache in the first rewrite

Why:

- simpler ownership
- less duplicated caching
- lower memory-management complexity

If profiling later proves that app-owned tile caching is needed, add it as a second-phase optimization.

## 12. Recommended internal data contracts

These do not need to be exact names, but the rewrite should have explicit equivalents.

- `NormalizedGeoRequest`
  - `requestId`
  - `userId`
  - original client coordinates
  - transposed `cellX/cellY`
  - `cellKey`
  - `tileKey`

- `CellKey`
  - `rasterId`
  - `resolution`
  - `cellX`
  - `cellY`

- `TileKey`
  - `rasterId`
  - `resolution`
  - `tileX`
  - `tileY`

- `PendingCellWork`
  - `cellKey`
  - attached requests
  - current state

- `PendingTileBucket`
  - `tileKey`
  - pending cell work references
  - first enqueue time
  - current item count

- `InvalidationMarker`
  - `requestId`
  - `userId`
  - reason
  - timestamp

- `CompletedGeoResult`
  - `cellKey`
  - resolved municipality result
  - attached request IDs

## 13. Suggested rewrite order

### Phase 1: keying and state model

- add `CellKey`
- add `TileKey`
- add normalized request shape
- reshape `Batched` into a concurrent state store

### Phase 2: submission boundary

- update `PositionResolveQue`
- assign request IDs
- validate and transpose early
- return accepted/rejected result without waiting

### Phase 3: batching logic

- add exact in-flight dedupe by `cellKey`
- add `tileKey` bucket ownership
- add latest-request-per-user tracking
- preserve lazy invalidation semantics

### Phase 4: readiness promotion and worker dispatch

- use `IBatchFlusher` to promote ready tile buckets
- dispatch them through a ready-work channel
- make `BulkDispatcher` consume claimed tile work

### Phase 5: completion and push

- fan out resolved results by `requestId`
- ignore stale requests
- push current results through SignalR

### Phase 6: cache tightening and profiling

- add exact-result cache
- benchmark before considering any custom decompressed tile cache

## 14. Decisions locked in by this plan

- request correlation is explicit `requestId`, not `GetHashCode()`
- request succession is keyed by `userId`
- spatial dedupe is keyed by `cellKey`
- tile batching is keyed by `tileKey`
- first tile sizing follows actual GeoTIFF block size
- lazy dequeue/invalidation semantics are preserved
- channels are dispatch tools, not batching logic
- custom tile-memory caching is deferred

## 15. Immediate question answered

If the rewrite is currently blocked on "how do I structure the tile key?", the answer should be:

```text
tileWidthCells = 256
tileHeightCells = 256
tileX = floor(cellX / 256)
tileY = floor(cellY / 256)
tileKey = (rasterId, resolution, tileX, tileY)
```

and that logic should sit behind a small deterministic spatial key dependency rather than being scattered across the batcher.
