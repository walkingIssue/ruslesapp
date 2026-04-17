using Microsoft.Extensions.Hosting;
using Microsoft.Graph.Drives.Item.Items.Item.Workbook.Functions.IntRate;

using rustles.core.Entities;
using rustles.core.Events;
using rustles.core.Systems.ResolutionQue.Abstractions;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;

namespace rustles.core.Systems.ResolutionQue
{
    /// <summary>
    /// Stateobject containing data for the batch runs.
    /// 
    /// This will effectively be a singleton, there needs to be a facade abstraction to connect it to the scoped 
    /// IBatcher
    /// </summary>
    public class Batched : BackgroundService, ITileBatcher<TileKey>, ISignalBatchFull
    {
        public Batched()
        {
        }

        public TilingResult _mergedTilingObject;

        private int _batchNumber = 0;

        //worker started last tick
        long _lastTick = 0;
        long _lastRuntime = 0;

        const long tickInterval = 2_500_000;

        SemaphoreSlim _runSemaphore = new SemaphoreSlim(1,1);


        public event ISignalBatchFull.SignalBatchRun SignalEvent;

        private readonly ConcurrentDictionary<Guid, HotcacheValue> _hotCache = new();
        public ConcurrentDictionary<Guid, HotcacheValue> HotCache => _hotCache;


        private readonly ConcurrentDictionary<TileKey, SortedDictionary<int, RequestCell>> _computeIds = new();
        public ConcurrentDictionary<TileKey, SortedDictionary<int, RequestCell>> ComputeIds => _computeIds;


        public ConcurrentDictionary<DequeKey, DequedValue> _deQued = new();
        public ConcurrentDictionary<DequeKey, DequedValue> Dequed => _deQued;


        public ConcurrentDictionary<Guid, byte> _dequeAllClient = new();


        public ConcurrentDictionary<Guid, byte> DequeAllClient => _dequeAllClient;

        public TilingResult TileHotCacheValues(CancellationToken token, bool deque = true, bool dequeAll = true, bool backSearch = false)
        {
            var result = new TilingResult();

            if(token.IsCancellationRequested)
                return result;


            var currentCount = _hotCache.Count;
            var dequedCount = _deQued.Count;
            var disconnectedCount = _dequeAllClient.Count;

            var poppedDequeAll = new List<Guid>();

            int hotCacheHandledCount = 0;

            //localized snapshot of the hotcache
            var currentHotCacheCopy = _hotCache.ToArray();

            if (token.IsCancellationRequested)
                return result;

            foreach(var kvp in currentHotCacheCopy)
            {
                if (token.IsCancellationRequested)
                    return result;

                hotCacheHandledCount++;
                //safety for multithreading wierdness
                if(_dequeAllClient.ContainsKey(kvp.Key))
                {
                    if(dequeAll && _hotCache.TryRemove(kvp.Key, out var _))
                    {
                        poppedDequeAll.Add(kvp.Key);
                        result.DequedRequestsCount++;
                        continue;
                    }
                }
                //Inflight not yet in compute
                var hotcacheValue = kvp.Value;

                if (hotcacheValue.prefilght)
                {
                    RemovePreflightValue(kvp.Key, hotcacheValue, ref result);
                    continue;
                }

                var dequeKey = new DequeKey(kvp.Value.RequestId, kvp.Value.clientCell);
                if (_deQued.TryGetValue(dequeKey, out var dequed)
                    && _computeIds.TryGetValue(dequed.Tile, out var computeIds))
                {
                    if (computeIds.TryGetValue(dequed.TileLocalizedId, out var cellRequests))
                    {
                        cellRequests.RequestIds.Remove(HashCode.Combine(dequed.RequestUserKey.RequestId, dequed.RequestUserKey.UserId));
                        result.DequeuedInflightCount++;
                    }
                    continue;
                }
                
                var preflightedValue = hotcacheValue with { prefilght = true };

                // move on if we cannot mark the current value as preflight, means a newer request won the race.
                if (!_hotCache.TryUpdate(kvp.Key, preflightedValue, hotcacheValue))
                    continue;
                
                //user disconnected
                if (_dequeAllClient.ContainsKey(kvp.Key))
                {
                    result.DequeuedInflightCount++;
                    continue;
                }

                var requestTile = new TileKey(kvp.Value.clientCell.latti,
                    kvp.Value.clientCell.longi,
                    GDAL.Enums.RasterResolution.R_5M);
                var cell = new RequestCell(kvp.Value.clientCell, requestTile);

                if(!_computeIds.TryGetValue(requestTile, out var computeCells))
                {
                    var tileDict = new SortedDictionary<int, RequestCell>();
                    var clientRequest = new RequestUserKey(kvp.Key, kvp.Value.RequestId);

                    cell.RequestIds.Add(HashCode.Combine(kvp.Key, kvp.Value.RequestId), clientRequest);
                    tileDict.Add(cell.GetHashCode(), cell);
                    _computeIds.TryAdd(requestTile, tileDict);
                    result.CellCount++;
                    result.UserCount++;
                    result.ReadyTilesCount++;
                    continue;
                }
                if (computeCells == null)
                {
                    var tileDict = _computeIds[requestTile] = new();
                    tileDict.Add(cell.GetHashCode(), cell);
                    result.CellCount++;
                }
                if(!computeCells!.TryAdd(cell.GetHashCode(), cell))
                {
                    var userRequest = new RequestUserKey(kvp.Key, kvp.Value.RequestId);
                    computeCells![cell.GetHashCode()].RequestIds.Add(userRequest.GetHashCode(), userRequest);
                    result.UserCount++;
                    result.RequestCount++;
                    continue;
                }

                result.CellCount++;
                result.UserCount++;
            }
            return result;
        }

        private void RemovePreflightValue(Guid userId, HotcacheValue hotcacheValue, ref TilingResult result)
        {
            var requestTile = new TileKey(
                hotcacheValue.clientCell.latti,
                hotcacheValue.clientCell.longi,
                GDAL.Enums.RasterResolution.R_5M);

            if (_computeIds.TryGetValue(requestTile, out var computeCells))
            {
                var requestCellId = hotcacheValue.clientCell.GetHashCode();
                if (computeCells.TryGetValue(requestCellId, out var cellRequests))
                {
                    cellRequests.RequestIds.Remove(HashCode.Combine(userId, hotcacheValue.RequestId));
                    if (cellRequests.RequestIds.Count == 0)
                        computeCells.Remove(requestCellId);

                    if (computeCells.Count == 0)
                        _computeIds.TryRemove(requestTile, out var _);

                    result.DequeuedInflightCount++;
                }
            }

            _hotCache.TryRemove(userId, out var _);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(new TimeSpan(tickInterval));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            if (!await _runSemaphore.WaitAsync(0, stoppingToken))
                continue;
            var started = DateTime.Now.Ticks;
            var sw = Stopwatch.StartNew();
            try
            {
                var runResult = await Task.Run<TilingResult>(() => TileHotCacheValues(stoppingToken));
                var mergedRest = new TilingResult()
                {
                    CellCount = this._mergedTilingObject.CellCount + runResult.CellCount,
                    DequedRequestsCount = this._mergedTilingObject.DequedRequestsCount + runResult.DequedRequestsCount,
                    ReadyTilesCount = this._mergedTilingObject.ReadyTilesCount + runResult.ReadyTilesCount,
                    RequestCount = this._mergedTilingObject.RequestCount + runResult.RequestCount,
                    UserCount = this._mergedTilingObject.UserCount + runResult.UserCount,
                    DequeuedInflightCount = this._mergedTilingObject.DequeuedInflightCount + runResult.DequeuedInflightCount,
                    Result = BatcherActions.Ok
                };
                Interlocked.Exchange(ref _mergedTilingObject, runResult);

                if (this._mergedTilingObject.UserCount > 1_000)
                    this.SignalEvent.Invoke(this, new GDAL.SignalBatchRunEventArgs() { BatchCollectionNumber = ++this._batchNumber });
            }
            finally
            {
                sw.Stop();
                Interlocked.Exchange(ref _lastRuntime, sw.Elapsed.Ticks);
                Interlocked.Exchange(ref _lastTick, started);
                _runSemaphore.Release();
            }
        }
    }
}
