using System.ComponentModel.DataAnnotations;

namespace Fpfl.Api.Dtos.ItemTypes;

/// <summary>Creates a item type. The id is chosen by the caller, as in API-JDK.</summary>
public sealed record ItemTypeRequest(
    [Range(1, long.MaxValue, ErrorMessage = "ItemType Id is required and must be a positive integer")]
    long Id,
    [Required(ErrorMessage = "ItemType Name is required and cannot be blank")]
    [MaxLength(75, ErrorMessage = "ItemType Name must be at most 75 characters")]
    string Name);

/// <summary>
///     Updates the item type at the route id. <c>name</c> is required (as in API-JDK);
///     <c>id</c> is optional and, when different from the route id, changes the id.
/// </summary>
public sealed record UpdateItemTypeRequest(
    [Range(1, long.MaxValue, ErrorMessage = "ItemType Id must be a positive integer")]
    long? Id,
    [Required(ErrorMessage = "ItemType Name is required and cannot be blank")]
    [MaxLength(75, ErrorMessage = "ItemType Name must be at most 75 characters")]
    string Name);
