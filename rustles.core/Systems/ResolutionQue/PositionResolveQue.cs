using rustles.core.Entities.DTOs;
using rustles.core.Entities;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Shared;
using System.Runtime.InteropServices.Marshalling;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.Systems.ResolutionQue
{

    //what consumes this? nothing it's a toplevel method that 1. gets consumed by a "engine" 2. gets cosumed by the application layer directly
    public class PositionResolveQue<U> : IRequestPostion<U>
    {
        readonly IBatcher<AssignUserRoom, ClientPosition> _batcher;
        readonly IFactory<PositionResolution<AssignUserRoom, U>> _posResFactory;

        IOrderedEnumerable<int> _inputs;

        public PositionResolveQue(
            IBatcher<AssignUserRoom, ClientPosition> batcher,
            IFactory<PositionResolution<AssignUserRoom, U>> posResFactory)
        {
            _batcher = batcher;
            _posResFactory = posResFactory;
        }
        public virtual void DequeRequest(ref ClientPosition position)
        {
            _batcher.DeQue(position);
        }

        public virtual PositionResolution<AssignUserRoom, U> QueResultionRequest(
            ClientPosition    pos,
            Action<int, U>    progressCallback,
            CancellationToken token)
            /*
             * there has to be some way to.. at this stage start the process of deciding the tiling/windowing for the read
             * can of worms that can wait 
             */
        {
            _inputs = _inputs.Append(pos.GetHashCode()).Order();

            var callerTask = _batcher.Enque(pos, token);
            //public record PositionResolution<T, U>(ValueTask<T> Awaiter, Func<U> ProgressStatus, ActionResult Result);
            return _posResFactory.Create(callerTask, () => ProgressReport(progressCallback), ActionResult.Ok);
        }
        U ProgressReport(Action<int, U> handler)
        {
            U? def = default;
            handler(0, def);
            if (def is null)
                throw new ArgumentException("You, in fact, didnt handle the instantiation correctly :)");

            return def;
        }
        /// <summary>
        /// Not sure if there will be a need for a awaitable implementation...
        /// actually there will be, if we create a max amount of batches, and won't be able to create any more
        /// this will just hang the thread....
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="progressCallback"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">call this at your own risk</exception>
        public virtual async Task<PositionResolution<AssignUserRoom, U>> QueResultionRequestAsync(ClientPosition pos, Action<int, U> progressCallback, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
