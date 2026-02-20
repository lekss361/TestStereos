namespace SearchOrchestrator.Application.Interfaces;

public interface ITaskQueue
{
    ValueTask EnqueueAsync(Guid taskId, CancellationToken ct);
    ValueTask<Guid> DequeueAsync(CancellationToken ct);
}
