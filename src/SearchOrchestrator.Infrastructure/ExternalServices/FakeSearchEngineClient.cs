using Microsoft.Extensions.Logging;
using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace SearchOrchestrator.Infrastructure.ExternalServices;

public class FakeSearchEngineClient : ISearchEngineClient
{
    private readonly ConcurrentDictionary<Guid, List<FakeDocument>> _index = new();
    private readonly ILogger<FakeSearchEngineClient> _logger;
    private static readonly Random _random = new();

    public FakeSearchEngineClient(ILogger<FakeSearchEngineClient> logger)
    {
        _logger = logger;
    }

    public async Task<IndexingResult> IndexDocumentsAsync(
        Guid sourceId, string sourceLocation, CancellationToken ct)
    {
        await Task.Delay(_random.Next(200, 800), ct);

        var documentIds = Enumerable.Range(1, _random.Next(5, 15))
            .Select(i => $"doc-{sourceId:N}-{i}")
            .ToList();

        var failedDocs = documentIds.Where(_ => _random.NextDouble() < 0.1).ToList();
        var successDocs = documentIds.Except(failedDocs).ToList();

        var documents = successDocs
            .Select(id => new FakeDocument(id, sourceId, $"Title for {id}", $"Content of {id}"))
            .ToList();

        _index.AddOrUpdate(sourceId, documents, (_, existing) =>
        {
            existing.AddRange(documents);
            return existing;
        });

        _logger.LogInformation("Indexed {Success}/{Total} for source {SourceId}",
            successDocs.Count, documentIds.Count, sourceId);

        return new IndexingResult(
            successDocs.Count,
            failedDocs.Count,
            failedDocs,
            failedDocs.Count > 0 ? "Some documents failed during indexing" : null);
    }

    public async Task<SearchResult> SearchAsync(
        string query, int skip, int take, Guid? sourceId, CancellationToken ct)
    {
        await Task.Delay(_random.Next(10, 100), ct);

        var allDocs = sourceId.HasValue
            ? _index.GetValueOrDefault(sourceId.Value, [])
            : _index.Values.SelectMany(x => x).ToList();

        var matches = allDocs
            .Where(d => d.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || d.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select((d, i) => new SearchHit(d.Id, d.Title, $"...{query}...", 1.0 - i * 0.05, d.SourceId))
            .ToList();

        var paged = matches.Skip(skip).Take(take).ToList();
        return new SearchResult(paged, matches.Count, TimeSpan.FromMilliseconds(_random.Next(5, 50)));
    }

    public async Task DeleteIndexAsync(Guid sourceId, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        _index.TryRemove(sourceId, out _);
        _logger.LogInformation("Index deleted for {SourceId}", sourceId);
    }

    private record FakeDocument(string Id, Guid SourceId, string Title, string Content);
}
