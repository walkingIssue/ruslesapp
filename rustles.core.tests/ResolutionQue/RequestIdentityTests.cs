using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.tests.ResolutionQue;

public sealed class RequestIdentityTests
{
    [Fact]
    public void RequestUserKey_uses_user_then_request_hash_order()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var requestId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var key = new RequestUserKey(userId, requestId);

        Assert.Equal(HashCode.Combine(userId, requestId), key.GetHashCode());
    }

    [Fact]
    public void RequestUserKey_can_remove_dictionary_entry_by_equivalent_identity()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var requestId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var stored = new RequestUserKey(userId, requestId);
        var equivalent = new RequestUserKey(userId, requestId);
        var requestIds = new SortedDictionary<int, RequestUserKey>
        {
            [stored.GetHashCode()] = stored
        };

        var removed = requestIds.Remove(equivalent.GetHashCode());

        Assert.True(removed);
        Assert.Empty(requestIds);
    }

    [Fact]
    public void ClientCell_hash_is_stable_for_same_raster_cell()
    {
        var first = new ClientCell(12345, 67890);
        var second = new ClientCell(12345, 67890);

        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(first, second);
    }
}
