using rustles.core.Entities.DTOs;
using rustles.core.Entities;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Shared;
using rustles.core.Systems.GDAL;
using rustles.core.Systems.ResolutionQue.Abstractions;
using rustles.core.Events;

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

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
                    new RequestUserKey(userId, value.RequestId)));
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
            {
                    isCacheUpdated = _batched.HotCache
                        .TryUpdate(request.ClientId,
                        new HotcacheValue(request.RequestId, request.Position),
                        hotCacheValue!);
                if (isCacheUpdated) return SystemAction.Ok;
                else return SystemAction.Failed | SystemAction.Superseded;
            }
            //handle already preflight values
            else if(requestExists && hotCacheValue is not null && hotCacheValue.prefilght)
            {
                //update cache with the new request value
                isCacheUpdated = _batched.HotCache.TryUpdate(request.ClientId,
                        new HotcacheValue(request.RequestId, request.Position),
                        hotCacheValue);

                //deque old request if cache update is successful
                if(isCacheUpdated) _batched.Dequed.TryAdd(new DequeKey(hotCacheValue.RequestId, hotCacheValue.clientCell), 
                new DequedValue(
                    new TileKey(hotCacheValue.clientCell.latti, hotCacheValue.clientCell.longi, resolution),
                hotCacheValue.clientCell, new RequestUserKey(request.ClientId, hotCacheValue.RequestId)));


                if(isCacheUpdated) return SystemAction.Ok;
            }

            //no recovery from this fail, assume this thread is stale and behind in execution order.
            //or duplicate request
            //if insertion to inFlight deque failed means this request already
            //has cancellation pending with the same exact request and clientpos.
            if (requestExists && !isCacheUpdated) return SystemAction.Failed | SystemAction.Miss;
            
            bool isInserted = _batched.HotCache.
                TryAdd(request.ClientId,
                new HotcacheValue(request.RequestId, request.Position));

            if(!isInserted) return SystemAction.Failed | SystemAction.Superseded;
            return SystemAction.Ok;
        }
    }

    public struct TileKey : IEquatable<TileKey>
    {
        public bool Equals(TileKey other)
        {
            return Resolution == other.Resolution
                && RasterIdX == other.RasterIdX
                && RasterIdY == other.RasterIdY;
        }

        public override bool Equals(object? obj)
        {
            return obj is TileKey other && Equals(other);
        }

        public static bool operator ==(TileKey left, TileKey right) => left.Equals(right);

        public static bool operator !=(TileKey left, TileKey right) => !left.Equals(right);

        private int _hashCode = 0;
        public TileKey(int clientLatti, int clientLongi, GDAL.Enums.RasterResolution resolution)
        {
            this._rasterIdX = clientLongi / TILE_SIZE;
            this._rasterIdY = clientLatti / TILE_SIZE;
            this.Resolution = resolution;
            this._hashCode = HashCode.Combine(resolution, RasterIdX, RasterIdY);
        }

        public TileKey()
        {
            this.GetHashCode();
        }

        //copy constructor to preserve hashcode for dequeued values referencing the same tilekey
        public TileKey(TileKey other)
        {
            this._rasterIdX = other.RasterIdX;
            this._rasterIdY = other.RasterIdY;
            this.Resolution = other.Resolution;
            this._hashCode = other.GetHashCode();
        }

        public static TileKey ResolveTile(BatchRequest request, GDAL.Enums.RasterResolution resolution)
        {
            return new TileKey
            {
                Resolution = resolution,
                RasterIdX = request.Position.longi,
                RasterIdY = request.Position.latti
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
