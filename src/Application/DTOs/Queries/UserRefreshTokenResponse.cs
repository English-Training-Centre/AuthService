namespace AuthService.src.Application.DTOs.Queries;

public sealed record UserRefreshTokenResponse
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public static UserRefreshTokenResponse Failure() => new() { IsSuccess = false };
}