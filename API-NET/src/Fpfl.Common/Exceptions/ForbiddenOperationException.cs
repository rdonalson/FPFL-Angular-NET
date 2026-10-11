namespace Fpfl.Common.Exceptions;

/// <summary>The caller is not allowed to perform the operation. Maps to 403 Forbidden.</summary>
public sealed class ForbiddenOperationException(string message) : DomainException(message) { }
