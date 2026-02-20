using Microsoft.AspNetCore.Mvc;
using SearchOrchestrator.Application.DTOs.Requests;
using SearchOrchestrator.Application.Services;

namespace SearchOrchestrator.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpPost]
    public async Task<IActionResult> Search([FromBody] SearchRequest request, CancellationToken ct)
    {
        var result = await _searchService.SearchAsync(request, ct);
        return Ok(result);
    }
}