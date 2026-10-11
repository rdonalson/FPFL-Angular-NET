namespace Fpfl.Common.Exceptions;

/// <summary>
///     A data-access failure. Maps to 500; the client gets a generic message and the
///     details stay in the log under the correlation ID.
/// </summary>
public sealed class RepositoryException(string message, Exception innerException)
    : DomainException(message, innerException) { }
