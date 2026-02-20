using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SearchOrchestrator.Application.DTOs.Requests;
using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Application.Services;
using SearchOrchestrator.Domain.ValueObjects;

namespace SearchOrchestrator.Tests.Services;

public class SearchServiceTests
{
    private readonly Mock<ISearchEngineClient> _searchEngineMock = new();
    private readonly SearchService _service;

    public SearchServiceTests()
    {
        _service = new SearchService(
            _searchEngineMock.Object,
            NullLogger<SearchService>.Instance);
    }

    [Fact]
    public async Task Search_ValidQuery_ReturnsResults()
    {
        var hits = new List<SearchHit>
        {
            new("doc-1", "Title 1", "snippet", 0.9, Guid.NewGuid())
        };
        _searchEngineMock.Setup(c => c.SearchAsync("test", 0, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult(hits, 1, TimeSpan.FromMilliseconds(15)));

        var result = await _service.SearchAsync(
            new SearchRequest("test"), CancellationToken.None);

        Assert.Single(result.Hits);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task Search_EmptyQuery_ThrowsArgument()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SearchAsync(new SearchRequest(""), CancellationToken.None));
    }

    [Fact]
    public async Task Search_Pagination_CalculatesSkipCorrectly()
    {
        _searchEngineMock.Setup(c => c.SearchAsync("q", 20, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult([], 0, TimeSpan.Zero));

        await _service.SearchAsync(
            new SearchRequest("q", PageSize: 10, Page: 3), CancellationToken.None);

        _searchEngineMock.Verify(c => c.SearchAsync("q", 20, 10, null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Search_WithSourceFilter_PassesSourceId()
    {
        var sourceId = Guid.NewGuid();
        _searchEngineMock.Setup(c => c.SearchAsync("q", 0, 20, sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult([], 0, TimeSpan.Zero));

        await _service.SearchAsync(
            new SearchRequest("q", SourceId: sourceId), CancellationToken.None);

        _searchEngineMock.Verify(c => c.SearchAsync("q", 0, 20, sourceId, It.IsAny<CancellationToken>()));
    }
}
