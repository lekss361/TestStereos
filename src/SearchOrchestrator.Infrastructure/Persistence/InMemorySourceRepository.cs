using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Domain.Entities;
using System.Collections.Concurrent;

namespace SearchOrchestrator.Infrastructure.Persistence;

public class InMemorySourceRepository : ISourceRepository
{
    private readonly ConcurrentDictionary<Guid, Source> _sources = new();

    public Task<Source?> GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_sources.GetValueOrDefault(id));

    public Task<Source?> GetByLocationAsync(string location, CancellationToken ct)
        => Task.FromResult(_sources.Values.FirstOrDefault(s => s.Location == location));

    public Task AddAsync(Source source, CancellationToken ct)
    {
        _sources[source.Id] = source;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Source source, CancellationToken ct)
    {
        _sources[source.Id] = source;
        return Task.CompletedTask;
    }
}
