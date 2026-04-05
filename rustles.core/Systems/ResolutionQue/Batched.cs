using rustles.core.Entities;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{
    /// <summary>
    /// In all Honesty this class needs to become a ICollection cus we need to override some of the ICollection implementations methods
    /// this does in fact need a rewrite
    /// </summary>
    public class Batched : IBatched<AssignUserRoom, ClientPosition>
    {
        public Dictionary<ClientPosition, WorkItem<AssignUserRoom>> WorkBatch { get; private set; } = new();

        public ICollection<WorkItem<AssignUserRoom>> WorkQue { get; private set; } = [];

        public ICollection<ClientPosition> Dequed { get; private set; } = [];
        /// <summary>
        /// Dumps que in its entirety if batchamount not specified. :)
        /// </summary>
        /// <param name="batchAmount"></param>
        /// <returns></returns>
        public ICollection<Func<Task>> GetNextBatch(int? batchAmount = null)
        {
            List<WorkItem<AssignUserRoom>> dequed = new();
            List<ClientPosition> remove = new();
            IEnumerable<WorkItem<AssignUserRoom>>? toRun = null;
            foreach(var a in Dequed)
            {
                if (WorkBatch.TryGetValue(a, out var workItem))
                {
                    dequed.Add(workItem);
                    remove.Add(a);
                    WorkBatch.Remove(a);
                }
            }

            Dequed = Dequed.Except(remove).ToList();
            WorkQue = WorkQue.Except(dequed).ToList();

            if (batchAmount.HasValue)
                toRun = WorkQue.Take(batchAmount.Value);

            if(toRun is null)
                toRun = WorkQue;

            return toRun
                .Select<WorkItem<AssignUserRoom>, Func<Task>>(x => x.Sink.HandleCallerPromise)
                .ToList();
        }
    }
}
