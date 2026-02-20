using SearchOrchestrator.Domain.Enums;
using SearchOrchestrator.Domain.Exceptions;

namespace SearchOrchestrator.Domain.Entities;

public class IndexingTask
{
    public Guid Id { get; private set; }
    public Guid SourceId { get; private set; }
    public IndexingTaskStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int DocumentsProcessed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? IdempotencyKey { get; private set; }

    private IndexingTask() { }

    public static IndexingTask Create(Guid sourceId, string? idempotencyKey = null)
    {
        return new IndexingTask
        {
            Id = Guid.NewGuid(),
            SourceId = sourceId,
            Status = IndexingTaskStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            IdempotencyKey = idempotencyKey
        };
    }

    public void MarkStarted()
    {
        EnsureStatus(IndexingTaskStatus.Pending);
        Status = IndexingTaskStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(int docsProcessed)
    {
        EnsureStatus(IndexingTaskStatus.InProgress);
        Status = IndexingTaskStatus.Completed;
        DocumentsProcessed = docsProcessed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPartiallyCompleted(int docsProcessed, string error)
    {
        EnsureStatus(IndexingTaskStatus.InProgress);
        Status = IndexingTaskStatus.PartialSuccess;
        DocumentsProcessed = docsProcessed;
        ErrorMessage = error;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        if (Status is IndexingTaskStatus.Completed or IndexingTaskStatus.Cancelled)
            throw new DomainException($"Can't fail task in status {Status}");

        Status = IndexingTaskStatus.Failed;
        ErrorMessage = error;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled()
    {
        if (Status == IndexingTaskStatus.Completed || Status == IndexingTaskStatus.Failed
            || Status == IndexingTaskStatus.PartialSuccess)
        {
            throw new DomainException($"Can't cancel task in status {Status}");
        }

        Status = IndexingTaskStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureStatus(IndexingTaskStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Expected {expected} but got {Status}");
    }
}
