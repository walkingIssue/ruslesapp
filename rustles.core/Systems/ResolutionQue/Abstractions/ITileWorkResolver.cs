using Microsoft.Extensions.Hosting;

using rustles.core.Entities;
using rustles.core.Systems.GDAL.Abstractions;

using System.Buffers;
using System.Threading.Channels;

namespace rustles.core.Systems.ResolutionQue.Abstractions
{
    //gets cosumed by the background process.... but also handles the starting and stopping of the tasks
    // effectivly this part creates the lumped task that acts as a abstraction for all the incoming requests
    public interface ITileWorkResolver
    {
        Channel<TileWorkResult> GeotileWorkChannel { get; }
    }

    public class TileWorkResolver : BackgroundService, ITileWorkResolver
    {
        private readonly Channel<GeoRasterWorkItem> _channel;
        private readonly Channel<TileWorkResult> _geoTileWork;
        
        private readonly IGeoRasterAccess _geoRasterAccess;
        public Channel<TileWorkResult> GeotileWorkChannel => _geoTileWork;

        public TileWorkResolver(IBatchFlusher batchFlusher, IGeoRasterAccess geoRasterAccess)
        {
            _geoRasterAccess = geoRasterAccess;
            _channel = batchFlusher.GeotileWorkChannel;
            _geoTileWork = Channel.CreateBounded<TileWorkResult>(new BoundedChannelOptions(100)
            {
                SingleReader = false,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropWrite,
                AllowSynchronousContinuations = true,
            });
        }
        //this channel becomes bound by the completionsink and the output capability of asp.net SignalR
        //not optmial but for MVP this is what we're going with
        private async Task ResolveWork(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var workItem = await _channel.Reader.ReadAsync(token);
                var tileWork = workItem.TileWork.ToArray();
                
                //current tile buffer
                ushort[] workSpan = new ushort[tileWork.Length - 1];
                
                //move over the tiles by key is it's the tileLocalizedId
                //there's a bunch of abstraction rules broken here, alas
                for(int i = 0; i < tileWork.Length; i++)
                {
                    workSpan[i] = (ushort)tileWork[i].Key;
                }
                int[] workResult = new int[workSpan.Length];
                _geoRasterAccess.ResolvePosition(workItem.TileKey, workSpan, workResult);
                for(int i = 0; i < tileWork.Length - 1; i++)
                {
                    var tileResult = new TileWorkResult(workResult[i], new SortedList<Guid, RequestUserKey>());
                    //order and add the request userkeys/requestids to the cell result
                    var requests = tileWork[i].Value.ToArray();
                    for (int j = 0; j < requests.Length; j++)
                        tileResult.RequestCell.Add(requests[j].UserId, requests[j]);
                    await _geoTileWork.Writer.WriteAsync(tileResult, token);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ResolveWork(stoppingToken);
                }
                catch (Exception exc)
                {
                    //log the exception, but keep the service running to handle future work items.
                    Console.WriteLine($"Error in TileWorkResolver: {exc.Message}");
                }
            }
        }
    }
    
    //class cus this gets thrown out to the completionsink where the stackallocation semantics can break
    public record class TileWorkResult(int MunipId, SortedList<Guid, RequestUserKey> requestCells)
    {
        public int MunipId { get; init; } = MunipId;
        //order by the userId
        public SortedList<Guid, RequestUserKey> RequestCell { get; set; } = requestCells;
    }
}

