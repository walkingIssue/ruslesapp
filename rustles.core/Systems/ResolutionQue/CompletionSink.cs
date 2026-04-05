using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{
    public class CompletionSink<TResult> : ICompletionsink<TResult>
    {
        readonly TaskCompletionSource<TResult> _source;
        readonly Func<TResult> _work;
        readonly CancellationToken _token;
        public CompletionSink(
            TaskCompletionSource<TResult> source,
            Func<TResult> work,
            CancellationToken token)
        {
            _source = source;
            _work = work;
            _token = token;
        }

        public Task HandleCallerPromise()
        {
            return new Task(() =>
            {
                try {
                    TResult? res = default;

                    if(!_token.IsCancellationRequested)
                        res = _work();

                    if (_token.IsCancellationRequested)
                        _source.TrySetCanceled(_token);

                    if(!_token.IsCancellationRequested && res is not null)
                        _source.TrySetResult(res);

                    throw new Exception("Work had no return");
                }
                catch(Exception exc)
                {
                    _source.TrySetException(exc);
                    return;
                }
            },
            _token,
            TaskCreationOptions.PreferFairness | 
            TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
