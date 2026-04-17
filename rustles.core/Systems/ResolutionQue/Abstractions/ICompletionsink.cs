using rustles.core.Entities;

using System.Threading.Channels;

namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    public interface ICompletionsink
    {
        Channel<AssignUserRoom> UserRoomChannel { get; }
    }
}
