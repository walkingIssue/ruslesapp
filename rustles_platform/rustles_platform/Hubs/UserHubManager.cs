using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

using rustles.contracts;
using rustles.core.Entities;
using rustles.core.Systems.ResolutionQue.Abstractions;
using rustles_platform.Services;

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace rustles_platform.Hubs;

public class UserHubManager
{
    public IReadOnlyList<MunipGroup> MunicipalityGroups { get; } = MunipGroup.InstantiateMunips(20).ToList();

    public class UserConnectionLedgerEntry(
        string? munipId,
        Guid? requestId,
        string? signalRConnectionId,
        string? permanentUserName)
    {
        public string? MunipId { get; set; } = munipId;
        public Guid? RequestId { get; set; } = requestId;
        public string? SignalRConnectionId { get; set; } = signalRConnectionId;
        public string? PermanentUserName { get; set; } = permanentUserName;
        public object LockObject { get; set; } = new();
    };

    public ConcurrentDictionary<string, UserConnectionLedgerEntry?> CurrentUserLedger { get; } = new();
}

public class MunipDispatcher : BackgroundService
{
    readonly UserHubManager _userManager;
    readonly IHubContext<MunicipalityHub, IMunipClientMethods> _context;
    readonly Channel<AssignUserRoom> _channel;
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger _logger;
    public MunipDispatcher(
        IHubContext<MunicipalityHub, 
        IMunipClientMethods> context, 
        ICompletionsink sink, 
        UserHubManager manager, 
        IServiceScopeFactory scopeFactory,
        ILogger<MunipDispatcher> logger)
    {
        _context = context;
        _channel = sink.UserRoomChannel;
        _userManager = manager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentRead = await _channel.Reader.ReadAsync(stoppingToken);
                if(_userManager.CurrentUserLedger.TryGetValue(currentRead.UserId.ToString(), out var value)
                    && value is not null
                    && value.RequestId == currentRead.RequestId)
                {
                    //if we have no connection id something is really cooked
                    //most likely the user disconnected
                    if (value.SignalRConnectionId is null)
                        continue;
                    
                    //disconnect old group
                    if(value.MunipId is not null && value.MunipId != currentRead.NaturalName[0])
                        await _context.Groups.RemoveFromGroupAsync(
                            value.SignalRConnectionId,
                            value.MunipId!,
                            stoppingToken);
                    
                    //create new entry with updated values.
                    // this is a copy the reference update needs be atomic so we don't lock until that happens.
                    var newLedger = new UserHubManager.UserConnectionLedgerEntry(currentRead.NaturalName[0],
                        value.RequestId,
                        value.SignalRConnectionId,
                        value.PermanentUserName);

                    //update ledger object
                    lock (value.LockObject)
                    {
                        _userManager.CurrentUserLedger[currentRead.UserId.ToString()] = newLedger;
                    }

                    //connect with updated values
                    //bang it cus we just assigned.
                    await _context.Groups.AddToGroupAsync(
                        newLedger.SignalRConnectionId!,
                        newLedger.MunipId!,
                        stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var chatStore = scope.ServiceProvider.GetRequiredService<IMunicipalityChatStore>();
                    var messages = await chatStore.GetCatchupAsync(newLedger.MunipId!, stoppingToken);
                    var pinnedMessages = await chatStore.GetPinnedMessagesAsync(newLedger.MunipId!, stoppingToken);

                    //now that i think about it reading from the ledger object is slightly cursed
                    //this is subject to multithreaded change :D
                    await _context.Clients.Client(newLedger.SignalRConnectionId!).MunipConnect(
                        new ConnectMunipAck(
                            newLedger.MunipId!,
                            newLedger.PermanentUserName!,
                            "ConnectionOk",
                            messages,
                            pinnedMessages));
                }
            } 
            catch(Exception ex)
            {
               _logger.Log(logLevel: LogLevel.Error, message: $"{ex}\t\t###\tAt:\t{nameof(MunipDispatcher)}");
            }
        }
    }
}
