using rustles.core.Shared;
using rustles.core.Systems.GDAL.Enums;

namespace rustles.core.Entities.DTOs
{
    public record UserActionResult(Guid UserId, SystemAction Result, Exception? exception, string? Value, UserActionType UserAction);


}
