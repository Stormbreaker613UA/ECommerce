using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.API.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected or the request was cancelled by the
            // server. There is no valid response to write at this point.
            _logger.LogDebug(
                "Request {TraceId} was cancelled because the request token was aborted.",
                context.TraceIdentifier);
        }
        catch (OperationCanceledException ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    ex,
                    "Request {TraceId} was cancelled after the response started.",
                    context.TraceIdentifier);
                throw;
            }

            _logger.LogWarning(
                ex,
                "Request {TraceId} timed out or was cancelled before completion.",
                context.TraceIdentifier);

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status408RequestTimeout,
                ex);
        }
        catch (Exception ex) when (context.RequestAborted.IsCancellationRequested)
        {
            // A disconnected request has no client that can receive an error
            // response. Avoid turning a transport cancellation into a 500.
            _logger.LogDebug(
                ex,
                "Request {TraceId} ended after its request token was aborted.",
                context.TraceIdentifier);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started");
                throw;
            }

            _logger.LogError(ex, "Unhandled exception");

            await WriteErrorResponseAsync(
                context,
                GetStatusCodeForException(ex),
                ex);
        }
    }

    private async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var errorResponse = new
        {
            title = "An error occurred",
            status = statusCode,
            detail = _environment.IsDevelopment() ? exception.ToString() : null,
            instance = context.Request.Path.ToString(),
            traceId = context.TraceIdentifier
        };

        // Do not reuse RequestAborted here. Request cancellation is handled
        // above without attempting a response write, and internal cancellation
        // is deliberately mapped to 408 with a non-cancelled write token.
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse),
            CancellationToken.None);
    }

    private int GetStatusCodeForException(Exception ex)
    {
        return ex switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
