using rustles.core.Entities.DTOs;
using rustles.core.Systems.GDAL.Enums;

namespace rustles.core.Systems.MessageHandler
{
    /// <summary>
    /// Boundry for Messages 
    /// </summary>
    public interface IManageUserRoomMessage
    {
        UserActionResult PostUserMessage(Guid userId, string message);
        UserActionResult RemoveUserMessage(Guid userId, Guid messageId);
        UserActionResult EditUserMessage(Guid userId, Guid MessageId, string message);
        UserActionResult PostUserMessageInterAction(Guid UserId, Guid MessageId, UserInterAction interaction, string? message = null);
        UserActionResult RemoveUserMessageInterAction(Guid UserId, Guid MessageId, UserInterAction interaction, string? message =null);

        Task<UserActionResult> PostUserMessageAsync(Guid userId, string message);
        Task<UserActionResult> RemoveUserMessageAsync(Guid userId, Guid messageId);
        Task<UserActionResult> EditUserMessageAsync(Guid userId, Guid MessageId, string message);
        Task<UserActionResult> PostUserMessageInterActionAsync(Guid UserId, Guid MessageId, string? message = null);
        Task<UserActionResult> RemoveUserMessageInterActionAsync(Guid UserId, Guid MessageId, string? message = null);
    }
}
