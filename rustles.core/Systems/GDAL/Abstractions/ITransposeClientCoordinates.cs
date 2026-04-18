using OSGeo.OSR;

using rustles.core.Systems.GDAL.Enums;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.GDAL.Abstractions
{
    public interface ITransposeClientCoordinates
    {
        ClientCell TransposeToRaster(double longi, double lat, RasterResolution resolution);
    }

    public class TransposeClientCoordinates : ITransposeClientCoordinates
    {
        private CoordinateTransformation _coordinateTransf { get; init; }
        private readonly double[] _geoTransform = new double[6];

        public TransposeClientCoordinates(OSGeo.GDAL.Dataset dataset)
        {
            dataset.GetGeoTransform(_geoTransform); //why do i need this?

            SpatialReference sourceRef = new SpatialReference("");
            SpatialReference targetRef = new SpatialReference("");
            sourceRef.ImportFromEPSG(4326);
            targetRef.ImportFromEPSG(3006);
            sourceRef.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
            targetRef.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

            _coordinateTransf = new(sourceRef, targetRef);
        }
        public ClientCell TransposeToRaster(double longi, double lat, RasterResolution resolution)
        {
            //espg:4326(wsg84) to espg:3006(sweref99tm) coordinate transformation
            double[] point = [longi, lat, 0d];
            _coordinateTransf.TransformPoint(point);

            var easting = point[0];
            var northing = point[1];

            var originX = _geoTransform[0];
            var pixelWidth = _geoTransform[1];

            var originY = _geoTransform[3];
            var pixelHeight = _geoTransform[5];

            //projection into raster cooridinates
            var rasterX = (int)Math.Floor((easting - originX) / pixelWidth);
            var rasterY = (int)Math.Floor((northing - originY) / pixelHeight);

            return new ClientCell(rasterY, rasterX);
        }
    }
}
