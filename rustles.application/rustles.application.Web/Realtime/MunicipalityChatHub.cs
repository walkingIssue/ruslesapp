using Microsoft.AspNetCore.SignalR;

namespace rustles.application.Web.Realtime;

public sealed class MunicipalityChatHub : Hub
{
    private readonly MunicipalityConnectionRegistry _registry;
    private readonly MunicipalityChatStore _store;

    public MunicipalityChatHub(MunicipalityConnectionRegistry registry, MunicipalityChatStore store)
    {
        _registry = registry;
        _store = store;
    }

    public async Task Register(Guid userId)
    {
        _registry.Register(userId, Context.ConnectionId);

        if (!_registry.TryGetMunicipality(userId, out var assignment))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(assignment.MunicipalityId));
        await Clients.Caller.SendAsync("PositionResolved", assignment);
        await Clients.Caller.SendAsync("ChatHistory", _store.GetHistory(assignment.MunicipalityId));
    }

    public async Task SendMessage(Guid userId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!_registry.TryGetMunicipality(userId, out var assignment))
            return;

        var chatMessage = new ChatMessageDto(
            Guid.NewGuid(),
            userId,
            assignment.MunicipalityId,
            message.Trim(),
            DateTimeOffset.UtcNow);

        _store.Append(chatMessage);

        await Clients
            .Group(GroupName(assignment.MunicipalityId))
            .SendAsync("MessageReceived", chatMessage);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.Unregister(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    internal static string GroupName(int municipalityId) => $"munip:{municipalityId}";
}
