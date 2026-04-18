using rustles.core.Systems.GDAL.Enums;
using rustles.core.Systems.ResolutionQue;
using rustles.core.Systems.ResolutionQue.Abstractions;

namespace rustles.core.tests.ResolutionQue;

public sealed class BatchFlusherBehaviorTests
{
    [Fact]
    public async Task Flush_claims_compute_tile_and_writes_one_work_item_per_tile()
    {
        var state = new Batched();
        var flusher = new BatchFlusher(state, state);
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var requestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var cell = new ClientCell(1_000, 2_000);
        var tile = AddComputeRequest(state, userId, requestId, cell);

        await flusher.Flush(CancellationToken.None);

        Assert.True(flusher.GeotileWorkChannel.Reader.TryRead(out var workItem));
        Assert.Equal(tile, workItem.TileKey);
        Assert.False(state.ComputeIds.ContainsKey(tile));
        var cellWork = Assert.Single(workItem.TileWork);
        Assert.Equal(RequestCell.CalulateTileLocalizedId(cell, tile), cellWork.Key);
        Assert.Contains(new RequestUserKey(userId, requestId), cellWork.Value);
    }

    [Fact]
    public async Task Flush_preserves_multiple_requests_attached_to_same_cell()
    {
        var state = new Batched();
        var flusher = new BatchFlusher(state, state);
        var firstUser = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var secondUser = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var firstRequest = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var secondRequest = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var cell = new ClientCell(1_000, 2_000);
        AddComputeRequest(state, firstUser, firstRequest, cell);
        AddComputeRequest(state, secondUser, secondRequest, cell);

        await flusher.Flush(CancellationToken.None);

        Assert.True(flusher.GeotileWorkChannel.Reader.TryRead(out var workItem));
        var cellWork = Assert.Single(workItem.TileWork.Values);
        Assert.Contains(new RequestUserKey(firstUser, firstRequest), cellWork);
        Assert.Contains(new RequestUserKey(secondUser, secondRequest), cellWork);
    }

    private static TileKey AddComputeRequest(
        Batched state,
        Guid userId,
        Guid requestId,
        ClientCell cell)
    {
        var tile = new TileKey(cell.latti, cell.longi, RasterResolution.R_5M);
        var requestCell = state.ComputeIds.TryGetValue(tile, out var tileCells)
            && tileCells.TryGetValue(RequestCell.CalulateTileLocalizedId(cell, tile), out var existing)
                ? existing
                : new RequestCell(cell, tile);

        var requestKey = new RequestUserKey(userId, requestId);
        requestCell.RequestIds[userId] = requestKey;

        if (!state.ComputeIds.TryGetValue(tile, out tileCells))
        {
            tileCells = new SortedDictionary<int, RequestCell>();
            state.ComputeIds[tile] = tileCells;
        }

        tileCells[requestCell.TileLocalizedId] = requestCell;
        return tile;
    }
}
