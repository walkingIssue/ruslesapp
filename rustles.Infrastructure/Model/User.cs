

namespace rustles.Infrastructure.Model;


public class User
{
    public Guid UId { get; set; }
    public Guid? OAuthId { get; set; }
    public Guid? OIDCId { get; set; }
    public ICollection<Consent>? ContestId { get; set; } = [];
    public ICollection<Message>? Messages { get; set; }
    public ICollection<Room>? Rooms { get; set; }
    public ICollection<User>? Friends { get; set; }
    public ICollection<Conversation>? Conversations { get; set; }
    public ICollection<Position>? Positions { get; set; }
}

