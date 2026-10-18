using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

using Fpfl.Common.Exceptions;

using Microsoft.Extensions.Logging;

namespace Fpfl.Common.Sanitization;

/// <summary>
///     Port of API-JDK's SanitizerImpl. Walks an object graph (POCOs, records, lists,
///     dictionaries, arrays) and cleans every string it finds, in place.
///     <list type="bullet">
///         <item>Layer A, normalize: strip BOM, zero-width/invisible and control characters, trim, collapse whitespace.</item>
///         <item>Layer B, strict (default): normalized text must contain only letters, digits, space and <c>-_'.</c>.</item>
///         <item>Layer C, lenient (<see cref="LenientTextAttribute" />): normalize only.</item>
///         <item>URI: strings starting with <c>/</c>, <c>http://</c> or <c>https://</c> are checked against a URI character set.</item>
///     </list>
///     Objects implementing <see cref="INoSanitize" /> are skipped. Cycles are detected by reference.
///     Violations throw <see cref="SanitizationException" />, which the API returns as 400.
/// </summary>
public sealed partial class Sanitizer(ILogger<Sanitizer> logger) : ISanitizer
{
    [GeneratedRegex(@"^[\p{L}0-9 \-_'.]*$")]
    private static partial Regex StrictTextPattern();

    [GeneratedRegex(@"^[A-Za-z0-9:/?&=._#%\-+]*$")]
    private static partial Regex UriPattern();

    [GeneratedRegex(@"[​-‏‪-‮]")]
    private static partial Regex InvisiblePattern();

    [GeneratedRegex(@"\p{Cc}")]
    private static partial Regex ControlPattern();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultiSpacePattern();

    // ============================================================
    //  Public API
    // ============================================================

    public void Sanitize(object? root)
    {
        if (root is null or INoSanitize)
        {
            return;
        }

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        SanitizeValue(root, visited);
    }

    public string? SanitizeString(string? value) => SanitizeStrict(value);

    // ============================================================
    //  Layers
    // ============================================================

    /// <summary>Layer A: normalization shared by every mode.</summary>
    public static string? Normalize(string? s)
    {
        if (s is null)
        {
            return null;
        }

        s = s.Replace("﻿", string.Empty);           // BOM
        s = InvisiblePattern().Replace(s, string.Empty); // zero-width / bidi controls
        s = ControlPattern().Replace(s, string.Empty);   // control chars (incl. tab/newline)
        s = s.Trim();
        return MultiSpacePattern().Replace(s, " ");     // collapse whitespace
    }

    private static string? SanitizeStrict(string? s)
    {
        s = Normalize(s);
        if (s is null)
        {
            return null;
        }

        if (!StrictTextPattern().IsMatch(s))
        {
            throw new SanitizationException("Invalid characters in string");
        }

        return s;
    }

    private static string? SanitizeLenient(string? s) => Normalize(s);

    private static string? SanitizeUri(string? s)
    {
        s = Normalize(s);
        if (s is null)
        {
            return null;
        }

        if (!UriPattern().IsMatch(s))
        {
            throw new SanitizationException("Invalid characters in URI string");
        }

        return s;
    }

    private static bool LooksLikeUri(string s)
    {
        var t = s.Trim();
        return t.StartsWith('/')
               || t.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    //  Recursive walk
    // ============================================================

    private object? SanitizeValue(object? value, HashSet<object> visited)
    {
        switch (value)
        {
            case null:
                return null;
            case string s:
                return LooksLikeUri(s) ? SanitizeUri(s) : SanitizeStrict(s);
        }

        var type = value.GetType();
        if (IsLeaf(type) || value is INoSanitize || !visited.Add(value))
        {
            return value;
        }

        switch (value)
        {
            case Array array:
                for (var i = 0; i < array.Length; i++)
                {
                    var old = array.GetValue(i);
                    var replaced = SanitizeValue(old, visited);
                    if (!ReferenceEquals(old, replaced))
                    {
                        array.SetValue(replaced, i);
                    }
                }

                return value;

            case IDictionary dictionary:
                foreach (var key in dictionary.Keys.Cast<object>().ToList())
                {
                    var old = dictionary[key];
                    var replaced = SanitizeValue(old, visited);
                    if (!ReferenceEquals(old, replaced) && !dictionary.IsReadOnly)
                    {
                        dictionary[key] = replaced;
                    }
                }

                return value;

            case IList list:
                for (var i = 0; i < list.Count; i++)
                {
                    var old = list[i];
                    var replaced = SanitizeValue(old, visited);
                    if (!ReferenceEquals(old, replaced) && !list.IsReadOnly)
                    {
                        list[i] = replaced;
                    }
                }

                return value;

            case IEnumerable enumerable:
                // Read-only sequences: nested objects are still cleaned in place,
                // but replaced strings cannot be written back.
                foreach (var item in enumerable)
                {
                    SanitizeValue(item, visited);
                }

                return value;
        }

        SanitizeProperties(value, type, visited);
        return value;
    }

    private void SanitizeProperties(object target, Type type, HashSet<object> visited)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            try
            {
                var old = property.GetValue(target);
                var replaced = old is string s
                    ? SanitizeAnnotatedString(s, property)
                    : SanitizeValue(old, visited);

                if (ReferenceEquals(old, replaced))
                {
                    continue;
                }

                // Includes init-only setters, so records are updated in place.
                if (property.SetMethod is not null)
                {
                    property.SetValue(target, replaced);
                }
                else
                {
                    logger.LogDebug("Cannot set property {Property} on {Type}", property.Name, type.Name);
                }
            }
            catch (SanitizationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error sanitizing property {Property} on {Type}", property.Name, type.Name);
            }
        }
    }

    private static string? SanitizeAnnotatedString(string s, PropertyInfo property)
    {
        if (property.IsDefined(typeof(LenientTextAttribute), inherit: true))
        {
            return SanitizeLenient(s);
        }

        if (property.IsDefined(typeof(StrictTextAttribute), inherit: true))
        {
            return SanitizeStrict(s);
        }

        return LooksLikeUri(s) ? SanitizeUri(s) : SanitizeStrict(s);
    }

    /// <summary>
    ///     Types that never contain user text: value types (numbers, decimal, dates, Guid, enums)
    ///     and framework reference types such as Type or Uri.
    /// </summary>
    private static bool IsLeaf(Type type) =>
        type.IsValueType
        || typeof(Delegate).IsAssignableFrom(type)
        || (type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true
            && !typeof(IEnumerable).IsAssignableFrom(type));
}
