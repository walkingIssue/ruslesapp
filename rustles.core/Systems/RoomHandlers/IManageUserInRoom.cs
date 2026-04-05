using rustles.core.Entities.DTOs;

namespace rustles.core.Systems.RoomManager
{
    //
    //Boundry for User
    //
    public interface IManageUserInRoom
    {
        RoomActionResult AddUser(Guid userId, Guid roomId);
        RoomActionResult RemoveUser(Guid userId, Guid roomId);
        Task<RoomActionResult> AddUserAsync(Guid userId, Guid roomId);
        Task<RoomActionResult> RemoveUserAsync(Guid userId, Guid roomId);
    }
}
