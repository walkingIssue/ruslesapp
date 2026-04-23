using rustles.contracts;

using System.Collections.Concurrent;

public class UserHubManager
{
    public List<MunipGroup> munipGroups = MunipGroup.InstantiateMunips(20).ToList();

    public ConcurrentDictionary<MunipGroup, ConcurrentQueue<MunipChatMessage>> MunipChats { get; set; } = new();
    
    /// <summary>
    /// Next step is to save userdetails to skip the roundtripping to the database on every user message sent. 
    /// </summary>
    public ConcurrentDictionary<string, string> MunipByUser { get; set; } = new();
 
}
