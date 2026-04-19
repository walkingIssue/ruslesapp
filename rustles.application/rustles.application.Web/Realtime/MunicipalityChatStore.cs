using System.Collections.Concurrent;

namespace rustles.application.Web.Realtime;

public sealed class MunicipalityChatStore
{
    private const int MaxMessagesPerRoom = 20;
    private readonly ConcurrentDictionary<int, Queue<ChatMessageDto>> _rooms = new();
    private readonly object _gate = new();

    public void Append(ChatMessageDto message)
    {
        lock (_gate)
        {
            var room = _rooms.GetOrAdd(message.MunicipalityId, _ => new Queue<ChatMessageDto>());
            room.Enqueue(message);

            while (room.Count > MaxMessagesPerRoom)
                room.Dequeue();
        }
    }

    public ChatMessageDto[] GetHistory(int municipalityId)
    {
        lock (_gate)
        {
            return _rooms.TryGetValue(municipalityId, out var room)
                ? room.ToArray()
                : [];
        }
    }
}
