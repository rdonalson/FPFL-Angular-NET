namespace Fpfl.Common.Exceptions;

/// <summary>Input or state violates a domain rule. Maps to 400 Bad Request.</summary>
public sealed class DomainValidationException(string message) : DomainException(message) { }
