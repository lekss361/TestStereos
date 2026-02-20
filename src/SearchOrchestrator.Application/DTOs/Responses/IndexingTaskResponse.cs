namespace SearchOrchestrator.Application.DTOs.Responses;

public sealed record IndexingTaskResponse(
    Guid TaskId,
    Guid SourceId,
    string Status,
    string? ErrorMessage,
    int DocumentsProcessed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt
);