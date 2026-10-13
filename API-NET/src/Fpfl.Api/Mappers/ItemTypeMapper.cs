using Fpfl.Api.Dtos.ItemTypes;
using Fpfl.Common.Sanitization;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Mappers;

/// <summary>
///     Explicit ItemType mapping. Like API-JDK's ItemTypeMapper, incoming names are run
///     through the strict sanitizer (letters, digits, space and -_'. only) before they reach the
///     service; outgoing data is sanitized by the response factory.
/// </summary>
public sealed class ItemTypeMapper(ISanitizer sanitizer)
{
    public ItemTypeResponse ToResponse(ItemType entity) => new(entity.Id, entity.Name);

    public ItemType ToEntity(ItemTypeRequest request) => new()
    {
        Id = request.Id,
        Name = SanitizeName(request.Name)
    };

    public string SanitizeName(string name) => sanitizer.SanitizeString(name) ?? string.Empty;
}
