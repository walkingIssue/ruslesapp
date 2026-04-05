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
    }
}
