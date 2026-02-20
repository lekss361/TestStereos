namespace SearchOrchestrator.Application.Configuration;

public class OrchestratorOptions
{
    public const string SectionName = "Orchestrator";

    public int MaxQueueSize { get; set; } = 100;
    public int TaskTimeoutSeconds { get; set; } = 300;
}
