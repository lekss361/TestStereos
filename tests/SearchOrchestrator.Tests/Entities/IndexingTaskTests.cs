using SearchOrchestrator.Domain.Entities;
using SearchOrchestrator.Domain.Enums;
using SearchOrchestrator.Domain.Exceptions;

namespace SearchOrchestrator.Tests.Entities;

public class IndexingTaskTests
{
    [Fact]
    public void Create_SetsCorrectInitialState()
    {
        var sourceId = Guid.NewGuid();
        var task = IndexingTask.Create(sourceId, "key-1");

        Assert.Equal(sourceId, task.SourceId);
        Assert.Equal(IndexingTaskStatus.Pending, task.Status);
        Assert.Equal("key-1", task.IdempotencyKey);
        Assert.Equal(0, task.DocumentsProcessed);
    }

    [Fact]
    public void MarkStarted_FromPending_Succeeds()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();

        Assert.Equal(IndexingTaskStatus.InProgress, task.Status);
        Assert.NotNull(task.StartedAt);
    }

    [Fact]
    public void MarkStarted_FromCompleted_Throws()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();
        task.MarkCompleted(10);

        Assert.Throws<DomainException>(() => task.MarkStarted());
    }

    [Fact]
    public void MarkCompleted_SetsDocumentsProcessed()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();
        task.MarkCompleted(42);

        Assert.Equal(IndexingTaskStatus.Completed, task.Status);
        Assert.Equal(42, task.DocumentsProcessed);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void MarkPartiallyCompleted_SetsErrorMessage()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();
        task.MarkPartiallyCompleted(8, "2 docs failed");

        Assert.Equal(IndexingTaskStatus.PartialSuccess, task.Status);
        Assert.Equal(8, task.DocumentsProcessed);
        Assert.Equal("2 docs failed", task.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_SetsErrorMessage()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();
        task.MarkFailed("timeout");

        Assert.Equal(IndexingTaskStatus.Failed, task.Status);
        Assert.Equal("timeout", task.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_FromCompleted_Throws()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();
        task.MarkCompleted(10);

        Assert.Throws<DomainException>(() => task.MarkFailed("error"));
    }

    [Fact]
    public void MarkCancelled_FromPending_Succeeds()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkCancelled();

        Assert.Equal(IndexingTaskStatus.Cancelled, task.Status);
    }

    [Fact]
    public void MarkCancelled_FromInProgress_Succeeds()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();
        task.MarkCancelled();

        Assert.Equal(IndexingTaskStatus.Cancelled, task.Status);
    }

    [Fact]
    public void MarkCancelled_FromCompleted_Throws()
    {
        var task = IndexingTask.Create(Guid.NewGuid());
        task.MarkStarted();
        task.MarkCompleted(10);

        Assert.Throws<DomainException>(() => task.MarkCancelled());
    }
}
