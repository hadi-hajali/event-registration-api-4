using System.Net;
using System.Text.Json;
using EventRegistration.Api.Exceptions;

namespace EventRegistration.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => (int)HttpStatusCode.NotFound,
            ConflictException => (int)HttpStatusCode.Conflict,
            EventRegistration.Api.Exceptions.ValidationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        object payload;
        if (exception is EventRegistration.Api.Exceptions.ValidationException validationException)
        {
            payload = new
            {
                title = "Validation failed",
                status = statusCode,
                errors = validationException.Errors
            };
        }
        else
        {
            payload = new
            {
                title = exception.Message,
                status = statusCode
            };
        }

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

