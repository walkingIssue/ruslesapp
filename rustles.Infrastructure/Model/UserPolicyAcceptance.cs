using System.ComponentModel.DataAnnotations;

namespace rustles.Infrastructure.Model;

public class UserPolicyAcceptance
{
    public int Id { get; private set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public int PlatformPolicyId { get; set; }

    public PlatformPolicy PlatformPolicy { get; set; } = null!;

    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;
}
