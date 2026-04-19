using Microsoft.Extensions.Hosting;

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

            if (token.IsCancellationRequested)
                return result;

            var markedForKill = _deQued.ToArray();

            for(int i= 0; i <markedForKill.Length; i++)
            {
                if (token.IsCancellationRequested)
                    return result;

                RemovePreflightValue(
                    markedForKill[i].Value.RequestUserKey,
                    markedForKill[i].Value.Tile,
                    ref result,
                    markedForKill[i].Value.TileLocalizedId,
                    markedForKill[i].Key.Cell);

                _deQued.TryRemove(markedForKill[i].Key, out var _);
                
            }


            if(token.IsCancellationRequested)
                return result;

            var currentHotCacheCopy = _hotCache.ToArray();

            var currentDequeAllCopy = _dequeAllClient.ToDictionary();
            foreach(var kvp in currentHotCacheCopy)
            {
                if (token.IsCancellationRequested)
                    return result;

                hotCacheHandledCount++;
                //safety for multithreading wierdness
                if(currentDequeAllCopy.ContainsKey(kvp.Key))
                {
                    //dequeue means nothing within this server compute frame
                    //even if the request just came in, the user trying to reconnect to frequently
                    if(dequeAll && _hotCache.TryRemove(kvp.Key, out var _))
                    {
                        result.DequedRequestsCount++;
                        continue;
                    }
                }
                //Inflight not yet in compute
                var hotcacheValue = kvp.Value;

                //skip already qued or duplication
                if (hotcacheValue.prefilght)
                    continue;

                //mark the hotcache value as preflight, communicate to the batcher this value will be computed.
                var preflightedValue = hotcacheValue with { prefilght = true };

                //move on if we cannot mark the current value as preflight, means a newer request won the race.
                //hopefully stabilizes by next tiling run.
                if (!_hotCache.TryUpdate(kvp.Key, preflightedValue, hotcacheValue))
                    continue;
                
                var requestTile = new TileKey(kvp.Value.clientCell.latti,
                    kvp.Value.clientCell.longi,
                    GDAL.Enums.RasterResolution.R_5M);
                var cell = new RequestCell(kvp.Value.clientCell, requestTile);

                //Create a new Tile entry in the compute dictionary, add the cell to it, and add the user request to the cell.
                if (!_computeIds.TryGetValue(requestTile, out var computeCells))
                {
                    var tileDict = new SortedDictionary<int, RequestCell>();
                    var clientRequest = new RequestUserKey(kvp.Key, kvp.Value.RequestId);

                    cell.RequestIds.Add(kvp.Key, clientRequest);
                    tileDict.Add(cell.TileLocalizedId, cell);
                    _computeIds.TryAdd(requestTile, tileDict);
                    result.CellCount++;
                    result.UserCount++;
                    result.ReadyTilesCount++;
                    continue;
                }
                // The cell was removed at some point, create a computeCell holding the compute points
                // add the cell with the user request to the compute cell, add the compute cell to the compute dictionary
                if (computeCells == null)
                {
                    var tileDict = _computeIds[requestTile] = new();
                    tileDict.Add(cell.TileLocalizedId, cell);
                    result.CellCount++;
                }
                //compute cell exists, add the user request to the cell if the cell exists,
                //otherwise create the cell with the user request and add it to the compute cell
                if (!computeCells!.TryAdd(cell.TileLocalizedId, cell))
                {
                    var userRequest = new RequestUserKey(kvp.Key, kvp.Value.RequestId);
                    computeCells![cell.TileLocalizedId].RequestIds.Add(userRequest.UserId, userRequest);
                    result.UserCount++;
                    result.RequestCount++;
                    continue;
                }

                result.CellCount++;
                result.UserCount++;
            }

            return result;
        }

        private void RemovePreflightValue(RequestUserKey userRequest, TileKey tile, ref TilingResult result, int requestCellId, ClientCell cell)
        {

            if (_computeIds.TryGetValue(tile, out var computeCells))
            {
                if (computeCells.TryGetValue(requestCellId, out var cellRequests))
                {
                    cellRequests.RequestIds.Remove(userRequest.UserId);
                    if (cellRequests.RequestIds.Count == 0)
                        computeCells.Remove(requestCellId);

                    if (computeCells.Count == 0)
                        _computeIds.TryRemove(tile, out var _);

                    result.DequeuedInflightCount++;

                    _hotCache.TryRemove(new KeyValuePair<Guid, HotcacheValue>(
                    userRequest.UserId,
                    new HotcacheValue(userRequest.RequestId,
                    cell,
                    true)));
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(new TimeSpan(tickInterval));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
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
                    _mergedTilingObject = mergedRest;

                    if (this._mergedTilingObject.UserCount > 1_000)
                        this.SignalEvent?.Invoke(this, new GDAL.SignalBatchRunEventArgs() { BatchCollectionNumber = ++this._batchNumber });
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
}
