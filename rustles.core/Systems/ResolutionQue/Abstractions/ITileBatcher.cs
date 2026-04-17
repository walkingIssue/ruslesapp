
using Microsoft.Graph.Education.Classes.Item.Assignments.Item.Submissions.Item.Return;

using rustles.core.Entities;

using System.Collections.Concurrent;

namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    [Flags]
    public enum BatcherActions
    {
        Ok = 0,
        Failed = 1,
        Complete = 2,
        Partial = 4,
    }
    public interface ITileBatcher<TTileKey> where TTileKey : notnull 
    {
        /// <summary>
        /// Request Succession for prefilght updates 
        /// with latest wins semantics
        /// UserId is Key
        /// Currently incoming preflight request
        /// </summary>
        ConcurrentDictionary<Guid, HotcacheValue> HotCache { get; }

        /// <summary>
        /// Tile is a collection of cells and the workitem
        /// Flight ready reqeusts
        /// 
        /// contents of priority queue key only get managed by a single batching thread
        /// then the entire kvp entry get removed once the batch is flushed
        /// </summary>
        ConcurrentDictionary<TTileKey, SortedDictionary<int, RequestCell>> ComputeIds { get; }

        /// <summary>
        /// Lazy requestId deque for inflight batch runs
        /// Requests to be dequed once in flight or not in hotcache
        /// </summary>
        ConcurrentDictionary<DequeKey, DequedValue> Dequed { get; }

        ConcurrentDictionary<Guid, byte> DequeAllClient { get; }
        /// <summary>
        /// Fill the CompteId with the latest cache values, return a object 
        /// </summary>
        /// <param name="deque"> crossreference new tiling insertions with deque to prevent dead requests getting resolved </param>
        /// <param name="dequeAll"> crossreference new tiling insertions with dequeAll to prevent disconnected users getting resolved </param>
        /// <param name="backSearch"> backsearch the tiles' request id's for dequed inflight references marked in previous batching runs </param>
        /// <returns>TilingResult summary object</returns>
        public TilingResult TileHotCacheValues(
            CancellationToken cancellationToken,
            bool deque = true,
            bool dequeAll = true,
            bool backSearch = false);

    }
    
    public struct TilingResult
        (int? readyTilesCount = null,
        int? cellCount = null,
        int? userCount = null,
        int? requestCount = null,
        int? dequedRequestsCount = null,
        int? dequeuedInflightCount = null,
        BatcherActions? result = null)
    {

        public int RequestCount { get; set; } = requestCount ?? 0;
        public int ReadyTilesCount { get; set; } = readyTilesCount ?? 0;
        public int CellCount { get; set; } = cellCount ?? 0;
        public int UserCount { get; set; } = userCount ?? 0;
        public int DequedRequestsCount { get; set; } = dequedRequestsCount ?? 0;
        public int DequeuedInflightCount { get; set; } = dequeuedInflightCount ?? 0;
        public BatcherActions Result { get; set; } = result ?? BatcherActions.Failed;
    }

    public record HotcacheValue(Guid RequestId, ClientCell clientCell, bool prefilght = false);

    public record DequeKey(Guid requestId,  ClientCell Cell)
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(requestId, Cell?.latti, Cell?.longi);
        }
    }

    public class DequedValue(TileKey tileKey, ClientCell cell, RequestUserKey requestUserKey)
    {
        public TileKey Tile { get; set; } = tileKey; 
        public int TileId => Tile.GetHashCode();

        //normalize the cell coordinates to the tile local coordinates to generate a id row-major, 
        public ushort TileLocalizedId => (ushort)((cell.latti -tileKey.RasterIdX *256) * 256 + (cell.longi -tileKey.RasterIdY * 256));
        public RequestUserKey RequestUserKey { get; set; } = requestUserKey;
    }


    public class RequestCell(ClientCell cell, TileKey parent)
    {
        public TileKey TileKeyId { get; set; } = parent;
        public ushort TileLocalizedId => CalulateTileLocalizedId(cell, parent);
        public ClientCell Cell => cell;
        public SortedDictionary<int, RequestUserKey> RequestIds { get; set; } = new SortedDictionary<int, RequestUserKey>();
    

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cell"> The transformed coordinates within the expected raster </param>h
        /// <param name="parent"> TileKey parent, parent can be calculated by deviding the transformed cell with 256 </param>
        /// <returns></returns>
        public static ushort CalulateTileLocalizedId(ClientCell cell, TileKey parent)
        {
            return (ushort)((cell.latti -parent.RasterIdX *256) * 256 + (cell.longi -parent.RasterIdY * 256));
        }
    }

    public record class RequestUserKey(Guid UserId, Guid RequestId)
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(UserId, RequestId);
        }
    }


    public record ClientCell(int latti, int longi)
     {
         private int _hash = HashCode.Combine(latti, longi);
         public override int GetHashCode()
         {
            return _hash;
         }
     };
}
