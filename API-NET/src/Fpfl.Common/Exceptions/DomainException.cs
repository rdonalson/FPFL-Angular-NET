namespace Fpfl.Common.Exceptions;

/// <summary>
///     Base class for all domain-specific errors. The API's global exception handler
///     maps each subclass to an HTTP status and a standard ApiResponse body.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }

    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}
