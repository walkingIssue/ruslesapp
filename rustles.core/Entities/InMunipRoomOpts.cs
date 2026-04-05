namespace rustles.core.Entities
{
    //Create Rooms?
    //public record MunipOrigin(Guid munipId, int geoId, )
    public record InMunipRoomOpts<T>(
        bool? isPrivate,
        bool? isTransient,
        bool? isLocationTied,
        Position? position,
        bool? adverstise,
        int? maxUsers,
        bool? shareHistory,
        T? roomContextual = null) where T : class
    {
        bool IsPrivate { get; init; } = isPrivate ?? false;
        bool IsTransient { get; init; } = isTransient ?? true;
        bool IsLocationTied { get; init; } = isLocationTied ?? false;
        Position? Position { get; init; } = position;
        bool Advertise { get; init; } = adverstise ?? false;
        int? maxUsers { get; init; } = maxUsers ?? int.MaxValue;
        bool ShareHistory { get; init; } = shareHistory ?? false;
        T? RoomContextual { get; set; } = roomContextual;
    }


}
