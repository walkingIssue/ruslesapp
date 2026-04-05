using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{
    public class WorkFactory<TResult> : IWorkFactory<TResult>
    {
        public WorkItem<TResult> CreateWorker(Func<TResult> request, CancellationToken token)
        {
            var source = new TaskCompletionSource<TResult>(
                TaskCreationOptions.RunContinuationsAsynchronously
                );

            return new WorkItem<TResult>(source.Task, new CompletionSink<TResult>(source, request, token));
        }
    }
}
