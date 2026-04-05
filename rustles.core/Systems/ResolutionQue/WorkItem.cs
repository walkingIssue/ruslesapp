using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{
    //
    // The result is the task from the taskcompletionsource, what i still need the func doing the work
    // 
    public sealed record WorkItem<TResult>(Task<TResult> Result, ICompletionsink<TResult> Sink);
}
