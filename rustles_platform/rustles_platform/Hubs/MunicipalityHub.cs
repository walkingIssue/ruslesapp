using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using rustles.contracts;
using rustles.Infrastructure.Model;

using System.Collections.Concurrent;
using System.Text;

[Authorize]
public partial class MunicipalityHub : Microsoft.AspNetCore.SignalR.Hub<IMunipClientMethods>, IMunipServerMethods
{
    public MunicipalityHub(UserHubManager manager, UserManager<User> userManager)
    {
        _manager = manager;
        _userManager = userManager;
    }
    
    private readonly UserHubManager _manager;
    private readonly UserManager<User> _userManager;

    private ConnectionFault f_e()
    {
        return ConnectionFault.fail | ConnectionFault.DoesntExist;
    }
    private ConnectionFault f_O()
    {
        return ConnectionFault.fail | ConnectionFault.OutofOrder;
    }
    
    private MunipGroup? UserMunip(int chatId)
    {
        return _manager.munipGroups.Where(x => x.PublicMunipId == chatId).SingleOrDefault();
    }

    private MunipGroup? UserMunip(string groupId) => _manager.munipGroups.Where(x => x.GroupId == groupId).SingleOrDefault();

    private IReadOnlyList<MunipChatMessage>? CurrentMessages(int id)
    {
        var item = UserMunip(id);

        if (item is null) return null;

        if(_manager.MunipChats.TryGetValue(item, out var CurrentChat))
        {
            return CurrentChat.ToArray();
        }

        MunipChatMessage[] newChat = [new("rustles_system","rustles", "... can you hear the rustling through the breeze", DateTimeOffset.UtcNow)];
        var concurrent = new ConcurrentQueue<MunipChatMessage>(newChat);
        if(!_manager.MunipChats.TryAdd(item, concurrent))
        {
            //assume the we got descheduled and try again.
            _manager.MunipChats.TryAdd(item, concurrent);
        }
        return newChat.AsReadOnly();
    }

    private readonly MunipChatError FailedRequest = new(rustles.contracts.ConnectionFault.fail, ChatErrorMessage.error);
    public async Task<ConnectMunipAck?> ConnectUser(ConnectMunipRequest request)
    {

        if (request.MunipId < 0 || request.MunipId > _manager.munipGroups.Count)
        {
            await Clients.Caller.ConnectionError(FailedRequest);
            return null;
        }

        var userMunip = UserMunip(request.MunipId);

        if (userMunip is null)
        {
            await Clients.Caller.ConnectionError(FailedRequest);
            return null;
        }

        var currentMessages = CurrentMessages(request.MunipId); 

        if (currentMessages == null)
        {
            await Clients.Caller.ConnectionError(FailedRequest);
        }

        if (_manager.MunipByUser.TryGetValue(Context.ConnectionId, out var value))
            _manager.MunipByUser.TryUpdate(Context.ConnectionId, userMunip!.GroupId, value);

        else _manager.MunipByUser.TryAdd(Context.ConnectionId, userMunip!.GroupId);

        await Groups.AddToGroupAsync(Context.ConnectionId, userMunip!.GroupId);
        return new(userMunip.Name, $"You've been connected to Munip {userMunip.Name}", currentMessages!);
    }

    public async Task MunipUserMessage(string message)
    {
        if (Context.UserIdentifier == null)
        {
            await Clients.Caller.ConnectionError(FailedRequest);
            return;
        }

        if(!_manager.MunipByUser.TryGetValue(Context.ConnectionId, out var value))
        {
            await Clients.Caller.ConnectionError(FailedRequest);
            return;
        }

        var defaultUser = new User() { PermanentUsername = "unknown" };
        var permanentUserName = await _userManager.Users.SingleOrDefaultAsync(x => x.Id == Context.UserIdentifier) ?? defaultUser;

        if(permanentUserName.PermanentUsername == "" || permanentUserName.PermanentUsername == null)
        {
            permanentUserName.PermanentUsername = defaultUser.PermanentUsername;
        }

        var userMessage = new MunipChatMessage(
            Context.UserIdentifier!, 
            permanentUserName.PermanentUsername, 
            message, DateTimeOffset.UtcNow);

        if(value is not null){
            var t = Clients.OthersInGroup(value).MessageByUser(userMessage);
            if (_manager.MunipChats.TryGetValue(UserMunip(value)!, out var que))
                que.Enqueue(userMessage);
            await t;
        }
    }
}
