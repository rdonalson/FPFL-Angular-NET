namespace Fpfl.Api.Responses;

/// <summary>
///     Builds sanitized <see cref="ApiResponse{T}" /> envelopes for controllers.
/// </summary>
public interface IApiResponseFactory
{
    ApiResponse<T> Success<T>(T data, string message);

    ApiResponse<object?> Success(string message);

    ApiResponse<T> Created<T>(T data, string message, string location);
}
