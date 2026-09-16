using System.Net;
using System.Text.Json;
using FluentValidation;

namespace SmartLedger.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid operation"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (status == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning(exception, "Request failed: {Title}", title);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        object body = exception is ValidationException validation
            ? new
            {
                title,
                status = (int)status,
                errors = validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            }
            : new
            {
                title,
                status = (int)status,
                detail = exception.Message
            };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
