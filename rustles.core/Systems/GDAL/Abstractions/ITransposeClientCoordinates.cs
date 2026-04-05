using rustles.core.Entities;
using rustles.core.Systems.GDAL.Enums;

namespace rustles.core.Systems.GDAL.Abstractions
{
    public interface ITransposeClientCoordinates
    {
        Position TransposeToRaster(double longi, double lat, RasterResolution resolution);
    }

}
