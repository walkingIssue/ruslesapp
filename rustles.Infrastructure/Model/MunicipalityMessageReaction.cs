using System.ComponentModel.DataAnnotations;

namespace rustles.Infrastructure.Model;

public class MunicipalityMessageReaction
{
    public long Id { get; set; }

    public Guid MunicipalityChatMessageId { get; set; }

    public MunicipalityChatMessage MunicipalityChatMessage { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    [Required]
    [StringLength(24)]
    public string Emoji { get; set; } = string.Empty;

    public DateTimeOffset ReactedAt { get; set; } = DateTimeOffset.UtcNow;
}
