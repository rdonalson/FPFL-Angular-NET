namespace Fpfl.Common.Sanitization;

/// <summary>
///     Forces strict sanitization (letters, digits, space and <c>-_'.</c> only) on a property.
///     Strict is already the default; use this to make intent explicit.
///     On a positional record parameter write <c>[property: StrictText]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StrictTextAttribute : Attribute { }
