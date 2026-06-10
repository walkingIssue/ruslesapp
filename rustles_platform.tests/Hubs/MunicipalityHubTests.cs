using System.Collections.Concurrent;
using System.Security.Claims;

using Microsoft.AspNetCore.SignalR;

using Moq;

using rustles.contracts;
using rustles_platform.Hubs;
using rustles_platform.Services;

namespace rustles_platform.tests.Hubs;

public sealed class MunicipalityHubTests
{
    [Fact]
    public async Task MunipUserMessage_broadcasts_message_to_other_users_in_current_municipality()
    {
        var userId = Guid.NewGuid().ToString();
        var manager = CreateManagerWithConnectedUser(userId, "connection-1", "Stockholm", "alice");
        var recipient = new Mock<IMunipClientMethods>(MockBehavior.Strict);
        var message = new MunipChatMessage(Guid.NewGuid(), "alice", "Hej Stockholm", DateTimeOffset.UtcNow);
        var store = new Mock<IMunicipalityChatStore>(MockBehavior.Strict);
        MunipChatMessage? deliveredMessage = null;

        store
            .Setup(item => item.IsUserBannedAsync("Stockholm", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        store
            .Setup(item => item.AddMessageAsync(
                "Stockholm",
                userId,
                "alice",
                "Hej Stockholm",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        recipient
            .Setup(client => client.MessageByUser(It.IsAny<MunipChatMessage>()))
            .Callback<MunipChatMessage>(message => deliveredMessage = message)
            .Returns(Task.CompletedTask);

        var clients = CreateClientsForGroup("Stockholm", recipient.Object);
        var hub = CreateHub(manager, clients.Object, store.Object, userId, "connection-1");

        await hub.MunipUserMessage(new MunipMessageRequest("Hej Stockholm"));

        Assert.NotNull(deliveredMessage);
        Assert.Equal("alice", deliveredMessage.DisplayName);
        Assert.Equal("Hej Stockholm", deliveredMessage.Message);
        Assert.Equal(message.MessageId, deliveredMessage.MessageId);
        clients.Verify(client => client.Group("Stockholm"), Times.Once);
        recipient.Verify(client => client.MessageByUser(It.IsAny<MunipChatMessage>()), Times.Once);
        store.VerifyAll();
    }

    [Fact]
    public async Task ReactToMessage_broadcasts_reaction_update_and_pinned_messages_to_group()
    {
        var userId = Guid.NewGuid().ToString();
        var manager = CreateManagerWithConnectedUser(userId, "connection-1", "Stockholm", "alice");
        var recipient = new Mock<IMunipClientMethods>();
        var messageId = Guid.NewGuid();
        var update = new MunipReactionUpdate(messageId, [new MunipReactionSummary("🔥", 2)]);
        var pinned = new MunipPinnedMessages([
            new MunipChatMessage(messageId, "alice", "hot take", DateTimeOffset.UtcNow, Reactions: update.Reactions, IsPinned: true)
        ]);
        var store = new Mock<IMunicipalityChatStore>(MockBehavior.Strict);

        store
            .Setup(item => item.AddReactionAsync("Stockholm", userId, messageId, "🔥", It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);
        store
            .Setup(item => item.IsUserBannedAsync("Stockholm", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        store
            .Setup(item => item.GetPinnedMessagesAsync("Stockholm", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pinned.Messages);

        recipient.Setup(client => client.ReactionUpdated(update)).Returns(Task.CompletedTask);
        recipient.Setup(client => client.PinnedMessagesUpdated(It.IsAny<MunipPinnedMessages>())).Returns(Task.CompletedTask);

        var clients = CreateClientsForGroup("Stockholm", recipient.Object);
        var hub = CreateHub(manager, clients.Object, store.Object, userId, "connection-1");

        await hub.ReactToMessage(new MunipReactionRequest(messageId, "🔥"));

        clients.Verify(client => client.Group("Stockholm"), Times.Exactly(2));
        recipient.Verify(client => client.ReactionUpdated(update), Times.Once);
        recipient.Verify(client => client.PinnedMessagesUpdated(It.Is<MunipPinnedMessages>(item => item.Messages.Single().MessageId == messageId)), Times.Once);
        store.VerifyAll();
    }

    [Fact]
    public async Task MunipUserMessage_without_active_ledger_reports_connection_error_to_caller()
    {
        var userId = Guid.NewGuid().ToString();
        var manager = new UserHubManager();
        var store = new Mock<IMunicipalityChatStore>(MockBehavior.Strict);
        var caller = new Mock<IMunipClientMethods>(MockBehavior.Strict);
        MunipChatError? reportedError = null;

        caller
            .Setup(client => client.ConnectionError(It.IsAny<MunipChatError>()))
            .Callback<MunipChatError>(error => reportedError = error)
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubCallerClients<IMunipClientMethods>>(MockBehavior.Strict);
        clients.SetupGet(client => client.Caller).Returns(caller.Object);

        var hub = CreateHub(manager, clients.Object, store.Object, userId, "connection-1");

        await hub.MunipUserMessage(new MunipMessageRequest("this should not leave the caller"));

        Assert.NotNull(reportedError);
        Assert.Equal(rustles.contracts.ConnectionFault.fail, reportedError.Fault);
        Assert.Equal(ChatErrorMessage.error, reportedError.ErrorMessage);
        clients.VerifyGet(client => client.Caller, Times.Once);
        caller.Verify(client => client.ConnectionError(It.IsAny<MunipChatError>()), Times.Once);
    }

    [Fact]
    public async Task MunipUserMessage_when_user_is_banned_reports_connection_error()
    {
        var userId = Guid.NewGuid().ToString();
        var manager = CreateManagerWithConnectedUser(userId, "connection-1", "Stockholm", "alice");
        var store = new Mock<IMunicipalityChatStore>(MockBehavior.Strict);
        var caller = new Mock<IMunipClientMethods>(MockBehavior.Strict);
        MunipChatError? reportedError = null;

        store
            .Setup(item => item.IsUserBannedAsync("Stockholm", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        caller
            .Setup(client => client.ConnectionError(It.IsAny<MunipChatError>()))
            .Callback<MunipChatError>(error => reportedError = error)
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubCallerClients<IMunipClientMethods>>(MockBehavior.Strict);
        clients.SetupGet(client => client.Caller).Returns(caller.Object);

        var hub = CreateHub(manager, clients.Object, store.Object, userId, "connection-1");

        await hub.MunipUserMessage(new MunipMessageRequest("blocked"));

        Assert.NotNull(reportedError);
        Assert.Equal(ChatErrorMessage.clientDenied, reportedError.ErrorMessage);
        store.VerifyAll();
        caller.Verify(client => client.ConnectionError(It.IsAny<MunipChatError>()), Times.Once);
    }

    private static UserHubManager CreateManagerWithConnectedUser(
        string userId,
        string connectionId,
        string municipality,
        string userName)
    {
        var manager = new UserHubManager();
        manager.CurrentUserLedger[userId] =
            new UserHubManager.UserConnectionLedgerEntry(
                municipality,
                Guid.NewGuid(),
                connectionId,
                userName);

        return manager;
    }

    private static MunicipalityHub CreateHub(
        UserHubManager manager,
        IHubCallerClients<IMunipClientMethods> clients,
        IMunicipalityChatStore store,
        string userId,
        string connectionId)
    {
        return new MunicipalityHub(manager, userManager: null!, store)
        {
            Clients = clients,
            Context = CreateContext(userId, connectionId)
        };
    }

    private static Mock<IHubCallerClients<IMunipClientMethods>> CreateClientsForGroup(
        string municipality,
        IMunipClientMethods recipient)
    {
        var clients = new Mock<IHubCallerClients<IMunipClientMethods>>(MockBehavior.Strict);
        clients
            .Setup(client => client.Group(municipality))
            .Returns(recipient);

        return clients;
    }

    private static HubCallerContext CreateContext(string userId, string connectionId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            authenticationType: "Test");
        var user = new ClaimsPrincipal(identity);
        var context = new Mock<HubCallerContext>(MockBehavior.Strict);

        context.SetupGet(item => item.User).Returns(user);
        context.SetupGet(item => item.UserIdentifier).Returns(userId);
        context.SetupGet(item => item.ConnectionId).Returns(connectionId);
        context.SetupGet(item => item.ConnectionAborted).Returns(CancellationToken.None);

        return context.Object;
    }
}
