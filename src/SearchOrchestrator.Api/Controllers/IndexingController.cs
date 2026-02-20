using Microsoft.AspNetCore.Mvc;
using SearchOrchestrator.Application.DTOs.Requests;
using SearchOrchestrator.Application.Services;

namespace SearchOrchestrator.Api.Controllers;

[ApiController]
[Route("api/indexing")]
public class IndexingController : ControllerBase
{
    private readonly IndexingOrchestrationService _service;

    public IndexingController(IndexingOrchestrationService service)
    {
        _service = service;
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> StartIndexing(
        [FromBody] StartIndexingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await _service.StartIndexingAsync(request, idempotencyKey, ct);
        return AcceptedAtAction(nameof(GetTaskStatus), new { taskId = result.TaskId }, result);
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<IActionResult> GetTaskStatus(Guid taskId, CancellationToken ct)
    {
        var result = await _service.GetTaskStatusAsync(taskId, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> ListTasks([FromQuery] Guid? sourceId, CancellationToken ct)
    {
        var result = await _service.ListTasksAsync(sourceId, ct);
        return Ok(result);
    }

    [HttpPost("tasks/{taskId:guid}/cancel")]
    public async Task<IActionResult> CancelTask(Guid taskId, CancellationToken ct)
    {
        await _service.CancelTaskAsync(taskId, ct);
        return NoContent();
    }
}
