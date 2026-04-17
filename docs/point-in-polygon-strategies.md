# Point-in-Polygon Strategies for Municipality Resolution

## Purpose

This note describes the "point in polygon" problem in the context of Rustles and compares the main implementation strategies.

The two questions it focuses on are:

1. which approach is most likely to return correct municipality data
2. which approach is most efficient from a compute and operational point of view

## The problem being solved

At runtime, Rustles receives a point:

- longitude / latitude from a client

The system needs to answer:

- which municipality polygon contains this point

That is a classic point-in-polygon problem.

For Sweden municipality borders, the source dataset currently contains:

- `290` municipality features in the `kommun` layer

That is not a huge polygon count. The hard part is less about polygon count and more about:

- polygon complexity
- boundary correctness
- latency under repeated requests
- how much preprocessing is acceptable

## Main solution families

There are four practical families of solutions for this problem.

### 1. Direct vector point-in-polygon

This is the most literal approach:

1. keep the municipality polygons as vector geometry
2. transform the incoming point into the same CRS
3. test whether the point is inside each polygon, or a narrowed candidate set

Typical acceleration layers:

- polygon bounding-box filter first
- spatial index such as an R-tree
- prepared geometries

Advantages:

- highest geometric fidelity
- no rasterization loss
- easiest correctness story at borders
- easiest to reason about legally and mathematically because the source geometry remains authoritative

Disadvantages:

- more CPU per lookup than a simple raster cell read
- more memory pressure if many prepared geometries are kept hot
- implementation may be slower in pure application code if no proper spatial index is used

For only `290` municipalities, direct vector lookup is much more viable than it would be for datasets with hundreds of thousands of polygons.

## 2. Rasterized lookup grid

This is the current Rustles direction:

1. burn municipality codes into a raster
2. convert the client point to a raster cell
3. read one pixel
4. map the burned code back to a municipality

Advantages:

- extremely cheap lookup once the raster exists
- predictable per-request work
- easy to batch and cache by raster window
- no geometry traversal on the hot path

Disadvantages:

- correctness depends on raster resolution
- boundaries can be wrong or ambiguous
- coastlines, narrow corridors, and small islands are vulnerable to raster loss
- the preprocessing pipeline can become large, slow, and storage-heavy

This approach is best understood as a precomputed approximation of the vector truth.

## 3. Hybrid lookup

A hybrid approach combines both:

1. use a coarse raster or tile index to narrow the candidate set quickly
2. if the result is uncertain, near a boundary, or nodata, fall back to vector point-in-polygon

Advantages:

- much better correctness than pure raster
- much less CPU than checking all polygons every time
- can keep the hot path fast while preserving an exact fallback

Disadvantages:

- more implementation complexity
- two systems to maintain instead of one

This is often the most balanced design when both speed and correctness matter.

## 4. Database-backed spatial lookup

This means moving the point-in-polygon test into a spatial database such as PostGIS.

Advantages:

- strong spatial indexing
- well-tested geometry operations
- easy to reason about exact polygon semantics
- good if the system already depends on a spatial database

Disadvantages:

- adds database round-trips to the hot path
- pushes operational complexity into infrastructure
- overkill if the application otherwise does not need spatial SQL

For Rustles as it exists today, this is probably heavier than necessary.

## Compute-efficiency comparison

### Pure raster lookup

Once the raster exists, pure raster lookup is the cheapest hot-path option.

The work per request is basically:

- one coordinate transform or transpose
- one cell calculation
- one tiny raster read
- one municipality-code lookup

That is hard to beat on raw request-time CPU cost.

### Pure vector lookup

Pure vector lookup is more expensive per request, but that cost is often overestimated when the polygon count is only `290`.

With a reasonable spatial narrowing strategy, the system is not actually testing the point against "all of Sweden" in some absurd way every time. It is usually:

- index or bounds filter
- small candidate set
- exact geometry containment test

For a moderate request rate, that can still be completely acceptable.

### Hybrid lookup

Hybrid lookup often wins on total system cost rather than raw hot-path micro-cost.

Why:

- fast most of the time
- exact when needed
- can use a smaller raster than a pure-raster design would need

That often means:

- less disk
- less preprocessing time
- lower correctness risk

## Correctness comparison

### Pure vector lookup

Best correctness.

If the source geometry is authoritative and the point transform is correct, this is the exact answer.

### Pure raster lookup

Correctness is resolution-dependent.

A raster can only say:

- this cell belongs to municipality X
- this cell belongs to municipality Y
- or nodata

That means the answer at boundaries is always quantized to the grid.

If the raster cell is coarse enough, it can misclassify legitimate points near:

- coastlines
- municipal borders
- thin land connections
- islands

### Hybrid lookup

Almost always the best practical correctness story if exact vector fallback exists.

The raster does the cheap broad work. Vector geometry settles the edge cases.

## Is the current raster approach the most compute-efficient?

If the question is strictly:

- "what is the cheapest thing to do per request?"

then yes, a raster lookup is probably the cheapest hot-path computation.

But that is not the only compute question that matters.

The more honest question is:

- "what is the cheapest overall system that still returns trustworthy results?"

From that perspective, pure raster is not automatically the winner.

Why:

- generating and storing very large rasters has real preprocessing and storage cost
- higher-resolution rasters explode in size
- correctness pressure forces you toward ever-finer grids
- very fine grids create batching, I/O, and deployment headaches

That can erase part of the hot-path win.

## Rustles-specific recommendation

For Sweden municipalities, the current geometry count is small enough that Rustles does not obviously need a pure raster-only strategy.

The most likely good options are:

### Option A: exact vector lookup first

Use the original municipality polygons in `EPSG:3006` and perform exact point-in-polygon lookups with spatial narrowing.

Why this is attractive:

- the dataset is small
- correctness is strongest
- implementation is conceptually simpler than maintaining a huge raster asset pipeline

### Option B: projected raster plus exact fallback

Keep a raster, but make it:

- projected
- meter-based
- smaller and more intentionally sized

Then fall back to exact vector lookup for:

- nodata
- cells close to boundaries
- validation mode

Why this is attractive:

- still very fast
- less brittle than pure raster
- aligns better with the current `R_200M / R_400M / R_800M` naming

## What I would not recommend

I would not recommend going deeper into a pure `EPSG:4326` high-resolution raster-only design without first proving that:

- exact vector lookup is too slow
- a hybrid design is still too slow

Otherwise there is a real risk of paying the cost of:

- huge files
- approximate edges
- awkward projection semantics

without actually needing that tradeoff.

## Bottom line

For raw per-request compute, raster lookup is the cheapest.

For total system design, pure raster is not automatically the most efficient choice.

For Rustles specifically, the current municipality count is small enough that:

- pure vector lookup is probably more viable than it first appears
- hybrid lookup may be the strongest long-term balance

So the current raster approach is defensible as a speed-first prototype, but it is not yet proven to be the best compute-efficient architecture overall.
