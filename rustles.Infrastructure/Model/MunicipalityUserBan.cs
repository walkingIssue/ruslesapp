using System.ComponentModel.DataAnnotations;

namespace rustles.Infrastructure.Model;

public sealed class MunicipalityUserBan
{
    public long Id { get; set; }

    [Required]
    [StringLength(128)]
    public string MunicipalityName { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    [Required]
    public string BannedByUserId { get; set; } = string.Empty;

    public User BannedByUser { get; set; } = null!;

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiresAt { get; set; }
}
