using rustles.core.Systems.GDAL;

namespace rustles.core.Events
{
    public interface ISignalBatchFull
    {
        delegate void SignalBatchRun(object sender, SignalBatchRunEventArgs eventARgs);
        event SignalBatchRun SignalEvent;
    }
}
