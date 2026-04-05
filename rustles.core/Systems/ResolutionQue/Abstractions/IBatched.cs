namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    //handles the logic for the comprised batches, it will interface with the completionsink
    public interface IBatched<TResult, TReference>
    {
        // this is nessecary to corralte a response together with a caller...?
        // in a way yes.. because the tasks will complete in a different order but.. 
        // once they're up and running?? i don't really care for getting the requests back in order that they were placed

        Dictionary<TReference, WorkItem<TResult>> WorkBatch { get; }
        /// <summary>
        /// there's still the issue of correct queing.
        /// </summary>
        ICollection<WorkItem<TResult>> WorkQue { get; }
        /// <summary>
        /// implementational detail, this should be handled by this class
        /// </summary>
        ICollection<TReference> Dequed { get; }
        /// <summary>
        /// Return the next batch that will run.
        /// </summary>
        /// <param name="deque"></param>
        /// <returns></returns>
        ICollection<Func<Task>> GetNextBatch(int? amount = null);
    }
}
