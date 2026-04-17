using Microsoft.Extensions.Hosting;

namespace rustles.core.Systems.ResolutionQue
{
    /// <summary>
    /// Legacy dispatcher kept as a placeholder while the channel-based pipeline replaces it.
    /// </summary>
    [Obsolete("Replaced by BatchFlusher, TileWorkResolver, and CompletionSink.")]
    public sealed class BulkDispatcher : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
