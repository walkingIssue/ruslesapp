using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{
    public class WorkFactory<TResult> : IWorkFactory<TResult>
    {
        public Tile<TResult> CreateWorker(Func<TResult> request, CancellationToken token)
        {
            throw new NotSupportedException(
                "WorkFactory belongs to the legacy TaskCompletionSource pipeline. Use the channel-based resolution pipeline instead.");
        }
    }
}
