namespace Fpfl.Common.Exceptions;

/// <summary>Login or token is invalid. Maps to 401 Unauthorized.</summary>
public sealed class InvalidCredentialsException(string message) : DomainException(message) { }
