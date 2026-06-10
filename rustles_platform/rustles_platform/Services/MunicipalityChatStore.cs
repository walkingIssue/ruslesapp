using Microsoft.EntityFrameworkCore;

using rustles.Infrastructure;
using rustles.Infrastructure.Model;
using rustles.contracts;

namespace rustles_platform.Services;

public interface IMunicipalityChatStore
{
    Task<IReadOnlyList<MunipChatMessage>> GetCatchupAsync(string municipalityName, CancellationToken cancellationToken);
    Task<IReadOnlyList<MunipChatMessage>> GetPinnedMessagesAsync(string municipalityName, CancellationToken cancellationToken);
    Task<bool> IsUserBannedAsync(string municipalityName, string userId, CancellationToken cancellationToken);
    Task<MunipChatMessage> AddMessageAsync(
        string municipalityName,
        string userId,
        string displayName,
        string body,
        Guid? replyToMessageId,
        CancellationToken cancellationToken);
    Task<MunipReactionUpdate?> AddReactionAsync(
        string municipalityName,
        string userId,
        Guid messageId,
        string emoji,
        CancellationToken cancellationToken);
    Task<MunicipalityModerationResult?> DeleteMessageAsync(
        Guid messageId,
        string moderatorUserId,
        string? reason,
        CancellationToken cancellationToken);
    Task<MunicipalityModerationResult?> BanMessageAuthorAsync(
        Guid messageId,
        string moderatorUserId,
        string? reason,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);
}

public sealed record MunicipalityModerationResult(
    string MunicipalityName,
    Guid MessageId,
    string TargetUserId);

public sealed class MunicipalityChatStore : IMunicipalityChatStore
{
    public const int CatchupMessageLimit = 20;
    public const int PinnedMessageLimit = 2;
    public const int MaxMessageLength = 500;
    public const int MaxEmojiLength = 24;

    private readonly UsersDataContext _dbContext;
    private readonly IClock _clock;

    public MunicipalityChatStore(UsersDataContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<IReadOnlyList<MunipChatMessage>> GetCatchupAsync(
        string municipalityName,
        CancellationToken cancellationToken)
    {
        var pinnedIds = (await GetPinnedMessagesAsync(municipalityName, cancellationToken))
            .Select(message => message.MessageId)
            .ToHashSet();

        var messages = await BaseMessageQuery()
            .Where(message => message.MunicipalityName == municipalityName)
            .ToListAsync(cancellationToken);

        return messages
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Take(CatchupMessageLimit)
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.Id)
            .Select(message => ToDto(message, pinnedIds.Contains(message.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<MunipChatMessage>> GetPinnedMessagesAsync(
        string municipalityName,
        CancellationToken cancellationToken)
    {
        var messages = await BaseMessageQuery()
            .Where(message => message.MunicipalityName == municipalityName && message.Reactions.Count > 1)
            .ToListAsync(cancellationToken);

        return messages
            .Select(message => new
            {
                Message = message,
                Score = CalculateReactionVelocity(message.Reactions),
                LatestReactionAt = message.Reactions.Max(reaction => reaction.ReactedAt)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.LatestReactionAt)
            .Take(PinnedMessageLimit)
            .Select(item => ToDto(item.Message, isPinned: true))
            .ToList();
    }

    public async Task<MunipChatMessage> AddMessageAsync(
        string municipalityName,
        string userId,
        string displayName,
        string body,
        Guid? replyToMessageId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(municipalityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var trimmed = body.Trim();
        if (trimmed.Length > MaxMessageLength)
            throw new ArgumentOutOfRangeException(nameof(body), $"Messages cannot exceed {MaxMessageLength} characters.");

        MunicipalityChatMessage? replyToMessage = null;
        if (replyToMessageId is not null)
        {
            replyToMessage = await _dbContext.MunicipalityChatMessages
                .SingleOrDefaultAsync(
                    message => message.Id == replyToMessageId && message.MunicipalityName == municipalityName,
                    cancellationToken);

            if (replyToMessage is null)
                throw new InvalidOperationException("Reply target does not exist in the current municipality.");
        }

        var message = new MunicipalityChatMessage
        {
            MunicipalityName = municipalityName,
            UserId = userId,
            DisplayName = displayName,
            Body = trimmed,
            SentAt = _clock.UtcNow,
            ReplyToMessageId = replyToMessage?.Id,
            ReplyToMessage = replyToMessage
        };

        _dbContext.MunicipalityChatMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(message, isPinned: false);
    }

    public Task<bool> IsUserBannedAsync(
        string municipalityName,
        string userId,
        CancellationToken cancellationToken) =>
        HasActiveBanAsync(municipalityName, userId, cancellationToken);

    public async Task<MunipReactionUpdate?> AddReactionAsync(
        string municipalityName,
        string userId,
        Guid messageId,
        string emoji,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(municipalityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);

        var normalizedEmoji = emoji.Trim();
        if (normalizedEmoji.Length > MaxEmojiLength)
            throw new ArgumentOutOfRangeException(nameof(emoji), $"Emoji reactions cannot exceed {MaxEmojiLength} characters.");

        var messageExists = await _dbContext.MunicipalityChatMessages
            .AnyAsync(
                message => message.Id == messageId
                    && message.MunicipalityName == municipalityName
                    && message.DeletedAt == null,
                cancellationToken);

        if (!messageExists)
            return null;

        _dbContext.MunicipalityMessageReactions.Add(new MunicipalityMessageReaction
        {
            MunicipalityChatMessageId = messageId,
            UserId = userId,
            Emoji = normalizedEmoji,
            ReactedAt = _clock.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var reactions = await GetReactionSummaryAsync(messageId, cancellationToken);
        return new MunipReactionUpdate(messageId, reactions);
    }

    public async Task<MunicipalityModerationResult?> DeleteMessageAsync(
        Guid messageId,
        string moderatorUserId,
        string? reason,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moderatorUserId);

        var message = await _dbContext.MunicipalityChatMessages
            .SingleOrDefaultAsync(item => item.Id == messageId && item.DeletedAt == null, cancellationToken);

        if (message is null)
            return null;

        message.DeletedAt = _clock.UtcNow;
        message.DeletedByUserId = moderatorUserId;
        message.DeletionReason = NormalizeReason(reason);

        var replies = await _dbContext.MunicipalityChatMessages
            .Where(item => item.ReplyToMessageId == messageId)
            .ToListAsync(cancellationToken);

        foreach (var reply in replies)
            reply.ReplyToMessageId = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MunicipalityModerationResult(message.MunicipalityName, message.Id, message.UserId);
    }

    public async Task<MunicipalityModerationResult?> BanMessageAuthorAsync(
        Guid messageId,
        string moderatorUserId,
        string? reason,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moderatorUserId);

        var message = await _dbContext.MunicipalityChatMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == messageId, cancellationToken);

        if (message is null || message.UserId == moderatorUserId)
            return null;

        var now = _clock.UtcNow;
        var alreadyBanned = await HasActiveBanAsync(
            message.MunicipalityName,
            message.UserId,
            cancellationToken);

        if (!alreadyBanned)
        {
            _dbContext.MunicipalityUserBans.Add(new MunicipalityUserBan
            {
                MunicipalityName = message.MunicipalityName,
                UserId = message.UserId,
                BannedByUserId = moderatorUserId,
                Reason = NormalizeReason(reason),
                CreatedAt = now,
                ExpiresAt = expiresAt
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new MunicipalityModerationResult(message.MunicipalityName, message.Id, message.UserId);
    }

    private IQueryable<MunicipalityChatMessage> BaseMessageQuery()
    {
        return _dbContext.MunicipalityChatMessages
            .AsNoTracking()
            .Where(message => message.DeletedAt == null)
            .Include(message => message.Reactions)
            .Include(message => message.ReplyToMessage);
    }

    private async Task<bool> HasActiveBanAsync(
        string municipalityName,
        string userId,
        CancellationToken cancellationToken)
    {
        var bans = await _dbContext.MunicipalityUserBans
            .AsNoTracking()
            .Where(ban => ban.MunicipalityName == municipalityName && ban.UserId == userId)
            .Select(ban => ban.ExpiresAt)
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        return bans.Any(expiresAt => expiresAt is null || expiresAt > now);
    }

    private async Task<IReadOnlyList<MunipReactionSummary>> GetReactionSummaryAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var summaries = await _dbContext.MunicipalityMessageReactions
            .Where(reaction => reaction.MunicipalityChatMessageId == messageId)
            .GroupBy(reaction => reaction.Emoji)
            .Select(group => new { Emoji = group.Key, Count = group.Count() })
            .OrderByDescending(summary => summary.Count)
            .ThenBy(summary => summary.Emoji)
            .ToListAsync(cancellationToken);

        return summaries
            .Select(summary => new MunipReactionSummary(summary.Emoji, summary.Count))
            .ToList();
    }

    private static MunipChatMessage ToDto(MunicipalityChatMessage message, bool isPinned)
    {
        return new MunipChatMessage(
            message.Id,
            message.DisplayName,
            message.Body,
            message.SentAt,
            message.ReplyToMessageId,
            message.ReplyToMessage?.DisplayName,
            CreateReplyPreview(message.ReplyToMessage?.Body),
            CreateReactionSummary(message.Reactions),
            isPinned);
    }

    private static IReadOnlyList<MunipReactionSummary> CreateReactionSummary(
        IEnumerable<MunicipalityMessageReaction> reactions)
    {
        return reactions
            .GroupBy(reaction => reaction.Emoji)
            .Select(group => new MunipReactionSummary(group.Key, group.Count()))
            .OrderByDescending(summary => summary.Count)
            .ThenBy(summary => summary.Emoji)
            .ToList();
    }

    private static string? CreateReplyPreview(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        return body.Length <= 80 ? body : $"{body[..77]}...";
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;

        var trimmed = reason.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }

    private static double CalculateReactionVelocity(IEnumerable<MunicipalityMessageReaction> reactions)
    {
        var ordered = reactions
            .OrderBy(reaction => reaction.ReactedAt)
            .ToList();

        if (ordered.Count < 2)
            return 0;

        var duration = ordered[^1].ReactedAt - ordered[0].ReactedAt;
        var seconds = Math.Max(duration.TotalSeconds, 0.001);

        return ordered.Count / seconds;
    }
}
