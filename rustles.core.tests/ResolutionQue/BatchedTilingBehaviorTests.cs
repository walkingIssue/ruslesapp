using rustles.core.Systems.GDAL.Enums;
using rustles.core.Systems.ResolutionQue;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.tests.ResolutionQue;

public sealed class BatchedTilingBehaviorTests
{
    [Fact]
    public void TileHotCacheValues_promotes_fresh_hotcache_value_to_computeids_and_marks_preflight()
    {
        var state = new Batched();
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var requestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var cell = new ClientCell(1_000, 2_000);
        state.HotCache[userId] = new HotcacheValue(requestId, cell);

        var result = state.TileHotCacheValues(CancellationToken.None);

        Assert.Equal(1, result.ReadyTilesCount);
        Assert.True(state.HotCache.TryGetValue(userId, out var hotValue));
        Assert.True(hotValue.prefilght);
        AssertRequestIsInComputeIds(state, userId, requestId, cell);
    }

    [Fact]
    public void TileHotCacheValues_does_not_remove_preflight_value_without_dequed_marker()
    {
        var state = new Batched();
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var requestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var cell = new ClientCell(1_000, 2_000);
        AddPreflightedComputeRequest(state, userId, requestId, cell);

        state.TileHotCacheValues(CancellationToken.None);

        Assert.True(state.HotCache.ContainsKey(userId));
        AssertRequestIsInComputeIds(state, userId, requestId, cell);
    }

    [Fact]
    public void TileHotCacheValues_applies_targeted_dequed_marker_to_known_tile_cell_request()
    {
        var state = new Batched();
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var requestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var cell = new ClientCell(1_000, 2_000);
        var tile = AddPreflightedComputeRequest(state, userId, requestId, cell);
        var dequeKey = new DequeKey(requestId, cell);
        state.Dequed[dequeKey] = new DequedValue(tile, cell, new RequestUserKey(userId, requestId));

        var result = state.TileHotCacheValues(CancellationToken.None);

        Assert.Equal(1, result.DequeuedInflightCount);
        Assert.False(state.Dequed.ContainsKey(dequeKey));
        Assert.False(state.HotCache.ContainsKey(userId));
        AssertRequestIsNotInComputeIds(state, userId, requestId, cell);
    }

    private static TileKey AddPreflightedComputeRequest(
        Batched state,
        Guid userId,
        Guid requestId,
        ClientCell cell)
    {
        var tile = new TileKey(cell.latti, cell.longi, RasterResolution.R_5M);
        var requestCell = new RequestCell(cell, tile);
        var requestKey = new RequestUserKey(userId, requestId);

        requestCell.RequestIds[userId] = requestKey;
        state.ComputeIds[tile] = new SortedDictionary<int, RequestCell>
        {
            [requestCell.TileLocalizedId] = requestCell
        };
        state.HotCache[userId] = new HotcacheValue(requestId, cell, prefilght: true);

        return tile;
    }

    private static void AssertRequestIsInComputeIds(
        Batched state,
        Guid userId,
        Guid requestId,
        ClientCell cell)
    {
        var tile = new TileKey(cell.latti, cell.longi, RasterResolution.R_5M);
        var localizedCellId = RequestCell.CalulateTileLocalizedId(cell, tile);
        var requestKey = new RequestUserKey(userId, requestId);

        Assert.True(state.ComputeIds.TryGetValue(tile, out var tileCells));
        Assert.True(tileCells.TryGetValue(localizedCellId, out var requestCell));
        Assert.Contains(requestKey, requestCell.RequestIds.Values);
    }

    private static void AssertRequestIsNotInComputeIds(
        Batched state,
        Guid userId,
        Guid requestId,
        ClientCell cell)
    {
        var tile = new TileKey(cell.latti, cell.longi, RasterResolution.R_5M);
        var localizedCellId = RequestCell.CalulateTileLocalizedId(cell, tile);
        var requestKey = new RequestUserKey(userId, requestId);

        if (!state.ComputeIds.TryGetValue(tile, out var tileCells))
            return;

        if (!tileCells.TryGetValue(localizedCellId, out var requestCell))
            return;

        Assert.DoesNotContain(requestKey, requestCell.RequestIds.Values);
    }
}
