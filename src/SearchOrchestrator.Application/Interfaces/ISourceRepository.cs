using SearchOrchestrator.Domain.Entities;

namespace SearchOrchestrator.Application.Interfaces;

public interface ISourceRepository
{
    Task<Source?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Source?> GetByLocationAsync(string location, CancellationToken ct);
    Task AddAsync(Source source, CancellationToken ct);
    Task UpdateAsync(Source source, CancellationToken ct);
}
