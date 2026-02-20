using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchOrchestrator.Application.Configuration;
using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Application.Services;
using SearchOrchestrator.Infrastructure.BackgroundServices;
using SearchOrchestrator.Infrastructure.ExternalServices;
using SearchOrchestrator.Infrastructure.Persistence;

namespace SearchOrchestrator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrchestratorOptions>(
            configuration.GetSection(OrchestratorOptions.SectionName));

        services.AddSingleton<IIndexingTaskRepository, InMemoryIndexingTaskRepository>();
        services.AddSingleton<ISourceRepository, InMemorySourceRepository>();
        services.AddSingleton<ITaskQueue, ChannelTaskQueue>();
        services.AddScoped<ISearchEngineClient, FakeSearchEngineClient>();
        services.AddScoped<IndexingOrchestrationService>();
        services.AddScoped<SearchService>();
        services.AddHostedService<IndexingTaskProcessor>();

        return services;
    }
}
