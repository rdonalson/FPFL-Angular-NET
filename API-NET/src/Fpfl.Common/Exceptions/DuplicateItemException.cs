namespace Fpfl.Common.Exceptions;

/// <summary>The entity already exists. Maps to 409 Conflict.</summary>
public sealed class DuplicateItemException(string message) : DomainException(message) { }
