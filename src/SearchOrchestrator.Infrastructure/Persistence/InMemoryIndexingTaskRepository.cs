using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Domain.Entities;
using System.Collections.Concurrent;

namespace SearchOrchestrator.Infrastructure.Persistence;

public class InMemoryIndexingTaskRepository : IIndexingTaskRepository
{
    private readonly ConcurrentDictionary<Guid, IndexingTask> _tasks = new();

    public Task<IndexingTask?> GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_tasks.GetValueOrDefault(id));

    public Task<IndexingTask?> GetByIdempotencyKeyAsync(string key, CancellationToken ct)
        => Task.FromResult(_tasks.Values.FirstOrDefault(t => t.IdempotencyKey == key));

    public Task<IReadOnlyList<IndexingTask>> GetBySourceIdAsync(Guid sourceId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IndexingTask>>(
            _tasks.Values.Where(t => t.SourceId == sourceId).OrderByDescending(t => t.CreatedAt).ToList());

    public Task<IReadOnlyList<IndexingTask>> GetAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IndexingTask>>(
            _tasks.Values.OrderByDescending(t => t.CreatedAt).ToList());

    public Task AddAsync(IndexingTask task, CancellationToken ct)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(IndexingTask task, CancellationToken ct)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }
}
