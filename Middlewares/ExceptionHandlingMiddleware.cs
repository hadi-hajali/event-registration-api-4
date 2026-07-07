using EventRegistration.Api.Exceptions;

namespace EventRegistration.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

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