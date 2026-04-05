using rustles.core.Abstractions;

namespace rustles.core.Entities
{
    public class Room
    {
        public Guid UId { get; }
        public ICollection<User> Users { get; }
        public string RoomName { get; }
        public string RoomPosName { get; }
        public NPos RoomPos { get; }

    }
}