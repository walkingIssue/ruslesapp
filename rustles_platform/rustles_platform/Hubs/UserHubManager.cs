using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

using rustles.contracts;
using rustles.core.Entities;
using rustles.core.Systems.ResolutionQue.Abstractions;

using System.Collections.Concurrent;
using System.Threading.Channels;

public class UserHubManager
{

    public List<MunipGroup> munipGroups = MunipGroup.InstantiateMunips(20).ToList();

    public ConcurrentDictionary<string, ConcurrentQueue<MunipChatMessage>> MunipChats { get; } = new();

    public class UserConnectionLedgerEntry(
        string? munipId,
        Guid? requestId,
        string? singalRConnectionId,
        string? parmanentUserName)
    {
        public string? MunipId { get; set; } = munipId;
        public Guid? RequestId { get; set; } = requestId;
        public string? SingalRConnectionId { get; set; } = singalRConnectionId;
        public string? ParmanentUserName { get; set; } = parmanentUserName;
        public object LockObject { get; set; } = new();
    };

    public ConcurrentDictionary<string, UserConnectionLedgerEntry?> CurrentUserLedger { get; } = new();

    public IReadOnlyList<MunipChatMessage> CurrentMessages(string munipId)
    {

        if (MunipChats.TryGetValue(munipId, out var CurrentChat))
        {
            return CurrentChat.ToArray();
        }

        MunipChatMessage[] newChat = [new("rustles", "... can you hear the rustling through the breeze", DateTimeOffset.UtcNow)];
        var concurrent = new ConcurrentQueue<MunipChatMessage>(newChat);
        if (!MunipChats.TryAdd(munipId, concurrent))
        {
            //assume the we got descheduled and try again.
            MunipChats.TryAdd(munipId, concurrent);
        }
        return newChat.AsReadOnly();
    }

}

public class MunipDispatcher : BackgroundService
{
    readonly UserHubManager _userManager;
    readonly IHubContext<MunicipalityHub, IMunipClientMethods> _context;
    readonly Channel<AssignUserRoom> _channel;
    readonly ILogger _logger;
    public MunipDispatcher(
        IHubContext<MunicipalityHub, 
        IMunipClientMethods> context, 
        ICompletionsink sink, 
        UserHubManager manager, 
        ILogger<MunipDispatcher> logger)
    {
        _context = context;
        _channel = sink.UserRoomChannel;
        _userManager = manager;
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
                    if (value.SingalRConnectionId is null)
                        continue;
                    
                    //disconnect old group
                    if(value.MunipId is not null && value.MunipId != currentRead.NaturalName[0])
                        await _context.Groups.RemoveFromGroupAsync(
                            value.SingalRConnectionId,
                            value.MunipId!,
                            stoppingToken);
                    
                    //create new entry with updated values.
                    // this is a copy the reference update needs be atomic so we don't lock until that happens.
                    var newLedger = new UserHubManager.UserConnectionLedgerEntry(currentRead.NaturalName[0],
                        value.RequestId,
                        value.SingalRConnectionId,
                        value.ParmanentUserName);

                    //update ledger object
                    lock (value.LockObject)
                    {
                        _userManager.CurrentUserLedger[currentRead.UserId.ToString()] = newLedger;
                    }

                    //connect with updated values
                    //bang it cus we just assigned.
                    await _context.Groups.AddToGroupAsync(
                        newLedger.SingalRConnectionId!, 
                        newLedger.MunipId!, 
                        stoppingToken);

                    //this will throw if the user is on a large lake on in nomansland
                    //(meaning within 5m of a border)
                    var messages = _userManager.CurrentMessages(newLedger.MunipId!);

                    if (messages == null)
                        continue;
                    //now that i think about it reading from the ledger object is slightly cursed
                    //this is subject to multithreaded change :D
                    await _context.Clients.Client(newLedger.SingalRConnectionId!).MunipConnect(
                        new ConnectMunipAck(
                            newLedger.ParmanentUserName!,
                            "ConnectionOk",
                            messages));
                }
            } 
            catch(Exception ex)
            {
               _logger.Log(logLevel: LogLevel.Error, message: $"{ex}\t\t###\tAt:\t{nameof(MunipDispatcher)}");
            }
        }
    }
}