namespace Fpfl.Common.Exceptions;

/// <summary>A requested entity does not exist. Maps to 404 Not Found.</summary>
public sealed class ItemNotFoundException(string message) : DomainException(message) { }
