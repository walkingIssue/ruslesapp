using rustles.core.Entities;
using rustles.core.Shared;
using rustles.core.Systems.MessageHandler;
using rustles.core.Systems.RoomManager;

namespace rustles.core.Systems.MunipRoomManager
{
    public interface IMunipManager
    {
        bool CreateMunip(Guid munipId, int geoId, ICollection<User>? users = null);
        bool CreateRoomInMunip<T>(Guid munipId, Guid roomOwner, InMunipRoomOpts<T> roomOpts, ICollection<User?> users = null) where T : class;
        bool RemoveMunipRoom(Guid munipId);
        bool RemoveRoomInMunip(Guid munipId);
    }

    public interface IUserRoomResovler
    {
        ActionResult InsertUser();
        IManageUserRoomMessage InsertUserAsync();
        void RemoveUser();
        Task RemoveUserAsync();
    }
}
