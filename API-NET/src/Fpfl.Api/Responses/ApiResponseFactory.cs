using Fpfl.Common.Sanitization;

namespace Fpfl.Api.Responses;

/// <summary>
///     Port of API-JDK's ApiResponseFactory: every payload is run through the sanitizer
///     before it leaves the API.
/// </summary>
public sealed class ApiResponseFactory(ISanitizer sanitizer) : IApiResponseFactory
{
    public ApiResponse<T> Success<T>(T data, string message)
    {
        sanitizer.Sanitize(data);
        return new ApiResponse<T>(StatusCodes.Status200OK, message, data);
    }

    public ApiResponse<object?> Success(string message) =>
        new(StatusCodes.Status200OK, message);

    public ApiResponse<T> Created<T>(T data, string message, string location)
    {
        sanitizer.Sanitize(data);
        return new ApiResponse<T>(StatusCodes.Status201Created, message, data, location);
    }
}
