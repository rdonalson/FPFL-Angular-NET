using System.ComponentModel.DataAnnotations;
using Fpfl.Api.Dtos.ItemTypes;

namespace Fpfl.Api.Dtos.InitialAmounts;

/// <summary>Body of POST /initial-amount and PUT /initial-amount/{id}. The owner comes from the access token.</summary>
public sealed record InitialAmountRequest(
    [Required(ErrorMessage = "InitialAmount Amount is required")]
    double? Amount);

/// <summary>A user's starting balance. <c>beginDate</c> is when it was last set.</summary>
public sealed record InitialAmountResponse(
    long Id,
    Guid UserId,
    string Name,
    double Amount,
    ItemTypeResponse? ItemType,
    DateOnly? BeginDate);
