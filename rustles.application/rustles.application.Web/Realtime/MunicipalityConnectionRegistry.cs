using System.Collections.Concurrent;

namespace rustles.application.Web.Realtime;

public sealed class MunicipalityConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, string> _connectionByUser = new();
    private readonly ConcurrentDictionary<string, Guid> _userByConnection = new();
    private readonly ConcurrentDictionary<Guid, MunicipalityAssignment> _municipalityByUser = new();

    public void Register(Guid userId, string connectionId)
    {
        _connectionByUser.AddOrUpdate(userId, connectionId, (_, oldConnectionId) =>
        {
            _userByConnection.TryRemove(oldConnectionId, out _);
            return connectionId;
        });

        _userByConnection[connectionId] = userId;
    }

    public void Unregister(string connectionId)
    {
        if (!_userByConnection.TryRemove(connectionId, out var userId))
            return;

        _connectionByUser.TryRemove(userId, out _);
    }

    public bool TryGetConnection(Guid userId, out string connectionId)
    {
        return _connectionByUser.TryGetValue(userId, out connectionId!);
    }

    public bool TryGetMunicipality(Guid userId, out MunicipalityAssignment assignment)
    {
        return _municipalityByUser.TryGetValue(userId, out assignment!);
    }

    public MunicipalityAssignment? SetMunicipality(MunicipalityAssignment assignment)
    {
        _municipalityByUser.TryGetValue(assignment.UserId, out var previous);
        _municipalityByUser[assignment.UserId] = assignment;
        return previous;
    }
}
