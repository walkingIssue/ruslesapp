using rustles.core.Entities.DTOs;
using rustles.core.Entities;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Shared;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{

    /// <summary>
    /// Takes responsibility for request validation, if we don't resolve within a client window and get a request for a new position
    /// This prepares us to validate requests more granurally
    /// </summary>
    public class PositionResolveQue : IRequestPostion
    {
        readonly IBatcher<BatchRequest> _batcher;
        readonly ITransposeClientCoordinates _transpose;
        readonly IValidateTransposedCoordinates _validator;
        readonly IFactory<BatchRequest> _requestFactory;

        public PositionResolveQue(
            ITransposeClientCoordinates transpose,
            IValidateTransposedCoordinates validator,
            IBatcher<BatchRequest> batcher,
            IFactory<BatchRequest> requestFactory)
        {
            _transpose = transpose;
            _validator = validator;
            _batcher = batcher;
            _requestFactory = requestFactory;
        }

        public void DequeRequest(Guid userId)
        {
            _batcher.DeQue(userId);
        }
        /// <summary>
        /// Top level resolution que
        /// </summary>
        /// <param name="requestId">requestId of user the push service will be able to resolve once work is complete</param>
        /// <param name="position">client gps cooridnates</param>
        /// <returns>SystemAction Flags combination appropriate for reqeust</returns>
        public SystemAction QueResultionRequest(Guid requestId, ClientPosition position)
        {
            var gdalCoorinates = _transpose.TransposeToRaster(position.longi, position.latti, GDAL.Enums.RasterResolution.R_5M);

            if (!_validator.Validate(gdalCoorinates, GDAL.Enums.RasterResolution.R_5M))
                return SystemAction.Failed | SystemAction.Miss | SystemAction.Unresolvable;

            return _batcher.Enque(MutateBatchRequest(requestId, gdalCoorinates, position.UserId), GDAL.Enums.RasterResolution.R_5M);
        }

        private BatchRequest MutateBatchRequest(Guid requestId, ClientCell position, Guid userId)
        {
            var request = _requestFactory.Create();
            request.Position = position;
            request.RequestId = requestId;
            request.ClientId = userId;

            return request;
        }
    }

    public class BatchRequest
    {
        public Guid RequestId { get; set; }
        public Guid ClientId { get; set; }
        public ClientCell Position { get; set; }
        public BatchRequest(Guid requestId, ClientCell position)
        {
            RequestId = requestId;
            Position = position;
        }
    };

    public interface IValidateTransposedCoordinates
    {
        public bool Validate(ClientCell position, GDAL.Enums.RasterResolution resolution);
    }

    public class ValidateTransposedCoordinates : IValidateTransposedCoordinates
    {
        private readonly OSGeo.GDAL.Dataset _dataset;
        public ValidateTransposedCoordinates(OSGeo.GDAL.Dataset dataset)
        {
            _dataset = dataset;
        }

        public bool Validate(ClientCell position, GDAL.Enums.RasterResolution resolution)
        {
            return position.latti >= 0 && position.longi >= 0
                && position.longi < _dataset.RasterXSize && position.latti < _dataset.RasterYSize;
        }
    }
}
