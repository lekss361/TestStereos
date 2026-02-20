using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SearchOrchestrator.Application.Configuration;
using SearchOrchestrator.Application.DTOs.Requests;
using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Application.Services;
using SearchOrchestrator.Infrastructure.Persistence;

namespace SearchOrchestrator.Tests.Services;

public class IndexingOrchestrationServiceTests
{
    private readonly InMemoryIndexingTaskRepository _taskRepository = new();
    private readonly InMemorySourceRepository _sourceRepository = new();
    private readonly Mock<ISearchEngineClient> _searchEngineMock = new();
    private readonly Mock<ITaskQueue> _queueMock = new();
    private readonly IOptions<OrchestratorOptions> _options =
        Options.Create(new OrchestratorOptions { TaskTimeoutSeconds = 5 });
    private readonly IndexingOrchestrationService _service;

    public IndexingOrchestrationServiceTests()
    {
        _service = new IndexingOrchestrationService(
            _taskRepository, _sourceRepository, _searchEngineMock.Object, _queueMock.Object,
            _options, NullLogger<IndexingOrchestrationService>.Instance);
    }

    [Fact]
    public async Task StartIndexing_CreatesTaskAndEnqueues()
    {
        var request = new StartIndexingRequest("Test Source", "/data/files");

        var result = await _service.StartIndexingAsync(request, null, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.TaskId);
        Assert.Equal("Pending", result.Status);
        _queueMock.Verify(q => q.EnqueueAsync(result.TaskId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartIndexing_WithIdempotencyKey_ReturnsSameTask()
    {
        var request = new StartIndexingRequest("Test Source", "/data/files");

        var first = await _service.StartIndexingAsync(request, "key-1", CancellationToken.None);
        var second = await _service.StartIndexingAsync(request, "key-1", CancellationToken.None);

        Assert.Equal(first.TaskId, second.TaskId);
        _queueMock.Verify(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartIndexing_DifferentKeys_CreatesTwoTasks()
    {
        var request = new StartIndexingRequest("Test Source", "/data/files");

        var first = await _service.StartIndexingAsync(request, "key-1", CancellationToken.None);
        var second = await _service.StartIndexingAsync(request, "key-2", CancellationToken.None);

        Assert.NotEqual(first.TaskId, second.TaskId);
    }

    [Fact]
    public async Task ProcessTask_Success_MarksCompleted()
    {
        var request = new StartIndexingRequest("Source", "/path");
        var created = await _service.StartIndexingAsync(request, null, CancellationToken.None);

        _searchEngineMock.Setup(c => c.IndexDocumentsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingResult(10, 0, [], null));

        await _service.ProcessTaskAsync(created.TaskId, CancellationToken.None);

        var status = await _service.GetTaskStatusAsync(created.TaskId, CancellationToken.None);
        Assert.Equal("Completed", status!.Status);
        Assert.Equal(10, status.DocumentsProcessed);
    }

    [Fact]
    public async Task ProcessTask_PartialFailure_MarksPartialSuccess()
    {
        var request = new StartIndexingRequest("Source", "/path");
        var created = await _service.StartIndexingAsync(request, null, CancellationToken.None);

        _searchEngineMock.Setup(c => c.IndexDocumentsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingResult(8, 2, ["doc-1", "doc-2"], "Some failed"));

        await _service.ProcessTaskAsync(created.TaskId, CancellationToken.None);

        var status = await _service.GetTaskStatusAsync(created.TaskId, CancellationToken.None);
        Assert.Equal("PartialSuccess", status!.Status);
        Assert.Equal(8, status.DocumentsProcessed);
    }

    [Fact]
    public async Task ProcessTask_TotalFailure_MarksFailed()
    {
        var request = new StartIndexingRequest("Source", "/path");
        var created = await _service.StartIndexingAsync(request, null, CancellationToken.None);

        _searchEngineMock.Setup(c => c.IndexDocumentsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexingResult(0, 5, ["a", "b", "c", "d", "e"], "All failed"));

        await _service.ProcessTaskAsync(created.TaskId, CancellationToken.None);

        var status = await _service.GetTaskStatusAsync(created.TaskId, CancellationToken.None);
        Assert.Equal("Failed", status!.Status);
    }

    [Fact]
    public async Task ProcessTask_Exception_MarksFailed()
    {
        var request = new StartIndexingRequest("Source", "/path");
        var created = await _service.StartIndexingAsync(request, null, CancellationToken.None);

        _searchEngineMock.Setup(c => c.IndexDocumentsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        await _service.ProcessTaskAsync(created.TaskId, CancellationToken.None);

        var status = await _service.GetTaskStatusAsync(created.TaskId, CancellationToken.None);
        Assert.Equal("Failed", status!.Status);
        Assert.Contains("Connection refused", status.ErrorMessage);
    }

    [Fact]
    public async Task ProcessTask_Timeout_MarksFailed()
    {
        var request = new StartIndexingRequest("Source", "/path");
        var created = await _service.StartIndexingAsync(request, null, CancellationToken.None);

        _searchEngineMock.Setup(c => c.IndexDocumentsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid _, string _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new IndexingResult(0, 0, [], null);
            });

        await _service.ProcessTaskAsync(created.TaskId, CancellationToken.None);

        var status = await _service.GetTaskStatusAsync(created.TaskId, CancellationToken.None);
        Assert.Equal("Failed", status!.Status);
        Assert.Contains("timed out", status.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelTask_Pending_MarksCancelled()
    {
        var request = new StartIndexingRequest("Source", "/path");
        var created = await _service.StartIndexingAsync(request, null, CancellationToken.None);

        await _service.CancelTaskAsync(created.TaskId, CancellationToken.None);

        var status = await _service.GetTaskStatusAsync(created.TaskId, CancellationToken.None);
        Assert.Equal("Cancelled", status!.Status);
    }

    [Fact]
    public async Task CancelTask_NotFound_ThrowsKeyNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CancelTaskAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListTasks_BySourceId_ReturnsFiltered()
    {
        var r1 = new StartIndexingRequest("Source1", "/path1");
        var r2 = new StartIndexingRequest("Source2", "/path2");

        var t1 = await _service.StartIndexingAsync(r1, null, CancellationToken.None);
        await _service.StartIndexingAsync(r2, null, CancellationToken.None);

        var tasks = await _service.ListTasksAsync(t1.SourceId, CancellationToken.None);

        Assert.Single(tasks);
        Assert.Equal(t1.TaskId, tasks[0].TaskId);
    }
}
