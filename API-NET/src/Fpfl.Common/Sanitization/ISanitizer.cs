namespace Fpfl.Common.Sanitization;

/// <summary>
///     Cleans and validates text in objects before they cross the API boundary.
/// </summary>
public interface ISanitizer
{
    /// <summary>Recursively sanitizes every string reachable from <paramref name="root" />, in place.</summary>
    void Sanitize(object? root);

    /// <summary>Normalizes a single string and enforces strict-text rules.</summary>
    string? SanitizeString(string? value);
}
