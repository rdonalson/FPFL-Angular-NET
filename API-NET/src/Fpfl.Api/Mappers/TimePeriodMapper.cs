using Fpfl.Api.Dtos.TimePeriods;
using Fpfl.Common.Sanitization;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Mappers;

/// <summary>
///     Explicit TimePeriod mapping. Like API-JDK's TimePeriodMapper, incoming names are run
///     through the strict sanitizer (letters, digits, space and -_'. only) before they reach the
///     service; outgoing data is sanitized by the response factory.
/// </summary>
public sealed class TimePeriodMapper(ISanitizer sanitizer)
{
    public TimePeriodResponse ToResponse(TimePeriod entity) => new(entity.Id, entity.Name);

    public TimePeriod ToEntity(TimePeriodRequest request) => new()
    {
        Id = request.Id,
        Name = SanitizeName(request.Name)
    };

    public string SanitizeName(string name) => sanitizer.SanitizeString(name) ?? string.Empty;
}
