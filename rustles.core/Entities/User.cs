using rustles.core.Abstractions;

namespace rustles.core.Entities
{
    public class User
    {
        public Guid UId { get; }
        public RasterPosition Position { get; }
        public ICollection<Room> Rooms { get; }
        public IPositionFacade PositionFacade { get; }
        public IRoomFacade RoomFacade { get; set; }
    }
}
