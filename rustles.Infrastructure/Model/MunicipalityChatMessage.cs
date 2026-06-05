using System.ComponentModel.DataAnnotations;

namespace rustles.Infrastructure.Model;

public class MunicipalityChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public string MunicipalityName { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    [Required]
    [StringLength(64)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Body { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    [StringLength(500)]
    public string? DeletionReason { get; set; }

    public Guid? ReplyToMessageId { get; set; }

    public MunicipalityChatMessage? ReplyToMessage { get; set; }

    public ICollection<MunicipalityChatMessage> Replies { get; set; } = [];

    public ICollection<MunicipalityMessageReaction> Reactions { get; set; } = [];
}
