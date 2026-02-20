using SearchOrchestrator.Domain.Entities;

namespace SearchOrchestrator.Application.Interfaces;

public interface IIndexingTaskRepository
{
    Task<IndexingTask?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IndexingTask?> GetByIdempotencyKeyAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<IndexingTask>> GetBySourceIdAsync(Guid sourceId, CancellationToken ct);
    Task<IReadOnlyList<IndexingTask>> GetAllAsync(CancellationToken ct);
    Task AddAsync(IndexingTask task, CancellationToken ct);
    Task UpdateAsync(IndexingTask task, CancellationToken ct);
}
