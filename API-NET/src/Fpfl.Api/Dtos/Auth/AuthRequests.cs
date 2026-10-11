using System.ComponentModel.DataAnnotations;

namespace Fpfl.Api.Dtos.Auth;

// Validation attributes run automatically ([ApiController]) and failures come back as a 400 ApiResponse.

public sealed record LoginRequest(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be valid")]
    string Email,
    [Required(ErrorMessage = "Password is required")]
    string Password);

public sealed record RegisterRequest(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be valid")]
    [MaxLength(255, ErrorMessage = "Email must be at most 255 characters")]
    string Email,
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    string Password,
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(50, ErrorMessage = "First name must be at most 50 characters")]
    string First,
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name must be at most 100 characters")]
    string Last);

public sealed record RefreshRequest(
    [Required(ErrorMessage = "Refresh token is required")]
    string RefreshToken);

public sealed record LogoutRequest(
    [Required(ErrorMessage = "Refresh token is required")]
    string RefreshToken);

public sealed record ChangePasswordRequest(
    [Required(ErrorMessage = "Current password is required")]
    string CurrentPassword,
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    string NewPassword);
