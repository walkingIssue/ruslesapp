using rustles.core.Entities.DTOs;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Systems.GDAL.Enums;

namespace rustles.core.Systems.GDAL
{
    public class Raster2KMAccess : IResolvePosition
    {
        OSGeo.GDAL.Dataset _dataset;
        ITransposeClientCoordinates _transposition;
        OSGeo.GDAL.Band _rasterBand;
        IResolveMunip _name;
        public Raster2KMAccess(OSGeo.GDAL.Dataset dataset,
            ITransposeClientCoordinates transposeClientCoordinates,
            IResolveMunip munip)
        {
            _dataset = dataset;
            _transposition = transposeClientCoordinates;
            _rasterBand = _dataset.GetRasterBand(1);
            _name = munip;
        }
        public Municipality ResolvePosition(double longitude, double lattitude)
        {
            var request = _transposition.TransposeToRaster(longitude, lattitude, RasterResolution.R_200M);
            int[] buffer = new int[2];
            _rasterBand.ReadRaster(request.Long, request.lat, 1, 1, buffer, 1, 1, 0, 0);
            _name.GetMunipNaturalName(buffer[0]);
            return _name.GetMunicipality(buffer[0]);
        }

        public Task<Municipality> ResolvePositionAsync(double longitude, double lattitude)
        {
            return Task.Run(() => { return ResolvePosition(longitude, lattitude); });
        }
    }

}
