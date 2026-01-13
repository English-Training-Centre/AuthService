namespace AuthService.src.Application.DTOs.Queries;

public sealed record SignInResponse
{
    public bool IsSuccess { get; init; }
    public Guid UserId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string AccessToken { get; init; }  = string.Empty;
    public string RefreshToken { get; init; }  = string.Empty;

    public static SignInResponse Failure() => new() { IsSuccess = false };
}