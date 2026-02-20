namespace SearchOrchestrator.Application.DTOs.Requests;

public sealed record StartIndexingRequest(
    string SourceName,
    string SourceLocation
);