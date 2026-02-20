namespace SearchOrchestrator.Domain.Enums;

public enum IndexingTaskStatus
{
    Pending,
    InProgress,
    Completed,
    PartialSuccess,
    Failed,
    Cancelled
}
