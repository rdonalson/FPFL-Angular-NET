using Fpfl.Api.Responses;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Fpfl.Api.Security;

/// <summary>
///     Makes 401 and 403 responses produced by JWT bearer authentication use the same
///     ApiResponse envelope as every other error, instead of an empty body.
/// </summary>
public static class AuthEnvelopeEvents
{
    public static JwtBearerEvents Create() => new()
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiResponse<object?>(
                StatusCodes.Status401Unauthorized,
                "Authentication is required or the access token has expired"));
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new ApiResponse<object?>(
                StatusCodes.Status403Forbidden,
                "You do not have permission to perform this action"));
        }
    };
}
