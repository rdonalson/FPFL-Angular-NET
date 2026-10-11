namespace Fpfl.Common.Sanitization;

/// <summary>
///     Applies lenient sanitization (normalization only, no character whitelist) to free-form text.
///     On a positional record parameter write <c>[property: LenientText]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class LenientTextAttribute : Attribute { }
