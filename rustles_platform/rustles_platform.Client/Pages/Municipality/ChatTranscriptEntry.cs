using rustles.contracts;

namespace rustles_platform.Client.Pages.Municipality;

public enum ChatEntryKind
{
    System,
    Message
}

public sealed record ChatTranscriptEntry(
    ChatEntryKind Kind,
    string Title,
    string Body,
    DateTimeOffset? Timestamp = null,
    bool IsOwn = false,
    Guid? MessageId = null,
    Guid? ReplyToMessageId = null,
    string? ReplyToDisplayName = null,
    string? ReplyToPreview = null,
    IReadOnlyList<MunipReactionSummary>? Reactions = null,
    bool IsPinned = false);

public sealed record ChatReactionSelection(Guid? MessageId, string Emoji);
