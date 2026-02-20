using Microsoft.Extensions.Options;
using SearchOrchestrator.Application.Configuration;
using SearchOrchestrator.Application.Interfaces;
using System.Threading.Channels;

namespace SearchOrchestrator.Infrastructure.BackgroundServices;

public class ChannelTaskQueue : ITaskQueue
{
    private readonly Channel<Guid> _channel;

    public ChannelTaskQueue(IOptions<OrchestratorOptions> options)
    {
        var channelOptions = new BoundedChannelOptions(options.Value.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Guid>(channelOptions);
    }

    public async ValueTask EnqueueAsync(Guid taskId, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(taskId, ct);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken ct)
    {
        return await _channel.Reader.ReadAsync(ct);
    }
}
