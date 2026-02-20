namespace SearchOrchestrator.Application.DTOs.Responses;

public sealed record ErrorResponse(
    string TraceId,
    string Code,
    string Message
);
