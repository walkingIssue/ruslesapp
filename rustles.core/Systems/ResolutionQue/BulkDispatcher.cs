using rustles.core.Entities;
using Microsoft.Extensions.Hosting;
using rustles.core.Systems.GDAL;
using rustles.core.Systems.ResolutionQue.Abstractions;
using rustles.core.Events;

namespace rustles.core.Systems.ResolutionQue
{
    public sealed class BulkDispatcher : BackgroundService, IBulkDispatcher
    {
        readonly IBatched<AssignUserRoom, ClientPosition> _batch;
        readonly ISignalBatchFull _fullSignal;

        readonly SemaphoreSlim _flushSemaphore = new(0, 1);
        readonly SemaphoreSlim _runsSemaphore = new(0,4);

        const int flushMsInterval = 400;

        int lastFlush = 0;    


        List<Func<Task>> currentRun;

        object _batchLock;
        int _readyBatches;
        public BulkDispatcher(IBatched<AssignUserRoom, ClientPosition> batch, ISignalBatchFull fullSignal)
        {
            _batch = batch;
            _fullSignal = fullSignal;
            _readyBatches = 0;
            _fullSignal.SignalEvent += FullSignal_SignalEvent;
        }

        private void FullSignal_SignalEvent(object sender, SignalBatchRunEventArgs eventARgs)
        {
            _readyBatches += 1;
        }

        public async ValueTask Flush(CancellationToken token)
        {
            await _flushSemaphore.WaitAsync(token);
            try
            {
                lock (_batchLock)
                {
                    currentRun.AddRange(_batch.GetNextBatch());
                    lastFlush = DateTime.UtcNow.Millisecond;
                }
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }

        public async ValueTask StartAsync(IEnumerable<Func<Task>> worker, CancellationToken token)
        {
            Task[] curr = worker.Select(x=> x()).ToArray();
            for (int i = 0; i < curr.Length-1; i++)
            {
                curr[i].Start();
            }

            await Task.WhenAll(curr);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stdFlush = new PeriodicTimer(TimeSpan.FromMilliseconds(flushMsInterval));
            var executors = new Task[2];
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_readyBatches > 0)
                {
                    executors[0] = HandleFushSignal(stoppingToken);
                    stdFlush.Period = stdFlush.Period.TotalMilliseconds + 100 < lastFlush + flushMsInterval ?
                        stdFlush.Period : TimeSpan.FromMilliseconds(lastFlush + flushMsInterval);
                }
                await stdFlush.WaitForNextTickAsync(stoppingToken);
                executors[1] = FlushRun(stoppingToken);
                await Task.WhenAll(executors);
            }
        }
        private async Task<List<Func<Task>>> GetProcessReadyBatch(CancellationToken stoppingToken)
        {
             await _flushSemaphore.WaitAsync(stoppingToken);
            try
            {
                await Flush(stoppingToken);
                List<Func<Task>> current = new();
                lock (_batchLock)
                {
                    current.AddRange(currentRun);
                    currentRun.Clear();
                }
                return current;
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }
        private async Task HandleFushSignal(CancellationToken stoppingToken)
        {
            int runCount = _readyBatches;
            Task[] totalClear = new Task[runCount];
            for(int i =0; i < runCount; i++)
            {
                await _runsSemaphore.WaitAsync(stoppingToken);
                try
                {
                   List<Func<Task>> tasks = await GetProcessReadyBatch(stoppingToken);
                   totalClear[i] = StartAsync(tasks, stoppingToken).AsTask();
                }
                finally
                {
                    _runsSemaphore.Release();
                }
            }
            await Task.WhenAll(totalClear);
        }

        private async Task FlushRun(CancellationToken token)
        {
            await _runsSemaphore.WaitAsync(token);
            try
            {
                List<Func<Task>> tasks = await GetProcessReadyBatch(token);
                await StartAsync(tasks, token);
            }
            finally
            {
                _runsSemaphore.Release();
            }
        }
        
        ValueTask IBulkDispatcher.StopAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }


}
