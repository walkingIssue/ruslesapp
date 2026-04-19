using Microsoft.AspNetCore.SignalR;

using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.application.Web.Realtime;

public sealed class PositionResolutionDispatcher : BackgroundService
{
    private readonly ICompletionsink _completionSink;
    private readonly IHubContext<MunicipalityChatHub> _hubContext;
    private readonly MunicipalityConnectionRegistry _registry;
    private readonly MunicipalityChatStore _store;

    public PositionResolutionDispatcher(
        ICompletionsink completionSink,
        IHubContext<MunicipalityChatHub> hubContext,
        MunicipalityConnectionRegistry registry,
        MunicipalityChatStore store)
    {
        _completionSink = completionSink;
        _hubContext = hubContext;
        _registry = registry;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var result in _completionSink.UserRoomChannel.Reader.ReadAllAsync(stoppingToken))
        {
            var municipalityName = result.NaturalName.FirstOrDefault()
                ?? result.MunipId.ToString();

            var assignment = new MunicipalityAssignment(
                result.UserId,
                result.MunipId,
                municipalityName);

            var previous = _registry.SetMunicipality(assignment);

            if (!_registry.TryGetConnection(result.UserId, out var connectionId))
                continue;

            if (previous is not null && previous.MunicipalityId != assignment.MunicipalityId)
            {
                await _hubContext.Groups.RemoveFromGroupAsync(
                    connectionId,
                    MunicipalityChatHub.GroupName(previous.MunicipalityId),
                    stoppingToken);
            }

            await _hubContext.Groups.AddToGroupAsync(
                connectionId,
                MunicipalityChatHub.GroupName(assignment.MunicipalityId),
                stoppingToken);

            await _hubContext.Clients.Client(connectionId)
                .SendAsync("PositionResolved", assignment, stoppingToken);

            await _hubContext.Clients.Client(connectionId)
                .SendAsync("ChatHistory", _store.GetHistory(assignment.MunicipalityId), stoppingToken);
        }
    }
}
