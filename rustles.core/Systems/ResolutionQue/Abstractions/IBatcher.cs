namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    /*
     * 1. Parameterized Dispatch: X amount of work, or Y amount of time
     * 2. Fixed amount of workers: deploy 5-6 threads
     * 3. Varying amount of Dispatchers/Buffers -> if workbuffer overflows during previous work run
     *      (5% of reads have a tendency to 100x the average time, 15% 5x average worktime) 
     *      ->if unlucky run instantiate a new buffer/Dispatcher to handle incoming requests.
     *      ->autoscaling gets "built in" granted it's a very naive implmentation
     * 4. Simplified interface to application layer returns Task<Tresult> inputs paramsDto [X]
     * 5. Wrapped in a backgroundprocess continuesly running on the host
     */


    // Batcher returns a valuetask to the caller (application layer facing wrapper)
    //
    // Another issue, the batcher needs to be shared(singleton instace) across x amount of requests..
    // but eventually has to get supreseded by a different instance 
    //
    // lifetime handling will be a bitch... think it's solved, the actual batched work references will be handled here
    //  well somehow they have to find their way into the dispatcher...
    // 
    // Do i need a Flush method? if the enque doesnt lazily call into the batcher, then the batcher can instantiate the return
    // task and immediately the caller has a reference to their task which supposedly is ready for processing
    //
    // Flush would return... not consumer facing.. this is a detail of the system...
    // Flush here means -> the caller has the logic for when the batch should be ran.
    //  It would seem completely logical that flush would be here, since enque is here, ergo the batch of tasks to run is here
    //
    // This also means that whatever implements this type has to consume the BulkDispatcher, which is fine...?
    //
    //  
    public interface IBatcher<TResult, TReference>
    {
        //implementation detail
        //ICollection<IBatched<TResult, TReference>> Batch { get; }

        /// <summary>
        /// DO NOT FOR THE LIFE OF ME AWAIT THIS, THIS VALUETASK REFERENCE GETS THROWN BACK TO THE CALLER
        /// Creates the ValueTask object to return to the caller, schedules into the next batch
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        TResult Enque(TReference request, CancellationToken token);

        /// <summary>
        /// this has to be here in order to account for when the amount of threads is maxxed and we can't dispatch another run
        /// otherwise the host will hang... granted i have no clue how to even start implementing that :)
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <exception cref="NotImplementedException">Not sure if there's a use for this</exception>
        /// <returns></returns>
        ValueTask<TResult> EnqueAsync(TReference request, CancellationToken token);

        /// <summary>
        /// Inserts the dequed object refence into the deque collection, so we don't resolve a position unnessecarily
        /// </summary>
        /// <param name="request"></param>
        void DeQue(TReference request);
    }
}
