using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{
    //
    // The result is the task from the taskcompletionsource
    // 
    public sealed record Tile<TWorkItem>(ICollection<TWorkItem> cells);
}
