using SearchOrchestrator.Application.DTOs.Responses;
using SearchOrchestrator.Domain.Exceptions;
using System.Diagnostics;

namespace SearchOrchestrator.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "DOMAIN_ERROR", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteError(context, StatusCodes.Status404NotFound, "NOT_FOUND", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message);
        }
        catch (OperationCanceledException)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteError(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Something went wrong");
        }
    }

    private static async Task WriteError(HttpContext context, int statusCode, string code, string message)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponse(traceId, code, message));
    }
}
