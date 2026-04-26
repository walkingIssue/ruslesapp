using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using rustles.contracts;
using rustles.core.Entities;
using rustles.core.Systems.ResolutionQue.Abstractions;

using System.Security.Claims;
using System.Threading.Channels;


[Authorize]
public class MunicipalityHub : Microsoft.AspNetCore.SignalR.Hub<IMunipClientMethods>, IMunipServerMethods
{
    public MunicipalityHub(
        UserHubManager manager, 
        UserManager<rustles.Infrastructure.Model.User> userManager)
    {
        _manager = manager;
        _userManager = userManager;
    }

    
    private readonly UserHubManager _manager;
    private readonly UserManager<rustles.Infrastructure.Model.User> _userManager;


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
            userEntry.SingalRConnectionId = Context.ConnectionId;
        }

        await base.OnConnectedAsync();
    }

    public Task MunipUserMessage(string message)
    {

        if (Context.User is null)
            return Task.CompletedTask;

        var connected = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (connected == null)
            return Task.CompletedTask;

        if (Context.UserIdentifier == null 
            || !_manager.CurrentUserLedger.TryGetValue(connected, out var value) 
            || value is null)
        {
            return Clients.Caller.ConnectionError(FailedRequest);
        }

        var userMessage = new MunipChatMessage(
            value.ParmanentUserName!, 
            message,
            DateTimeOffset.UtcNow);

        if(value is not null && value.MunipId is not null)
        {
            var t = Clients.OthersInGroup(value.MunipId).MessageByUser(userMessage);
            if (_manager.MunipChats.TryGetValue(value.MunipId!, out var que))
            {
                if (que.Count > 20)
                    que.TryDequeue(out _);
                que.Enqueue(userMessage);
            }
            return t;
        }
        return Task.CompletedTask;
    }
}
