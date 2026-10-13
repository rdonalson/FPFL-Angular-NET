using System.ComponentModel.DataAnnotations;

namespace Fpfl.Api.Dtos.TimePeriods;

/// <summary>Creates a time period. The id is chosen by the caller, as in API-JDK.</summary>
public sealed record TimePeriodRequest(
    [Range(1, long.MaxValue, ErrorMessage = "TimePeriod Id is required and must be a positive integer")]
    long Id,
    [Required(ErrorMessage = "TimePeriod Name is required and cannot be blank")]
    [MaxLength(75, ErrorMessage = "TimePeriod Name must be at most 75 characters")]
    string Name);

/// <summary>Renames a time period (the id comes from the route).</summary>
public sealed record UpdateTimePeriodNameRequest(
    [Required(ErrorMessage = "TimePeriod Name is required and cannot be blank")]
    [MaxLength(75, ErrorMessage = "TimePeriod Name must be at most 75 characters")]
    string Name);
