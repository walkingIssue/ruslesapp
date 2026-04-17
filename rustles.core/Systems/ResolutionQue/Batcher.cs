using rustles.core.Entities.DTOs;
using rustles.core.Entities;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Shared;
using rustles.core.Systems.GDAL;
using rustles.core.Systems.ResolutionQue.Abstractions;
using rustles.core.Events;

using System.Runtime.CompilerServices;

namespace rustles.core.Systems.ResolutionQue
{
    /// <summary>
    /// This will be scoped to the userrequest
    /// </summary>
    public class Batcher : IBatcher<BatchRequest>
    {
        readonly ITileBatcher<TileKey> _batched;
        public Batcher(ITileBatcher<TileKey> batched)
        {
            _batched = batched;
        }

        public void DeQue(Guid userId)
        {
            bool isHotCleared = _batched.HotCache.TryRemove(userId, out var value);

            if (isHotCleared && value is not null)
            {
                //Deque the known request
                //Dont care about the return, there's a catchall deque for inflight requests
                _batched.Dequed.TryAdd(new DequeKey(userId, value.clientCell), 
                    new DequedValue(new TileKey(value.clientCell.latti, value.clientCell.longi,
                    GDAL.Enums.RasterResolution.R_5M), 
                    value.clientCell,
                    new RequestUserKey(value.RequestId,
                    userId)));
            }
            _batched.DequeAllClient[userId] = 0;
        }

        public SystemAction Enque(BatchRequest request, GDAL.Enums.RasterResolution resolution)
        {
            bool isCacheUpdated = false;
            bool requestExists = false;

            requestExists = _batched.HotCache.TryGetValue(request.ClientId, out var hotCacheValue);
            //handle existing request
            if(requestExists && hotCacheValue is not null && !hotCacheValue.prefilght)
                    isCacheUpdated = _batched.HotCache
                        .TryUpdate(request.ClientId,
                        new HotcacheValue(request.RequestId, request.Position),
                        hotCacheValue!);
            //handle already preflight values
            else if(hotCacheValue is not null && hotCacheValue.prefilght)
                _batched.Dequed.TryAdd(new DequeKey(hotCacheValue.RequestId, hotCacheValue.clientCell), 
                new DequedValue(
                    TileKey.ResolveTile(request,
                    GDAL.Enums.RasterResolution.R_5M),
                hotCacheValue.clientCell, new RequestUserKey(hotCacheValue.RequestId, request.ClientId)));

            //no recovery from this fail, assume this thread is stale and behind in execution order.
            //or duplicate request
            //if insertion to inFlight deque failed means this request already
            //has cancellation pending with the same exact request and clientpos.
            if (!isCacheUpdated) return SystemAction.Failed | SystemAction.Superseded;
            
            bool isInserted = _batched.HotCache.
                TryAdd(request.ClientId,
                new HotcacheValue(request.RequestId, request.Position));

            if(!isInserted) return SystemAction.Failed | SystemAction.Superseded;
            return SystemAction.Ok;
        }
    }

    public struct TileKey
    {
        private int _hashCode = 0;
        public TileKey(int clientLatti, int clientLongi, GDAL.Enums.RasterResolution resolution)
        {
            this._hashCode = HashCode.Combine(Resolution,  clientLatti, clientLongi);
            this._rasterIdX = clientLatti;
            this._rasterIdY = clientLongi;
            this.Resolution = resolution;
        }

        public TileKey()
        {
        }
        
        public static TileKey ResolveTile(BatchRequest request, GDAL.Enums.RasterResolution resolution)
        {
            return new TileKey
            {
                Resolution = resolution,
                RasterIdX = request.Position.latti,
                RasterIdY = request.Position.longi
            };
        }
        const Int16 TILE_SIZE = 256;
        public readonly GDAL.Enums.RasterResolution Resolution { get; init; }
        private readonly int _rasterIdX;
        public int RasterIdX {
            get 
            {
                return _rasterIdX;
            }
            private init
            {
                _rasterIdX = value/TILE_SIZE;
            }
        }

        private readonly int _rasterIdY;
        public int RasterIdY { 
            get 
            {
                return _rasterIdY;
            }
            init
            {
                 _rasterIdY = value/TILE_SIZE;
            }
        }

        public override int GetHashCode()
        {
            if(_hashCode != 0) return _hashCode;

            if(_hashCode == 0) return _hashCode = HashCode.Combine(Resolution, RasterIdX, RasterIdY);

            return HashCode.Combine(Resolution, RasterIdX, RasterIdY);
        }
    }
}
