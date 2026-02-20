using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SearchOrchestrator.Application.Interfaces;
using SearchOrchestrator.Application.Services;

namespace SearchOrchestrator.Infrastructure.BackgroundServices;

public class IndexingTaskProcessor : BackgroundService
{
    private readonly ITaskQueue _taskQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IndexingTaskProcessor> _logger;

    public IndexingTaskProcessor(
        ITaskQueue taskQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<IndexingTaskProcessor> logger)
    {
        _taskQueue = taskQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IndexingTaskProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var taskId = await _taskQueue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IndexingOrchestrationService>();

                await service.ProcessTaskAsync(taskId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task");
            }
        }

        _logger.LogInformation("IndexingTaskProcessor stopped");
    }
}
