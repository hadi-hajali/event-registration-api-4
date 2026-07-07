namespace EventRegistration.Api.Exceptions;

public sealed class DuplicateResourceException : Exception
{
    public DuplicateResourceException(string message) : base(message)
    {
    }
}