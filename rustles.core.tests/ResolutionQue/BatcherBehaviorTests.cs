using rustles.core.Shared;
using rustles.core.Systems.GDAL.Enums;
using rustles.core.Systems.ResolutionQue;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.tests.ResolutionQue;

public sealed class BatcherBehaviorTests
{
    [Fact]
    public void Enque_adds_first_request_as_fresh_hotcache_value()
    {
        var state = new Batched();
        var batcher = new Batcher(state);
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var requestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var cell = new ClientCell(1_000, 2_000);
        var request = new BatchRequest(requestId, cell) { ClientId = userId };

        var result = batcher.Enque(request, RasterResolution.R_5M);

        Assert.Equal(SystemAction.Ok, result);
        Assert.True(state.HotCache.TryGetValue(userId, out var hotValue));
        Assert.Equal(requestId, hotValue.RequestId);
        Assert.Equal(cell, hotValue.clientCell);
        Assert.False(hotValue.prefilght);
        Assert.Empty(state.Dequed);
    }

    [Fact]
    public void Enque_replaces_nonpreflight_request_without_dequing_old_request()
    {
        var state = new Batched();
        var batcher = new Batcher(state);
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var oldRequestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var newRequestId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var oldCell = new ClientCell(1_000, 2_000);
        var newCell = new ClientCell(1_001, 2_001);
        state.HotCache[userId] = new HotcacheValue(oldRequestId, oldCell);
        var request = new BatchRequest(newRequestId, newCell) { ClientId = userId };

        var result = batcher.Enque(request, RasterResolution.R_5M);

        Assert.Equal(SystemAction.Ok, result);
        Assert.True(state.HotCache.TryGetValue(userId, out var hotValue));
        Assert.Equal(newRequestId, hotValue.RequestId);
        Assert.Equal(newCell, hotValue.clientCell);
        Assert.False(hotValue.prefilght);
        Assert.Empty(state.Dequed);
    }

    [Fact]
    public void Enque_deques_old_preflight_request_and_stores_new_hotcache_value()
    {
        var state = new Batched();
        var batcher = new Batcher(state);
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var oldRequestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var newRequestId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var oldCell = new ClientCell(1_000, 2_000);
        var newCell = new ClientCell(1_100, 2_100);
        state.HotCache[userId] = new HotcacheValue(oldRequestId, oldCell, prefilght: true);
        var request = new BatchRequest(newRequestId, newCell) { ClientId = userId };

        var result = batcher.Enque(request, RasterResolution.R_5M);

        Assert.Equal(SystemAction.Ok, result);
        var dequed = Assert.Single(state.Dequed.Values);
        Assert.Equal(new RequestUserKey(userId, oldRequestId), dequed.RequestUserKey);
        Assert.True(state.HotCache.TryGetValue(userId, out var hotValue));
        Assert.Equal(newRequestId, hotValue.RequestId);
        Assert.Equal(newCell, hotValue.clientCell);
        Assert.False(hotValue.prefilght);
    }

    [Fact]
    public void DeQue_removes_known_hotcache_value_and_records_user_disconnect()
    {
        var state = new Batched();
        var batcher = new Batcher(state);
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var requestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var cell = new ClientCell(1_000, 2_000);
        state.HotCache[userId] = new HotcacheValue(requestId, cell, prefilght: true);

        batcher.DeQue(userId);

        Assert.False(state.HotCache.ContainsKey(userId));
        Assert.True(state.DequeAllClient.ContainsKey(userId));
        var dequed = Assert.Single(state.Dequed.Values);
        Assert.Equal(new RequestUserKey(userId, requestId), dequed.RequestUserKey);
    }
}
