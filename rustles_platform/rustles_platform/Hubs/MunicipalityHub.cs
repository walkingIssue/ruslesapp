using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using rustles.contracts;
using rustles_platform.Services;

using System.Security.Claims;

namespace rustles_platform.Hubs;

[Authorize]
public class MunicipalityHub : Microsoft.AspNetCore.SignalR.Hub<IMunipClientMethods>, IMunipServerMethods
{
    public MunicipalityHub(
        UserHubManager manager, 
        UserManager<rustles.Infrastructure.Model.User> userManager,
        IMunicipalityChatStore chatStore)
    {
        _manager = manager;
        _userManager = userManager;
        _chatStore = chatStore;
    }

    
    private readonly UserHubManager _manager;
    private readonly UserManager<rustles.Infrastructure.Model.User> _userManager;
    private readonly IMunicipalityChatStore _chatStore;


    private readonly MunipChatError FailedRequest = new(rustles.contracts.ConnectionFault.fail, ChatErrorMessage.error);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
          if (Context.User is null)
            return;

        var connected = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);


        if (connected == null || await _userManager.FindByIdAsync(connected) is null)
            return;

        if(!_manager.CurrentUserLedger.TryRemove(connected, out var userEntry))
            return;


        await base.OnDisconnectedAsync(exception);
    }
    public override async Task OnConnectedAsync()
    {
        if (Context.User is null)
            return;

        var connected = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);


        if (connected == null || await _userManager.FindByIdAsync(connected) is null)
            return;

        if(!_manager.CurrentUserLedger.TryGetValue(connected, out var userEntry) || userEntry is null)
            return;

        //single connection per device.
        lock (userEntry.LockObject)
        {
            userEntry.SignalRConnectionId = Context.ConnectionId;
        }

        await base.OnConnectedAsync();
    }

    public async Task MunipUserMessage(MunipMessageRequest request)
    {

        if (Context.User is null)
            return;

        var connected = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (connected == null)
            return;

        if (Context.UserIdentifier == null 
            || !_manager.CurrentUserLedger.TryGetValue(connected, out var value) 
            || value is null
            || value.MunipId is null)
        {
            await Clients.Caller.ConnectionError(FailedRequest);
            return;
        }

        try
        {
            if (await _chatStore.IsUserBannedAsync(value.MunipId, connected, Context.ConnectionAborted))
            {
                await Clients.Caller.ConnectionError(new MunipChatError(ConnectionFault.fail, ChatErrorMessage.clientDenied));
                return;
            }

            var displayName = await ResolveDisplayNameAsync(connected, value);
            var userMessage = await _chatStore.AddMessageAsync(
                value.MunipId,
                connected,
                displayName,
                request.Message,
                request.ReplyToMessageId,
                Context.ConnectionAborted);

            await Clients.Group(value.MunipId).MessageByUser(userMessage);
        }
        catch (ArgumentOutOfRangeException)
        {
            await Clients.Caller.ConnectionError(new MunipChatError(ConnectionFault.fail, ChatErrorMessage.length));
        }
        catch (InvalidOperationException)
        {
            await Clients.Caller.ConnectionError(new MunipChatError(ConnectionFault.OutofOrder, ChatErrorMessage.error));
        }
    }

    private async Task<string> ResolveDisplayNameAsync(
        string userId,
        UserHubManager.UserConnectionLedgerEntry ledger)
    {
        if (!string.IsNullOrWhiteSpace(ledger.PermanentUserName))
            return ledger.PermanentUserName.Trim();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User no longer exists.");

        var displayName = UserDisplayName.Resolve(user);
        ledger.PermanentUserName = displayName;
        return displayName;
    }

    public async Task ReactToMessage(MunipReactionRequest request)
    {
        if (Context.User is null)
            return;

        var connected = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (connected == null)
            return;

        if (Context.UserIdentifier == null
            || !_manager.CurrentUserLedger.TryGetValue(connected, out var value)
            || value is null
            || value.MunipId is null)
        {
            await Clients.Caller.ConnectionError(FailedRequest);
            return;
        }

        try
        {
            if (await _chatStore.IsUserBannedAsync(value.MunipId, connected, Context.ConnectionAborted))
            {
                await Clients.Caller.ConnectionError(new MunipChatError(ConnectionFault.fail, ChatErrorMessage.clientDenied));
                return;
            }

            var update = await _chatStore.AddReactionAsync(
                value.MunipId,
                connected,
                request.MessageId,
                request.Emoji,
                Context.ConnectionAborted);

            if (update is null)
            {
                await Clients.Caller.ConnectionError(new MunipChatError(ConnectionFault.DoesntExist, ChatErrorMessage.error));
                return;
            }

            await Clients.Group(value.MunipId).ReactionUpdated(update);

            var pinnedMessages = await _chatStore.GetPinnedMessagesAsync(value.MunipId, Context.ConnectionAborted);
            await Clients.Group(value.MunipId).PinnedMessagesUpdated(new MunipPinnedMessages(pinnedMessages));
        }
        catch (ArgumentOutOfRangeException)
        {
            await Clients.Caller.ConnectionError(new MunipChatError(ConnectionFault.fail, ChatErrorMessage.forbiddenSymbols));
        }
    }
}
