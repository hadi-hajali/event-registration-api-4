namespace EventRegistration.Api.Exceptions;

public sealed class AppValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public AppValidationException(string message, IReadOnlyList<string> errors) : base(message)
    {
        Errors = errors;
    }
}