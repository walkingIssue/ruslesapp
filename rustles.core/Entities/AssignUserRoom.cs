using Azure.Core;

namespace rustles.core.Entities
{
    public record AssignUserRoom(
        Guid UserId,
        Guid RequestId,
        int MunipId,
        string[] NaturalName);
}