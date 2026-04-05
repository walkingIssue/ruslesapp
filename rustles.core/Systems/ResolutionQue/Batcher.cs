using rustles.core.Entities.DTOs;
using rustles.core.Entities;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Shared;
using rustles.core.Systems.GDAL;
using rustles.core.Systems.ResolutionQue.Abstractions;
using rustles.core.Events;

namespace rustles.core.Systems.ResolutionQue
{
    public class Batcher : IBatcher<PositionResolution<AssignUserRoom, int>, ClientPosition>, ISignalBatchFull
    {
        readonly IBatched<AssignUserRoom, ClientPosition> _batched;
        readonly IGeoRasterAccess<AssignUserRoom> _raster;
        readonly IWorkFactory<AssignUserRoom> _workFactory;
        public Batcher(
            IGeoRasterAccess<AssignUserRoom> rasterAccess, 
            IBatched<AssignUserRoom, ClientPosition> batched,
            IWorkFactory<AssignUserRoom> workFactory
            )
        {
            _batched = batched;
            _raster = rasterAccess;
            _workFactory = workFactory;
        }

        public event ISignalBatchFull.SignalBatchRun SignalEvent;
        
        public void DeQue(ClientPosition request)
        {
            _batched.Dequed.Add(request);
        }
        /// <summary>
        /// Decide what batch to add too, 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public PositionResolution<AssignUserRoom, int> Enque(ClientPosition request, CancellationToken token)
        {
            var t = _workFactory.CreateWorker(
                () => _raster.ResolvePosition(
                    request.longi,
                    request.latti), token);

            _batched.WorkQue.Add(t);
            _batched.WorkBatch.Add(request, t);

            SignalBatchRun(_batched.GetNextBatch(), 0);

            return new PositionResolution<AssignUserRoom, int>(
                new ValueTask<AssignUserRoom>(t.Result),
                () => 0,
                ActionResult.Ok);
        }

        public ValueTask<PositionResolution<AssignUserRoom, int>> EnqueAsync(ClientPosition request, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        private void SignalBatchRun(ICollection<Func<Task>> currentBatch, int collectionIndex)
        {
            if (currentBatch.Count > 55)
                SignalEvent?.Invoke(this, new SignalBatchRunEventArgs() { BatchCollectionNumber = collectionIndex });
        }

        public void Subscribe(ISignalBatchFull.SignalBatchRun batchRun)
        {
            SignalEvent += batchRun;
        }
    }
}
