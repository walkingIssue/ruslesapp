using Microsoft.Extensions.Hosting;

using rustles.core.Events;

using System.Diagnostics;
using System.Threading.Channels;

namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    public interface IBatchFlusher
    {
            
        /// <summary>
        /// Initiates a run, clears the batch... do i have to lock that batch...? probably but i just won't 
        /// </summary>
        /// <param name="token">DO NOT FOR THE LIFE OF ME MANAGE TO TIE THIS TOKEN TO A RANDOM REQUEST
        /// IT'LL KILL THE ENTIRE BATCH</param>
        /// <returns></returns>
        ValueTask Flush(CancellationToken token);
        Channel<GeoRasterWorkItem> GeotileWorkChannel { get; }
    }


    public class BatchFlusher : BackgroundService, IBatchFlusher
    {
        private readonly SemaphoreSlim _runlock = new(1, 1);
        private readonly SemaphoreSlim _flushSignal = new(0, 1);

        private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);
        private long _lastFlushedTime = 0;
        private int _BatchesReady = 0;

        private readonly ITileBatcher<TileKey> _tileBatcher;

        public BatchFlusher(ITileBatcher<TileKey> tileBatcher, ISignalBatchFull signalBatchFull) 
        {
            _tileBatcher = tileBatcher;
            _channel = Channel.CreateBounded<GeoRasterWorkItem>(new BoundedChannelOptions(100)
            {
                SingleReader = false,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropWrite,
                AllowSynchronousContinuations = true,
            });
            signalBatchFull.SignalEvent += SignalBatchFull_SignalEvent;
        }

        private void SignalBatchFull_SignalEvent(object sender, GDAL.SignalBatchRunEventArgs eventArgs)
        {
            Interlocked.Exchange(ref _BatchesReady, eventArgs.BatchCollectionNumber);

            try
            {
                _flushSignal.Release();
            }
            catch (SemaphoreFullException)
            {
                //Flush is already pending; collapse duplicate full-batch signals into one run.
            }
        }

        private readonly Channel<GeoRasterWorkItem> _channel;
        public Channel<GeoRasterWorkItem> GeotileWorkChannel => _channel;

        public async ValueTask Flush(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;


            var dequedSnapShot = _tileBatcher.Dequed.ToArray();
            //local snapshot of current ComputeIds
            var tileKeys = _tileBatcher.ComputeIds.ToArray();

            var dequedIds = dequedSnapShot.Select(x => x.Value)
                .ToHashSet(/*impolement requestidentitycomparer*/);

            
            for(int i = 0; i < tileKeys.Length; i++)
            {
                if (token.IsCancellationRequested)
                    return;

                var tileCellsTransformed = tileKeys[i].Value.Values.ToArray();
                var geoRasterWorkList = new SortedList<int, IReadOnlyList<RequestUserKey>>();
                for(int j =0; j < tileCellsTransformed.Length; j++)
                {
                    if (token.IsCancellationRequested)
                        return;

                    var requestIds = tileCellsTransformed[j].RequestIds.Values.ToArray();
                    geoRasterWorkList.Add(tileCellsTransformed[j].TileLocalizedId, requestIds);

                    //clean the hotcache, the value will now be computed, and pushed to the client.
                    //unless the client disconnected at which point the completionsink handles that.
                    for(int k = 0; k < requestIds.Length; k++)
                       _tileBatcher.HotCache.TryRemove(
                           new KeyValuePair<Guid, HotcacheValue>(tileCellsTransformed[j].RequestIds[k].UserId,
                           new HotcacheValue(tileCellsTransformed[j].RequestIds[k].RequestId, tileCellsTransformed[j].Cell,
                           true)));
                }

                var finnshedWorkItem = new GeoRasterWorkItem(tileKeys[i].Key, geoRasterWorkList);

                if (!_channel.Writer.TryWrite(finnshedWorkItem))
                    await _channel.Writer.WriteAsync(finnshedWorkItem, token);

                
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //This returns on either signal or timeout. A false result means the periodic drain elapsed.
                await _flushSignal.WaitAsync(_interval, stoppingToken);

                //Waits for the current timer Flushjob
                if (!await _runlock.WaitAsync(0, stoppingToken))
                    continue;

                var sw = Stopwatch.StartNew();

                try
                {
                    await Flush(stoppingToken);
                }
                finally
                {
                    sw.Stop();
                    Interlocked.Exchange(ref _lastFlushedTime, sw.Elapsed.Ticks);
                    _flushSignal.Wait(0); //reset the semaphore to 1.
                    _runlock.Release();
                }
            }
        }
    }
    public class GeoRasterWorkItem(TileKey tilekey, SortedList<int, IReadOnlyList<RequestUserKey>> rasterWork)
    {
        public TileKey TileKey => tilekey;
        public SortedList<int, IReadOnlyList<RequestUserKey>> TileWork => rasterWork;
    };
}
