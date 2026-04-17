# Geodata Projection, Raster Math, and Design Tradeoffs

## Purpose

This note explains the projection and raster-design choices behind the municipality lookup pipeline, with two priorities:

1. compute efficiency
2. correctness of result data

It is intentionally more conceptual than `geodata-raster-pipeline-technical.md`.

## The two coordinate systems in play

The current assets show two distinct coordinate systems:

- `EPSG:3006` (`SWEREF99 TM`)
  - projected coordinates
  - units are meters
  - good for expressing real-world widths and heights
- `EPSG:4326` (`WGS 84`)
  - geographic coordinates
  - units are degrees
  - good for client APIs and browser/mobile location input

The original Sweden administrative GeoPackage appears to be in `EPSG:3006`, while the rasterized files in the current pipeline were produced from a reprojected `EPSG:4326` variant.

## What a raster is doing mathematically

A raster is just a regular grid plus an affine transform.

For a north-up raster, the geotransform is typically:

```text
[originX, pixelWidth, 0, originY, 0, pixelHeight]
```

Where:

- `originX` is the world x-coordinate of the upper-left corner
- `originY` is the world y-coordinate of the upper-left corner
- `pixelWidth` is the world width of one pixel
- `pixelHeight` is usually negative because rows move downward while world y moves downward from the top origin

To convert a world coordinate to raster indices:

```text
col = floor((x - originX) / pixelWidth)
row = floor((y - originY) / pixelHeight)
```

That is exactly the logic the `spatial` program uses when validating raster reads.

For the current `EPSG:4326` TIFFs:

- `x` is longitude
- `y` is latitude
- `pixelWidth` / `pixelHeight` are degree fractions

For a projected `EPSG:3006` raster:

- `x` is easting in meters
- `y` is northing in meters
- `pixelWidth` / `pixelHeight` are meter sizes

## What the current pipeline appears to have done

The original data was meter-based in `EPSG:3006`, then reprojected to `EPSG:4326`, then rasterized there.

That gives a simple runtime story:

- client sends lon/lat
- raster is already lon/lat
- no extra CRS transform is needed before the pixel lookup

That simplicity is real, but it comes with tradeoffs.

## Why meter-based rasterization is usually a better fit

If the goal is fast municipality lookup with a controlled spatial error budget, a projected meter-based raster is usually the stronger design.

### 1. The cell size means what it says

In a projected meter CRS:

- `50` means `50 meters`
- `100` means `100 meters`
- `200` means `200 meters`

That makes design and capacity decisions legible.

In `EPSG:4326`, pixel size is in degrees, not meters. A degree-based pixel does not correspond to a fixed east-west ground distance across Sweden.

Approximate ground size of the current degree steps:

| Latitude | `0.001 deg` lon | `0.0005 deg` lon | `0.0001 deg` lon |
| --- | --- | --- | --- |
| `55` | `63.9 m` | `31.9 m` | `6.4 m` |
| `62` | `52.3 m` | `26.1 m` | `5.2 m` |
| `69` | `39.9 m` | `19.9 m` | `4.0 m` |

North-south size does not shrink the same way:

- `0.001 deg` latitude is about `111.3 m`
- `0.0005 deg` latitude is about `55.7 m`
- `0.0001 deg` latitude is about `11.1 m`

So a degree-based raster in Sweden is anisotropic:

- north-south cell size is one thing
- east-west cell size changes with latitude

That is awkward if the intended resolution concept is "200m", "400m", or "800m".

### 2. The semantics are more stable

If the lookup grid exists to answer "which municipality contains this point?", then a projected raster gives you a stable spatial tolerance across the country.

With degree-based rasterization:

- the same nominal pixel size behaves differently in Skane versus Norrbotten
- boundary fidelity varies across latitude

With meter-based rasterization:

- the same pixel size behaves much more consistently across Sweden

That is better for correctness.

### 3. File size is easier to reason about

For the original `EPSG:3006` extent, approximate raster sizes would be:

| Cell size | Approx raster size | Raw `UInt16` size |
| --- | --- | --- |
| `50 m` | `13520 x 31196` | about `804 MiB` |
| `100 m` | `6760 x 15598` | about `201 MiB` |
| `200 m` | `3380 x 7799` | about `50 MiB` |
| `400 m` | `1690 x 3900` | about `12.6 MiB` |
| `800 m` | `845 x 1950` | about `3.1 MiB` |

These numbers are much easier to budget around than degree-based guesses.

They also line up with the names currently present in the Rustles enums:

- `R_200M`
- `R_400M`
- `R_800M`

Those names make more sense in a projected meter grid than in `EPSG:4326`.

## Why the current degree-based files got huge

The current TIFFs are degree-based and dense. That means:

- smaller pixel size explodes both width and height
- no practical compression means file size tracks raw cell count closely

That is why the current outputs scale like:

- about `389 MB` at `0.001 deg`
- about `1.53 GB` at `0.0005 deg`
- about `37.9 GB` at `0.0001 deg`

This is not a mysterious GDAL failure. It is mostly the expected cost of a very fine uncompressed geographic raster.

## Why someone might still choose `EPSG:4326`

There are a few reasons the current path may have felt attractive:

- browser and phone clients already speak lon/lat
- debugging is easier when coordinates look familiar
- runtime lookup can skip an explicit CRS transform

Those are real advantages.

But they are usually operational conveniences, not strong arguments for the raster itself to live in `EPSG:4326`.

## Compute-efficiency perspective

From a compute-efficiency point of view, the stronger design is usually:

1. keep the authoritative vector geometry in its projected meter CRS
2. burn the lookup raster in that same projected meter CRS
3. transform incoming client lon/lat to the raster CRS at query time
4. do the pixel lookup there

Why this is usually cheaper overall:

- the per-request coordinate transform is tiny compared with raster I/O
- the raster can be much smaller for the same real-world error tolerance
- batching and tile math are easier to define in real units
- "200m", "400m", and "800m" become actual engineering parameters

In other words, moving the transform cost to request time is often cheaper than paying for a distorted or over-detailed raster forever.

## Correctness perspective

From a correctness point of view, a projected meter-based burn is usually safer because:

- pixel dimensions correspond to real distances
- boundary loss is easier to reason about
- error budgets are easier to state and test

The current `EPSG:4326` raster can still work, but the correctness story becomes fuzzier:

- the same pixel size does not mean the same ground resolution across Sweden
- north/south and east/west error behave differently
- "high resolution" may still be inaccurate in thin coastal or boundary regions if the burn rule is too coarse

Also note that the current burn uses:

- `ALLTOUCHED=FALSE`

That means a polygon does not claim every touched cell. For municipality borders, islands, narrow corridors, and coastlines, this may leave more `NoData` or boundary miss cases than expected at coarser resolutions.

## What the current files imply

The current raster names and enums suggest a meter-oriented mental model:

- `Raster2KMAccess`
- `R_200M`
- `R_400M`
- `R_800M`

But the actual current TIFFs are degree-based.

That mismatch matters. It makes the system harder to reason about because the code language says "meters" while the files say "degrees".

## Practical direction suggested by the current evidence

If the goal is a production-worthy municipality lookup raster, the cleaner path is probably:

1. treat `kommun-lan-rike_aktuell.gpkg` in `EPSG:3006` as the authoritative source
2. rasterize in `EPSG:3006`
3. choose a real meter cell size based on acceptable lookup error
4. transform incoming client WGS84 coordinates into `EPSG:3006` before raster lookup

That would improve:

- correctness of the resolution semantics
- predictability of file size
- clarity of the batching / tiling story

## What is still unknown

Two things are still unresolved:

- whether the old reprojection from `EPSG:3006` to `EPSG:4326` happened with `ogr2ogr`, GDAL API calls, or another one-off tool
- whether the current runtime bugs are due to the raster generation itself or due to the missing / external coordinate transpose implementation

Those questions should be answered before regenerating assets.

## Bottom line

For this problem, "use lat/long everywhere" is not automatically the best path.

For municipality lookup, a meter-based projected raster is usually better on both of the dimensions that matter here:

- compute efficiency
- correctness of result data

The current `EPSG:4326` raster is understandable as a prototype shortcut, but it is not obviously the right long-term representation.
