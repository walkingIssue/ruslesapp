# Georesolution HotCache and Tile Batching Design

This note documents the current rewrite direction for the georesolution pipeline in `rustles.core`.

It is not a statement that the rewrite is finished. It is a technical explanation of why the new structure is being shaped this way, which ownership boundaries it is trying to create, and which tradeoffs are intentional.

This note should be read together with:

- [ADR 0001: Move Geolocation Requests to Async Processing and Push Delivery](../adr/0001-async-geolocation-delivery.md)
- [ADR 0002: Add Spatial Bucketing, In-Flight Dedupe, and Bounded Reader Workers for Georesolution](../adr/0002-spatial-batching-and-cache-for-georesolution.md)
- [Georesolution Rewrite Implementation Plan](georesolution-rewrite-implementation-plan.md)

## Why this rewrite exists

The earlier queue/batch shape mixed too many responsibilities together:

- request submission
- coordinate transpose and validation
- user-request succession
- batching
- compute ownership
- completion

The rewrite is trying to separate those concerns without making request submission expensive.

Two constraints drive almost every decision here:

1. Request threads need to stay cheap.
2. Tile batching and compute state need one clear owner.

That leads to the current split:

- [`PositionResolveQue`](../rustles.core/Systems/ResolutionQue/PositionResolveQue.cs) handles ingress and validation.
- [`Batcher`](../rustles.core/Systems/ResolutionQue/Batcher.cs) mutates cheap hot state and invalidation state.
- [`Batched`](../rustles.core/Systems/ResolutionQue/Batched.cs) is the background owner that materializes compute-ready tile buckets.

## Relation to ADR 0001

ADR 0001 says geolocation should become asynchronous and stop tying HTTP request lifetime directly to resolution lifetime.

The current rewrite reflects that by making `PositionResolveQue` a submission boundary, not a compute boundary.

The intended flow is:

1. Client sends coordinates and a new `requestId`.
2. Coordinates are transposed into raster space.
3. Invalid coordinates are rejected immediately.
4. Valid requests are normalized into a `BatchRequest`.
5. The request is inserted into hot batching state.
6. The actual tile promotion and compute happen later in background services.
7. Completion will eventually be pushed back through the async delivery path rather than keeping the original HTTP request alive.

The important architectural point is that the request layer should not wait for raster work.

## Relation to ADR 0002

ADR 0002 says the pipeline should become spatially aware:

- exact work should be deduped
- nearby work should be bucketed by tile
- readers should stay bounded
- completion should remain separate from raster logic

The current rewrite does not yet implement the whole finished pipeline, but it is already shaping state around those requirements:

- a user-owned hot cache for the latest request
- tile-local compute buckets
- explicit invalidation state
- tile-local integer addressing

The key design choice is that tile buckets are not built directly on every incoming request. They are built by a background owner from a snapshot of hot state.

## The core design idea

The rewrite intentionally allows the fast ingress state to lag behind reality for a short time.

That sounds odd at first, but it is the central performance decision.

Instead of trying to eagerly and precisely rewrite every structure on every incoming request, the system uses:

- a cheap mutable ingress surface (`HotCache`)
- lazy invalidation (`Dequed`, `DequeAllClient`)
- a background promotion step (`TileHotCacheValues`)

This means:

- request threads do not need to walk live tile buckets
- disconnects do not need to tear through compute structures immediately
- tile-local compute state has one main owner

The cost is that some state remains briefly stale until the flusher or completion stage reconciles it.

That tradeoff is intentional.

## Current pipeline shape

### 1. `PositionResolveQue`: ingress boundary

[`PositionResolveQue`](../rustles.core/Systems/ResolutionQue/PositionResolveQue.cs) exists to do the work that must happen before batching:

- accept client coordinates
- transpose them into raster space
- validate the transformed coordinates against the raster bounds
- mutate a normalized `BatchRequest`
- pass that request to the batcher

This class should stay small because it sits on the hot path of request submission.

It is not supposed to own tile logic, compute logic, or completion logic.

### 2. `Batcher`: cheap mutation of hot state

[`Batcher`](../rustles.core/Systems/ResolutionQue/Batcher.cs) is intentionally trying to avoid turning every incoming request into direct compute-bucket surgery.

Its job is closer to:

- record the latest user-owned request in hot state
- mark older work as invalid when needed
- handle explicit dequeue/disconnect signals cheaply

This is why it depends on `ITileBatcher<TileKey>` but does not itself build out the full compute tile contents.

The reason is concurrency ownership:

- if request threads mutate `ComputeIds` directly
- and the background compute-prep path also mutates `ComputeIds`
- the batching structure becomes much harder to reason about

The rewrite is pushing toward a model where request threads mostly touch the cheap ingress structures, and the background flusher owns promotion into real tile batches.

### 3. `Batched`: the hotcache flusher and tile materializer

[`Batched`](../rustles.core/Systems/ResolutionQue/Batched.cs) is the most important structural piece in the rewrite.

It is both:

- the shared state object behind the batching system
- the background owner that periodically flushes hot ingress state into compute-ready tile buckets

Its `TileHotCacheValues(...)` method is the ownership boundary where:

- a snapshot of `HotCache` is taken
- disconnect and dequeue markers are checked
- requests are removed from hot state
- requests are promoted into `ComputeIds`

This means the system does not try to keep `ComputeIds` perfectly current in real time from request threads. Instead, it allows `HotCache` to act as a staging area and promotes requests into spatial buckets on a background cadence.

That is the main reason the rewrite currently feels different from a normal concurrent queue design: it is aiming for staged ownership, not immediate structural consistency.

## State structures and why they exist

### `HotCache`

Defined through [`ITileBatcher`](../rustles.core/Systems/ResolutionQue/Abstractions/ITileBatcher.cs) as:

- `ConcurrentDictionary<Guid, HotcacheValue>`

Current meaning:

- key: `UserId`
- value: latest request id and latest transformed raster cell for that user

Why it is keyed by `UserId`:

- the server cannot rely on the client to send a previous request id back for cleanup
- the current request flow is interested in "latest wins" semantics per user
- disconnect handling is naturally user-scoped

Why it only keeps the latest request:

- this is the cheapest useful ingress state
- request succession should happen before work fans out into tile-local compute state
- if a newer user request arrives before the previous one is flushed for compute, the older hot request should not survive as independently active work

### `ComputeIds`

Defined as:

- `ConcurrentDictionary<TileKey, SortedDictionary<int, RequestCell>>`

This is the compute-ready spatial batching structure.

Why it exists separately from `HotCache`:

- `HotCache` is optimized for cheap request-thread mutation
- `ComputeIds` is optimized for tile-local intermittent batching

Why the per-tile collection is a `SortedDictionary<int, RequestCell>`:

- the `int` key is intended to be a cheap tile-local lookup key
- the structure gives deterministic ordered traversal
- the intent is to support cheap local lookup behavior during intermittent tile batching rather than repeatedly scanning arbitrary collections

In other words, the tile bucket is not just a bag of requests. It is being shaped as a tile-local indexed compute structure.

### `Dequed`

Defined as:

- `ConcurrentDictionary<DequeKey, DequedValue>`

This is the targeted invalidation structure.

It exists because once work has escaped the hot path, the system still needs a way to say:

- this request/cell pairing is no longer valid
- do not deliver or continue this result if it shows up later

The current rewrite is using this as a lazy invalidation path instead of trying to eagerly rewrite all live batching structures immediately.

### `DequeAllClient`

Defined as:

- `ConcurrentStack<Guid>`

Current meaning:

- user-scoped disconnect invalidation

This is the blunt instrument compared to `Dequed`.

Why it exists separately:

- a disconnect should be cheap to record
- a disconnect means no future result from that user should be sent back
- it is acceptable to resolve some already-started work and suppress completion later rather than walk all compute structures immediately

That is an explicit design tradeoff in favor of cheaper disconnect handling.

## Why stale state is allowed on purpose

One of the hardest parts of the rewrite is accepting that not all state needs to become immediately perfect on every request.

The rewrite intentionally allows:

- stale entries to remain in `HotCache` briefly
- dequeue/disconnect markers to exist without immediate full cleanup
- compute cleanup to happen when work is promoted or completed

The reason is throughput and lock pressure.

If every user movement or disconnect eagerly walked:

- the hot cache
- every tile bucket
- every pending request collection

then the request path would become much heavier and more contended.

The current design is choosing to spend that cleanup cost later at clearer ownership boundaries.

## The `preflight` idea and the current ownership problem

An important unresolved part of the design is the earlier idea of a `preflight` marker on hot state.

The intended meaning of that marker was:

- a request is still in `HotCache`
- but it has already been logically passed beyond the pure ingress stage
- a newer request for the same user should therefore invalidate it through dequeue semantics rather than treating it as untouched fresh hot state

That idea matters because the rewrite wants to keep older already-batched requests in `HotCache` until the real compute flush happens, rather than removing them too aggressively.

The current pressure point is that `HotCache` stores record-like values, which makes "mark this exact existing value as preflighted" more awkward than a mutable state object would be.

That means the design still has an open ownership question:

- how to cheaply represent "already logically batched" inside `HotCache`
- without forcing expensive lookups or heavy mutation on request threads

This is not a cosmetic issue. It sits directly at the seam between:

- cheap request ingestion
- lazy invalidation
- single-owner promotion into compute state

## Tile identity and why `256 x 256` matters

The current tile model is based on `256 x 256` raster-aligned tiles.

This is not arbitrary.

The rewrite is deliberately trying to align batching identity with raster-friendly tile boundaries so that:

- tile-local lookups stay cheap
- compute ownership is spatially coherent
- later raster reads can be shaped around the same spatial partitioning

`TileKey` currently normalizes transformed raster coordinates by dividing by `256`, which means the key is intended to represent a coarse parent tile rather than an exact cell.

The same `256` assumption also shows up in tile-local id calculations for `RequestCell` and `DequedValue`.

This is the beginning of a tile-local coordinate system:

- parent tile identity
- cell-local position within that tile
- stable local lookup id

That structure is what allows `ComputeIds` to behave as more than just a plain batch list.

## Why the completion shape is still open

The completion side of the rewrite is not fully settled yet, and that is okay.

What is already clear is the intended behavior:

- if the user disconnected, do not send the result
- if the request was invalidated by newer work, suppress the result
- if the work still matters, push it back through the async delivery path

The exact class shape for that logic is still open, but the responsibilities are already visible:

- correlate the result back to the original request and user
- check disconnect invalidation
- check targeted dequeue invalidation
- remove resolved invalidation markers where appropriate
- either push the result or swallow it

The rewrite is explicitly favoring correctness of delivery semantics over eager structural cleanup.

That is why it can be acceptable to resolve work that will never be sent back, as long as the completion path suppresses it correctly.

## Concurrency model the rewrite is aiming for

The current rewrite is not trying to make every structure equally concurrent in every phase.

Instead, it is moving toward staged concurrency:

### Request threads

Should stay cheap and mostly mutate:

- `HotCache`
- `Dequed`
- `DequeAllClient`

### Background flusher

Owns:

- snapshotting hot state
- promotion into `ComputeIds`
- first-pass dequeue reconciliation during promotion

### Compute phase

Should consume tile-local structures that are already materially prepared for raster work.

### Completion phase

Should decide whether work is still deliverable and either push it or suppress it.

This staged model is the architectural reason the rewrite is not pursuing eager global consistency.

## Why this structure is reasonable even though it looks odd

At first glance, keeping stale state around and cleaning it up later can feel cooked.

But in this specific problem, the shape is defensible because:

- georesolution requests can arrive frequently for the same user
- disconnects need to stay cheap
- raster work is not naturally async
- tile-local compute state should have clearer ownership than request ingress state

The rewrite is trying to optimize for:

- cheap ingress
- user-level succession
- tile-level intermittent batching
- delayed but explicit invalidation

That is a more honest fit for the workload than pretending the request path can eagerly maintain perfect spatial compute structures at all times.

## Known open questions

The following points are still intentionally open:

- how `preflight` state should be represented without making hot-state mutation too expensive
- the final shape of the completion sink or completion stage
- whether completion should null-return, drop silently, or emit an explicit suppressed-result state internally
- the exact flushing cadence and thresholds
- how `BulkDispatcher` should consume `ComputeIds` once the batching side settles

These are implementation details still in motion, but they do not invalidate the current architectural direction.

## Short summary

The current rewrite is built around one main idea:

Use a cheap user-owned hot cache for ingress, allow invalidation to be lazy, and let a background owner promote requests into tile-local compute structures.

That gives the design:

- short request-path work
- user-level latest-wins semantics
- cheap disconnect handling
- spatial compute state with clearer ownership

It also explains why the rewrite looks unusual in places: it is deliberately accepting some temporary staleness in exchange for a cleaner and cheaper batching boundary.
