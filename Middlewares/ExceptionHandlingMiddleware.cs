using System.Net;
using System.Text.Json;
using EventRegistration.Api.Exceptions;
using FluentValidation.Results;
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

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
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
        catch (Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                DuplicateResourceException => StatusCodes.Status409Conflict,
                BusinessException => StatusCodes.Status409Conflict,
                AppValidationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var errors = exception is AppValidationException validationException
                ? validationException.Errors
                : Array.Empty<string>();

            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                timestamp = DateTime.UtcNow,
                message = exception.Message,
                errors
            });
        }
    }
}
