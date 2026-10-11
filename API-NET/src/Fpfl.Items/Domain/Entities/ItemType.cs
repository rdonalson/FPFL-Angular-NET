namespace Fpfl.Items.Domain.Entities;

/// <summary>
///     Item type lookup (Credit, Debit, Initial Amount). Maps to <c>fpfl.item_types</c>.
/// </summary>
public sealed class ItemType
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
