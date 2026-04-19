namespace rustles.application.Web.Realtime;

public sealed record PositionResolveRequest(Guid UserId, double Latitude, double Longitude);

public sealed record PositionResolveQueued(Guid RequestId, string Action);

public sealed record PositionResolveRejected(string Action);

public sealed record MunicipalityAssignment(Guid UserId, int MunicipalityId, string MunicipalityName);

public sealed record ChatMessageDto(
    Guid MessageId,
    Guid UserId,
    int MunicipalityId,
    string Message,
    DateTimeOffset SentAt);
