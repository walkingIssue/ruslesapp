using Microsoft.EntityFrameworkCore;

namespace rustles_platform.tests.Services;

public sealed class MunicipalityChatStoreTests
{
    private const string FireEmoji = "\U0001F525";

    [Fact]
    public async Task Migrated_database_supports_chat_catchup_queries()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync(useMigrations: true);

        await fixture.Store.AddMessageAsync(
            "Stockholm",
            fixture.UserId,
            "alice",
            "migrated catchup",
            null,
            CancellationToken.None);

        var catchup = await fixture.Store.GetCatchupAsync("Stockholm", CancellationToken.None);

        var message = Assert.Single(catchup);
        Assert.Equal("migrated catchup", message.Message);
    }

    [Fact]
    public async Task GetCatchupAsync_returns_latest_twenty_messages_in_chronological_order()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync();

        for (var i = 1; i <= 25; i++)
        {
            fixture.Clock.UtcNow = DateTimeOffset.Parse($"2026-01-01T00:00:{i:00}Z");
            await fixture.Store.AddMessageAsync(
                "Stockholm",
                fixture.UserId,
                "alice",
                $"message-{i:00}",
                null,
                CancellationToken.None);
        }

        var catchup = await fixture.Store.GetCatchupAsync("Stockholm", CancellationToken.None);

        Assert.Equal(20, catchup.Count);
        Assert.Equal("message-06", catchup.First().Message);
        Assert.Equal("message-25", catchup.Last().Message);
    }

    [Fact]
    public async Task AddReactionAsync_counts_multiple_reactions_with_the_same_emoji()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync();
        var message = await fixture.Store.AddMessageAsync(
            "Stockholm",
            fixture.UserId,
            "alice",
            "hello",
            null,
            CancellationToken.None);

        await fixture.Store.AddReactionAsync("Stockholm", fixture.UserId, message.MessageId, FireEmoji, CancellationToken.None);
        var update = await fixture.Store.AddReactionAsync("Stockholm", fixture.UserId, message.MessageId, FireEmoji, CancellationToken.None);

        var summary = Assert.Single(update!.Reactions);
        Assert.Equal(FireEmoji, summary.Emoji);
        Assert.Equal(2, summary.Count);
    }

    [Fact]
    public async Task GetPinnedMessagesAsync_returns_two_messages_with_steepest_reaction_velocity()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync();
        var slow = await fixture.Store.AddMessageAsync("Stockholm", fixture.UserId, "alice", "slow heat", null, CancellationToken.None);
        var fast = await fixture.Store.AddMessageAsync("Stockholm", fixture.UserId, "alice", "fast heat", null, CancellationToken.None);
        var fastest = await fixture.Store.AddMessageAsync("Stockholm", fixture.UserId, "alice", "fastest heat", null, CancellationToken.None);

        await ReactAtAsync(fixture, slow.MessageId, 0);
        await ReactAtAsync(fixture, slow.MessageId, 10);

        await ReactAtAsync(fixture, fast.MessageId, 20);
        await ReactAtAsync(fixture, fast.MessageId, 21);

        await ReactAtAsync(fixture, fastest.MessageId, 30);
        await ReactAtAsync(fixture, fastest.MessageId, 31);
        await ReactAtAsync(fixture, fastest.MessageId, 31.2);

        var pinned = await fixture.Store.GetPinnedMessagesAsync("Stockholm", CancellationToken.None);

        Assert.Equal(2, pinned.Count);
        Assert.Equal(fastest.MessageId, pinned[0].MessageId);
        Assert.Equal(fast.MessageId, pinned[1].MessageId);
        Assert.All(pinned, message => Assert.True(message.IsPinned));
    }

    [Fact]
    public async Task AddMessageAsync_preserves_reply_context_for_all_clients()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync();
        var parent = await fixture.Store.AddMessageAsync(
            "Stockholm",
            fixture.UserId,
            "alice",
            "where should this point?",
            null,
            CancellationToken.None);

        var reply = await fixture.Store.AddMessageAsync(
            "Stockholm",
            fixture.SecondUserId,
            "bob",
            "@alice right here",
            parent.MessageId,
            CancellationToken.None);

        Assert.Equal(parent.MessageId, reply.ReplyToMessageId);
        Assert.Equal("alice", reply.ReplyToDisplayName);
        Assert.Equal("where should this point?", reply.ReplyToPreview);
    }

    [Fact]
    public async Task Deleting_user_removes_their_messages_and_reactions_without_deleting_other_users_replies()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync();
        var parent = await fixture.Store.AddMessageAsync("Stockholm", fixture.UserId, "alice", "parent", null, CancellationToken.None);
        var reply = await fixture.Store.AddMessageAsync("Stockholm", fixture.SecondUserId, "bob", "reply", parent.MessageId, CancellationToken.None);
        await fixture.Store.AddReactionAsync("Stockholm", fixture.UserId, reply.MessageId, FireEmoji, CancellationToken.None);

        var user = await fixture.DbContext.Users.SingleAsync(user => user.Id == fixture.UserId);
        fixture.DbContext.Users.Remove(user);
        await fixture.DbContext.SaveChangesAsync();

        Assert.False(await fixture.DbContext.MunicipalityChatMessages.AnyAsync(message => message.Id == parent.MessageId));
        Assert.False(await fixture.DbContext.MunicipalityMessageReactions.AnyAsync(reaction => reaction.UserId == fixture.UserId));

        var survivingReply = await fixture.DbContext.MunicipalityChatMessages.SingleAsync(message => message.Id == reply.MessageId);
        Assert.Null(survivingReply.ReplyToMessageId);
    }

    [Fact]
    public async Task DeleteMessageAsync_removes_message_from_catchup_and_pinned_results()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync();
        var message = await fixture.Store.AddMessageAsync("Stockholm", fixture.UserId, "alice", "delete me", null, CancellationToken.None);
        await fixture.Store.AddReactionAsync("Stockholm", fixture.SecondUserId, message.MessageId, FireEmoji, CancellationToken.None);
        await fixture.Store.AddReactionAsync("Stockholm", fixture.UserId, message.MessageId, FireEmoji, CancellationToken.None);

        var result = await fixture.Store.DeleteMessageAsync(message.MessageId, fixture.SecondUserId, "cleanup", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(await fixture.Store.GetCatchupAsync("Stockholm", CancellationToken.None));
        Assert.Empty(await fixture.Store.GetPinnedMessagesAsync("Stockholm", CancellationToken.None));
    }

    [Fact]
    public async Task BanMessageAuthorAsync_blocks_user_in_that_municipality()
    {
        await using var fixture = await ChatStoreFixture.CreateAsync();
        var message = await fixture.Store.AddMessageAsync("Stockholm", fixture.UserId, "alice", "ban source", null, CancellationToken.None);

        var result = await fixture.Store.BanMessageAuthorAsync(message.MessageId, fixture.SecondUserId, "spam", null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(await fixture.Store.IsUserBannedAsync("Stockholm", fixture.UserId, CancellationToken.None));
        Assert.False(await fixture.Store.IsUserBannedAsync("Uppsala", fixture.UserId, CancellationToken.None));
    }

    private static async Task ReactAtAsync(ChatStoreFixture fixture, Guid messageId, double seconds)
    {
        fixture.Clock.UtcNow = DateTimeOffset.Parse("2026-01-01T00:00:00Z").AddSeconds(seconds);
        await fixture.Store.AddReactionAsync("Stockholm", fixture.UserId, messageId, FireEmoji, CancellationToken.None);
    }
}
