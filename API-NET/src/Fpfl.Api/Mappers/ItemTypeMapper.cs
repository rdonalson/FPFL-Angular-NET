using Fpfl.Api.Dtos.ItemTypes;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Mappers;

/// <summary>
///     Explicit entity-to-DTO mapping (no reflection). Sanitizing happens in the response factory.
/// </summary>
public sealed class ItemTypeMapper
{
    public ItemTypeResponse ToResponse(ItemType entity) => new(entity.Id, entity.Name);
}
