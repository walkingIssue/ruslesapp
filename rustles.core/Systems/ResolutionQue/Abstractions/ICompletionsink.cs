namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    public interface ICompletionsink<TResult>
    {
        Task HandleCallerPromise();
    }
}
