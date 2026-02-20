using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchOrchestrator.Application.Configuration;
using SearchOrchestrator.Application.DTOs.Requests;
using SearchOrchestrator.Application.DTOs.Responses;
using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Domain.Entities;
using SearchOrchestrator.Domain.Enums;

namespace SearchOrchestrator.Application.Services;

public class IndexingOrchestrationService
{
    private readonly IIndexingTaskRepository _taskRepository;
    private readonly ISourceRepository _sourceRepository;
    private readonly ISearchEngineClient _searchEngine;
    private readonly ITaskQueue _taskQueue;
    private readonly IOptions<OrchestratorOptions> _options;
    private readonly ILogger<IndexingOrchestrationService> _logger;

    public IndexingOrchestrationService(
        IIndexingTaskRepository taskRepository,
        ISourceRepository sourceRepository,
        ISearchEngineClient searchEngine,
        ITaskQueue taskQueue,
        IOptions<OrchestratorOptions> options,
        ILogger<IndexingOrchestrationService> logger)
    {
        _taskRepository = taskRepository;
        _sourceRepository = sourceRepository;
        _searchEngine = searchEngine;
        _taskQueue = taskQueue;
        _options = options;
        _logger = logger;
    }

    public async Task<IndexingTaskResponse> StartIndexingAsync(
        StartIndexingRequest request, string? idempotencyKey, CancellationToken ct)
    {
        if (idempotencyKey is not null)
        {
            var existing = await _taskRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null)
            {
                _logger.LogInformation("Idempotent hit for key {Key}, task {TaskId}", idempotencyKey, existing.Id);
                return MapToResponse(existing);
            }
        }

        var source = await _sourceRepository.GetByLocationAsync(request.SourceLocation, ct);
        if (source is null)
        {
            source = Source.Create(request.SourceName, request.SourceLocation);
            await _sourceRepository.AddAsync(source, ct);
        }

        var task = IndexingTask.Create(source.Id, idempotencyKey);
        await _taskRepository.AddAsync(task, ct);
        await _taskQueue.EnqueueAsync(task.Id, ct);

        _logger.LogInformation("Task {TaskId} created for source {SourceId}", task.Id, source.Id);

        return MapToResponse(task);
    }

    public async Task ProcessTaskAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, ct)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        if (task.Status != IndexingTaskStatus.Pending)
        {
            _logger.LogWarning("Task {TaskId} already {Status}, skipping", taskId, task.Status);
            return;
        }

        var source = await _sourceRepository.GetByIdAsync(task.SourceId, ct);

        task.MarkStarted();
        await _taskRepository.UpdateAsync(task, ct);

        _logger.LogInformation("Processing task {TaskId}", taskId);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.Value.TaskTimeoutSeconds));

            var result = await _searchEngine.IndexDocumentsAsync(
                task.SourceId, source?.Location ?? string.Empty, timeoutCts.Token);

            if (result.FailureCount == 0)
            {
                task.MarkCompleted(result.SuccessCount);
                _logger.LogInformation("Task {TaskId} completed, {Count} docs", taskId, result.SuccessCount);
            }
            else if (result.SuccessCount > 0)
            {
                task.MarkPartiallyCompleted(result.SuccessCount,
                    $"Failed documents: {string.Join(", ", result.FailedDocumentIds)}");
                _logger.LogWarning("Task {TaskId} partial: {Success}/{Total}",
                    taskId, result.SuccessCount, result.SuccessCount + result.FailureCount);
            }
            else
            {
                task.MarkFailed(result.ErrorMessage ?? "All documents failed");
                _logger.LogError("Task {TaskId} failed", taskId);
            }

            if (source is not null && task.Status is IndexingTaskStatus.Completed or IndexingTaskStatus.PartialSuccess)
            {
                source.MarkIndexed();
                await _sourceRepository.UpdateAsync(source, ct);
            }
        }
        catch (OperationCanceledException)
        {
            task.MarkFailed("Task timed out or was cancelled");
            _logger.LogWarning("Task {TaskId} timed out", taskId);
        }
        catch (Exception ex)
        {
            task.MarkFailed($"Something went wrong: {ex.Message}");
            _logger.LogError(ex, "Task {TaskId} failed", taskId);
        }

        await _taskRepository.UpdateAsync(task, ct);
    }

    public async Task<IndexingTaskResponse?> GetTaskStatusAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, ct);
        return task is null ? null : MapToResponse(task);
    }

    public async Task<IReadOnlyList<IndexingTaskResponse>> ListTasksAsync(Guid? sourceId, CancellationToken ct)
    {
        var tasks = sourceId.HasValue
            ? await _taskRepository.GetBySourceIdAsync(sourceId.Value, ct)
            : await _taskRepository.GetAllAsync(ct);

        return tasks.Select(MapToResponse).ToList();
    }

    public async Task CancelTaskAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, ct)
            ?? throw new KeyNotFoundException($"Task {taskId} not found");

        task.MarkCancelled();
        await _taskRepository.UpdateAsync(task, ct);

        _logger.LogInformation("Task {TaskId} cancelled", taskId);
    }

    static IndexingTaskResponse MapToResponse(IndexingTask task) => new(
        task.Id, task.SourceId, task.Status.ToString(),
        task.ErrorMessage, task.DocumentsProcessed,
        task.CreatedAt, task.StartedAt, task.CompletedAt
    );
}
