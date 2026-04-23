public class MunipGroup
{
    private readonly Guid munipId = Guid.NewGuid();

    public int PublicMunipId { get; set; }
    public string GroupId {
        get {
            return $"{munipId}_JumpingBunny_{PublicMunipId}";
        }
    }
    public string Name { get; init; } = string.Empty;

    public MunipGroup(int id, string name)
    {
        PublicMunipId = id;
        Name = name;
    }

    public static IEnumerable<MunipGroup> InstantiateMunips(int count)
    {
        for(int i = count; i > 0; i--)
        {
            yield return new MunipGroup(i, $"Munip-{i}");
        }
    }
}
