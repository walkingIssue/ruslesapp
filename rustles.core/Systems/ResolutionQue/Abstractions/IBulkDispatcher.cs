namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    //gets cosumed by the background process.... but also handles the starting and stopping of the tasks
    // effectivly this part creates the lumped task that acts as a abstraction for all the incoming requests
    public interface IBulkDispatcher : IBatchFlusher
    {
        ValueTask StartAsync(IEnumerable<Func<Task>> worker, CancellationToken token);
        ValueTask StopAsync(CancellationToken token);
    }
}
