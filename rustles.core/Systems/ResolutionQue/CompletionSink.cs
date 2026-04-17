using Microsoft.Extensions.Hosting;

using rustles.core.Entities;
using rustles.core.Systems.GDAL.Abstractions;
using rustles.core.Systems.ResolutionQue.Abstractions;

using System.Threading.Channels;

using static rustles.core.Systems.ResolutionQue.CompletionSink;

namespace rustles.core.Systems.ResolutionQue
{
    public class CompletionSink : BackgroundService, ICompletionsink
    {
        private readonly Channel<TileWorkResult> _input;
        private readonly Channel<AssignUserRoom> _output;
        private readonly IResolveMunip _resolveMunip;
        private readonly ITileBatcher<TileKey> _batcher;

        public CompletionSink(ITileWorkResolver resolver, IResolveMunip resolveMunip, ITileBatcher<TileKey> batcher)
        {
            _batcher = batcher;
            _input = resolver.GeotileWorkChannel;
            _resolveMunip = resolveMunip;
            _output = Channel.CreateBounded<AssignUserRoom>(new BoundedChannelOptions(100)
            {
                SingleReader = false,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropWrite,
                AllowSynchronousContinuations = true,
            });
        }

        public Channel<AssignUserRoom> UserRoomChannel => _output;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var tileResult = await _input.Reader.ReadAsync(stoppingToken);
                foreach (var request in tileResult.RequestCell.Values)
                {
                    if (_batcher.HotCache.TryGetValue(request.UserId, out var cachedValue) ||
                        _batcher.DequeAllClient.ContainsKey(request.UserId))
                        continue;

                    var result = new AssignUserRoom(
                        request.UserId,
                        request.RequestId,
                        tileResult.MunipId,
                        _resolveMunip.GetMunipNaturalName(tileResult.MunipId));

                    await _output.Writer.WriteAsync(result, stoppingToken);
                }
            }
        }
    }
}
