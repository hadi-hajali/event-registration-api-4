using System.Net;
using System.Text.Json;
using EventRegistration.Api.Exceptions;
using FluentValidation;

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
            DuplicateResourceException => (int)HttpStatusCode.Conflict,
            BusinessException => (int)HttpStatusCode.Conflict,
            ConflictException => (int)HttpStatusCode.Conflict,
            AppValidationException => (int)HttpStatusCode.BadRequest,
            EventRegistration.Api.Exceptions.ValidationException => (int)HttpStatusCode.BadRequest,
            FluentValidation.ValidationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errors = exception switch
        {
            EventRegistration.Api.Exceptions.ValidationException validationException => validationException.Errors
                .SelectMany(x => x.Value)
                .ToArray(),
            AppValidationException appValidationException => appValidationException.Errors.ToArray(),
            FluentValidation.ValidationException fluentValidationException => fluentValidationException.Errors
                .Select(x => x.ErrorMessage)
                .ToArray(),
            _ => Array.Empty<string>()
        };

        var payload = new
        {
            success = false,
            timestamp = DateTimeOffset.UtcNow,
            message = statusCode == (int)HttpStatusCode.InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            errors
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}

