# Geodata Raster Pipeline Technical Notes

## Purpose

This note documents the geodata pipeline as it exists today across:

- `C:\Users\Bartek\source\repos\spatial`
- `C:\Users\Bartek\source\repos\rustles_solution`

The goal is to record what is currently verifiable, what is inferred, and where the current pipeline is incomplete.

## Current source assets

The important files are:

- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Assets\kommun-lan-rike_aktuell.gpkg`
- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Assets\Current_4326.gpkg`
- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Assets\Raster_Current_4326.tif`
- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Assets\Raster4M_Current_4326.tif`

The same files also exist in:

- `C:\Users\Bartek\source\repos\spatial\Spatial\Data`

The copies are byte-identical. The matching files were verified by SHA256 hash.

## CRS and dataset roles

There are two GeoPackage variants in play:

- `kommun-lan-rike_aktuell.gpkg`
  - CRS: `EPSG:3006` (`SWEREF99 TM`)
  - coordinate space: projected meters
  - extent: approximately `245209..921177` east and `6111268..7671055` north
- `Current_4326.gpkg`
  - CRS: `EPSG:4326` (`WGS 84`)
  - coordinate space: longitude / latitude in degrees
  - extent: approximately `10.5935..24.1777` lon and `55.1371..69.0600` lat

That means the original authoritative administrative geometry appears to be meter-based, and the `_4326` variant is already a reprojected derivative.

## What the Spatial project is using

`C:\Users\Bartek\source\repos\spatial\Spatial\Spatial.csproj` references:

- `MaxRev.Gdal.Core`
- `MaxRev.Gdal.WindowsRuntime.Minimal`

This project is a small GDAL/OGR scratch console app rather than a reusable library. It appears to have been used as an experiment harness for:

- reading the municipality GeoPackage
- rasterizing the municipality polygons
- validating raster lookups against sample points

## What the Spatial program actually does

The relevant file is:

- `C:\Users\Bartek\source\repos\spatial\Spatial\Program.cs`

The current program flow is:

1. configure GDAL with `GdalBase.ConfigureAll()`
2. register GDAL and OGR drivers with:
   - `Gdal.AllRegister()`
   - `Ogr.RegisterAll()`
3. open `Current_4326.gpkg` using `Ogr.Open(path, 0)`
4. read the `kommun` layer with `GetLayerByName("kommun")`
5. get the layer extent with `layer.GetExtent(...)`
6. derive raster width and height from:
   - layer extent
   - hardcoded pixel size `px` / `py`
7. create a GeoTIFF dataset using the `GTiff` driver
8. set the raster geotransform and projection
9. initialize band 1 to `NoData = 0`
10. rasterize the `kommun` layer into band 1 using:
    - `ATTRIBUTE=kommunkod`
    - `ALLTOUCHED=FALSE`
11. reopen the resulting TIFF and test point lookups with `ReadRaster(...)`

The dataset creation block is currently commented out in the repo snapshot, but the code is still present and documents the method that was used.

## The rasterization call and why it matters

The core burn step is the equivalent of:

```csharp
Gdal.RasterizeLayer(
    ds,
    1,
    [1],
    layer,
    IntPtr.Zero,
    IntPtr.Zero,
    1,
    [1.0],
    ["ALLTOUCHED=FALSE", "ATTRIBUTE=kommunkod"],
    null,
    null);
```

This means:

- the output raster has a single band
- the value written into each raster cell is taken from the `kommunkod` field of the source polygon
- the TIFF is not visual imagery, it is a lookup grid

In other words, each pixel represents:

- `0` for nodata / no municipality burned into that cell
- a municipality code such as `188`, `1280`, `1480`, `2462`, etc.

## What the resulting TIFF file is

The output file is a `GeoTIFF` with:

- one band
- type `UInt16`
- `NoData = 0`
- georeferencing metadata
- a pixel-to-world affine transform

This is effectively a precomputed spatial index. Rustles does not use it as an image. Rustles uses it as a fast `point -> municipality code` lookup table.

## Verified TIFF metadata

From `gdalinfo`, the current files are:

| File | Size (pixels) | Pixel size | Band type | On-disk size |
| --- | --- | --- | --- | --- |
| `Raster_Current_4326.tif` | `13585 x 13923` | `0.001` deg | `UInt16` | about `389 MB` |
| `Raster4M_Current_4326.tif` | `27169 x 27846` | `0.0005` deg | `UInt16` | about `1.53 GB` |
| `Raster8M_Current_4326.tif` | `135842 x 139229` | `0.0001` deg | `UInt16` | about `37.9 GB` |

All three files:

- use `EPSG:4326`
- have one raster band
- have `NoData Value = 0`
- contain non-zero municipality-code-like values

Observed band statistics:

- `Raster_Current_4326.tif`: min/max `188..2462`
- `Raster4M_Current_4326.tif`: min/max `188..2462`
- `Raster8M_Current_4326.tif`: min/max `192..2523`

So the current TIFFs are not empty. The band data is present.

## Why the files get so large

The current output is effectively uncompressed for practical purposes:

- `COMPRESS=NONE` is present in the commented creation code
- the raster is a dense single-band `UInt16`
- file size grows roughly with total pixel count

That means resolution increases are expensive very quickly:

- halving the pixel size in both axes means about `4x` as many cells
- reducing pixel size from `0.001` to `0.0001` degrees means about `100x` as many cells

That is why the `0.0001` degree raster jumps to roughly `38 GB`.

## What Rustles does with the GeoPackage

Rustles itself does not generate the TIFF today. It reads:

- the municipality GeoPackage through `GeoOgrAccess`
- the raster TIFF through `Raster2KMAccess`

### GeoPackage read path

Relevant files:

- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Systems\GDAL\GeoOgrAccess.cs`
- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Entities\DTOs\Municipality.cs`

`GeoOgrAccess`:

1. receives an `OSGeo.OGR.DataSource`
2. opens the `kommun` layer
3. iterates every feature with `GetNextFeature()`
4. maps selected fields into a `Municipality`

The mapped fields are currently positional rather than name-based:

- field `12` -> municipality display name (`namnkortform`)
- field `8` -> municipality code (`kommunkod`)
- field `7` -> county code (`lanskod`)
- field `9` -> country code (`landskod`)

The returned model is:

```csharp
public record Municipality(string? name, int? munipCode, int? countyCode, int? countryCode)
```

So the GeoPackage read path returns a sequence of `Municipality` records.

### Municipality lookup path

Relevant file:

- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Systems\GDAL\ResolveMunipName.cs`

`ResolveMunip`:

1. loads municipalities from `IGeoOgrAccess`
2. stores them in memory
3. sorts them
4. performs a binary search by municipality code

The intent is:

- raster returns municipality code
- municipality resolver maps that code to the richer `Municipality` DTO

## What Rustles does with the TIFF

Relevant file:

- `C:\Users\Bartek\source\repos\rustles_solution\rustles.core\Systems\GDAL\Raster2KMAccess.cs`

`Raster2KMAccess`:

1. receives a GDAL `Dataset`
2. gets band 1 with `GetRasterBand(1)`
3. converts client coordinates into raster indices via `ITransposeClientCoordinates`
4. reads one pixel with `ReadRaster(...)`
5. resolves the returned municipality code to a `Municipality`

The current usage pattern is:

- one point request
- one single-pixel raster read
- one municipality lookup

## Important current gaps

There are some missing or unresolved parts in the current repo state:

- the implementation of `ITransposeClientCoordinates` is not present in the obvious Rustles source tree
- no committed code was found for the reprojection step from `kommun-lan-rike_aktuell.gpkg` to `Current_4326.gpkg`
- no committed command history was found for `ogr2ogr`, `gdal_rasterize`, or `gdal_translate`

So the current evidence supports this reconstruction:

1. original administrative geometry existed in `EPSG:3006`
2. a reprojected `Current_4326.gpkg` was produced somehow
3. the `spatial` scratch app rasterized that `Current_4326.gpkg`
4. the resulting TIFFs were copied into `rustles.core/Assets`

The reprojection step is still inferred rather than directly documented in code.

## Practical conclusion

The current pipeline is not "vector data goes directly into Rustles and gets resolved live."

It is:

1. read vector polygons from GeoPackage
2. rasterize municipality codes into a GeoTIFF lookup grid
3. at runtime, convert client coordinates to raster indices
4. read one pixel
5. map the burned municipality code back into a `Municipality` model

That is the actual technical shape of the system as it exists today.
