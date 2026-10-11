namespace Fpfl.Common.Exceptions;

/// <summary>Text failed sanitization rules. Maps to 400 Bad Request.</summary>
public sealed class SanitizationException(string message) : DomainException(message) { }
