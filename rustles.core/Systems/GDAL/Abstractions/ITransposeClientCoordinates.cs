using rustles.core.Entities;
using rustles.core.Systems.GDAL.Enums;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.GDAL.Abstractions
{
    public interface ITransposeClientCoordinates
    {
        ClientCell TransposeToRaster(double longi, double lat, RasterResolution resolution);
    }

}
