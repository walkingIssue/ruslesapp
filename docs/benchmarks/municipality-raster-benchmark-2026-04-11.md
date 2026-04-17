# Municipality Raster Benchmark

Date: 2026-04-11

This note captures the structured benchmark run used to compare the old municipality lookup raster with the new projected `5m` raster. The goal was to answer three practical questions:

1. Is the new projected raster still fast enough for stage 1 municipality chat?
2. Does compression meaningfully hurt runtime reads?
3. When does tile batching start to matter?

## What Was Benchmarked

The benchmark harness currently lives in the external `spatial` scratch project rather than this repo. The benchmark outputs in this note were produced from that local harness.

It compares two raster strategies:

- Old raster: [Raster4M_Current_4326.tif](../../rustles.core/Assets/Raster4M_Current_4326.tif)
  - CRS: `EPSG:4326`
  - Resolution: `0.0005` degrees
  - Tiled
  - Uncompressed
- New raster: [Municipality_5m_3006.tif](../../rustles.core/Assets/Municipality_5m_3006.tif)
  - CRS: `EPSG:3006`
  - Resolution: `5m`
  - Tiled
  - `ZSTD` compressed

The source municipality polygons come from:

- [kommun-lan-rike_aktuell.gpkg](../../rustles.core/Assets/kommun-lan-rike_aktuell.gpkg)

The harness loads both rasters once, keeps them open, and times:

- coordinate transform cost
- raster read cost
- end-to-end lookup cost
- direct `1x1` reads
- tile-batched reads
- sparse nationwide samples
- dense local-cluster samples

## Important Setup Details

- GDAL axis order was forced to traditional GIS order with `OSR_DEFAULT_AXIS_MAPPING_STRATEGY=TRADITIONAL_GIS_ORDER`. Without this, the `WGS84 -> EPSG:3006` transform produced wrong coordinates.
- The new raster path includes a real coordinate reprojection step before pixel lookup.
- The old raster path does not need reprojection because the raster itself is already in `4326`.
- "Cold" means the first pass in a scenario.
- "Warm" means repeated passes against an already-open dataset with hot GDAL and OS cache.

## Sample Sets

Two access patterns were benchmarked.

### Sparse Nationwide Set

- `537` sample points
- scattered across Sweden
- represents stage 1 municipality resolution under broad geographic spread

### Dense Local Cluster

- `1024` sample points
- concentrated around Stockholm
- represents the kind of locality that stage 2 targeted rooms will create

## Raster Metadata

### Old Raster

- Size: `27169 x 27846`
- Pixel size: `0.0005 x -0.0005`
- Band type: `UInt16`
- Block size: `256 x 256`

### New Raster

- Size: `135195 x 311959`
- Pixel size: `5 x -5`
- Band type: `UInt16`
- Block size: `256 x 256`

## Key Results

### Sparse Nationwide, Direct `1x1` Reads

| Scenario | Transform avg | Read avg | Total avg |
| --- | ---: | ---: | ---: |
| Old cold ordered | `0.16 us` | `56.96 us` | `57.32 us` |
| Old cold shuffled | `0.05 us` | `48.96 us` | `49.19 us` |
| Old warm ordered | `0.03 us` | `0.35 us` | `0.51 us` |
| Old warm shuffled | `0.03 us` | `0.24 us` | `0.35 us` |
| New cold ordered | `1.10 us` | `108.93 us` | `110.20 us` |
| New cold shuffled | `1.48 us` | `117.74 us` | `119.52 us` |
| New warm ordered | `0.30 us` | `0.19 us` | `0.57 us` |
| New warm shuffled | `0.30 us` | `0.22 us` | `0.59 us` |

What this says:

- The new projected raster is slower on cold single-point reads than the old uncompressed raster.
- Even so, the new raster still lands around `110-120 us` per cold lookup, which is comfortably usable for municipality chat.
- Once the raster is hot, the read cost becomes tiny and the coordinate transform starts to matter as much as the read itself.

## Dense Local Cluster, Direct `1x1` Reads

| Scenario | Transform avg | Read avg | Total avg |
| --- | ---: | ---: | ---: |
| Old cold local-cluster | `0.02 us` | `0.22 us` | `0.32 us` |
| Old warm local-cluster | `0.04 us` | `0.18 us` | `0.32 us` |
| New cold local-cluster | `0.29 us` | `1.86 us` | `2.22 us` |
| New warm local-cluster | `0.28 us` | `0.13 us` | `0.48 us` |

What this says:

- Locality changes the picture a lot.
- Even without custom batching logic, repeated nearby access causes the tiled raster to behave extremely well.
- For the new raster, the cold local-cluster read is much cheaper than the cold nationwide read because nearby points keep hitting the same block region.

## Tile-Batched Results

The harness also tested a simple tile-batched mode:

1. transform incoming points
2. group them by GDAL block
3. read each block once
4. resolve many points from the in-memory tile buffer

### Sparse Nationwide Tile-Batched

| Scenario | Avg points per tile | Effective read per point | Total per point |
| --- | ---: | ---: | ---: |
| Old cold | `1.00` | `74.53 us` | `74.56 us` |
| Old warm | `1.00` | `23.47 us` | `23.50 us` |
| New cold | `1.00` | `131.64 us` | `131.93 us` |
| New warm | `1.00` | `22.58 us` | `22.92 us` |

What this says:

- For sparse nationwide access, batching does not help.
- Each point lands in its own tile, so batching just adds block-read overhead without amortizing anything.

### Dense Local Cluster Tile-Batched

| Scenario | Tiles read | Avg points per tile | Effective read per point | Total per point |
| --- | ---: | ---: | ---: | ---: |
| Old cold local-cluster | `1` | `1024.00` | `0.12 us` | `0.14 us` |
| Old warm local-cluster | `4` | `1024.00` | `0.02 us` | `0.05 us` |
| New cold local-cluster | `1` | `1024.00` | `2.11 us` | `2.49 us` |
| New warm local-cluster | `4` | `1024.00` | `0.02 us` | `0.37 us` |

What this says:

- This is where the structural win appears.
- When many nearby points share a tile, one block read can serve a large batch.
- For the new raster, warm tile-batched local access effectively makes read cost disappear into the noise.

## Correctness Comparison

The harness compared old and new cold-pass municipality results across the sparse nationwide sample set.

- Total mismatches: `3`
- Samples checked: `537`

Mismatches:

- `(16.0000, 57.0000)` old=`881:Nybro` new=`821:Högsby`
- `(11.5000, 57.5000)` old=`1480:Göteborg` new=`1384:Kungsbacka`
- `(18.0000, 58.5000)` old=`192:Nynäshamn` new=`0:?`

This is a good result, not a clean bill of correctness.

The new raster is close to the old behavior, but the remaining mismatches are exactly the kind of edge-case locations where raster lookup alone is not trustworthy enough. That strengthens the earlier design direction:

- use raster lookup as the fast first pass
- fall back to exact vector lookup around borders, nodata edges, or uncertainty zones

## Compression Impact

The new raster is:

- projected in meters
- tiled
- `ZSTD` compressed

On disk it is still tiny relative to the raw theoretical size, but compression does have a runtime effect:

- cold single-point reads are slower than the old uncompressed raster
- warm reads remain extremely fast
- for local clustered access, the tiled structure matters more than the compression penalty

Practical read:

- compression hurts the cold path some
- compression is not a blocker for this use case
- the trade is absolutely worth it for the storage savings and deployment simplicity

## Bottom Line

For Rustles stage 1 and stage 2 planning, the benchmark supports this story:

- The `5m` projected municipality raster is viable.
- Cold nationwide single-point lookups are still fast enough for municipality chat.
- Warm repeated reads are extremely cheap.
- Tile batching only becomes worthwhile when requests are spatially local.
- That means stage 1 municipality chat can stay simple, while stage 2 local-room work should explicitly group lookups by tile.
- Exact vector fallback is still needed near borders and nodata edges.

## Recommended Next Step

The next useful technical step is not more raster compression work. It is:

1. wire the `EPSG:4326 -> EPSG:3006` coordinate transform into `rustles.core`
2. read from the new projected raster
3. define a small vector-fallback rule for border and nodata cases
4. keep tile batching as the optimization path for stage 2 local-room density
