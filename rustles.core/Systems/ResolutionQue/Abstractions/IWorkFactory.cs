namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    public interface IWorkFactory<TResult>
    {
        // Creates the TaskCompletionSource
        // If the batchrunner takes in a bridge that automatically sets the result...
        // the method that runs inside the BatchDispatcher must handle setting of the result into
        // the taskresult object
        WorkItem<TResult> CreateWorker(Func<TResult> request, CancellationToken token);
    }
}
