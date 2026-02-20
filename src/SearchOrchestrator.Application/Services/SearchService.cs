using Microsoft.Extensions.Logging;
using SearchOrchestrator.Application.DTOs.Requests;
using SearchOrchestrator.Application.DTOs.Responses;
using SearchOrchestrator.Application.Interfaces;

namespace SearchOrchestrator.Application.Services;

public class SearchService
{
    private readonly ISearchEngineClient _searchEngine;
    private readonly ILogger<SearchService> _logger;

    public SearchService(ISearchEngineClient searchEngine, ILogger<SearchService> logger)
    {
        _searchEngine = searchEngine;
        _logger = logger;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            throw new ArgumentException("Search query can't be empty");

        int skip = (request.Page - 1) * request.PageSize;

        var result = await _searchEngine.SearchAsync(
            request.Query, skip, request.PageSize, request.SourceId, ct);

        _logger.LogInformation("Search \"{Query}\": {Count} results, {Duration}ms",
            request.Query, result.TotalCount, (long)result.QueryDuration.TotalMilliseconds);

        return new SearchResponse(
            result.Hits.Select(h => new SearchHitDto(
                h.DocumentId, h.Title, h.Snippet, h.Score, h.SourceId
            )).ToList(),
            result.TotalCount,
            (long)result.QueryDuration.TotalMilliseconds,
            request.Page,
            request.PageSize
        );
    }
}
