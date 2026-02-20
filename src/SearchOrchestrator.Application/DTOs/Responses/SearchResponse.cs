namespace SearchOrchestrator.Application.DTOs.Responses;

public sealed record SearchResponse(
    IReadOnlyList<SearchHitDto> Hits,
    int TotalCount,
    long QueryDurationMs,
    int Page,
    int PageSize
);

public sealed record SearchHitDto(
    string DocumentId,
    string Title,
    string Snippet,
    double Score,
    Guid SourceId
);
