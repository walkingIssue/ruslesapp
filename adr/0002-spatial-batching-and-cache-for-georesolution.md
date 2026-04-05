# ADR 0002: Add Spatial Bucketing, In-Flight Dedupe, and Bounded Reader Workers for Georesolution

- Status: Proposed
- Date: 2026-04-05

## Context

The current geolocation implementation still has two major technical weaknesses, even if request delivery is moved to an asynchronous model:

- raster reads are effectively synchronous because the underlying library does not provide a meaningful async implementation
- requests are not yet grouped by spatial locality before reading from the GeoTIFF

At the moment, the system behaves more like a generic batch queue than a spatially-aware resolver. That means repeated or nearby lookups may still cause redundant reads, poor cache reuse, and unpredictable latency under burst traffic.

This matters because the georesolution path is likely to become one of the most compute-sensitive parts of Rustles. If we do not shape it deliberately, the web host can still pay too much for redundant reads even after HTTP request lifetime is decoupled from processing lifetime.

## Decision

The georesolution pipeline should move to a spatially-aware execution model with four explicit concepts:

1. exact-request dedupe by cell key
2. spatial bucketing by tile or read window
3. bounded reader workers
4. completion through a dedicated completion sink

The intent is to reuse raster reads whenever requests fall into the same spatial region, while also avoiding unbounded parallelism against a synchronous file-read layer.

## Proposed shape

### 1. Normalize the request into spatial keys

For every incoming geolocation request:

- convert coordinates into raster coordinates
- derive an exact `cellKey` for the requested cell
- derive a coarser `tileKey` for the larger raster read window

The tile boundary should follow raster-friendly edges, ideally aligned with the underlying GeoTIFF layout or another fixed grid, not arbitrary geographic boxes.

### 2. Dedupe exact in-flight work

Before enqueueing new work:

- check whether the exact `cellKey` is already cached
- check whether the exact `cellKey` is already in flight

If a cached result exists, return it immediately.

If the cell is already in flight, attach the new `requestId` to the existing pending work instead of scheduling another read.

### 3. Bucket work by spatial tile

If the request is not already cached or in flight:

- place it into a pending bucket keyed by `tileKey`
- retain the `requestId`, cell information, and any completion metadata required later

Requests inside a tile bucket should be processed in a fair order. This proposal does not recommend LIFO as the default scheduling rule, because LIFO can starve older requests during bursts.

### 4. Use bounded reader workers

A scheduler should assign tile buckets to a small, bounded set of reader workers.

The worker count should start as a fixed limit, not as a dynamic estimate derived from guessed read times. The file-read layer is synchronous, so uncontrolled concurrency is more likely to create worse contention than better throughput.

### 5. Read once per spatial region

Each reader worker should:

- load the raster window for the assigned tile
- resolve all pending cell requests within that tile
- publish completions to the completion sink
- update caches and in-flight registries

This turns one file read into many request completions whenever there is spatial overlap.

## Caching model

The proposal uses two different reuse layers:

### Exact-result cache

- keyed by `cellKey`
- used for exact repeat lookups
- bounded in size
- evicted with an LRU-style policy or another age-and-size-aware policy

### In-flight registry

- keyed by `cellKey`
- tracks requests already being resolved
- allows multiple callers to share one underlying read
- cleared when the result is completed or the work fails

Tile buckets are not the same thing as the cache. They are a scheduling structure, not a long-term retention structure.

## Completion sink responsibilities

The completion sink should remain simple.

It should:

- accept resolved results keyed by `requestId`
- fan out the result to every waiting caller attached to that work
- update the result store or notification path
- remove the work from in-flight tracking

It should not contain raster logic or bucket-selection logic.

## Why this is the preferred direction

- It acknowledges that the real raster read is synchronous and treats it honestly.
- It improves locality by grouping nearby requests into one read window.
- It avoids duplicate work for exact repeated requests.
- It gives us a controlled place to apply concurrency limits.
- It keeps caching, batching, reading, and completion as separate responsibilities.

## Consequences

This will reshape the current georesolution internals significantly.

The likely impact areas are:

- request normalization and key generation
- queue and batch orchestration
- scheduling strategy
- completion fan-out
- cache ownership and eviction
- instrumentation and timing

This does not necessarily force another public API contract change by itself, but it does require a deep internal refactor of the current batching and resolution path.

## Tradeoffs

### Gains

- better cache hit potential
- fewer redundant reads
- tighter control over synchronous file-read concurrency
- better throughput under clustered traffic
- cleaner separation between scheduling and resolution

### Costs

- more internal complexity
- more bookkeeping for keys, buckets, and in-flight requests
- more tuning work around tile size, worker count, and cache size
- more difficult debugging than a naive direct-read implementation

## What this ADR does not decide

- exact tile size
- exact cache size and eviction tuning
- whether a tile window itself should also be cached or only final cell results
- whether the first version should persist results beyond process memory
- whether progress notifications are useful enough to expose to clients

## Suggested mapping to the current codebase

This is only a directional mapping, not a final class design:

- `PositionResolveQue`: submission boundary or facade
- `Batcher`: tile-bucket manager
- `BulkDispatcher`: bounded worker scheduler
- raster/GDAL layer: actual tile or window reader
- `CompletionSink`: final result completion and fan-out

## Follow-up work

Before implementation, we should sketch one end-to-end internal flow:

1. derive spatial keys
2. check cache and in-flight registry
3. enqueue into tile bucket
4. assign worker
5. read tile
6. resolve all pending cells in that tile
7. complete all attached `requestId` values

That flow should be agreed before changing the batching layer, because this proposal affects the geometry of the whole read pipeline.
