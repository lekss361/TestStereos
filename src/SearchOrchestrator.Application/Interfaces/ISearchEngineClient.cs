using SearchOrchestrator.Domain.ValueObjects;

namespace SearchOrchestrator.Application.Interfaces;

public interface ISearchEngineClient
{
    Task<IndexingResult> IndexDocumentsAsync(Guid sourceId, string sourceLocation, CancellationToken ct);
    Task<SearchResult> SearchAsync(string query, int skip, int take, Guid? sourceId, CancellationToken ct);
    Task DeleteIndexAsync(Guid sourceId, CancellationToken ct);
}

public sealed record IndexingResult(
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> FailedDocumentIds,
    string? ErrorMessage
);
