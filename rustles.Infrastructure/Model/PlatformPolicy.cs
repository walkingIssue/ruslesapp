using System.ComponentModel.DataAnnotations;

namespace rustles.Infrastructure.Model;

public class PlatformPolicy
{
    public int Id { get; private set; }

    [Required]
    public string PolicyKey { get; set; } = string.Empty;

    [Required]
    public string Version { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserPolicyAcceptance> UserAcceptances { get; set; } = [];
}
