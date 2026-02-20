namespace SearchOrchestrator.Domain.ValueObjects;

public sealed record SearchResult(
    IReadOnlyList<SearchHit> Hits,
    int TotalCount,
    TimeSpan QueryDuration
);

public sealed record SearchHit(
    string DocumentId,
    string Title,
    string Snippet,
    double Score,
    Guid SourceId
);
