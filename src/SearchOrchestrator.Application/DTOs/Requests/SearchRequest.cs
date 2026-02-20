namespace SearchOrchestrator.Application.DTOs.Requests;

public sealed record SearchRequest(
    string Query,
    int PageSize = 20,
    int Page = 1,
    Guid? SourceId = null
);
