namespace rustles_platform.Hubs;

public class MunipGroup
{
    private readonly Guid _municipalityId = Guid.NewGuid();

    public int PublicMunipId { get; set; }
    public string GroupId => $"{_municipalityId}_JumpingBunny_{PublicMunipId}";
    public string Name { get; init; } = string.Empty;

    public MunipGroup(int id, string name)
    {
        PublicMunipId = id;
        Name = name;
    }

    public static IEnumerable<MunipGroup> InstantiateMunips(int count)
    {
        for (var i = count; i > 0; i--)
        {
            yield return new MunipGroup(i, $"Munip-{i}");
        }
    }
}
